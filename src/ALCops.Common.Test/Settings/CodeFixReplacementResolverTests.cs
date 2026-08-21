using ALCops.Common.Settings;

namespace ALCops.Common.Test;

public class CodeFixReplacementResolverTests
{
    [Test]
    public void ResolveCodeFixReplacement_UsesConfiguredVariableAndMethods()
    {
        var settings = new ALCopsSettings
        {
            CodeFixOverrides = new Dictionary<string, CodeFixOverride>
            {
                ["AC0006"] = new CodeFixOverride
                {
                    Variable = "myPageMgt: Codeunit \"My Page Mgt\";",
                    Methods = new Dictionary<string, string>
                    {
                        ["PageRun"] = "RunPage",
                        ["PageRunModal"] = "RunPageModal",
                    }
                }
            }
        };

        var resolved = CodeFixReplacementResolver.ResolveCodeFixReplacement(
            settings,
            "AC0006",
            new CodeFixReplacementDefaults(
                "PageManagement: Codeunit \"Page Management\";",
                new Dictionary<string, string>
                {
                    ["PageRun"] = "PageRun",
                    ["PageRunModal"] = "PageRunModal",
                }),
            NamingPatternTarget.LocalVariable);

        Assert.That(resolved.VariableName, Is.EqualTo("MyPageMgt"));
        Assert.That(resolved.VariableTypeKeyword, Is.EqualTo("Codeunit"));
        Assert.That(resolved.VariableSubtypeName, Is.EqualTo("My Page Mgt"));
        Assert.That(resolved.GetMethodOrDefault("PageRun", "X"), Is.EqualTo("RunPage"));
        Assert.That(resolved.GetMethodOrDefault("PageRunModal", "X"), Is.EqualTo("RunPageModal"));
    }

    [Test]
    public void ResolveCodeFixReplacement_DerivesNameWhenVariableHasNoName()
    {
        var settings = new ALCopsSettings
        {
            CodeFixOverrides = new Dictionary<string, CodeFixOverride>
            {
                ["AC0005"] = new CodeFixOverride
                {
                    Variable = "Codeunit \"My Translation Helper\";"
                }
            }
        };

        var resolved = CodeFixReplacementResolver.ResolveCodeFixReplacement(
            settings,
            "AC0005",
            new CodeFixReplacementDefaults("TranslationHelper: Codeunit \"Translation Helper\";"),
            NamingPatternTarget.LocalVariable);

        Assert.That(resolved.VariableName, Is.EqualTo("MyTranslationHelper"));
        Assert.That(resolved.VariableSubtypeName, Is.EqualTo("My Translation Helper"));
    }

    [Test]
    public void ResolveCodeFixReplacement_FallsBackForInvalidVariableOverride()
    {
        var settings = new ALCopsSettings
        {
            CodeFixOverrides = new Dictionary<string, CodeFixOverride>
            {
                ["AC0006"] = new CodeFixOverride
                {
                    Variable = "InvalidDeclaration"
                }
            }
        };

        var resolved = CodeFixReplacementResolver.ResolveCodeFixReplacement(
            settings,
            "AC0006",
            new CodeFixReplacementDefaults("PageManagement: Codeunit \"Page Management\";"),
            NamingPatternTarget.LocalVariable);

        Assert.That(resolved.VariableName, Is.EqualTo("PageManagement"));
        Assert.That(resolved.VariableSubtypeName, Is.EqualTo("Page Management"));
    }

    [Test]
    public void ResolveCodeFixReplacement_AppendsNumericSuffixForReservedNames()
    {
        var resolved = CodeFixReplacementResolver.ResolveCodeFixReplacement(
            new ALCopsSettings(),
            "AC0006",
            new CodeFixReplacementDefaults("PageManagement: Codeunit \"Page Management\";"),
            NamingPatternTarget.LocalVariable,
            ["PageManagement"]);

        Assert.That(resolved.VariableName, Is.EqualTo("PageManagement2"));
    }
}