---
description: Build a concise, Ralph-ready understanding of the repo, its specs, and its constraints.
allowed-tools: Read, Glob, Grep, Bash
---

# Warmup
Perform the analyses defined in "Workflow" and produce the "Report".

## Workflow
- Run `git ls-files` to enumerate tracked files.
- Read `README.md` for product and architecture context.
- Read `.mill/ROADMAP.md` if present.
- Read all files under `.mill/standards/` and summarize rules.
- Read high-level architecture docs under `.mill/backend/` and `.mill/frontend/` as relevant.
- Scan recent history: `git log -n 20 --oneline`.

## Report
Provide a concise summary covering:
- Core product purpose and modules.
- Key architectural constraints and conventions.
- Relevant standards and non-negotiables.
- Notable recent changes that might affect the task.
