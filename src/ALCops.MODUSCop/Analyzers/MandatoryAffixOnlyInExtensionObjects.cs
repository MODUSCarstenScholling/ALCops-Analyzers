using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.MODUSCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class MandatoryAffixOnlyInExtensionObjects : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.MandatoryAffixOnlyInExtensionObjects);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterCompilationStartAction(CompilationStart);

    private static void CompilationStart(CompilationStartAnalysisContext context)
    {
        string[] affixes = AppSourceCopConfigurationProvider.GetMandatoryNameAffixes(context.Compilation);

        if (affixes.Length == 0)
        {
            return;
        }

        context.RegisterSymbolAction(
            symbolContext => AnalyzeObject(symbolContext, affixes),
            EnumProvider.SymbolKind.Codeunit,
            EnumProvider.SymbolKind.ControlAddIn,
            EnumProvider.SymbolKind.Entitlement,
            EnumProvider.SymbolKind.Enum,
            EnumProvider.SymbolKind.EnumExtension,
            EnumProvider.SymbolKind.Interface,
            EnumProvider.SymbolKind.Page,
            EnumProvider.SymbolKind.PageExtension,
            EnumProvider.SymbolKind.PermissionSet,
            EnumProvider.SymbolKind.PermissionSetExtension,
            EnumProvider.SymbolKind.Profile,
            EnumProvider.SymbolKind.ProfileExtension,
            EnumProvider.SymbolKind.Query,
            EnumProvider.SymbolKind.Report,
            EnumProvider.SymbolKind.ReportExtension,
            EnumProvider.SymbolKind.RequestPage,
            EnumProvider.SymbolKind.RequestPageExtension,
            EnumProvider.SymbolKind.Table,
            EnumProvider.SymbolKind.TableExtension,
            EnumProvider.SymbolKind.XmlPort);
    }

    private static void AnalyzeObject(SymbolAnalysisContext context, string[] affixes)
    {
        if (context.IsObsolete() || context.Symbol.IsSynthesized ||
            context.Symbol is IApplicationObjectExtensionTypeSymbol ||
            context.Symbol is not IContainerSymbol container)
        {
            return;
        }

        foreach (ISymbol member in container.GetMembers())
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            if (member.IsObsolete() || IsEventSubscriber(member))
            {
                continue;
            }

            foreach (string affix in affixes)
            {
                if (member.Name.IndexOf(affix, SemanticFacts.NameEqualityComparison) < 0)
                {
                    continue;
                }

                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.MandatoryAffixOnlyInExtensionObjects,
                    member.GetLocation(),
                    affix));
            }
        }
    }

    private static bool IsEventSubscriber(ISymbol symbol) =>
        symbol is IMethodSymbol method &&
        method.Attributes.Any(attribute => attribute.AttributeKind == EnumProvider.AttributeKind.EventSubscriber);
}
