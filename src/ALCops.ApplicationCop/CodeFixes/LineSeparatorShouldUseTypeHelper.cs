using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using ALCops.Common.Settings;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace ALCops.ApplicationCop.CodeFixes;

[CodeFixProvider(nameof(LineSeparatorShouldUseTypeHelperCodeFixProvider))]
public sealed class LineSeparatorShouldUseTypeHelperCodeFixProvider : CodeFixProvider
{
    private const int CrlfCarriageReturnAscii = 13;
    private const int LfAscii = 10;
    private const string LfSeparatorMethodKey = "LFSeparator";
    private const string CrlfSeparatorMethodKey = "CRLFSeparator";

#if NETSTANDARD2_1
    private sealed class CodeFixProperties
    {
        public CodeFixReplacementResolution Replacement { get; }

        private CodeFixProperties(CodeFixReplacementResolution replacement)
        {
            Replacement = replacement;
        }

        public static CodeFixProperties? TryParse(ImmutableDictionary<string, string>? properties)
        {
            if (!CodeFixReplacementPropertyBag.TryParse(properties, out var replacement) || replacement is null)
                return null;

            return new CodeFixProperties(replacement);
        }
    }
#endif

#if NET8_0_OR_GREATER
    private sealed record CodeFixProperties(CodeFixReplacementResolution Replacement)
    {
        public static CodeFixProperties? TryParse(ImmutableDictionary<string, string>? properties)
        {
            if (!CodeFixReplacementPropertyBag.TryParse(properties, out var replacement) || replacement is null)
                return null;

            return new CodeFixProperties(replacement);
        }
    }
#endif

    private sealed class LineSeparatorShouldUseTypeHelperCodeAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.Refactor;
        public override bool SupportsFixAll => false;
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;

        internal LineSeparatorShouldUseTypeHelperCodeAction(
            string title,
            Func<CancellationToken, Task<Document>> createChangedDocument,
            string equivalenceKey)
            : base(title, createChangedDocument, equivalenceKey)
        {
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.LineSeparatorShouldUseTypeHelper.Id);

