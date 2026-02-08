---
description: Generate codebase context and write .mill/context.md
allowed-tools: Read, Write, Glob, Grep, Bash(mill *, git *)
argument-hint: "[force] - regenerate even if context exists"
---

# Warmup

Build project context and write `.mill/context.md`.

## Prerequisites

Check if mill is initialized:
```bash
mill init --human
```

If not initialized, run `mill init` first.

## Progress Markers

Emit each marker on its own line BEFORE starting that step:
```
[1/8] Capturing commit hash...
[2/8] Enumerating project files...
[3/8] Reading project instructions...
[4/8] Reading product context...
[5/8] Analyzing standards...
[6/8] Reviewing recent changes...
[7/8] Mapping architecture...
[8/8] Writing context...
```

## Workflow

1. `git rev-parse HEAD` — capture current commit
2. `git ls-files` — enumerate project files
3. Read `AGENTS.md` (or `CLAUDE.md` if no AGENTS.md)
4. Read `README.md`, `.mill/ground/product.md` if present
5. Read `.mill/ground/standards/*.md`
6. `git log -n 20 --oneline`
7. Map architecture:
   - Identify entry points (main files, bootstrapping)
   - Trace layer boundaries (endpoints → services → providers)
   - Map key abstractions (interfaces, base classes)
   - Note data flow patterns
8. Write `.mill/context.md`

## Architecture Analysis

After reading project files, trace the architectural structure:

1. **Entry points** — Find main program files, API routes, CLI entry. Record with file:line.
2. **Layer boundaries** — Follow call chains. Identify layers (endpoints → services → providers).
3. **Abstractions** — Find interfaces and patterns that define extension points.
4. **Data flow** — How data moves through the system.

Keep concise — orientation, not exhaustive docs.

## Output Format

Write to `.mill/context.md`:

```markdown
<!-- mill-context-hash: {GIT_COMMIT_HASH} -->
# Project Context

## Summary
{Repo summary}

## Architecture

### Layers
- **Endpoints** (`api/Endpoints/`) — HTTP API surface
- **Services** (`api/Services/`) — Business logic

### Entry Points
- `api/Program.cs:1` — API bootstrap
- `cli/Program.cs:1` — CLI entry

### Key Abstractions
- `IProvider` at `api/Services/IProvider.cs:5` — Extension contract

### Data Flow
- HTTP request → endpoint → service → provider → response

## Recent Changes
{From git log}

## Tech Stack
{Languages, frameworks}

## Key Files
{5-15 files to read before making changes}

---
*Updated: {TIMESTAMP}*
```

First line MUST be the HTML comment with git hash (for staleness detection).

## Observations

During context generation, note discoveries for later review:
- New modules not in context
- Architecture changes detected
- Stale documentation found

### Writing Observations

Write observation files when discovering significant changes:

- Path: `.mill/observations/warmup-{date}-{slug}.md`
- Frontmatter: source: warmup, type: discovery
- Describe what was discovered

Example observation:

```markdown
---
source: warmup
type: discovery
created: 2025-02-08
---

# New Module: Notifications

Found new `src/Notifications/` module not covered in previous context.

## Details

- Location: `src/Notifications/`
- Files: 12 new files
- Purpose: Appears to handle push notifications

## Suggested Action

Review module structure and update ground/ if needed.
```

Don't interrupt the warmup flow — observations are reviewed later via /mill:ground.

## Report

Confirm `.mill/context.md` written with commit hash.
