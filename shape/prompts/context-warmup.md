# Warmup

Build project context, write `.mill/context.md`, and generate `AGENTS.md` if missing.

## Progress Markers
Emit each marker on its own line BEFORE starting that step:
```
[1/10] Capturing commit hash...
[2/10] Enumerating project files...
[3/10] Detecting repo structure...
[4/10] Reading project instructions...
[5/10] Reading product context...
[6/10] Analyzing standards and specs...
[7/10] Reviewing recent changes...
[8/10] Mapping architecture...
[9/10] Writing context artifact...
[10/10] Generating project instructions...
```

## Workflow
1. `git rev-parse HEAD`
2. `git ls-files`
3. Detect monorepo (see below)
4. Read project instructions: check `AGENTS.md` (or `CLAUDE.md` if no AGENTS.md)
5. Read `README.md`, `.mill/ROADMAP.md` if present
6. Read `.mill/standards/`, `.mill/backend/`, `.mill/frontend/`
7. `git log -n 20 --oneline`
8. **Map architecture** (see Architecture Analysis below)
9. Write `.mill/context.md`
10. If no `AGENTS.md` exists: generate it (see below)

## Architecture Analysis

After reading project files and before writing context, trace the project's architectural structure:

1. **Identify entry points** — Find main program files, API route registrations, app bootstrapping, CLI entry points. Record each with file:line.
2. **Trace layer boundaries** — For each entry point, follow the call chain one level deep. Identify the layers the project uses (e.g., endpoints → services → providers, components → hooks → API client). Note the key file at each layer.
3. **Map abstractions** — Find interfaces, base classes, and shared patterns that define how the project is extended (e.g., `ILlmProvider`, `IIssueProvider`, middleware pipelines, component patterns). Record each with file:line.
4. **Note data flow** — How does data move through the system? Request → response path for APIs; state → render path for UI; input → output for CLI tools.

Keep this analysis concise — the goal is orientation, not exhaustive documentation. Focus on what an agent needs to know to make changes without breaking things.

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

### Layers
{Name each layer and its responsibility. Example:}
- **Endpoints** (`api/Endpoints/`) — HTTP API surface, route registration
- **Services** (`api/Services/`) — Business logic, orchestration
- **Providers** (`api/Services/Providers/`) — External integrations (LLM, issue trackers)
- **UI Components** (`workbench/src/components/`) — React presentation layer

### Entry Points
{List with file:line references. Example:}
- `api/Program.cs:1` — API host bootstrap
- `workbench/src/main.tsx:1` — React app entry
- `cli/Program.cs:1` — Desktop launcher entry

### Key Abstractions
{Interfaces, base classes, and patterns that define extension points. Example:}
- `ILlmProvider` at `api/Services/Providers/ILlmProvider.cs:5` — LLM execution contract
- `IIssueProvider` at `api/Services/Providers/IIssueProvider.cs:3` — Issue tracker contract

### Data Flow
{How data moves through the system. Example:}
- HTTP request → endpoint → service → provider → external CLI/API → response
- UI action → API client → backend → SSE/response → React state → render

## Recent Changes
{From git log}

## Tech Stack
{Languages, frameworks, deps}

## Key Files
{5-15 files an agent should read before making changes, with one-line descriptions}

{IF MONOREPO — for each module:}
---
# {Module Name}
## Purpose
## Tech Stack
## Layers
## Entry Points
## Key Files
## Dependencies
---
{END MONOREPO}

---
*Updated: {TIMESTAMP}*
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
