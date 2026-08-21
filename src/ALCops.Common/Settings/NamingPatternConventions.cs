using System.Text.RegularExpressions;
using ALCops.Common.Extensions;
using ALCops.Common.Helpers;
using Microsoft.Dynamics.Nav.CodeAnalysis;

namespace ALCops.Common.Settings;

public enum NamingPatternTarget
{
    Procedure,
    LocalProcedure,
    GlobalProcedure,
    EventSubscriber,
    EventDeclaration,
    Variable,
    LocalVariable,
    GlobalVariable,
    Parameter,
    VarParameter,
    ReturnValue,
    Object,
    Field,
    Action,
    EnumValue,
    Control
}

public sealed class ResolvedNamingPattern
{
    public static readonly ResolvedNamingPattern Empty =
        new(null, null, null, null, null, null);

    public Regex? AllowRegex { get; }
    public Regex? DisallowRegex { get; }
    public string? AllowPatternString { get; }
    public string? DisallowPatternString { get; }
    public string? AllowDescription { get; }
    public string? DisallowDescription { get; }

    public ResolvedNamingPattern(
        Regex? allowRegex,
        Regex? disallowRegex,
        string? allowPatternString,
        string? disallowPatternString,
        string? allowDescription,
        string? disallowDescription)
    {
        AllowRegex = allowRegex;
        DisallowRegex = disallowRegex;
        AllowPatternString = allowPatternString;
        DisallowPatternString = disallowPatternString;
        AllowDescription = allowDescription;
        DisallowDescription = disallowDescription;
    }
}

