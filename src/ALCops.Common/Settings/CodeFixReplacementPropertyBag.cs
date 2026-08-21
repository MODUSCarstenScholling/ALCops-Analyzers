using System.Collections.Immutable;

namespace ALCops.Common.Settings;

public static class CodeFixReplacementPropertyBag
{
    private const string MethodPrefix = "Method:";
    private const string VariableNameKey = nameof(CodeFixReplacementResolution.VariableName);
    private const string VariableTypeKeywordKey = nameof(CodeFixReplacementResolution.VariableTypeKeyword);
    private const string VariableSubtypeNameKey = nameof(CodeFixReplacementResolution.VariableSubtypeName);

    public static ImmutableDictionary<string, string> Create(CodeFixReplacementResolution replacement)
    {
        var properties = ImmutableDictionary<string, string>.Empty
            .Add(VariableNameKey, replacement.VariableName)
            .Add(VariableTypeKeywordKey, replacement.VariableTypeKeyword)
            .Add(VariableSubtypeNameKey, replacement.VariableSubtypeName);

        foreach (var method in replacement.Methods)
            properties = properties.Add(MethodPrefix + method.Key, method.Value);

        return properties;
    }

    public static bool TryParse(
        ImmutableDictionary<string, string>? properties,
        out CodeFixReplacementResolution? replacement)
    {
        replacement = null;

        if (properties is null)
            return false;

        if (!properties.TryGetValue(VariableNameKey, out var variableName) || string.IsNullOrWhiteSpace(variableName))
            return false;

        if (!properties.TryGetValue(VariableTypeKeywordKey, out var variableTypeKeyword) || string.IsNullOrWhiteSpace(variableTypeKeyword))
            return false;

        if (!properties.TryGetValue(VariableSubtypeNameKey, out var variableSubtypeName) || string.IsNullOrWhiteSpace(variableSubtypeName))
            return false;

        var methods = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in properties)
        {
            if (!pair.Key.StartsWith(MethodPrefix, StringComparison.Ordinal))
                continue;

            var sourceMethodName = pair.Key.Substring(MethodPrefix.Length);
            if (string.IsNullOrWhiteSpace(sourceMethodName) || string.IsNullOrWhiteSpace(pair.Value))
                continue;

            methods[sourceMethodName] = pair.Value;
        }

        replacement = new CodeFixReplacementResolution(
            variableName,
            variableTypeKeyword,
            variableSubtypeName,
            methods);

        return true;
    }
}