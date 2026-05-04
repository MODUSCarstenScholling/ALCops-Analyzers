---
applyTo: 'src/ALCops.FormattingCop/**/PermissionDeclarationOrder*'
---

# FC0004: Permission Declaration Order

## Purpose

Detects `Permissions` property entries that are not sorted alphabetically by (type keyword, object name). Provides a CodeFix that reorders the entries and converts single-line format to multi-line when there are 2+ entries.

## Diagnostic properties

| Property | Value |
|---|---|
| ID | FC0004 |
| Category | Style |
| Severity | Info |
| Help URI | https://alcops.dev/docs/analyzers/formattingcop/fc0004/ |

## Architecture

```
src/ALCops.Common/
└── Permissions/
    ├── NaturalNameComparer.cs          # Natural/alphanumeric name comparison (AZ-compatible)
    └── PermissionSyntaxHelper.cs       # Shared sort logic, multi-line builder

src/ALCops.FormattingCop/
├── Analyzers/
│   └── PermissionDeclarationOrder.cs           # Analyzer (CompilationAction)
└── CodeFixes/
    └── PermissionDeclarationOrderCodeFixProvider.cs  # CodeFix (sort + reformat)
```

### Sort order

Entries are sorted using AZ Dev Tools-compatible ordering (matches `PermissionComparer` from al-code-outline):

1. **Table types first**: `table` and `tabledata` entries are grouped before all other types
   - Within table types, entries are sorted by **object name** first (natural/alphanumeric comparison)
   - For the same object name, `table` sorts before `tabledata`
2. **Remaining types alphabetically**: `codeunit` < `page` < `query` < `report` < `xmlport`
   - Within the same type, sorted by **object name** (natural/alphanumeric comparison)

**Name comparison** uses `NaturalNameComparer` (natural/alphanumeric sort):
- Splits names into text and numeric chunks
- Text chunks: spaces stripped, compared with `StringComparison.InvariantCultureIgnoreCase`
- Numeric chunks: compared as integers (`"Item 2"` < `"Item 10"`)
- Tiebreaker: shorter string first

Uses `StringComparison.OrdinalIgnoreCase` for type keyword comparison only (fixed ASCII vocabulary).

### Analysis flow

1. Iterate all objects via `compilation.GetDeclaredApplicationObjectSymbols()` (including permissionset/permissionsetextension)
2. For each object with a `Permissions` property containing 2+ entries
3. Check if entries are sorted using `PermissionSyntaxHelper.ArePermissionsSorted`
4. Report one diagnostic on the `PropertySyntax` node if not sorted

### Key difference from AC0031/AC0032

- Does NOT skip `permissionset`/`permissionsetextension` objects (AC0031/AC0032 skip them)
- Does NOT skip test codeunits with `TestPermissions = Disabled`
- Analyzes all permission types (codeunit, page, report, table, tabledata, etc.), not just `tabledata`

## Design decisions

| Decision | Rationale |
|---|---|
| `RegisterCompilationAction` | Same pattern as AC0032; iterates all objects in one pass |
| One diagnostic per object, not per entry | The fix reorders the entire list; per-entry diagnostics would be noise |
| Diagnostic on `PropertySyntax` node | Ensures CodeFix can find the node via `FindNode` + ancestor/descendant traversal |
| AZ Dev Tools-compatible sort order | Prevents false positives on code formatted by AZ Dev Tools' "Sort Permissions" |
| table/tabledata first, rest alphabetical | Matches AZ's fixed priority map; more future-proof for new permission types |
| Natural/alphanumeric name comparison | Matches AZ's `AlphanumComparatorFast`; handles numeric suffixes correctly |
| `InvariantCultureIgnoreCase` for names | Handles accented chars and punctuation weight; matches AZ behavior |
| Space stripping in text chunks | Matches AZ behavior; prevents spaces from affecting sort order |
| CodeFix always outputs multi-line for 2+ entries | Consistent formatting; single-line permissions are hard to scan |
| Single entry stays single-line | No formatting benefit from multi-line with one entry |
| Permission chars casing preserved | Each entry keeps its original `r`/`R`/`rimd`/`RIMD`/`X` casing |

## CodeFix

The `PermissionDeclarationOrderCodeFixProvider` sorts all entries and reformats to multi-line. Supports FixAll via BatchFixer.

### Scenarios

| Scenario | Behavior |
|---|---|
| Multi-line, unsorted | Reorder entries, preserve multi-line format with matching indentation |
| Single-line, 2+ entries, unsorted | Reorder and convert to multi-line format |
| Single entry | No diagnostic (trivially sorted) |

### Node finding

The CodeFix uses a robust node-finding pattern:
```csharp
var propertySyntax = node as PropertySyntax
    ?? node.FirstAncestorOrSelf<PropertySyntax>()
    ?? node.DescendantNodes().OfType<PropertySyntax>().FirstOrDefault();
```

### Shared helpers used

- `PermissionSyntaxHelper.GetSortedPermissions`: Sorts entries by (type, name)
- `PermissionSyntaxHelper.BuildMultiLinePermissionValue`: Creates multi-line formatted output
- `PermissionSyntaxHelper.GetEntryIndentation`: Detects existing indentation pattern

## Test coverage

**HasDiagnostic (6 cases):** UnsortedTabledata, UnsortedMixedTypes, UnsortedSingleType, UnsortedCodeunit, UnsortedNumeric, UnsortedTableNotFirst.
**NoDiagnostic (7 cases):** AlreadySorted, SingleEntry, NoPermissionsProperty, SortedTabledata, DotsBeforeLetters, NaturalNumericSort, TableTypesFirst.
**HasFix (4 cases):** ReorderTabledata, ReorderMixedTypes, SingleLineToMultiLine, PreserveCasing.

## Refactoring impact

The `PermissionSyntaxHelper` sort logic was changed from simple `OrdinalIgnoreCase` to AZ Dev Tools-compatible ordering. This affects:
- **FC0004**: Uses the new sort order for detection and code fix
- **AC0031's `FindInsertionIndex`**: Uses `ComparePermissionEntries` for insertion point calculation
- **AC0032**: Uses `ArePermissionsSorted` to determine if a list is sorted

The behavioral changes:
- table/tabledata types now sort before other types (previously alphabetical: `codeunit` came first)
- Names use natural/alphanumeric comparison (previously ordinal: "Item 10" came before "Item 2")
- Spaces in names are ignored during comparison (previously significant)
