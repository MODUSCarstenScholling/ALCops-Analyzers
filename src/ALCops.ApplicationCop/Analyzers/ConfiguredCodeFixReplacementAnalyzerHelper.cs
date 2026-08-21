using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;
using ALCops.Common.Reflection;

namespace ALCops.ApplicationCop.Analyzers;

internal static class ConfiguredCodeFixReplacementAnalyzerHelper
{
    internal static IEnumerable<string> CollectReservedNames(ISymbol containingSymbol)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (containingSymbol is IMethodSymbol methodSymbol)
        {
            foreach (var local in methodSymbol.LocalVariables)
                names.Add(local.Name);

            foreach (var parameter in methodSymbol.Parameters)
                names.Add(parameter.Name);

            if (methodSymbol.ReturnValueSymbol is { IsNamed: true } returnValue && !string.IsNullOrWhiteSpace(returnValue.Name))
                names.Add(returnValue.Name);
        }

        var containingObject = containingSymbol.GetContainingObjectTypeSymbol();
        foreach (var member in containingObject.GetMembers())
        {
            if (member.Kind == EnumProvider.SymbolKind.GlobalVariable && member is IVariableSymbol globalVariable)
                names.Add(globalVariable.Name);
        }

        return names;
    }
}