---
paths:
  - "src/ALCops.MODUSCop/**/SetupTableAccessRequiresReadOrInitialize*"
  - "src/ALCops.MODUSCop/Helpers/TableProcedureHelper.cs"
---

# MCP0022: SetupTableAccessRequiresReadOrInitialize

## Purpose

Directs setup-table reads through Read or Initialize when at least one nonobsolete wrapper exists.

## Design decisions

- Use the same structural-and-name classification as MCP0006, without publisher restrictions.
- Bind once per relevant body after a cheap, AL-case-insensitive identifier prefilter. The operation walker covers both parenthesized and parenthesis-free calls.
- Identify Get, Find, FindFirst, FindLast and FindSet by built-in method kind and the Table class, then resolve the receiver with Common's `GetReceiverTableType`. Bare calls, Rec, this, named variables and namespace-qualified record types therefore share the same path.
- Read and Initialize may read their own table directly. Compare the enclosing method's owning table with the receiver's original table definition; merely having either method name does not exempt access to another table.
- Preserve Warning severity and enabled-by-default behavior. Report on the called method name.

## Deliberate non-reports

- Invalid invocations, obsolete code or target tables, non-record built-ins, and tables without either nonobsolete wrapper are ignored.
- Wrapper signature and accessibility are intentionally not additional requirements.

## Test notes

No test project or fixtures are added by request. Validate the three target frameworks sequentially and format the changed projects.
