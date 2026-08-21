using System.Collections.Immutable;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using ALCops.Common.Reflection;
using ALCops.Common.Extensions;
using ALCops.Common.Settings;
using Microsoft.Dynamics.Nav.CodeAnalysis.Utilities;

namespace ALCops.ApplicationCop.CodeFixes;

[CodeFixProvider(nameof(GlobalLanguageImplementTranslationHelperCodeFixProvider))]
public sealed class GlobalLanguageImplementTranslationHelperCodeFixProvider : CodeFixProvider
{
    private const int DefaultGlobalLanguageId = 1033;
    private const string TranslationHelperCodeunitName = "Translation Helper";
    private const string SetGlobalLanguageByIdMethodKey = "SetGlobalLanguageById";
    private const string SetGlobalLanguageToDefaultMethodKey = "SetGlobalLanguageToDefault";

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

    private class GlobalLanguageImplementTranslationHelperCodeAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.Refactor;
        public override bool SupportsFixAll { get; }
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;

        public GlobalLanguageImplementTranslationHelperCodeAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.GlobalLanguageImplementTranslationHelper.Id);

    public sealed override FixAllProvider GetFixAllProvider() =>
         WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext ctx)
    {
        Document document = ctx.Document;
        TextSpan span = ctx.Span;
        CancellationToken cancellationToken = ctx.CancellationToken;

        SyntaxNode syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        RegisterInstanceCodeFix(ctx, syntaxRoot, span, document);
    }

    private static void RegisterInstanceCodeFix(CodeFixContext ctx, SyntaxNode syntaxRoot, TextSpan span, Document document)
    {
        SyntaxNode node = syntaxRoot.FindNode(span);
        var properties = CodeFixProperties.TryParse(ctx.Diagnostics[0].Properties);
        if (properties is null)
            return;

        ctx.RegisterCodeFix(CreateCodeAction(node, document, properties, generateFixAll: false), ctx.Diagnostics[0]);
    }

    private static GlobalLanguageImplementTranslationHelperCodeAction CreateCodeAction(SyntaxNode node, Document document,
        CodeFixProperties properties,
        bool generateFixAll)
    {
        return new GlobalLanguageImplementTranslationHelperCodeAction(
            ApplicationCopAnalyzers.GlobalLanguageImplementTranslationHelperCodeAction,
            ct => ImplementTranslationHelperCodeAction(document, node, properties, ct),
            nameof(GlobalLanguageImplementTranslationHelperCodeFixProvider),
            generateFixAll);
    }

    private static async Task<Document> ImplementTranslationHelperCodeAction(Document document, SyntaxNode node, CodeFixProperties properties, CancellationToken cancellationToken)
    {
        Task<SyntaxNode> syntaxRootTask = document.GetSyntaxRootAsync(cancellationToken);

        var originalInvocation = node.FirstAncestorOrSelf<InvocationExpressionSyntax>(); // GlobalLanguage(1033);
        var originalAssignment = node.FirstAncestorOrSelf<AssignmentStatementSyntax>();  // GlobalLanguage := 1033;
        if (originalInvocation is null && originalAssignment is null)
            return document;

        // For assignment we want to replace the whole statement; for invocation we replace the invocation expression.
        SyntaxNode anchorNode = (SyntaxNode?)originalAssignment ?? originalInvocation!;

        var containingMethodOrTrigger = ConfiguredObjectReplacementCodeFixHelper.GetContainingMethodOrTrigger(anchorNode);
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

        var variableName = replacementTarget.VariableName;

        // Track nodes across edits so we always operate on nodes from the current tree
        var root = await syntaxRootTask.ConfigureAwait(false);
        if (root is null)
            return document;

        var trackedRoot = root.TrackNodes(containingMethodOrTrigger, anchorNode);

        var currentMethodOrTrigger = trackedRoot.GetCurrentNode(containingMethodOrTrigger);
        var currentAnchorNode = trackedRoot.GetCurrentNode(anchorNode);
        if (currentMethodOrTrigger is null || currentAnchorNode is null)
            return document;

        SyntaxNode newRoot;
        switch (currentAnchorNode)
        {
            // GlobalLanguage(1033);
            case InvocationExpressionSyntax currentInvocation:
                {
                    var firstArgExpr =
                        currentInvocation.ArgumentList.Arguments.Count > 0
                            ? currentInvocation.ArgumentList.Arguments[0]
                            : null;

                    var replacementInvocation =
                        CreateSetGlobalLanguageInvocation(
                                variableName,
                                replacement,
                                currentInvocation.ArgumentList,
                                firstArgExpr)
                            .WithLeadingTrivia(currentInvocation.GetLeadingTrivia())
                            .WithTrailingTrivia(currentInvocation.GetTrailingTrivia());

                    newRoot = trackedRoot.ReplaceNode(currentInvocation, replacementInvocation);
                    break;
                }

            // GlobalLanguage := 1033;
            case AssignmentStatementSyntax currentAssignment:
                {
                    var args = default(SeparatedSyntaxList<CodeExpressionSyntax>).Add(currentAssignment.Source);
                    var argumentList = SyntaxFactory.ArgumentList(args);

                    var invocation =
                        CreateSetGlobalLanguageInvocation(
                            variableName,
                            replacement,
                            argumentList,
                            currentAssignment.Source);

                    var replacementStatement =
                        SyntaxFactory.ExpressionStatement(invocation, currentAssignment.SemicolonToken)
                            .WithLeadingTrivia(currentAssignment.GetLeadingTrivia())
                            .WithTrailingTrivia(currentAssignment.GetTrailingTrivia());

                    newRoot = trackedRoot.ReplaceNode(currentAssignment, replacementStatement);
                    break;
                }

            default:
                return document;
        }

        // If needed add "Translation Helper" codeunit as a local variable
        if (replacementTarget.RequiresLocalDeclaration)
        {
            var updatedMethodOrTrigger = newRoot.GetCurrentNode(containingMethodOrTrigger);
            if (updatedMethodOrTrigger is not null)
            newRoot = ConfiguredObjectReplacementCodeFixHelper.AddLocalVariable(newRoot, updatedMethodOrTrigger, variableName, replacement);
        }

        return document.WithSyntaxRoot(newRoot);
    }

    #region Variable Helpers
    #endregion

    #region Invocation Helpers

    private static InvocationExpressionSyntax CreateSetGlobalLanguageInvocation(
        string variableName,
        CodeFixReplacementResolution replacement,
        ArgumentListSyntax args,
        CodeExpressionSyntax? singleValueExpressionFor1033Check = null)
    {
        if (singleValueExpressionFor1033Check is not null &&
            IsLiteralIntValue(singleValueExpressionFor1033Check, DefaultGlobalLanguageId))
        {
            return CreateSetGlobalLanguageToDefaultInvocation(variableName, replacement);
        }

        return CreateSetGlobalLanguageByIdInvocation(variableName, replacement, args);
    }

    private static bool IsLiteralIntValue(CodeExpressionSyntax codeExpression, int expected)
    {
        if (codeExpression is not LiteralExpressionSyntax literalExpression)
            return false;

        if (literalExpression.Literal is not Int32SignedLiteralValueSyntax syntax)
            return false;

        return int.TryParse(syntax.Number.ValueText, out var value) &&
               value == expected;
    }

    private static InvocationExpressionSyntax CreateSetGlobalLanguageToDefaultInvocation(
        string variableName,
        CodeFixReplacementResolution replacement)
    {
        var methodName = replacement.GetMethodOrDefault(
            SetGlobalLanguageToDefaultMethodKey,
            SetGlobalLanguageToDefaultMethodKey);

        var memberAccess = SyntaxFactory.MemberAccessExpression(
            SyntaxFactory.IdentifierName(variableName),
            SyntaxFactory.Token(EnumProvider.SyntaxKind.DotToken),
            SyntaxFactory.IdentifierName(methodName));

        return SyntaxFactory.InvocationExpression(memberAccess, SyntaxFactory.ArgumentList());
    }

    private static InvocationExpressionSyntax CreateSetGlobalLanguageByIdInvocation(
        string variableName,
        CodeFixReplacementResolution replacement,
        ArgumentListSyntax originalArguments)
    {
        var methodName = replacement.GetMethodOrDefault(
            SetGlobalLanguageByIdMethodKey,
            SetGlobalLanguageByIdMethodKey);

        var memberAccess = SyntaxFactory.MemberAccessExpression(
            SyntaxFactory.IdentifierName(variableName),
            SyntaxFactory.Token(EnumProvider.SyntaxKind.DotToken),
            SyntaxFactory.IdentifierName(methodName));

        return SyntaxFactory.InvocationExpression(memberAccess, originalArguments);
    }
    #endregion
}