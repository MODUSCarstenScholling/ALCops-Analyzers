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

[CodeFixProvider(nameof(RunPageImplementPageManagementCodeFixProvider))]
public sealed class RunPageImplementPageManagementCodeFixProvider : CodeFixProvider
{
    private const string PageManagementCodeunitName = "Page Management";
    private const string PageManagementNamespace = "Microsoft.Utilities";
    private const string PageRunMethodKey = "PageRun";
    private const string PageRunModalMethodKey = "PageRunModal";
    private const string PageRunAtFieldMethodKey = "PageRunAtField";

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

    private class RunPageImplementPageManagementCodeAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.Refactor;
        public override bool SupportsFixAll { get; }
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;

        public RunPageImplementPageManagementCodeAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.RunPageImplementPageManagement.Id);

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

    private static RunPageImplementPageManagementCodeAction CreateCodeAction(SyntaxNode node, Document document,
        CodeFixProperties properties,
        bool generateFixAll)
    {
        return new RunPageImplementPageManagementCodeAction(
            ApplicationCopAnalyzers.RunPageImplementPageManagementCodeAction,
            ct => ImplementPageManagement(document, node, properties, ct),
            nameof(RunPageImplementPageManagementCodeFixProvider),
            generateFixAll);
    }

    private static async Task<Document> ImplementPageManagement(Document document, SyntaxNode node, CodeFixProperties properties, CancellationToken cancellationToken)
    {
        Task<SyntaxNode> syntaxRootTask = document.GetSyntaxRootAsync(cancellationToken);

        var originalInvocation = node.FirstAncestorOrSelf<InvocationExpressionSyntax>(); // Page.Run(...);
        if (originalInvocation is null)
            return document;

        var containingMethodOrTrigger = ConfiguredObjectReplacementCodeFixHelper.GetContainingMethodOrTrigger(originalInvocation);
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

        // Define the corresponding method key based on original invocation and map to configured method name.
        if (originalInvocation.Expression is not MemberAccessExpressionSyntax)
            return document;

        var runModel = originalInvocation.Expression.GetNameStringValue().IsSameName("RunModal");
        var methodKey = GetMethodKeyForPageManagement(originalInvocation, runModel);
        var methodName = replacement.GetMethodOrDefault(methodKey, methodKey);

        // Track nodes across edits so we always operate on nodes from the current tree
        var root = await syntaxRootTask.ConfigureAwait(false);
        if (root is null)
            return document;

        var trackedRoot = root.TrackNodes(containingMethodOrTrigger, originalInvocation);

        var currentMethodOrTrigger = trackedRoot.GetCurrentNode(containingMethodOrTrigger);
        var currentoriginalInvocation = trackedRoot.GetCurrentNode(originalInvocation);
        if (currentMethodOrTrigger is null || currentoriginalInvocation is null)
            return document;

        SyntaxNode newRoot;
        switch (currentoriginalInvocation)
        {
            case InvocationExpressionSyntax currentInvocation:
                {
                    var replacementInvocation =
                        CreateRunWithPageManagementCodeUnit(
                                methodName,
                                variableName,
                                runModel,
                                currentInvocation.ArgumentList)
                            .WithLeadingTrivia(currentInvocation.GetLeadingTrivia())
                            .WithTrailingTrivia(currentInvocation.GetTrailingTrivia());

                    newRoot = trackedRoot.ReplaceNode(currentInvocation, replacementInvocation);
                    break;
                }

            default:
                return document;
        }

        // If needed add "Page Management" codeunit as a local variable
        if (replacementTarget.RequiresLocalDeclaration)
        {
            var updatedMethodOrTrigger = newRoot.GetCurrentNode(containingMethodOrTrigger);
            if (updatedMethodOrTrigger is not null)
            newRoot = ConfiguredObjectReplacementCodeFixHelper.AddLocalVariable(newRoot, updatedMethodOrTrigger, variableName, replacement);
        }

#if NET8_0_OR_GREATER
        // For default replacement target we add the known namespace import when namespaces are used.
        if (replacement.VariableSubtypeName.IsSameName(PageManagementCodeunitName))
            newRoot = AddUsingDirectiveIfNeeded(newRoot, PageManagementNamespace);
#endif

        return document.WithSyntaxRoot(newRoot);
    }

    #region Method Helpers
    private static string GetMethodKeyForPageManagement(InvocationExpressionSyntax invocationExpressionSyntax, bool runModel)
    {
        if (invocationExpressionSyntax.ArgumentList.Arguments.Count == 3 &&
            IsLiteralIntValue(invocationExpressionSyntax.ArgumentList.Arguments[2]))
        {
            return PageRunAtFieldMethodKey;
        }

        if (runModel)
        {
            return PageRunModalMethodKey;
        }

        return PageRunMethodKey;
    }

    #endregion
    #region Invocation Helpers

    private static bool IsLiteralIntValue(CodeExpressionSyntax codeExpression)
    {
        if (codeExpression is not LiteralExpressionSyntax literalExpression)
            return false;

        if (literalExpression.Literal is not Int32SignedLiteralValueSyntax syntax)
            return false;

        return int.TryParse(syntax.Number.ValueText, out var value);
    }

    private static InvocationExpressionSyntax CreateRunWithPageManagementCodeUnit(string methodName, string variableName, bool runModel, ArgumentListSyntax originalArguments)
    {
        var memberAccess = SyntaxFactory.MemberAccessExpression(
            SyntaxFactory.IdentifierName(variableName),
            SyntaxFactory.Token(EnumProvider.SyntaxKind.DotToken),
            SyntaxFactory.IdentifierName(methodName));

        var newArguments = CreateNewArguments(originalArguments, methodName, runModel);

        return SyntaxFactory.InvocationExpression(memberAccess, newArguments);
    }

    private static ArgumentListSyntax CreateNewArguments(ArgumentListSyntax originalArguments, string methodName, bool runModel)
    {
        var identifier = originalArguments.Arguments
            .OfType<IdentifierNameSyntax>()
            .First();

        if (string.Equals(methodName, PageRunAtFieldMethodKey, StringComparison.Ordinal))
        {
            var fieldExpr = originalArguments.Arguments
                .OfType<LiteralExpressionSyntax>()
                .Last();

            var boolExpr =
                SyntaxFactory.LiteralExpression(
                    SyntaxFactory.BooleanLiteralValue(
                        SyntaxFactory.Token(runModel
                            ? EnumProvider.SyntaxKind.TrueKeyword
                            : EnumProvider.SyntaxKind.FalseKeyword)));

            return SyntaxFactory.ArgumentList(
                new SeparatedSyntaxList<CodeExpressionSyntax>()
                    .Add(identifier)
                    .Add(fieldExpr)
                    .Add(boolExpr));
        }

        return SyntaxFactory.ArgumentList(
            new SeparatedSyntaxList<CodeExpressionSyntax>()
                .Add(identifier));
    }
    #endregion

    #region Using Directive Helpers

