---
description: "Orient Claude to your codebase • https://mill.mindrevolution.com/warmup"
allowed-tools: Read, Write, Glob, Grep, Bash(*git *)
---

# Warmup

Orient Claude to your codebase. Loads existing context if fresh, regenerates if stale.

## Step 1: Determine Freshness

Read `.mill/context.md` — extract hash from first line: `<!-- mill-context-hash: {HASH} -->`. If file doesn't exist → `missing`.

Run `git rev-parse HEAD`. If hashes match → `fresh`. If different: `git rev-list --count {HASH}..HEAD` → 1–25 = `recent`, >25 = `stale`.

| Freshness | Action |
|-----------|--------|
| `fresh` | Load Mode |
| `recent` | Update Mode |
| `stale` / `missing` | Generate Mode |

---

## Load Mode (fresh)

1. Read `.mill/context.md`
2. `Glob(".mill/ground/**/*.md")` → read key ground files
3. Report: "Context loaded"

No writes. No git commands beyond freshness check.

---

## Update Mode (recent)

1. Read `.mill/context.md`
2. Read ground files (personas, rules, patterns)
3. `git log {HASH}..HEAD --oneline`
4. Report: "Context loaded ({N} new commits)"

---

## Generate Mode (stale or missing)

### Progress Markers

Emit before each step:
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

### Workflow

1. `git rev-parse HEAD`
2. `git ls-files`
3. Read `AGENTS.md` (or `CLAUDE.md`)
4. Read `README.md`, `.mill/ground/product.md` if present
5. `Glob(".mill/ground/standards/*.md")` → read each
6. `git log -n 20 --oneline`
7. Map architecture — keep concise (orientation, not exhaustive docs):
   - **Entry points** — main files, API routes, CLI entry (record file:line)
   - **Layer boundaries** — follow call chains (endpoints → services → providers)
   - **Abstractions** — interfaces, base classes, extension points
   - **Data flow** — how data moves through the system
8. Write `.mill/context.md`

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

First line MUST be the HTML comment with git hash.

## Observations

During generation, note discoveries:
- Empty ground folders
- Conventions in code (naming, architecture patterns)
- Personas implied by role enums or user types
- Domain vocabulary used consistently
- New modules not in previous context (update mode)

### High-confidence gaps (auto-write)

Write immediately:
- Path: `.mill/observations/warmup-{date}-{slug}.md`
- Frontmatter: `source: warmup`, `type: discovery|concern`, `created: {date}`

Example:

```markdown
---
source: warmup
type: concern
created: 2025-02-08
---

# Empty Ground Folders

No ground truth defined. Missing: personas/, rules/, patterns/.
Discovered conventions from code: [list findings].
Suggested: run /mill:ground to curate.
```

### Uncertain gaps (collect and ask)

Collect during warmup, ask at end via AskUserQuestion (multiSelect):

```yaml
AskUserQuestion:
  question: "Found potential ground truth. Note any for review?"
  header: "Observations"
  multiSelect: true
  options:
    - label: "Conventions discovered"
      description: "No Async suffix, enums as strings, kebab-case routes"
    - label: "Personas implied"
      description: "Producer, Director, Camera roles found in MuxRole enum"
```

For selected items, write observation files.

## Report

Confirm `.mill/context.md` written with commit hash. Mention observations written (if any).
