using ALCops.Common.Extensions;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Utilities;

namespace ALCops.Common.Settings;

public sealed class CodeFixReplacementDefaults
{
    public string VariableDeclaration { get; }
    public IReadOnlyDictionary<string, string> Methods { get; }

    public CodeFixReplacementDefaults(
        string variableDeclaration,
        IReadOnlyDictionary<string, string>? methods = null)
    {
        VariableDeclaration = variableDeclaration;
        Methods = methods ?? new Dictionary<string, string>(StringComparer.Ordinal);
    }
}

public sealed class CodeFixReplacementResolution
{
    public string VariableName { get; }
    public string VariableTypeKeyword { get; }
    public string VariableSubtypeName { get; }
    public IReadOnlyDictionary<string, string> Methods { get; }

    public CodeFixReplacementResolution(
        string variableName,
        string variableTypeKeyword,
        string variableSubtypeName,
        IReadOnlyDictionary<string, string> methods)
    {
        VariableName = variableName;
        VariableTypeKeyword = variableTypeKeyword;
        VariableSubtypeName = variableSubtypeName;
        Methods = methods;
    }

    public string GetMethodOrDefault(string key, string fallback)
    {
        foreach (var pair in Methods)
        {
            if (SemanticFacts.IsSameName(pair.Key, key))
                return pair.Value;
        }

        return fallback;
    }
}

public static class CodeFixReplacementResolver
{
    private sealed class ParsedVariableDeclaration
    {
        public string? Name { get; }
        public string TypeKeyword { get; }
        public string SubtypeName { get; }

        public ParsedVariableDeclaration(string? name, string typeKeyword, string subtypeName)
        {
            Name = name;
            TypeKeyword = typeKeyword;
            SubtypeName = subtypeName;
        }
    }

    public static CodeFixReplacementResolution ResolveCodeFixReplacement(
        ALCopsSettings? settings,
        string diagnosticId,
        CodeFixReplacementDefaults defaults,
        NamingPatternTarget variableNameTarget,
        IEnumerable<string>? reservedNames = null)
    {
        ParsedVariableDeclaration defaultVariable;
        if (!TryParseVariableDeclaration(defaults.VariableDeclaration, out defaultVariable))
            defaultVariable = new ParsedVariableDeclaration("Helper", "Codeunit", "Helper");

        TryGetCodeFixOverride(settings?.CodeFixOverrides, diagnosticId, out var configuredOverride);

        var effectiveVariable = defaultVariable;
        if (!string.IsNullOrWhiteSpace(configuredOverride?.Variable) &&
            TryParseVariableDeclaration(configuredOverride!.Variable, out var parsedConfigured) &&
            SemanticFacts.IsSameName(parsedConfigured.TypeKeyword, defaultVariable.TypeKeyword))
        {
            effectiveVariable = parsedConfigured;
        }

        var preferredNameBase = !string.IsNullOrWhiteSpace(effectiveVariable.Name)
            ? effectiveVariable.Name!
            : effectiveVariable.SubtypeName;

        var variableName = NamingPatternConventions.CreatePatternCompliantIdentifier(
            preferredNameBase,
            variableNameTarget,
            settings,
            reservedNames);

        var methods = ResolveMethods(defaults.Methods, configuredOverride?.Methods);

        return new CodeFixReplacementResolution(
            variableName,
            effectiveVariable.TypeKeyword,
            effectiveVariable.SubtypeName,
            methods);
    }

    private static IReadOnlyDictionary<string, string> ResolveMethods(
        IReadOnlyDictionary<string, string> defaults,
        Dictionary<string, string>? configured)
    {
        if (configured is null || configured.Count == 0)
            return new Dictionary<string, string>(defaults, StringComparer.Ordinal);

        var resolved = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var defaultMethod in defaults)
        {
            var replacement = GetConfiguredValue(configured, defaultMethod.Key);
            if (string.IsNullOrWhiteSpace(replacement))
                resolved[defaultMethod.Key] = defaultMethod.Value;
            else
                resolved[defaultMethod.Key] = replacement!;
        }

        return resolved;
    }

    private static string? GetConfiguredValue(Dictionary<string, string> configured, string key)
    {
        foreach (var pair in configured)
        {
            if (SemanticFacts.IsSameName(pair.Key, key))
                return pair.Value;
        }

        return null;
    }

    private static bool TryGetCodeFixOverride(
        Dictionary<string, CodeFixOverride>? codeFixOverrides,
        string diagnosticId,
        out CodeFixOverride? configuredOverride)
    {
        configuredOverride = null;

        if (codeFixOverrides is null)
            return false;

        foreach (var pair in codeFixOverrides)
        {
            if (SemanticFacts.IsSameName(pair.Key, diagnosticId))
            {
                configuredOverride = pair.Value;
                return true;
            }
        }

        return false;
    }

    private static bool TryParseVariableDeclaration(string? declaration, out ParsedVariableDeclaration parsed)
    {
        parsed = null!;

        if (string.IsNullOrWhiteSpace(declaration))
            return false;

        var value = declaration.Trim();
        if (value.EndsWith(";", StringComparison.Ordinal))
            value = value.Substring(0, value.Length - 1).TrimEnd();

        if (string.IsNullOrWhiteSpace(value))
            return false;

        string? variableName = null;
        var declarationPart = value;

        var colonIndex = value.IndexOf(':');
        if (colonIndex >= 0)
        {
            variableName = value.Substring(0, colonIndex).Trim().UnquoteIdentifier();
            declarationPart = value.Substring(colonIndex + 1).Trim();
        }

        if (string.IsNullOrWhiteSpace(declarationPart))
            return false;

        var firstSpace = declarationPart.IndexOf(' ');
        if (firstSpace <= 0 || firstSpace == declarationPart.Length - 1)
            return false;

        var typeKeyword = declarationPart.Substring(0, firstSpace).Trim();
        var subtype = declarationPart.Substring(firstSpace + 1).Trim().UnquoteIdentifier();

        if (string.IsNullOrWhiteSpace(typeKeyword) || string.IsNullOrWhiteSpace(subtype))
            return false;

        parsed = new ParsedVariableDeclaration(variableName, typeKeyword, subtype);
        return true;
    }
}