    public sealed override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext ctx)
    {
        var syntaxRoot = await ctx.Document.GetSyntaxRootAsync(ctx.CancellationToken).ConfigureAwait(false);
        var node = syntaxRoot.FindNode(ctx.Span);
        var properties = CodeFixProperties.TryParse(ctx.Diagnostics[0].Properties);
        if (properties is null)
            return;

        ctx.RegisterCodeFix(
            new LineSeparatorShouldUseTypeHelperCodeAction(
                ApplicationCopAnalyzers.LineSeparatorShouldUseTypeHelperCodeAction,
                cancellationToken => ApplyFix(ctx.Document, node, properties, cancellationToken),
                nameof(LineSeparatorShouldUseTypeHelperCodeFixProvider)),
            ctx.Diagnostics[0]);
    }

    private static async Task<Document> ApplyFix(
        Document document,
        SyntaxNode node,
        CodeFixProperties properties,
        CancellationToken cancellationToken)
    {
        var syntaxRootTask = document.GetSyntaxRootAsync(cancellationToken);

        var assignment = node.FirstAncestorOrSelf<AssignmentStatementSyntax>();
        if (assignment is null)
            return document;

        var containingMethodOrTrigger = ConfiguredObjectReplacementCodeFixHelper.GetContainingMethodOrTrigger(assignment);
        if (containingMethodOrTrigger is null)
            return document;

        var containingObject = ConfiguredObjectReplacementCodeFixHelper.GetContainingApplicationObject(containingMethodOrTrigger);
        if (containingObject is null)
            return document;

        var replacement = properties.Replacement;
        var replacementTarget = ConfiguredObjectReplacementCodeFixHelper.ResolveReplacementTarget(
            containingMethodOrTrigger,
            containingObject,
            replacement);
        if (replacementTarget is null)
            return document;

        var root = await syntaxRootTask.ConfigureAwait(false);
        if (root is null)
            return document;

        var isCrlf = TryGetCrlfPair(assignment, out var lineFeedAssignment);
        var trackedRoot = isCrlf
            ? root.TrackNodes(containingMethodOrTrigger, assignment, lineFeedAssignment!)
            : root.TrackNodes(containingMethodOrTrigger, assignment);

        var currentMethodOrTrigger = trackedRoot.GetCurrentNode(containingMethodOrTrigger);
        var currentAssignment = trackedRoot.GetCurrentNode(assignment);
        if (currentMethodOrTrigger is null || currentAssignment is null)
            return document;

        SyntaxNode newRoot;
        if (isCrlf)
        {
            var currentLineFeedAssignment = trackedRoot.GetCurrentNode(lineFeedAssignment!);
            if (currentLineFeedAssignment is null)
                return document;

            newRoot = ApplyCrlfFix(
                trackedRoot,
                currentAssignment,
                currentLineFeedAssignment,
                lineFeedAssignment!,
                replacementTarget.VariableName,
                replacement);
        }
        else
        {
            var lfInvocation = CreateSeparatorInvocation(
                replacementTarget.VariableName,
                replacement.GetMethodOrDefault(LfSeparatorMethodKey, LfSeparatorMethodKey));
            var replacementAssignment = currentAssignment.WithSource(lfInvocation)
                .WithTriviaFrom(currentAssignment);
            newRoot = trackedRoot.ReplaceNode(currentAssignment, replacementAssignment);
        }

        if (replacementTarget.RequiresLocalDeclaration)
        {
            var updatedMethodOrTrigger = newRoot.GetCurrentNode(containingMethodOrTrigger);
            if (updatedMethodOrTrigger is not null)
            {
                newRoot = ConfiguredObjectReplacementCodeFixHelper.AddLocalVariable(
                    newRoot,
                    updatedMethodOrTrigger,
                    replacementTarget.VariableName,
                    replacement);
            }
        }

        return document.WithSyntaxRoot(newRoot);
    }

    private static SyntaxNode ApplyCrlfFix(
        SyntaxNode root,
        AssignmentStatementSyntax carriageReturnAssignment,
        AssignmentStatementSyntax lineFeedAssignment,
        AssignmentStatementSyntax originalLineFeedAssignment,
        string variableName,
        CodeFixReplacementResolution replacement)
    {
        var crlfInvocation = CreateSeparatorInvocation(
            variableName,
            replacement.GetMethodOrDefault(CrlfSeparatorMethodKey, CrlfSeparatorMethodKey));

        if (TryGetTextElementAccessPair(carriageReturnAssignment, lineFeedAssignment, out var textExpression))
        {
            var textAssignment = carriageReturnAssignment
                .WithTarget(textExpression.WithTrailingTrivia(carriageReturnAssignment.Target.GetTrailingTrivia()))
                .WithSource(crlfInvocation)
                .WithTriviaFrom(carriageReturnAssignment);
            var rootWithTextAssignment = root.ReplaceNode(carriageReturnAssignment, textAssignment);
            var currentLineFeedAssignment = rootWithTextAssignment.GetCurrentNode(originalLineFeedAssignment);

            return currentLineFeedAssignment is null
                ? rootWithTextAssignment
                : rootWithTextAssignment.RemoveNode(currentLineFeedAssignment, SyntaxRemoveOptions.KeepNoTrivia) ?? rootWithTextAssignment;
        }

        var carriageReturnReplacement = carriageReturnAssignment.WithSource(CreateElementAccess(crlfInvocation, 1));
        var rootWithCarriageReturn = root.ReplaceNode(carriageReturnAssignment, carriageReturnReplacement);
        var currentLineFeedAssignmentForReplacement = rootWithCarriageReturn.GetCurrentNode(originalLineFeedAssignment);
        if (currentLineFeedAssignmentForReplacement is null)
            return rootWithCarriageReturn;

        var lineFeedReplacement = currentLineFeedAssignmentForReplacement.WithSource(CreateElementAccess(crlfInvocation, 2));
        return rootWithCarriageReturn.ReplaceNode(currentLineFeedAssignmentForReplacement, lineFeedReplacement);
    }

    private static bool TryGetCrlfPair(
        AssignmentStatementSyntax carriageReturnAssignment,
        out AssignmentStatementSyntax? lineFeedAssignment)
    {
        lineFeedAssignment = null;

        if (!IsIntLiteral(carriageReturnAssignment.Source, CrlfCarriageReturnAscii) ||
            carriageReturnAssignment.Parent is not BlockSyntax block)
        {
            return false;
        }

        for (int index = 0; index < block.Statements.Count - 1; index++)
        {
            if (!ReferenceEquals(block.Statements[index], carriageReturnAssignment))
                continue;

            lineFeedAssignment = block.Statements[index + 1] as AssignmentStatementSyntax;
            return lineFeedAssignment is not null && IsIntLiteral(lineFeedAssignment.Source, LfAscii);
        }

        return false;
    }

    private static bool TryGetTextElementAccessPair(
        AssignmentStatementSyntax carriageReturnAssignment,
        AssignmentStatementSyntax lineFeedAssignment,
        out CodeExpressionSyntax textExpression)
    {
        textExpression = null!;

        if (carriageReturnAssignment.Target is not ElementAccessExpressionSyntax carriageReturnTarget ||
            lineFeedAssignment.Target is not ElementAccessExpressionSyntax lineFeedTarget ||
            carriageReturnTarget.Expression is not IdentifierNameSyntax carriageReturnExpression ||
            lineFeedTarget.Expression is not IdentifierNameSyntax lineFeedExpression ||
            !carriageReturnExpression.Identifier.ValueText.IsSameName(lineFeedExpression.Identifier.ValueText) ||
            !IsElementAccessIndex(carriageReturnTarget, 1) ||
            !IsElementAccessIndex(lineFeedTarget, 2))
        {
            return false;
        }

        textExpression = carriageReturnExpression;
        return true;
    }

    private static bool IsElementAccessIndex(ElementAccessExpressionSyntax elementAccess, int expectedIndex)
    {
        return elementAccess.ArgumentList?.Arguments.Count == 1 &&
               IsIntLiteral(elementAccess.ArgumentList.Arguments[0], expectedIndex);
    }

    private static bool IsIntLiteral(CodeExpressionSyntax expression, int expectedValue)
    {
        return expression is LiteralExpressionSyntax literalExpression &&
               literalExpression.Literal is Int32SignedLiteralValueSyntax literalValue &&
               int.TryParse(literalValue.GetIdentifierOrLiteralValue(), out var value) &&
               value == expectedValue;
    }

    private static InvocationExpressionSyntax CreateSeparatorInvocation(string variableName, string methodName)
    {
        var memberAccess = SyntaxFactory.MemberAccessExpression(
            SyntaxFactory.IdentifierName(variableName),
            SyntaxFactory.Token(EnumProvider.SyntaxKind.DotToken),
            SyntaxFactory.IdentifierName(methodName));

        return SyntaxFactory.InvocationExpression(memberAccess, SyntaxFactory.ArgumentList());
    }

    private static ElementAccessExpressionSyntax CreateElementAccess(CodeExpressionSyntax expression, int index)
    {
        expression = expression.WithTrailingTrivia(SyntaxFactory.TriviaList());

        var indexExpression = SyntaxFactory.LiteralExpression(
            SyntaxFactory.Int32SignedLiteralValue(SyntaxFactory.Literal(index)));
        var arguments = new SeparatedSyntaxList<CodeExpressionSyntax>().Add(indexExpression);

        var bracketedArgumentList = SyntaxFactory.BracketedArgumentList(arguments)
            .WithOpenBracketToken(SyntaxFactory.Token(SyntaxKind.OpenBracketToken));

        return SyntaxFactory.ElementAccessExpression(
            expression,
            bracketedArgumentList);
    }
}