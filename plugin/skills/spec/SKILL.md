---
description: "Turn intent into a precise, complete spec • https://mill.mindrevolution.com/spec"
disable-model-invocation: true
allowed-tools: Read, Write, Glob, Grep, Bash(*gh *, *git *, *rm *, *start *, *open *, *xdg-open *)
argument-hint: "[intent] - what you want to build, or [draft-slug] to resume"
---

# Spec

Transform user intent into a complete, loop-ready specification.

## Interaction Pattern

**Always use AskUserQuestion** — never raw text questions. 2-4 options plus free text. One question per call.

```yaml
AskUserQuestion:
  question: "What type of change is this?"
  header: "Type"
  options:
    - label: "Feature"
      description: "Add new capability"
    - label: "Bug"
      description: "Fix broken behavior"
    - label: "Task"
      description: "Refactor, migrate, cleanup"
```

All subsequent AskUserQuestion calls follow this format.

## Principles

| Principle | Definition |
|-----------|------------|
| **Self-Containment** | ∀ statement σ ∈ spec: an implementer unfamiliar with the system can execute σ without querying the author |
| **Language Independence** | WHAT and WHY at an abstraction level invariant under language transformation. Implementation hints marked language-specific. |
| **Decision Completeness** | Every parameter bound to a concrete value. No TBD. For conditionals: `default UNLESS predicate → alternative`. |
| **Explicit Non-Applicability** | Silent omission conflates "rejected" with "overlooked." When N/A, state: `N/A: {reason}`. |
| **Binary State** | Document is either **draft** (requires clarification) or **ready** (self-contained). No intermediate states. |

### Self-Containment Test

If a reader must ask, the spec has failed:

| Fails | Passes |
|-------|--------|
| "Configure the stream endpoint" | "Set `RTMP_INGEST=rtmp://ingest.example.com:1935/live` in `encoder/.env`" |
| "Use the appropriate codec" | "Encode with H.264 Main Profile, 1080p@30fps, 4500kbps CBR" |
| "Handle transcoding errors" | "On transcode failure: retry 3x, then emit `stream.failed` with `{streamId, error, timestamp}`" |

## Overview

Spec produces a GitHub Issue with:
- **Requirements (R)** — what the solution must achieve
- **Approach (A)** — how we'll build it (parts + mechanisms)
- **Criteria (C)** — testable verification conditions
- **Coverage (R x A x C)** — proof chain: approach implements requirements, criteria verify them
- **Loop Contract** — test command, max iterations, verification commands

## File Operations

```
Glob(".mill/spec/drafts/*.md")              # list drafts
Read(".mill/spec/drafts/{slug}.md")         # get draft
Write(".mill/spec/drafts/{slug}.md", ...)   # create/update
Glob(".mill/ground/**/*.md")                # load ground knowledge
Read(".mill/context.md")                    # check context freshness
```

Spec templates by type:
- [templates/feature.md](templates/feature.md), [templates/bug.md](templates/bug.md), [templates/task.md](templates/task.md), [templates/security.md](templates/security.md)

## Workflow

### 0. Check Existing Drafts

`Glob(".mill/spec/drafts/*.md")` — read each for title and last-modified.

If drafts exist, ask via AskUserQuestion: resume a draft or start new.

### 1. Ensure Context

**Check observations** — `Glob(".mill/observations/*.md")`. If any exist, report: "{N} observations in the learning inbox — consider `/mill:ground` before drafting." Continue without blocking.

**Check context freshness:**
1. Read `.mill/context.md` — extract hash
2. `git rev-parse HEAD` — compare
3. If missing or stale → run `/mill:warmup`

Load ground knowledge: `Glob(".mill/ground/**/*.md")` → read key files. Search codebase for relevant files.

### 2. Classify Intent

| Type | Signals | Label |
|------|---------|-------|
| Security | risk, vulnerability, threat | `security` |
| Bug | broken, error, wrong | `bug` |
| Feature | add, create, new | `feature` |
| Task | refactor, update, migrate | `task` |

### 2b. Classify Domain

Ask via AskUserQuestion:

| Domain | Focus |
|--------|-------|
| `backend` | APIs, services, data layer |
| `application` | Interactive apps (web, mobile, desktop) |
| `website` | Pages (landing, marketing, content) |
| `platform` | Infrastructure, containers, CI/CD |
| `fullstack` | Multiple layers |

### 3. Create Draft Early

Write to `.mill/spec/drafts/{slug}.md`:

