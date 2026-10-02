using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using ALCops.MODUSCop.Helpers;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.MODUSCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class CompositeKeyTableRequiresFilterAndFindRecord : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.CompositeKeyTableRequiresFilterAndFindRecord);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSymbolAction(AnalyzeTable, EnumProvider.SymbolKind.Table);

    private static void AnalyzeTable(SymbolAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Symbol is not ITableTypeSymbol table ||
            table.TableType != EnumProvider.TableTypeKind.Normal || table.PrimaryKey is null ||
            table.PrimaryKey.Fields.Length < 2)
        {
            return;
        }

        if (!TableProcedureHelper.HasNonObsoleteMethod(table, "FilterRecord", ctx.CancellationToken) ||
            !TableProcedureHelper.HasNonObsoleteMethod(table, "FindRecord", ctx.CancellationToken))
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.CompositeKeyTableRequiresFilterAndFindRecord, table.GetLocation(), table.Name));
        }
    }
}
