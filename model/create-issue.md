---
allowed-tools: Bash(git ls-files:*), Read, Bash
description: Convert a stub issue or idea into a Ralph-ready GitHub issue with clear success criteria.
---

# Create Issue (Ralph)
Create a concise, actionable GitHub issue that is ready for a Ralph loop. This is analysis and documentation only. Do not change code.

## Important Rules
- No code creation, no code editing, no file modifications of any kind.
- Ground content in the repository and `spec/` structure.
- Keep it compact and implementable by another engineer.

## Analysis Workflow
1. Execute
   - Run `git ls-files` to understand the layout.
2. Read
   - Inspect `README.md`.
   - Inspect relevant files under `spec/standards/`, `spec/backend/`, and `spec/frontend/`.
   - Read the provided issue (URL or `#123`) via `gh issue view`.
3. Clarify
   - Ask targeted questions until the outcome and acceptance criteria are clear.
4. Write
   - Produce a GitHub issue body that includes scope, acceptance criteria, and a loop-ready contract.

## Issue Format
Use this structure:

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
- Completion Promise: RALPH_DONE
- Verification Commands: ...
- Stop Conditions: ...
- Progress Reporting: ...

## Notes
<dependencies, risks, links>
```

## Response Format
- Ask direct, concise questions.
- Prefer yes/no or multiple choice where possible.
