---
description: Execute a Ralph loop iteration for a plan in spec/plans/.
argument-hint: [path-to-plan]
allowed-tools: Read, Write, Edit, MultiEdit, Bash
---

# Implement (Ralph Loop Iteration)
Execute a single Ralph iteration against the plan at PATH_TO_PLAN. Always honor standards and the Loop Contract.

## Variables
PATH_TO_PLAN: $ARGUMENTS
PLAN_DIRECTORY: "./spec/plans/"
GITHUB_ISSUE_ID: provided via prompt arguments or inferred from the plan filename (ends with #NUMBER.md)

## Workflow
- If PATH_TO_PLAN is missing, request it and stop.
- Read `spec/standards/` and relevant `spec/backend/` or `spec/frontend/` docs.
- Load the plan from PATH_TO_PLAN and verify it includes a Loop Contract.
  - If missing, stop and request a corrected plan.
- Identify success criteria, completion promise, and validation commands.
- Implement tasks in order while minimizing unrelated changes.
- Run the validation commands from the plan.
- If all success criteria are met, finalize and output the completion promise token.
- If not complete, output a concise progress report and the next-step intent.

## Reporting
Provide a concise summary:
- What changed and why.
- Validation results.
- Remaining gaps, if any.
If a GitHub issue is known, prepare an update summary suitable for posting to the issue.

## Completion Output
If complete, output the exact completion promise token on its own line at the end.
If not complete, output `MILL_CONTINUE` on its own line at the end.
