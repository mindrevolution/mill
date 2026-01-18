# Model

Build a complete, executable model for a feature before implementation.

## Preconditions
- `spec/.context.md` must exist
- Read project instructions: check `AGENTS.md` (or `CLAUDE.md` if no AGENTS.md)
- Read `spec/features/`, `spec/standards/`, architecture docs

## Rules
- Ask questions until model is sufficient
- Integrate architecture, standards, constraints, success criteria
- If ambiguous, stop and request clarification

## Output: `spec/model/<name>.md`

```md
# Model: <title>

## Intent
<what and why>

## Inputs & Outputs
<boundaries>

## Constraints
<technical, product, security, operational>

## Assumptions

## Success Criteria
<machine-checkable, aligned to tests>

## Verification
<commands + expected outcomes>

## Limits
- Max iterations: <n>
- Time budget: <min>
- Cost budget: <optional>

## Non-Goals

## Risks
<known risks and mitigations>
```

## Input
{{USER_PROMPT}}
