---
paths:
  - "src/ALCops.MODUSCop/**/MODUSCopActive*"
---

# MCP0001: MODUSCopActive

## Purpose

Confirms that MODUS Cop is active by reporting an informational diagnostic at the app manifest's id property.

Registers `RegisterCompilationAction` so the notification does not depend on individual AL declarations being analyzed.

## Design decisions

| Decision | Rationale |
|---|---|
| Usage category, Info severity, enabled by default | The diagnostic confirms analyzer activation, rather than identifying a code defect or blocking compilation. |
| Report only at the manifest's id property | One app-level notification avoids repeating the same status on every AL object. Obsolete-symbol filtering does not apply to this manifest-only rule. |
| Use the shared manifest helper without an additional runtime gate | The existing helper handles manifest access across the three supported target frameworks. |
| Ruleset control only, no custom settings or CodeFix | Standard diagnostic suppression is sufficient; an activation notification offers nothing to rewrite. |

## Deliberate non-reports

- Compilations without an available manifest do not produce an activation notification.
- Individual objects, record receivers, fields, user procedure calls and table keys are not inspected.

## Test notes

- No test project or fixtures are included by explicit request; compatibility is checked with a three-target build.
