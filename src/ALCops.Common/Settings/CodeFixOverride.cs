namespace ALCops.Common.Settings;

public sealed class CodeFixOverride
{
    public string? Variable { get; set; }
    public Dictionary<string, string>? Methods { get; set; }
}
