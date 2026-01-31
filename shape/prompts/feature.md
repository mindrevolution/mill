# Feature

Turn a concept into a bounded, feasible feature spec.

## Preconditions
- `.mill/context.md` must exist
- Read project instructions: check `AGENTS.md` (or `CLAUDE.md` if no AGENTS.md)

## Rules
- Read relevant concept in `.mill/concepts/`
- If concept has open questions, stop and resolve
- Define scope, constraints, success signals
- If too large, split and stop

## Output: `.mill/features/<name>.md`

```md
# Feature: <title>

## Source Concept
<link>

## Scope
- In scope: ...
- Out of scope: ...

## Constraints
<from standards/architecture>

## Success Signals

## Open Questions
<if any, stop and request>

## Feasibility
- Single-issue feasible: yes|no
- If no, list minimal split and stop
```

## Input
{{USER_PROMPT}}
