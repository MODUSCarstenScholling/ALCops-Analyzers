using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using ALCops.MODUSCop.Helpers;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.MODUSCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class SetupTableRequiresReadAndInitialize : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.SetupTableRequiresReadAndInitialize);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSymbolAction(AnalyzeTable, EnumProvider.SymbolKind.Table);

    private static void AnalyzeTable(SymbolAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Symbol is not ITableTypeSymbol table || !TableProcedureHelper.IsSetupTable(table))
        {
            return;
        }

        if (!TableProcedureHelper.HasNonObsoleteMethod(table, "Read", ctx.CancellationToken) ||
            !TableProcedureHelper.HasNonObsoleteMethod(table, "Initialize", ctx.CancellationToken))
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.SetupTableRequiresReadAndInitialize, table.GetLocation(), table.Name));
        }
    }
}
