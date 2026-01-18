# Feature: {{TITLE}}

> {{ONE_SENTENCE_DESCRIPTION}}

## Type
Feature

## GitHub Labels
- enhancement
- {{additional labels}}

## Status
Draft | Ready | In Progress | Complete

---

## Context

### Problem
{{Why does this feature need to exist? What pain point or opportunity?}}

### Users
{{Who will use this? Personas or user types}}

### Value
{{What's the business/user value?}}

---

## User Stories

### Primary Story
As a {{user_type}}, I want {{goal}} so that {{reason}}.

### Additional Stories
{{Additional user stories if needed}}

---

## Acceptance Criteria

### Core Criteria

1. **{{Criterion Name}}**
   - Given: {{precondition}}
   - When: {{action}}
   - Then: {{expected result}}

2. **{{Criterion Name}}**
   - Given: {{precondition}}
   - When: {{action}}
   - Then: {{expected result}}

3. **{{Criterion Name}}**
   - Given: {{precondition}}
   - When: {{action}}
   - Then: {{expected result}}

### Edge Cases

- {{Edge case and expected behavior}}
- {{Edge case and expected behavior}}

---

## Scope

### In Scope
- {{What's included}}
- {{What's included}}

### Out of Scope
- {{What's explicitly excluded}}
- {{What's explicitly excluded}}

### Dependencies
- {{External dependencies, if any}}

---

## Verification

### Automated Tests
- [ ] {{Test description}}
- [ ] {{Test description}}

### Manual Verification
- [ ] {{Manual check}}
- [ ] {{Manual check}}

### Verification Commands
```bash
{{Commands to run to verify}}
```

---

## Loop Contract

- **Success Criteria:** All acceptance criteria pass; all verification commands succeed
- **Completion Promise:** `MILL_DONE`
- **Verification Commands:** {{list commands}}
- **Stop Conditions:** {{max iterations}} iterations or {{time limit}}
- **Rollback Strategy:** {{how to revert if needed}}

---

## Notes

{{Any additional context, decisions, or references}}

---

*Created: {{DATE}}*
*Source: Chat elicitation*
