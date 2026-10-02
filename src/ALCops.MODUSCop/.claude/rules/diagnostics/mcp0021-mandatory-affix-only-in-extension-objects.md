---
paths:
  - "src/ALCops.MODUSCop/Analyzers/MandatoryAffixOnlyInExtensionObjects.cs"
---

# MCP0021: MandatoryAffixOnlyInExtensionObjects

## Purpose

Restricts use of AppSourceCop mandatory affixes to extension objects, preserving the affix convention without applying extension naming to ordinary objects.

Registers `RegisterCompilationStartAction` to read configured affixes once through the SDK-backed AppSourceCop configuration provider, then inspects members of object symbols.

## Design decisions

| Decision | Rationale |
|---|---|
| Reuse diagnostic ID MCP0021, Design category, Warning severity, enabled by default | These match the original MODUSCop rule descriptor. |
| Obtain all mandatory name affixes through `AppSourceCopConfigurationProvider.GetMandatoryNameAffixes` | The SDK function reads the compilation's `AppSourceCop.json` and merges `mandatoryPrefix`, `mandatorySuffix` and `mandatoryAffixes`; reading once at compilation start avoids its documented repeated configuration I/O. |
| Match names with AL identifier comparison | AL identifiers are case-insensitive; substring matching preserves the original rule's behavior while using the SDK's comparison semantics. |
| Do not add a CodeFix or an `alcops.json` setting | Correct affix usage depends on the intended object role; AppSourceCop.json remains the source of truth for affixes. |

## Deliberate non-reports

- Extension objects are excluded because mandatory affixes are allowed there.
- Obsolete objects and members are ignored.
- Event-subscriber procedures are ignored because their names are bound to publisher conventions.
- No diagnostics are registered when AppSourceCop.json provides no mandatory affixes.

## Test notes

No MODUSCop test project or fixtures are maintained by request. Validate the analyzer by building all three supported target frameworks.
