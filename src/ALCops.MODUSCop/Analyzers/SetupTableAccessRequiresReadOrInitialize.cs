using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using ALCops.MODUSCop.Helpers;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Utilities;

namespace ALCops.MODUSCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class SetupTableAccessRequiresReadOrInitialize : DiagnosticAnalyzer
{
    private static readonly ImmutableHashSet<string> AccessMethods =
        ImmutableHashSet.Create(SemanticFacts.NameEqualityComparer, "Get", "Find", "FindFirst", "FindLast", "FindSet");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.SetupTableAccessRequiresReadOrInitialize);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterCodeBlockAction(AnalyzeBody);

    private static void AnalyzeBody(CodeBlockAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.CodeBlock is not MethodOrTriggerDeclarationSyntax method)
        {
            return;
        }

        bool hasAccess = false;

        foreach (var node in method.Body.DescendantNodes())
        {
            ctx.CancellationToken.ThrowIfCancellationRequested();

            if (node is IdentifierNameSyntax { Identifier.ValueText: { } name } && AccessMethods.Contains(name.UnquoteIdentifier()))
            {
                hasAccess = true;
                break;
            }
        }

        if (!hasAccess)
        {
            return;
        }

        var operation = ctx.SemanticModel.GetOperation(method.Body, ctx.CancellationToken);

        if (operation is not null)
        {
            new AccessWalker(ctx).Visit(operation);
        }
    }

    private sealed class AccessWalker(CodeBlockAnalysisContext context) : OperationWalker
    {
        public override void VisitInvocationExpression(IInvocationExpression operation)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            AnalyzeInvocation(operation);
            base.VisitInvocationExpression(operation);
        }

        private void AnalyzeInvocation(IInvocationExpression invocation)
        {
            var target = invocation.TargetMethod;

            if (invocation.IsInvalid || target.MethodKind != EnumProvider.MethodKind.BuiltInMethod ||
                target.ContainingSymbol is not IClassTypeSymbol containingClass ||
                !SemanticFacts.IsSameName(containingClass.Name, "Table") || !AccessMethods.Contains(target.Name))
            {
                return;
            }

            var table = invocation.GetReceiverTableType(context.OwningSymbol, out _);

            if (table is null || table.IsObsolete() || !TableProcedureHelper.IsSetupTable(table))
            {
                return;
            }

            var owner = context.OwningSymbol;

            if (owner is IMethodSymbol &&
                (SemanticFacts.IsSameName(owner.Name, "Read") || SemanticFacts.IsSameName(owner.Name, "Initialize")) &&
                owner.ContainingType?.OriginalDefinition.Equals(table.OriginalDefinition) == true)
            {
                return;
            }

            if (!TableProcedureHelper.HasNonObsoleteMethod(table, "Read", context.CancellationToken) &&
                !TableProcedureHelper.HasNonObsoleteMethod(table, "Initialize", context.CancellationToken))
            {
                return;
            }

            SyntaxNode syntax = invocation.Syntax;

            if (syntax is InvocationExpressionSyntax call)
            {
                syntax = call.Expression;
            }

            if (syntax is MemberAccessExpressionSyntax member)
            {
                syntax = member.Name;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.SetupTableAccessRequiresReadOrInitialize, syntax.GetLocation(), table.Name));
        }
    }
}
