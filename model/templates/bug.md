# Bug: {{TITLE}}

> {{ONE_SENTENCE_DESCRIPTION}}

## Type
Bug

## GitHub Labels
- bug
- {{additional labels}}

## Status
Draft | Ready | In Progress | Verified

## Severity
Critical | High | Medium | Low

---

## Problem

### Expected Behavior
{{What should happen}}

### Actual Behavior
{{What actually happens}}

### Impact
{{Who is affected, workarounds available, business impact}}

---

## Reproduction

### Preconditions
- {{Starting state}}
- {{Required data/setup}}

### Steps to Reproduce
1. {{Step 1}}
2. {{Step 2}}
3. {{Step 3}}
4. {{Observe: bug manifests}}

### Reproducibility
- [ ] 100% reproducible
- [ ] Intermittent (~{{percentage}}%)
- [ ] Environment-specific

### Environment
- **Browser/Client:** {{version}}
- **OS:** {{operating system}}
- **Environment:** Development | Staging | Production
- **User Role:** {{if relevant}}
- **Data Conditions:** {{if relevant}}

---

## Context

### When Started
{{When did this start? Recent changes?}}

### Related Changes
{{Any deployments, PRs, or changes that might be related}}

### Previous State
{{Did this ever work? When?}}

---

## Regression Test

### Failing Test Definition
```
Test: {{test name}}
Given: {{precondition}}
When: {{action that triggers bug}}
Then: {{should NOT exhibit bug behavior}}
```

### Test Location
{{Where this test should live}}

---

## Verification

### Bug Fixed When
- [ ] Reproduction steps no longer trigger the bug
- [ ] Regression test passes
- [ ] No new failures introduced

### Verification Commands
```bash
{{Commands to run to verify fix}}
```

---

## Loop Contract

- **Success Criteria:** Bug no longer reproducible; regression test passes; all existing tests pass
- **Completion Promise:** `MILL_DONE`
- **Verification Commands:** {{test commands}}
- **Stop Conditions:** {{max iterations}} iterations
- **Rollback Strategy:** Revert commit if fix introduces new issues

---

## Notes

{{Additional context, investigation notes, suspected cause}}

---

*Created: {{DATE}}*
*Source: Chat elicitation*
