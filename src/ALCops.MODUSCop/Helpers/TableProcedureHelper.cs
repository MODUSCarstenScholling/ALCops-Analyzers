using ALCops.Common.Extensions;
using ALCops.Common.Helpers;
using Microsoft.Dynamics.Nav.CodeAnalysis;

namespace ALCops.MODUSCop.Helpers;

internal static class TableProcedureHelper
{
    public static bool IsSetupTable(ITableTypeSymbol table) =>
        table.Name.Contains("Setup", SemanticFacts.NameEqualityComparison) &&
        TableHelper.IsSetupTable(table);

    public static bool HasNonObsoleteMethod(ITableTypeSymbol table, string name, CancellationToken cancellationToken)
    {
        foreach (var member in table.GetMembers(name))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (member is IMethodSymbol && !member.IsObsolete())
            {
                return true;
            }
        }

        return false;
    }
}
