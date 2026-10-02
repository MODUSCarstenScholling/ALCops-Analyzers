using NavChangeKind = Microsoft.Dynamics.Nav.CodeAnalysis.Symbols.ChangeKind;
using NavPropertyKind = Microsoft.Dynamics.Nav.CodeAnalysis.PropertyKind;
using NavSdkTypeKind = Microsoft.Dynamics.Nav.CodeAnalysis.NavTypeKind;

namespace ALCops.MODUSCop;

public static class MODUSCopEnumProvider
{
    private const int UnresolvedEnumValue = int.MaxValue;

    private static TEnum ParseEnum<TEnum>(string name, TEnum fallback = default)
        where TEnum : struct, Enum
    {
        try
        {
            return Enum.Parse<TEnum>(name);
        }
        catch (ArgumentException)
        {
            return fallback;
        }
    }

    public static class ChangeKind
    {
        private static readonly Lazy<NavChangeKind> AddAfterValue =
            new(() => ParseEnum("AddAfter", (NavChangeKind)UnresolvedEnumValue), LazyThreadSafetyMode.PublicationOnly);

        private static readonly Lazy<NavChangeKind> AddBeforeValue =
            new(() => ParseEnum("AddBefore", (NavChangeKind)UnresolvedEnumValue), LazyThreadSafetyMode.PublicationOnly);

        private static readonly Lazy<NavChangeKind> AddFirstValue =
            new(() => ParseEnum("AddFirst", (NavChangeKind)UnresolvedEnumValue), LazyThreadSafetyMode.PublicationOnly);

        private static readonly Lazy<NavChangeKind> MoveAfterValue =
            new(() => ParseEnum("MoveAfter", (NavChangeKind)UnresolvedEnumValue), LazyThreadSafetyMode.PublicationOnly);

        private static readonly Lazy<NavChangeKind> MoveBeforeValue =
            new(() => ParseEnum("MoveBefore", (NavChangeKind)UnresolvedEnumValue), LazyThreadSafetyMode.PublicationOnly);

        private static readonly Lazy<NavChangeKind> MoveFirstValue =
            new(() => ParseEnum("MoveFirst", (NavChangeKind)UnresolvedEnumValue), LazyThreadSafetyMode.PublicationOnly);

        private static readonly Lazy<NavChangeKind> MoveLastValue =
            new(() => ParseEnum("MoveLast", (NavChangeKind)UnresolvedEnumValue), LazyThreadSafetyMode.PublicationOnly);

        public static NavChangeKind AddAfter => AddAfterValue.Value;

        public static NavChangeKind AddBefore => AddBeforeValue.Value;

        public static NavChangeKind AddFirst => AddFirstValue.Value;

        public static NavChangeKind MoveAfter => MoveAfterValue.Value;

        public static NavChangeKind MoveBefore => MoveBeforeValue.Value;

        public static NavChangeKind MoveFirst => MoveFirstValue.Value;

        public static NavChangeKind MoveLast => MoveLastValue.Value;
    }

    public static class PropertyKind
    {
        private static readonly Lazy<NavPropertyKind> PromotedValue =
            new(() => ParseEnum<NavPropertyKind>(nameof(NavPropertyKind.Promoted)), LazyThreadSafetyMode.PublicationOnly);

        public static NavPropertyKind Promoted => PromotedValue.Value;
    }

    public static class NavTypeKind
    {
        private static readonly Lazy<NavSdkTypeKind> ArrayValue =
            new(() => ParseEnum("Array", (NavSdkTypeKind)UnresolvedEnumValue), LazyThreadSafetyMode.PublicationOnly);

        private static readonly Lazy<NavSdkTypeKind> CodeunitValue =
            new(() => ParseEnum("Codeunit", (NavSdkTypeKind)UnresolvedEnumValue), LazyThreadSafetyMode.PublicationOnly);

        private static readonly Lazy<NavSdkTypeKind> DotNetValue =
            new(() => ParseEnum("DotNet", (NavSdkTypeKind)UnresolvedEnumValue), LazyThreadSafetyMode.PublicationOnly);

        private static readonly Lazy<NavSdkTypeKind> EnumValue =
            new(() => ParseEnum("Enum", (NavSdkTypeKind)UnresolvedEnumValue), LazyThreadSafetyMode.PublicationOnly);

