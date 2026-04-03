---
category: rules
id: conventions
---

# Conventions

## Prompt Naming

Format: `[subject]-[verb].md` — e.g., `spec-draft.md`, `loop-iterate.md`, `context-warmup.md`.

## Template Naming

Noun form under `{phase}/templates/` — e.g., `feature.md`, `saas.md`.

## Directory Structure

Prompts and templates are organized by phase:

```
{phase}/
├── prompts/       # LLM prompts
└── templates/     # Output templates (if applicable)
```

## Two-Prompt Verification

Work and verification are always separated into distinct prompts. The work prompt cannot grade its own homework. Only the verify prompt can authorize completion.

## Spec Publishing

Specs publish to GitHub Issues (single source of truth). Drafts are local WIP (gitignored). Completed specs never live in the filesystem — only in the issue tracker.
