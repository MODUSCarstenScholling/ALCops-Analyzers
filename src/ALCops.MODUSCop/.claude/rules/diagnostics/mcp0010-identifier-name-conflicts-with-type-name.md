---
paths:
  - "src/ALCops.MODUSCop/Analyzers/IdentifierNameConflictsWithTypeName.cs"
---

# MCP0010: IdentifierNameConflictsWithTypeName

## Purpose

Reports global and local variables whose names conflict with the name of their declared AL data type.

Registers `RegisterSymbolAction` on global and local variable symbols.

## Design decisions

| Decision | Rationale |
|---|---|
| Reuse diagnostic ID MCP0010, Design category, Warning severity, enabled by default | These match the original MODUSCop descriptor. |
| Compare only global and local variable names with their declared type names | This preserves the original rule's analyzed symbol scope while avoiding syntax-node callbacks for every variable declaration. |
| Resolve array declarations to their element type and preserve AL type keywords for object-like types | This matches the reference rule's type-name interpretation while using symbol data and `SemanticFacts` for AL name comparison. Type-kind values are resolved locally to avoid runtime linkage to `ALCops.Common` enum getters that may be absent from the loaded helper assembly. |
| Do not port the original MODUSCop-specific naming whitelist or add an `alcops.json` setting | The whitelist belongs to the legacy analyzer configuration and has no corresponding shared ALCops setting; all matching variable/type names are checked. |
| No CodeFix | Choosing a different variable name depends on the surrounding AL code and cannot be safely automated by this diagnostic. |

## Deliberate non-reports

- Obsolete or synthesized variables are ignored.
- Parameters, return values, fields and procedure names are outside the original rule's scope.
- Variables whose names do not match their resolved declared type name under AL's case-insensitive comparison are ignored.

## Test notes

MODUSCop has no test project or fixtures. Validate the analyzer with builds for all three supported target frameworks.
