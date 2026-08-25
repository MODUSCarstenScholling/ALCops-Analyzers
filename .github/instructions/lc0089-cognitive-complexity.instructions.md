---
applyTo: 'src/ALCops.LinterCop/**/CognitiveComplexity*'
---

# LC0089/LC0090 Cognitive Complexity

## Purpose

LC0089 reports cognitive-complexity metrics, LC0089i reports individual increments, and LC0090 reports when the configured threshold is reached.

## Diagnostic properties

| Property | Value |
|----------|-------|
| IDs | LC0089, LC0089i, LC0090 |
| Category | Design |
| Severity | Info (LC0089/LC0089i), Warning (LC0090) |
| Enabled | LC0089/LC0089i: No; LC0090: Yes |
| CodeFix | No |

## Design decisions

| Decision | Rationale |
|----------|-----------|
| Resolve `Error(...)` and `FieldError(...)` guard clauses through the semantic model | User-defined procedures with the same names must retain their cognitive-complexity increment. The complete code expression is resolved so AL calls without parentheses are also covered. |
| Delegate `Error`/`FieldError` recognition to `FlowTerminatingBuiltIns.IsFlowTerminatingCall(IOperation?)` | That classifier accepts a clean bind (`MethodKind.BuiltInMethod`) or an invalid call on a `Dialog`/`Record`/`FieldRef` receiver, so `if X then Error(UndefinedVar)` stays a guard while arguments do not bind instead of flickering between +0 and +1, and user-defined/referenced-app procedures named `Error` stay demoted. Sharing the rule keeps LC0089/LC0090 aligned with PC0038 and FC0007. |
| Use `context.SemanticModel` from the code-block context | `Compilation.GetSemanticModel` creates an uncached `SyntaxTreeSemanticModel` on every call, so obtaining a model per code block re-bound each procedure from scratch; the code-block context already carries the model for its tree. |
| Keep `exit`, `continue`, and `CurrReport`/`CurrXMLport` commands syntactic | Their existing syntax-specific behavior is unchanged; semantic resolution is limited to the shared built-in terminator names. |

## Architecture

- Registers a code-block action and walks method and trigger syntax iteratively.
- Uses the semantic model supplied by the code-block context; no models are created by the analyzer.
- `IsGuardExpression` binds the `then` expression once and passes the operation to `FlowTerminatingBuiltIns.IsFlowTerminatingCall`; everything else falls through to the lexical `Break`/`Continue`/`Quit`/`Skip` checks.

## Known issues

- An `Error`/`FieldError` call whose receiver is itself unresolved (for example `Foo.Error(x)` with `Foo` never declared) is not recognised as a guard clause and scores +1 until the receiver is declared.

## Roadmap

- Unify the lexical and semantic guard models. `Break`, `Continue`, `Quit` and `Skip` (and the `CurrReport`/`CurrXMLport` receivers) are still matched purely lexically.

## Test coverage

**HasDiagnostic (9 cases):** ConditionalExpressionNested, IfStatement, IfStatementNested, RecursionDirect, RecursionIndirect, RecursionDirectWithoutParentheses, RecursionIndirectWithoutParentheses, UserDefinedErrorNotGuardClause, UserDefinedFieldErrorNotGuardClause.
**NoDiagnostic (9 cases):** CurrReportGuardClause, CurrXMLportGuardClause, IfStatement, DiscountConsecutiveAndOperator, IfStatementElseIf, IfStatementGuardClause, IfStatementGuardClauseFieldRefFieldErrorWithoutParentheses, IfStatementGuardClauseContinue, IfStatementGuardClauseFieldError.
**HasDiagnosticInDocumentWithErrors (1 case):** UserDefinedErrorNotGuardClauseUnboundArgument.
**NoDiagnosticInDocumentWithErrors (2 cases):** IfStatementGuardClauseErrorUnboundArgument, IfStatementGuardClauseFieldErrorUnboundArgument.

## CodeFix

No CodeFix is provided.
