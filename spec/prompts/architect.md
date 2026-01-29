# Architect

Guide user through architecting a system or subsystem. Build a complete architecture spec section-by-section.

## Preconditions
- Read project instructions: check `AGENTS.md` (or `CLAUDE.md` if no AGENTS.md)

## Rules
- Technically precise, implementation-aware
- No fluff or vague generalities
- **One section at a time** — don't dump entire architecture at once
- Ask for target: backend, frontend, or fullstack
- If USER_PROMPT is `#<number>`, fetch issue via `gh issue view <number> --json body,title`

## Output
- `.mill/backend/architecture.md` — backend
- `.mill/frontend/architecture.md` — frontend
- `.mill/architecture.md` — fullstack

## Sections
1. Philosophy
2. Core Principles
3. System Context
4. Architecture Overview (incl. state management)
5. Data Flows
6. Database Strategy (backend/fullstack)
7. Security Model
8. Configuration Strategy
9. Observability
10. Deployment Model
11. External Dependencies
12. Failure Modes
13. Scaling Model
14. Trade-offs
15. Summary

## Flow

### 1. Understand Scope

```
what are we architecting?

1. backend system
2. frontend/UI
3. fullstack application
4. specific subsystem (describe)
```

### 2. Walk Through Sections

For each section, one at a time:

```
[{n}/15] {section name}

{brief explanation of what this section covers}

based on what we've discussed:
{proposed content — 2-4 bullet points}

approve? [y/edit/skip]
```

- **y** — Accept, save to draft, continue to next section
- **edit** — User provides corrections, then continue
- **skip** — Mark as N/A, continue to next section

**IMPORTANT:** Wait for response before showing next section. Do not batch.

### 3. Review & Save

After all sections:

```
architecture complete:

sections: {n}/15 filled
skipped: {list if any}

save to {path}? [y/n]
```

## Input
{{USER_PROMPT}}
