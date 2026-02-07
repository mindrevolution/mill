---
description: Answer questions about the codebase without making changes
allowed-tools:
  - Read
  - Glob
  - Grep
  - Bash(git log*, git show*, git diff*)
model: sonnet
argument-hint: "<question about the codebase>"
---

# Question

Answer the user's question by examining repo structure, specs, and standards. No code changes.

## Workflow

1. Read project instructions: check `AGENTS.md` (or `CLAUDE.md` if no AGENTS.md)
2. Read `.mill/context.md` if it exists
3. Search codebase for relevant files
4. Read key files that answer the question
5. Synthesize answer

## Rules

- **No code creation or editing** — read-only exploration
- Ground responses in actual repo files
- If question implies changes, describe conceptually — never implement
- Cite file paths with line numbers: `src/api/service.ts:42`

## Response Style

- Direct, concise answers
- Include relevant code snippets
- Cite file paths and headings
- Note unknowns and suggest next steps if needed

## CLI Integration

Use mill CLI for structured data when helpful:
```bash
mill ground list           # List knowledge items
mill issue list            # List open issues
mill context               # Show project context
```

## Example

User: "Where is authentication handled?"

Good response:
> Authentication is handled in `src/middleware/auth.ts:15`. The `validateToken` function checks JWT tokens and extracts user info. It's registered as middleware in `src/app.ts:8`.

Bad response:
> Let me create an authentication system for you... (NO - this is a question, not a task)
