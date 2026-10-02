using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.MODUSCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class PageExtensionActionPlacement : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.PageExtensionActionPlacement);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSymbolAction(AnalyzeChange, EnumProvider.SymbolKind.Change);

    private static void AnalyzeChange(SymbolAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Symbol.IsSynthesized || ctx.Symbol is not IChangeSymbol change ||
            change.GetContainingApplicationObjectTypeSymbol() is not IPageExtensionTypeSymbol)
        {
            return;
        }

        var syntax = change.DeclaringSyntaxReference?.GetSyntax(ctx.CancellationToken);

        if (syntax is not ActionAddChangeSyntax && syntax is not ActionMoveChangeSyntax)
        {
            return;
        }

        var kind = change.ChangeKind;

        if (kind == MODUSCopEnumProvider.ChangeKind.AddAfter || kind == MODUSCopEnumProvider.ChangeKind.AddBefore ||
            kind == MODUSCopEnumProvider.ChangeKind.AddFirst || kind == MODUSCopEnumProvider.ChangeKind.MoveAfter ||
            kind == MODUSCopEnumProvider.ChangeKind.MoveBefore || kind == MODUSCopEnumProvider.ChangeKind.MoveFirst ||
            kind == MODUSCopEnumProvider.ChangeKind.MoveLast)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.PageExtensionActionPlacement, change.GetLocation()));
        }
    }
}
