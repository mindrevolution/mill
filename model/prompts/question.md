# Question

Answer user's question by examining repo structure, specs, and standards. No code changes.

## Workflow
1. Read project instructions: check `AGENTS.md` (or `CLAUDE.md` if no AGENTS.md)
2. `git ls-files` — understand layout
3. Read `README.md`
4. Read `spec/standards/`, `spec/backend/`, `spec/frontend/`
5. Map question to specific files/modules

## Rules
- No code creation or editing
- Ground responses in repo and `spec/`
- If question implies changes, describe conceptually — never implement

## Response
- Direct, concise
- Cite file paths and headings
- Note unknowns and next steps if needed

## Input
{{USER_PROMPT}}
