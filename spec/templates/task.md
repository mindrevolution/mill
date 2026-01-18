# {{TITLE}}

> {{ONE_SENTENCE_DESCRIPTION}}

## Status
Draft | Ready | In Progress | Complete

## Priority
High | Medium | Low

---

## Rationale

### Why This Work?
{{Technical motivation — why is this needed now?}}

### Driver
- [ ] Dependency update
- [ ] Technical debt
- [ ] Refactoring
- [ ] Configuration change
- [ ] Documentation
- [ ] Cleanup/removal
- [ ] Infrastructure
- [ ] Other: {{specify}}

### Impact of NOT Doing This
{{What happens if we defer? Risk of inaction.}}

---

## Scope

### Files/Modules Affected
- {{file or module}}
- {{file or module}}

### What Changes
{{Describe the concrete changes}}

### What Does NOT Change
{{Explicit exclusions — behavior, APIs, interfaces that must remain stable}}

---

## Acceptance Criteria

1. **{{Criterion}}**
   - Verifiable: {{how to check}}

2. **{{Criterion}}**
   - Verifiable: {{how to check}}

3. **{{Criterion}}**
   - Verifiable: {{how to check}}

---

## Verification

### Automated Checks
- [ ] {{test or check}}
- [ ] {{test or check}}

### Manual Verification
- [ ] {{manual check if needed}}

### Verification Commands
```bash
{{Commands to verify completion}}
```

---

## Regression Guardrails

### Must NOT Break
- {{existing behavior that must remain unchanged}}
- {{API contract that must remain stable}}

### Regression Tests
```bash
{{Commands to verify no regressions}}
```

---

## Loop Contract

- **Success Criteria:** All acceptance criteria met; no regressions; verification passes
- **Completion Promise:** `MILL_DONE`
- **Verification Commands:** {{list commands}}
- **Stop Conditions:** {{max iterations}} iterations
- **Rollback Strategy:** {{how to revert if needed}}

---

## Notes

{{Additional context, related work, dependencies}}

---

*Built from intent with MILL*