```markdown
---
title: Human Readable Title
type: feature
domain: application
status: draft
approach: A
---

## Problem
{What pain exists, why this matters}

## Requirements
| ID | Requirement | Status |
|----|-------------|--------|
| R1 | {core goal} | core |
| R2 | {must-have constraint} | must-have |

## Approach A: {short title}
| Part | Mechanism | Flag |
|------|-----------|:----:|
| A1 | {what we build/change} | |
| A2 | {another mechanism} | ⚠️ |

## Criteria
| ID | Condition |
|----|-----------|
| C1 | {testable condition for R1} |
| C2 | {testable condition for R2} |

## Coverage (R x A x C)
| Req | Requirement | Approach | Criteria |
|-----|-------------|----------|----------|
| R1 | {description} | A1 | C1 |
| R2 | {description} | A2 | C2 |

## Scope
**In:** {included}
**Out:** {excluded}

## Verification
{commands, manual checks}

## Loop Contract
**Test Command:** `npm test`
**Max Iterations:** 5
**Verification Commands:** {additional checks}
```

### 4. Elicit Requirements

One question at a time via AskUserQuestion:
1. Problem — what pain exists?
2. Core goal — what must this solve?
3. Constraints — what rules apply?
4. Nice-to-haves — what's bonus?

Add each to table with status (`core`, `must-have`, `nice-to-have`, `out`).

### 4b. Define Approach

1. Each part = mechanism (what we build/change)
2. Flag unknowns with ⚠️
3. Investigate ⚠️ before proceeding

### 4c. Coverage Check

Build R x A x C table:
- Approach column: parts (A1, A2) or ❌ if not covered
- Criteria column: criteria (C1, C2) or — if no verification

All `core` and `must-have` requirements need both approach parts and criteria.

### 5. Validate

**Principles:** Self-Containment, Language Independence, Decision Completeness, Explicit Non-Applicability — all must pass.

**Structure:**
- All core/must-have requirements have approach parts (no ❌)
- All core/must-have requirements have criteria (no —)
- No ⚠️ flags remain
- All criteria are testable conditions
- Scope clear (in/out)
- Loop Contract present with concrete values

Set `status: ready` only when all checks pass.

### 6. Challenge (complex specs)

Probe for gaps: assumptions, edge cases (empty input, concurrency), failure modes, integration conflicts.

### 7. Confirm and Publish

**Open the draft for review** — specs are always long enough to benefit from rendered markdown:
- Windows: `start .mill/spec/drafts/{slug}.md`
- macOS: `open .mill/spec/drafts/{slug}.md`
- Linux: `xdg-open .mill/spec/drafts/{slug}.md`

Present a brief validation summary inline (principles passed, coverage completeness). Then ask via AskUserQuestion: "Spec is open in your editor — create GitHub issue?" — Yes / Needs changes.

**Wait for explicit approval.**

### 8. Publish

1. Read draft, extract frontmatter
2. Write body to `.mill/.prompt`
3. `gh issue create --title "{title}" --body-file .mill/.prompt --label "spec,{type},{domain}"`
4. Parse issue URL
5. `rm .mill/spec/drafts/{slug}.md && rm .mill/.prompt`
6. Report issue URL

## Observations

During drafting, notice ground truth gaps:
- Unknown personas mentioned
- New domain terms not in vocabulary
- Requirements conflicting with rules
- Features implying new entities not in schema

### High-confidence gaps (auto-write)

Write immediately:
- Path: `.mill/observations/spec-{slug}-{gap}.md`
- Frontmatter: `source: spec`, `type: discovery`, `created: {date}`

```markdown
---
source: spec
type: discovery
created: 2025-02-08
---

# Unknown Persona: "Finance Admin"

Referenced during spec drafting for "Export Monthly Reports."
Current personas: developer, admin, operator.
Suggested: add to ground/personas/ or clarify if alias.
```

### Uncertain gaps (collect and ask)

Collect during drafting, ask at end via AskUserQuestion (multiSelect): which gaps to write as observations.

## Rules

**Process:**
1. Always use AskUserQuestion — never raw text prompts
2. Create draft early, update often
3. Security always wins classification
4. Scope creep → separate spec
5. Never publish without explicit approval
6. GitHub is source of truth — no local specs after publish
7. Write observations for ground truth gaps

**Quality (non-negotiable):**
8. Self-Containment — if reader must ask, spec has failed
9. Language Independence — WHAT/WHY universal, HOW marked language-specific
10. Decision Completeness — no TBD, no placeholders, all values concrete
11. Explicit Non-Applicability — state `N/A: {reason}`, never silently omit
12. Binary State — `status: draft` until all principles pass, then `status: ready`
