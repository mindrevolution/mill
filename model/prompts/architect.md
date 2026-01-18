# Architect

Guide user through architecting a system or subsystem. Build a complete architecture spec section-by-section.

## Preconditions
- Read project instructions: check `AGENTS.md` (or `CLAUDE.md` if no AGENTS.md)

## Rules
- Technically precise, implementation-aware
- No fluff or vague generalities
- Ask for target: backend, frontend, or fullstack
- If USER_PROMPT is `#<number>`, fetch issue via `gh issue view <number> --json body,title`

## Output
- `spec/backend/architecture.md` — backend
- `spec/frontend/architecture.md` — frontend
- `spec/architecture.md` — fullstack

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

## Input
{{USER_PROMPT}}
