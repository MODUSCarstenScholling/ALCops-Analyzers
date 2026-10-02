---
paths:
  - "src/ALCops.MODUSCop/**/CompositeKeyTableRequiresFilterAndFindRecord*"
  - "src/ALCops.MODUSCop/Helpers/TableProcedureHelper.cs"
---

# MCP0005: CompositeKeyTableRequiresFilterAndFindRecord

## Purpose

Requires FilterRecord and FindRecord on normal tables with composite primary keys so callers can use a consistent table-owned access API.

## Design decisions

- Read `PrimaryKey`, not the first declared key: the SDK also exposes synthesized keys through this property.
- Check each method independently; two overloads of one required name cannot satisfy the other. Resolve member names using the SDK's AL name lookup and count only nonobsolete method symbols, not fields or other members.
- Report once on the table from its symbol callback, without cross-callback state.
- Preserve Warning severity and enabled-by-default behavior.

## Deliberate non-reports

- Obsolete tables, non-normal table types and primary keys with fewer than two fields are outside this convention.
- Method visibility and signatures are deliberately unconstrained.

## Test notes

No test project or fixtures are added by request. Compatibility is validated through sequential builds for all three target frameworks and targeted formatting.
