using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;

namespace ALCops.MODUSCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class IdentifierNameConflictsWithTypeName : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.IdentifierNameConflictsWithTypeName);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSymbolAction(
            AnalyzeVariable,
            EnumProvider.SymbolKind.GlobalVariable,
            EnumProvider.SymbolKind.LocalVariable);

    private static void AnalyzeVariable(SymbolAnalysisContext context)
    {
        if (context.IsObsolete() || context.Symbol.IsSynthesized || context.Symbol is not IVariableSymbol variable)
        {
            return;
        }

        string typeName = GetTypeName(variable.Type);

        if (typeName.Length == 0 || !SemanticFacts.IsSameName(variable.Name, typeName))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.IdentifierNameConflictsWithTypeName,
            variable.GetLocation(),
            variable.Name,
            typeName));
    }

    private static string GetTypeName(ITypeSymbol type)
    {
        if (type.NavTypeKind == MODUSCopEnumProvider.NavTypeKind.Array)
        {
            type = type.GetTypeSymbol();
        }

        NavTypeKind typeKind = type.NavTypeKind;

        return IsTypeKindUsedAsName(typeKind) ? typeKind.ToString() : type.Name;
    }

    private static bool IsTypeKindUsedAsName(NavTypeKind typeKind) =>
        (typeKind == MODUSCopEnumProvider.NavTypeKind.Record ||
         typeKind == MODUSCopEnumProvider.NavTypeKind.Page ||
         typeKind == MODUSCopEnumProvider.NavTypeKind.TestPage ||
         typeKind == MODUSCopEnumProvider.NavTypeKind.TestRequestPage ||
         typeKind == MODUSCopEnumProvider.NavTypeKind.Report ||
         typeKind == MODUSCopEnumProvider.NavTypeKind.XmlPort ||
         typeKind == MODUSCopEnumProvider.NavTypeKind.Codeunit ||
         typeKind == MODUSCopEnumProvider.NavTypeKind.Interface ||
         typeKind == MODUSCopEnumProvider.NavTypeKind.Query ||
         typeKind == MODUSCopEnumProvider.NavTypeKind.PageExtension ||
         typeKind == MODUSCopEnumProvider.NavTypeKind.TableExtension ||
         typeKind == MODUSCopEnumProvider.NavTypeKind.PageCustomization ||
         typeKind == MODUSCopEnumProvider.NavTypeKind.Label ||
         typeKind == MODUSCopEnumProvider.NavTypeKind.Enum ||
         typeKind == MODUSCopEnumProvider.NavTypeKind.EnumExtension ||
         typeKind == MODUSCopEnumProvider.NavTypeKind.DotNet);
}
