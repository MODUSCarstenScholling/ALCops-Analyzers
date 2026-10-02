using System.Globalization;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.MODUSCop;

public static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor MODUSCopActive = new(
        id: DiagnosticIds.MODUSCopActive,
        title: MODUSCopAnalyzers.MODUSCopActiveTitle,
        messageFormat: MODUSCopAnalyzers.MODUSCopActiveMessageFormat,
        category: Category.Usage,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: MODUSCopAnalyzers.MODUSCopActiveDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.MODUSCopActive));

    public static readonly DiagnosticDescriptor CompositeKeyTableRequiresFilterAndFindRecord = new(
        id: DiagnosticIds.CompositeKeyTableRequiresFilterAndFindRecord,
        title: MODUSCopAnalyzers.CompositeKeyTableRequiresFilterAndFindRecordTitle,
        messageFormat: MODUSCopAnalyzers.CompositeKeyTableRequiresFilterAndFindRecordMessageFormat,
        category: Category.Usage,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: MODUSCopAnalyzers.CompositeKeyTableRequiresFilterAndFindRecordDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.CompositeKeyTableRequiresFilterAndFindRecord));

    public static readonly DiagnosticDescriptor SetupTableRequiresReadAndInitialize = new(
        id: DiagnosticIds.SetupTableRequiresReadAndInitialize,
        title: MODUSCopAnalyzers.SetupTableRequiresReadAndInitializeTitle,
        messageFormat: MODUSCopAnalyzers.SetupTableRequiresReadAndInitializeMessageFormat,
        category: Category.Usage,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: MODUSCopAnalyzers.SetupTableRequiresReadAndInitializeDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.SetupTableRequiresReadAndInitialize));

    public static readonly DiagnosticDescriptor IdentifierNameConflictsWithTypeName = new(
        id: DiagnosticIds.IdentifierNameConflictsWithTypeName,
        title: MODUSCopAnalyzers.IdentifierNameConflictsWithTypeNameTitle,
        messageFormat: MODUSCopAnalyzers.IdentifierNameConflictsWithTypeNameMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: MODUSCopAnalyzers.IdentifierNameConflictsWithTypeNameDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.IdentifierNameConflictsWithTypeName));

    public static readonly DiagnosticDescriptor SetupTableAccessRequiresReadOrInitialize = new(
        id: DiagnosticIds.SetupTableAccessRequiresReadOrInitialize,
        title: MODUSCopAnalyzers.SetupTableAccessRequiresReadOrInitializeTitle,
        messageFormat: MODUSCopAnalyzers.SetupTableAccessRequiresReadOrInitializeMessageFormat,
        category: Category.Usage,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: MODUSCopAnalyzers.SetupTableAccessRequiresReadOrInitializeDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.SetupTableAccessRequiresReadOrInitialize));

    public static readonly DiagnosticDescriptor PageExtensionActionPromotion = new(
        id: DiagnosticIds.PageExtensionActionPromotion,
        title: MODUSCopAnalyzers.PageExtensionActionPromotionTitle,
        messageFormat: MODUSCopAnalyzers.PageExtensionActionPromotionMessageFormat,
        category: Category.UIDesign,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: MODUSCopAnalyzers.PageExtensionActionPromotionDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.PageExtensionActionPromotion));

    public static readonly DiagnosticDescriptor PageExtensionActionPlacement = new(
        id: DiagnosticIds.PageExtensionActionPlacement,
        title: MODUSCopAnalyzers.PageExtensionActionPlacementTitle,
        messageFormat: MODUSCopAnalyzers.PageExtensionActionPlacementMessageFormat,
        category: Category.UIDesign,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: MODUSCopAnalyzers.PageExtensionActionPlacementDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.PageExtensionActionPlacement));

    public static readonly DiagnosticDescriptor MandatoryAffixOnlyInExtensionObjects = new(
        id: DiagnosticIds.MandatoryAffixOnlyInExtensionObjects,
        title: MODUSCopAnalyzers.MandatoryAffixOnlyInExtensionObjectsTitle,
        messageFormat: MODUSCopAnalyzers.MandatoryAffixOnlyInExtensionObjectsMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: MODUSCopAnalyzers.MandatoryAffixOnlyInExtensionObjectsDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.MandatoryAffixOnlyInExtensionObjects));

    public static string GetHelpUri(string identifier)
    {
        return string.Format(CultureInfo.InvariantCulture, "https://alcops.dev/docs/analyzers/moduscop/{0}/", identifier.ToLowerInvariant());
    }

    internal static class Category
    {
        public const string Design = "Design";
        public const string Usage = "Usage";
        public const string UIDesign = "UIDesign";
    }
}
