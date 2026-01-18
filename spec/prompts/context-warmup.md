# Warmup

Build project context, write `.mill/context.md`, and generate `AGENTS.md` if missing.

## Progress Markers
Emit each marker on its own line BEFORE starting that step:
```
[1/9] Capturing commit hash...
[2/9] Enumerating project files...
[3/9] Detecting repo structure...
[4/9] Reading project instructions...
[5/9] Reading product context...
[6/9] Analyzing standards and specs...
[7/9] Reviewing recent changes...
[8/9] Writing context artifact...
[9/9] Generating project instructions...
```

## Workflow
1. `git rev-parse HEAD`
2. `git ls-files`
3. Detect monorepo (see below)
4. Read project instructions: check `AGENTS.md` (or `CLAUDE.md` if no AGENTS.md)
5. Read `README.md`, `.mill/ROADMAP.md` if present
6. Read `.mill/standards/`, `.mill/backend/`, `.mill/frontend/`
7. `git log -n 20 --oneline`
8. Write `.mill/context.md`
9. If no `AGENTS.md` exists: generate it (see below)

## Monorepo Detection
Signs: multiple `package.json`/`.csproj`/`go.mod` files, `packages/`/`apps/`/`services/` dirs, workspace configs.

If monorepo: identify each module, read its README/entry files, note inter-module dependencies.

## Output: `.mill/context.md`

```markdown
<!-- mill-context-hash: {GIT_COMMIT_HASH} -->
# Project Context

## Summary
{Repo summary — note if monorepo}

## Standards
{Constraints from standards}

## Architecture
{Architecture references}

## Recent Changes
{From git log}

## Tech Stack
{Languages, frameworks, deps}

{IF MONOREPO — for each module:}
---
# {Module Name}
## Purpose
## Tech Stack
## Key Files
## Dependencies
---
{END MONOREPO}

---
*Generated: {TIMESTAMP}*
```

First line MUST be the HTML comment with git hash (for staleness detection).

## AGENTS.md Generation

If `AGENTS.md` does not exist, create it based on your analysis. This file provides project instructions for AI coding agents.

**Skip step 9 if `AGENTS.md` already exists.**

```markdown
# Project Instructions

## Overview
{One paragraph: what this project is, its purpose}

## Tech Stack
{Languages, frameworks, key dependencies — be specific with versions if detectable}

## Project Structure
{Key directories and their purposes}

## Conventions
{Coding style, naming conventions, patterns used — infer from existing code}

## Build & Test
{Commands to build, test, lint — detect from package.json, Makefile, etc.}

## Key Files
{Entry points, config files, important modules}
```

Keep it concise and factual. Only include what you can infer from the codebase.

## Report
Confirm `.mill/context.md` written. If `AGENTS.md` was created, confirm that too.

## Input
{{USER_PROMPT}}