#if NET8_0_OR_GREATER
    private static SyntaxNode AddUsingDirectiveIfNeeded(SyntaxNode root, string namespaceName)
    {
        if (root is not CompilationUnitSyntax compilationUnit)
            return root;

        if (compilationUnit.NamespaceDeclaration is null)
            return root;

        var namespaceText = namespaceName;
        for (int i = 0; i < compilationUnit.Usings.Count; i++)
        {
            if (compilationUnit.Usings[i].Name?.ToString().IsSameName(namespaceText) == true)
                return root;
        }

        var usingDirective = SyntaxFactory.UsingDirective(
            SyntaxFactory.ParseQualifiedName(namespaceText))
            .WithSemicolonToken(SyntaxFactory.Token(EnumProvider.SyntaxKind.SemicolonToken));

        return AddUsingInSortedOrder(compilationUnit, usingDirective);
    }

    private static CompilationUnitSyntax AddUsingInSortedOrder(CompilationUnitSyntax compilationUnit, UsingDirectiveSyntax newUsing)
    {
        var usings = compilationUnit.Usings;

        if (usings.Count == 0)
        {
            var eol = SyntaxFactory.EndOfLine(Environment.NewLine, elastic: false);
            var usingWithTrivia = newUsing
                .WithLeadingTrivia(eol)
                .WithTrailingTrivia(eol);
            return compilationUnit.WithUsings(
                new SyntaxList<UsingDirectiveSyntax>().Add(usingWithTrivia));
        }

        var newUsingName = newUsing.Name!.ToString();
        var newList = new SyntaxList<UsingDirectiveSyntax>();
        bool inserted = false;

        for (int i = 0; i < usings.Count; i++)
        {
            if (!inserted && string.CompareOrdinal(usings[i].Name?.ToString(), newUsingName) > 0)
            {
                newList = newList.Add(
                    newUsing.WithTrailingTrivia(SyntaxFactory.EndOfLine(Environment.NewLine)));
                inserted = true;
            }
            newList = newList.Add(usings[i]);
        }

        if (!inserted)
            newList = newList.Add(
                newUsing.WithTrailingTrivia(SyntaxFactory.EndOfLine(Environment.NewLine)));

        return compilationUnit.WithUsings(newList);
    }
#endif

    #endregion
}