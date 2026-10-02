---
paths:
  - "src/ALCops.MODUSCop/**/SetupTableRequiresReadAndInitialize*"
  - "src/ALCops.MODUSCop/Helpers/TableProcedureHelper.cs"
---

# MCP0006: SetupTableRequiresReadAndInitialize

## Purpose

Requires both Read and Initialize on setup tables to centralize singleton access and initialization.

## Design decisions

- Setup classification combines `TableHelper.IsSetupTable` with a table name containing Setup under AL name comparison rules. Neither the variable name nor the publisher participates.
- Keep this extra naming constraint in MODUSCop's shared helper rather than changing the broader Common heuristic for other cops.
- The shared heuristic recognizes its single Code primary-key pattern (including synthesized keys) or a parameterless, return-less GetRecordOnce method.
- Require each nonobsolete method independently. Overloads do not substitute for a missing name; visibility and signatures are not prescribed.
- Preserve Warning severity and enabled-by-default behavior.

## Deliberate non-reports

- Obsolete tables and tables failing either setup-classification condition are ignored.

## Test notes

No tests are requested. Three-framework builds and targeted formatting validate compatibility.
