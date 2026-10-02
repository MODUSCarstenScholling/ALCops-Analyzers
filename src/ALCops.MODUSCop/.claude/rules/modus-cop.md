---
paths:
  - "src/ALCops.MODUSCop/**"
---

# MODUSCop

MODUSCop is a Business Central AL analyzer project built on the NAV SDK. It uses the `MCP` diagnostic prefix and the `moduscop` help URI slug.

## Project conventions

- MODUSCop is included in the solution but is not included in the `ALCops.Analyzers` NuGet meta-package.
- This project has no sibling test project or test fixtures.
- Local builds target `net10.0`; CI builds target `netstandard2.1`, `net8.0` and `net10.0`.
- Diagnostics are controlled through the standard ruleset. There are no MODUSCop-specific settings or CodeFixes.
- Rule-specific documentation is stored in `.claude/rules/diagnostics/`.
- Rule help pages use `https://alcops.dev/docs/analyzers/moduscop/{id}/`.
- The diagnostic MCP0021 reads mandatory name affixes from `AppSourceCop.json` through the SDK-backed `AppSourceCopConfigurationProvider.GetMandatoryNameAffixes`.

## Diagnostics

| ID | Rule | Severity | Purpose |
|---|---|---|---|
| MCP0001 | MODUSCopActive | Info | Reports that MODUSCop is active at the app manifest's `id` property. |
| MCP0005 | CompositeKeyTableRequiresFilterAndFindRecord | Warning | Requires `FilterRecord` and `FindRecord` for a normal table with a composite primary key. |
| MCP0006 | SetupTableRequiresReadAndInitialize | Warning | Requires `Read` and `Initialize` on setup tables. |
| MCP0010 | IdentifierNameConflictsWithTypeName | Warning | Prevents variable identifiers from matching their declared type names. |
| MCP0017 | PageExtensionActionPromotion | Error | Rejects promoted actions and action references added in page extensions. |
| MCP0018 | PageExtensionActionPlacement | Error | Restricts page-extension action changes to `addlast`. |
| MCP0021 | MandatoryAffixOnlyInExtensionObjects | Warning | Restricts AppSourceCop mandatory affixes to extension objects. |
| MCP0022 | SetupTableAccessRequiresReadOrInitialize | Warning | Directs setup-table access through the available `Read` or `Initialize` wrapper. |

## Build

Build each supported framework separately:

```powershell
foreach ($tfm in @('netstandard2.1', 'net8.0', 'net10.0')) {
    dotnet build src\ALCops.MODUSCop\ALCops.MODUSCop.csproj -c Release -p:ContinuousIntegrationBuild=true -p:TargetFrameworks=$tfm -p:TargetFramework=$tfm --no-incremental -m:1
}
```