        private static readonly Lazy<NavSdkTypeKind> EnumExtensionValue =
            new(() => ParseEnum("EnumExtension", (NavSdkTypeKind)UnresolvedEnumValue), LazyThreadSafetyMode.PublicationOnly);

        private static readonly Lazy<NavSdkTypeKind> InterfaceValue =
            new(() => ParseEnum("Interface", (NavSdkTypeKind)UnresolvedEnumValue), LazyThreadSafetyMode.PublicationOnly);

        private static readonly Lazy<NavSdkTypeKind> LabelValue =
            new(() => ParseEnum("Label", (NavSdkTypeKind)UnresolvedEnumValue), LazyThreadSafetyMode.PublicationOnly);

        private static readonly Lazy<NavSdkTypeKind> PageCustomizationValue =
            new(() => ParseEnum("PageCustomization", (NavSdkTypeKind)UnresolvedEnumValue), LazyThreadSafetyMode.PublicationOnly);

        private static readonly Lazy<NavSdkTypeKind> PageExtensionValue =
            new(() => ParseEnum("PageExtension", (NavSdkTypeKind)UnresolvedEnumValue), LazyThreadSafetyMode.PublicationOnly);

        private static readonly Lazy<NavSdkTypeKind> PageValue =
            new(() => ParseEnum("Page", (NavSdkTypeKind)UnresolvedEnumValue), LazyThreadSafetyMode.PublicationOnly);

        private static readonly Lazy<NavSdkTypeKind> QueryValue =
            new(() => ParseEnum("Query", (NavSdkTypeKind)UnresolvedEnumValue), LazyThreadSafetyMode.PublicationOnly);

        private static readonly Lazy<NavSdkTypeKind> RecordValue =
            new(() => ParseEnum("Record", (NavSdkTypeKind)UnresolvedEnumValue), LazyThreadSafetyMode.PublicationOnly);

        private static readonly Lazy<NavSdkTypeKind> ReportValue =
            new(() => ParseEnum("Report", (NavSdkTypeKind)UnresolvedEnumValue), LazyThreadSafetyMode.PublicationOnly);

        private static readonly Lazy<NavSdkTypeKind> TableExtensionValue =
            new(() => ParseEnum("TableExtension", (NavSdkTypeKind)UnresolvedEnumValue), LazyThreadSafetyMode.PublicationOnly);

        private static readonly Lazy<NavSdkTypeKind> TestPageValue =
            new(() => ParseEnum("TestPage", (NavSdkTypeKind)UnresolvedEnumValue), LazyThreadSafetyMode.PublicationOnly);

        private static readonly Lazy<NavSdkTypeKind> TestRequestPageValue =
            new(() => ParseEnum("TestRequestPage", (NavSdkTypeKind)UnresolvedEnumValue), LazyThreadSafetyMode.PublicationOnly);

        private static readonly Lazy<NavSdkTypeKind> XmlPortValue =
            new(() => ParseEnum("XmlPort", (NavSdkTypeKind)UnresolvedEnumValue), LazyThreadSafetyMode.PublicationOnly);

        public static NavSdkTypeKind Array => ArrayValue.Value;

        public static NavSdkTypeKind Codeunit => CodeunitValue.Value;

        public static NavSdkTypeKind DotNet => DotNetValue.Value;

        public static NavSdkTypeKind Enum => EnumValue.Value;

        public static NavSdkTypeKind EnumExtension => EnumExtensionValue.Value;

        public static NavSdkTypeKind Interface => InterfaceValue.Value;

        public static NavSdkTypeKind Label => LabelValue.Value;

        public static NavSdkTypeKind PageCustomization => PageCustomizationValue.Value;

        public static NavSdkTypeKind PageExtension => PageExtensionValue.Value;

        public static NavSdkTypeKind Page => PageValue.Value;

        public static NavSdkTypeKind Query => QueryValue.Value;

        public static NavSdkTypeKind Record => RecordValue.Value;

        public static NavSdkTypeKind Report => ReportValue.Value;

        public static NavSdkTypeKind TableExtension => TableExtensionValue.Value;

        public static NavSdkTypeKind TestPage => TestPageValue.Value;

        public static NavSdkTypeKind TestRequestPage => TestRequestPageValue.Value;

        public static NavSdkTypeKind XmlPort => XmlPortValue.Value;
    }
}