public static class NamingPatternConventions
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(2);

    private static readonly (string? Allow, string? Disallow, string? AllowDesc, string? DisallowDesc) PascalCase =
        ("^[A-Z]", null, "should start with an uppercase letter", null);
    private static readonly (string? Allow, string? Disallow, string? AllowDesc, string? DisallowDesc) PascalCaseUnderscoreNoSpecial =
        ("^(?:[A-Za-z]$|[A-Z]|_[A-Z]|x[A-Z])", "[%&!?]", "should start with an uppercase letter, underscore followed by uppercase, or x followed by uppercase for xRec pattern (single-letter names are exempt)", "should not contain special characters (%, &, !, ?)");
    private static readonly (string? Allow, string? Disallow, string? AllowDesc, string? DisallowDesc) PascalCaseUnderscore =
        ("^(?:[A-Za-z]$|[A-Z]|_[A-Z]|x[A-Z])", null, "should start with an uppercase letter, underscore followed by uppercase, or x followed by uppercase for xRec pattern (single-letter names are exempt)", null);
    private static readonly (string? Allow, string? Disallow, string? AllowDesc, string? DisallowDesc) AnyCaseNoSpecial =
        ("^[A-Za-z]", "[%&!?]", "should start with a letter", "should not contain special characters (%, &, !, ?)");

    private static readonly Dictionary<NamingPatternTarget, (string? Allow, string? Disallow, string? AllowDesc, string? DisallowDesc)> BuiltInDefaults = new()
    {
        [NamingPatternTarget.Procedure] = PascalCase,
        [NamingPatternTarget.Variable] = PascalCaseUnderscoreNoSpecial,
        [NamingPatternTarget.LocalVariable] = PascalCaseUnderscoreNoSpecial,
        [NamingPatternTarget.GlobalVariable] = PascalCaseUnderscoreNoSpecial,
        [NamingPatternTarget.Parameter] = PascalCaseUnderscore,
        [NamingPatternTarget.VarParameter] = PascalCaseUnderscore,
        [NamingPatternTarget.ReturnValue] = PascalCase,
        [NamingPatternTarget.Object] = PascalCase,
        [NamingPatternTarget.Field] = AnyCaseNoSpecial,
        [NamingPatternTarget.Action] = PascalCase,
        [NamingPatternTarget.Control] = PascalCase,
    };

    private static readonly Dictionary<NamingPatternTarget, NamingPatternTarget> InheritanceMap = new()
    {
        [NamingPatternTarget.LocalProcedure] = NamingPatternTarget.Procedure,
        [NamingPatternTarget.GlobalProcedure] = NamingPatternTarget.Procedure,
        [NamingPatternTarget.EventSubscriber] = NamingPatternTarget.Procedure,
        [NamingPatternTarget.EventDeclaration] = NamingPatternTarget.Procedure,
        [NamingPatternTarget.LocalVariable] = NamingPatternTarget.Variable,
        [NamingPatternTarget.GlobalVariable] = NamingPatternTarget.Variable,
        [NamingPatternTarget.Parameter] = NamingPatternTarget.LocalVariable,
        [NamingPatternTarget.VarParameter] = NamingPatternTarget.Parameter,
        [NamingPatternTarget.ReturnValue] = NamingPatternTarget.LocalVariable,
    };

    public static ResolvedNamingPattern Resolve(NamingPatternTarget target, ALCopsSettings? settings)
    {
        return Resolve(target, settings?.NamingPatterns);
    }

    public static ResolvedNamingPattern Resolve(
        NamingPatternTarget target,
        Dictionary<string, NamingPattern>? userOverrides)
    {
        var resolved = ResolvePatternStrings(target, userOverrides);
        return new ResolvedNamingPattern(
            CompilePattern(resolved.Allow),
            CompilePattern(resolved.Disallow),
            resolved.Allow,
            resolved.Disallow,
            resolved.AllowDesc,
            resolved.DisallowDesc);
    }

    public static bool IsValidIdentifier(string identifier, ResolvedNamingPattern pattern)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            return false;

        if (pattern.AllowRegex is not null && !TryIsMatch(pattern.AllowRegex, identifier))
            return false;

        if (pattern.DisallowRegex is not null && TryIsMatch(pattern.DisallowRegex, identifier))
            return false;

        return true;
    }

    public static string CreatePatternCompliantIdentifier(
        string? baseName,
        NamingPatternTarget target,
        ALCopsSettings? settings,
        IEnumerable<string>? reservedNames = null)
    {
        var effectiveSettings = settings ?? new ALCopsSettings();
        var resolved = Resolve(target, effectiveSettings);
        var reserved = new HashSet<string>(reservedNames ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);

        var candidates = BuildCandidates(baseName, effectiveSettings);
        foreach (var candidate in candidates)
        {
            if (IsValidIdentifier(candidate, resolved) && !reserved.Contains(candidate))
                return candidate;

            if (TryCreateUniqueCandidate(candidate, resolved, reserved, out var uniqueCandidate))
                return uniqueCandidate;
        }

        var fallbackBase = candidates.Count > 0
            ? candidates[0]
            : "Value";

        if (TryCreateUniqueCandidate(fallbackBase, resolved, reserved, out var fallbackCandidate))
            return fallbackCandidate;

        return fallbackBase;
    }

    private static bool TryCreateUniqueCandidate(
        string baseCandidate,
        ResolvedNamingPattern pattern,
        HashSet<string> reserved,
        out string uniqueCandidate)
    {
        for (int i = 2; i <= 1000; i++)
        {
            var candidate = baseCandidate + i.ToString();
            if (!reserved.Contains(candidate) && IsValidIdentifier(candidate, pattern))
            {
                uniqueCandidate = candidate;
                return true;
            }
        }

        uniqueCandidate = string.Empty;
        return false;
    }

    private static List<string> BuildCandidates(string? baseName, ALCopsSettings settings)
    {
        var normalizedBase = string.IsNullOrWhiteSpace(baseName)
            ? "Value"
            : baseName.Trim();

        var acronyms = AcronymRegistry.Create(settings.KnownAcronyms);
        var pascal = IdentifierNameRenderer.Render(normalizedBase, IdentifierCaseStyle.Pascal, acronyms);
        var camel = IdentifierNameRenderer.Render(normalizedBase, IdentifierCaseStyle.Camel, acronyms);
        var snake = IdentifierNameRenderer.Render(normalizedBase, IdentifierCaseStyle.Snake, acronyms);

        var result = new List<string>();

        AddCandidate(result, pascal);
        AddCandidate(result, camel);
        AddCandidate(result, "_" + EnsureUpperFirst(pascal));
        AddCandidate(result, "x" + EnsureUpperFirst(pascal));
        AddCandidate(result, pascal + "Var");
        AddCandidate(result, camel + "Var");
        AddCandidate(result, pascal + "Value");
        AddCandidate(result, camel + "Value");
        AddCandidate(result, snake);

        return result;
    }

    private static void AddCandidate(List<string> candidates, string? candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
            return;

        if (!ContainsName(candidates, candidate))
            candidates.Add(candidate);
    }

    private static bool ContainsName(IEnumerable<string> names, string name)
    {
        foreach (var existingName in names)
        {
            // Keep case variants so case-sensitive naming patterns can choose between PageMgt and pageMgt.
            if (string.Equals(existingName, name, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static string EnsureUpperFirst(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        if (char.IsUpper(value[0]))
            return value;

        return char.ToUpperInvariant(value[0]) + value.Substring(1);
    }

    private static (string? Allow, string? Disallow, string? AllowDesc, string? DisallowDesc) ResolvePatternStrings(
        NamingPatternTarget target,
        Dictionary<string, NamingPattern>? userOverrides)
    {
        var chain = new List<NamingPatternTarget>();
        var current = target;

        while (true)
        {
            chain.Add(current);

            if (!InheritanceMap.TryGetValue(current, out var next))
                break;

            current = next;
        }

        if (userOverrides is not null)
        {
            foreach (var targetInChain in chain)
            {
                if (TryGetUserOverride(userOverrides, targetInChain, out var setting))
                {
                    return (
                        !string.IsNullOrEmpty(setting.AllowPattern) ? setting.AllowPattern : null,
                        !string.IsNullOrEmpty(setting.DisallowPattern) ? setting.DisallowPattern : null,
                        !string.IsNullOrEmpty(setting.AllowDescription) ? setting.AllowDescription : null,
                        !string.IsNullOrEmpty(setting.DisallowDescription) ? setting.DisallowDescription : null);
                }
            }
        }

        foreach (var targetInChain in chain)
        {
            if (BuiltInDefaults.TryGetValue(targetInChain, out var builtIn))
                return builtIn;
        }

        return (null, null, null, null);
    }

    private static bool TryGetUserOverride(
        Dictionary<string, NamingPattern> overrides,
        NamingPatternTarget target,
        out NamingPattern setting)
    {
        var targetName = target.ToString();
        foreach (var pair in overrides)
        {
            if (SemanticFacts.IsSameName(pair.Key, targetName))
            {
                setting = pair.Value;
                return true;
            }
        }

        setting = default!;
        return false;
    }

    private static Regex? CompilePattern(string? pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return null;

        try
        {
            return new Regex(
                pattern.Trim(),
                RegexOptions.Compiled | RegexOptions.CultureInvariant,
                RegexTimeout);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static bool TryIsMatch(Regex pattern, string input)
    {
        try
        {
            return pattern.IsMatch(input);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }
}