---
paths:
  - "src/ALCops.MODUSCop/**/PageExtensionActionPromotion*"
---

# MCP0017: PageExtensionActionPromotion

## Purpose

Prevents added page-extension actions from introducing promotion.

## Design decisions

- Inspect `AddedActionsFlattened` on the page-extension symbol so nested additions are covered without treating modified base actions as additions.
- Property presence is the rule: explicit `Promoted = false` is rejected just like true. Inspect the original action definition's property rather than a computed Boolean value.
- Action references also introduce promotion and are rejected even without a Promoted property.
- Preserve Error severity, UIDesign category and enabled-by-default behavior. Prefer the property location when available.

## Deliberate non-reports

- Obsolete page extensions and obsolete added actions are ignored.
- Base-page actions, control changes and modified actions are outside this rule.

## Test notes

No tests are added by request. The installed SDK interfaces are checked by three-framework builds and targeted formatting.
