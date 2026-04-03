---
category: stack
id: runtime
---

# Runtime Stack

mill runs as a set of Claude Code skills — no standalone binary, no server, no frontend app.

## Core Dependencies

| Component | Purpose |
|-----------|---------|
| **Claude Code CLI** | LLM runtime — skills execute inside Claude Code sessions |
| **`gh` CLI** | GitHub integration — issues as published specs, PRs from ship |
| **git** | Collaboration layer — `.mill/` state syncs via git |

## Prompt Assets

Prompts and templates are bundled with the skill plugin:

| Directory | Contents |
|-----------|----------|
| `shape/prompts/` | Spec elicitation, draft validation, context warmup |
| `ship/prompts/` | Work loop, verification, autopick, observations |
| `ground/prompts/` | Kickstart |
| `shape/templates/` | Spec output templates (feature, bug, security, task) |
| `ground/templates/` | Archetypes and tech stack profiles for kickstart |

## State

Per-repo state lives in `.mill/`:
- `config.json` — project config (scoring, health rules)
- `context.md` — auto-generated project context (hash-tracked freshness)
- `ground/` — product knowledge (committed, shared)
- `shape/drafts/` — spec drafts (gitignored, local WIP)
- `ship/history.json` — execution history (committed)
