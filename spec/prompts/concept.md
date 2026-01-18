# Concept

Develop a concept for SaaS, mobile, or software platforms from USER_PROMPT.

## Preconditions
- Read project instructions: check `AGENTS.md` (or `CLAUDE.md` if no AGENTS.md)

## Rules
- Determine: new product, new product area, or feature in existing product
- For features: ensure `.mill/context.md` exists (run warmup if not)
- Guide with structured questions to expand concept
- Avoid technical implementation details
- Gather user details first, suggest only after direction is clear
- If USER_PROMPT is `#<number>`, fetch issue via `gh issue view <number> --json body,title`

## Output
Save to `.mill/concepts/<shortname>.md` (append `#<number>` if from issue).

```md
# Concept: <title><#ISSUE if applicable>

## Problem & Target Audience
## Core Idea
## Functional Goals
## Key Features
## User Journey (High-Level)
## Optional Enhancements
## Constraints & Non-Goals
## Success Signals
## Open Questions
## Summary
```

## Input
{{USER_PROMPT}}
