# Create Issue

Create a concise, actionable GitHub issue ready for a MILL loop. Analysis only — no code changes.

## Rules
- No code creation or editing
- Ground content in repo and `spec/` structure
- Keep compact and implementable

## Workflow
1. Read project instructions: check `AGENTS.md` (or `CLAUDE.md` if no AGENTS.md)
2. `git ls-files` — understand layout
3. Read `README.md`
4. Read `spec/standards/`, `spec/backend/`, `spec/frontend/`
5. Read provided issue via `gh issue view`
6. Ask targeted questions until outcome and acceptance criteria are clear

## Issue Format
```md
## Summary
<one paragraph>

## Scope
- In scope: ...
- Out of scope: ...

## Acceptance Criteria
- ...

## Loop Contract
- Success Criteria: ...
- Completion Promise: MILL_DONE
- Verification Commands: ...
- Stop Conditions: ...
- Rollback Strategy: ...
- Progress Reporting: ...

## Notes
<dependencies, risks, links>
```

## Input
{{USER_PROMPT}}
