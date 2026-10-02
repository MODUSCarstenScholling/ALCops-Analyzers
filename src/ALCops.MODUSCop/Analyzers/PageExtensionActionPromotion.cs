using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.MODUSCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class PageExtensionActionPromotion : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.PageExtensionActionPromotion);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSymbolAction(AnalyzePageExtension, EnumProvider.SymbolKind.PageExtension);

    private static void AnalyzePageExtension(SymbolAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Symbol.IsSynthesized || ctx.Symbol is not IPageExtensionTypeSymbol pageExtension)
        {
            return;
        }

        foreach (var action in pageExtension.AddedActionsFlattened)
        {
            ctx.CancellationToken.ThrowIfCancellationRequested();

            if (action.IsObsolete())
            {
                continue;
            }

            var promoted = action.OriginalDefinition.GetProperty(MODUSCopEnumProvider.PropertyKind.Promoted);

            if (promoted is not null || action.ActionKind == EnumProvider.ActionKind.ActionRef)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.PageExtensionActionPromotion, promoted?.GetLocation() ?? action.GetLocation()));
            }
        }
    }
}
