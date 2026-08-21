---
applyTo: 'src/ALCops.ApplicationCop/**/LineSeparatorShouldUseTypeHelper*'
---

# AC0025: Use Type Helper line separators

## Purpose

Detects manually assigned line-feed and carriage-return/line-feed values and replaces them with `Type Helper` separator methods. The CodeFix uses the shared configurable object-replacement infrastructure so the replacement object, method names, and generated local variable naming follow `alcops.json`.

## Diagnostic properties

| Property | Value |
|---|---|
| ID | AC0025 |
| Prefix | AC |
| Severity | Warning |
| Category | Design |
| DiagnosticIds field | `LineSeparatorShouldUseTypeHelper` |
| Replacement properties | Serialized `CodeFixReplacementResolution` for `Type Helper` |

## Analyzer logic

Reports standalone `10` assignments to `Char`, `Code[1]`/`Code[2]`, and `Text[1]`/`Text[2]`. Two immediately adjacent assignments form a CRLF pair only when they are either `Text[1] := 13; Text[2] := 10;` on the same text variable or assignments to two `Char` variables. The diagnostic is reported on the `13` assignment only; the paired `10` assignment is suppressed.

## CodeFix logic

The CodeFix (`LineSeparatorShouldUseTypeHelperCodeFixProvider`) performs one of these transformations:

| Source | Replacement |
|---|---|
| Standalone LF | `<target> := TypeHelper.LFSeparator();` |
| Text CRLF pair | `<text> := TypeHelper.CRLFSeparator();` and removes the second assignment |
| Char CRLF pair | Assigns `TypeHelper.CRLFSeparator() [1]` and `[2]` to the two existing targets |

It reuses an existing compatible local or global object variable. Otherwise, it inserts a local declaration using the resolved replacement's type, subtype, and naming pattern. `CodeFixOverrides.AC0025` can override the variable declaration and map `LFSeparator` and `CRLFSeparator` to target method names.

## Design decisions

| Decision | Rationale |
|---|---|
| Require immediately adjacent CRLF assignments | Avoids rewriting unrelated values of `13` and `10` |
| Require same Text variable at indexes 1 and 2 | Only this sequence can collapse safely to one text assignment |
| Preserve separate Char assignments | Each assignment target may be semantically meaningful |
| Do not support FixAll | A CRLF fix can remove or modify the statement adjacent to its diagnostic |
| Resolve configuration in the analyzer | The CodeFix receives immutable diagnostic properties and does not reload settings |

## Test coverage

**HasDiagnostic (5 cases):** CRLFSeparatorChar, CRLFSeparatorText, LFSeparatorChar, LFSeparatorCode, LFSeparatorText.
**NoDiagnostic (2 cases):** LFSeparatorCodeElementAccess3, LFSeparatorTextElementAccess3.
**HasFix (6 cases):** CRLFSeparatorChar, CRLFSeparatorText, LFSeparatorChar, LFSeparatorConfiguredReplacement, LFSeparatorWithExistingGlobalTypeHelper, LFSeparatorWithExistingLocalTypeHelper.