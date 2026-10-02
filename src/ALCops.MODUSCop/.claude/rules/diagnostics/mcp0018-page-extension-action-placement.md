---
paths:
  - "src/ALCops.MODUSCop/**/PageExtensionActionPlacement*"
---

# MCP0018: PageExtensionActionPlacement

## Purpose

Requires page-extension action additions to use AddLast and prevents moving actions.

## Design decisions

- Register on Change symbols and constrain the containing application object to a page extension.
- Use the declared action-add/action-move syntax classes to distinguish action changes from layout control changes. These public SDK shapes avoid reflection over the hidden ChangeTargetKind property.
- Compare the bound ChangeKind through MODUSCop's local enum provider: reject AddAfter, AddBefore, AddFirst, MoveAfter, MoveBefore, MoveFirst and MoveLast. Its inert fallback cannot impersonate a real change when a runtime enum member is absent.
- Preserve Error severity, UIDesign category and enabled-by-default behavior.

## Deliberate non-reports

- AddLast, modifications, control changes, obsolete changes and synthesized symbols are ignored.

## Test notes

No tests are requested. Validate all three frameworks sequentially and targeted formatting.
