# Plan

Construct a loop-ready implementation plan. Must be single-issue sized with Loop Contract.

## Preconditions
- `spec/.context.md` must exist (run warmup if not)
- Read project instructions: check `AGENTS.md` (or `CLAUDE.md` if no AGENTS.md)
- If USER_PROMPT is `#<number>`, fetch issue via `gh issue view <number> --json body,title`
- Feature must exist in `spec/features/`, model in `spec/model/` (stop if missing)

## Rules
- If no USER_PROMPT, request and stop
- Inspect codebase for patterns
- Identify type (chore|feature|refactor|bug|enhancement) and complexity (simple|medium|complex)
- **Feasibility gate:** if not single-issue feasible, propose minimal split and stop
- Include success criteria and completion token
- Validation commands must be executable and unambiguous

## Format

```md
# Plan: <name><#ISSUE if provided>

## Type & Size
- Type: chore|feature|refactor|bug|enhancement
- Complexity: simple|medium|complex

## Description

## Objective

## Scope & Non-Goals

## Feasibility
- Single-issue feasible: yes|no
- Rationale:
- If no: list split and stop

## Approach

## Relevant Files
<with reasons>

## Steps
### 1. <Task>
- action
- action

### 2. <Task>
...

(Last step: validate)

## Testing Strategy

## Acceptance Criteria

## Loop Contract
- Success Criteria:
- Completion Promise: MILL_DONE
- Verification Commands:
- Stop Conditions:
- Rollback Strategy:
- Progress Reporting:

## Limits
- Max iterations:
- Time budget:
- Cost budget:

## Validation Commands

## Notes
```

## Input
{{USER_PROMPT}}
