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
  question: "Which persona is this for?"
  header: "Persona"
  options:
    - label: "Admin"
      description: "Back-office management"
    - label: "End user"
      description: "Public-facing experience"
```

All AskUserQuestion calls follow this format: 2-4 options plus free text.

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
- **Coverage (R x A x C)** — proof chain (feature specs only)
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
- [templates/feature.md](templates/feature.md), [templates/bug.md](templates/bug.md), [templates/chore.md](templates/chore.md), [templates/security.md](templates/security.md)

## Workflow

### 1. Check Existing Drafts

`Glob(".mill/spec/drafts/*.md")` — read each for title and last-modified.

If drafts exist, ask via AskUserQuestion: resume a draft or start new.

### 2. Ensure Context

**Check observations** — `Glob(".mill/observations/*.md")`. If any exist, report: "{N} observations in the learning inbox — consider `/mill:ground` before drafting." Continue without blocking.

**Check context freshness:**
1. Read `.mill/context.md` — extract hash
2. `git rev-parse HEAD` — compare
3. If missing or stale → run `/mill:warmup`

Load ground knowledge: `Glob(".mill/ground/**/*.md")` → read key files. Search codebase for relevant files.

### 3. Classify Intent

Infer type from the user's description — don't ask unless ambiguous.

| Type | Signals | Label |
|------|---------|-------|
| Security | risk, vulnerability, threat | `security` |
| Bug | broken, error, wrong | `bug` |
| Feature | add, create, new | `feature` |
| Chore | refactor, update, migrate, cleanup, debt | `chore` |

**Chore vs Feature:** Does the user need to explore alternatives? If the work and approach are known, it's a chore. If there are real design choices, it's a feature — regardless of size.

### 4. Classify Domain

Ask via AskUserQuestion:

| Domain | Focus |
|--------|-------|
| `backend` | APIs, services, data layer |
| `application` | Interactive apps (web, mobile, desktop) |
| `website` | Pages (landing, marketing, content) |
| `platform` | Infrastructure, containers, CI/CD |
| `fullstack` | Multiple layers |

### 5. Create Draft Early

Read `templates/{type}.md` and use it as the draft structure. Write to `.mill/spec/drafts/{slug}.md`.

Feature draft example:

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

## Failure Modes
| Trigger | Detection | Response | Recovery |
|---------|-----------|----------|----------|
| {what goes wrong} | {how we know} | {immediate action} | {return to good state} |

<details>
<summary>Alternatives Considered</summary>

### Approach B: {title}
{tradeoff summary and why it was rejected}

</details>

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

### 6. Elicit Requirements

One question at a time via AskUserQuestion:
1. Problem — what pain exists?
2. Core goal — what must this solve?
3. Constraints — what rules apply?
4. Nice-to-haves — what's bonus?

Add each to table with status (`core`, `must-have`, `nice-to-have`, `out`).

### 7. Forcing Questions (feature only)

Before jumping to approach, ask these via AskUserQuestion to sharpen the problem:

| Question | Purpose |
|----------|---------|
| **Narrowest wedge** — "What's the smallest version of this that still matters?" | Prevents over-scoping. The answer often becomes the actual spec scope. |
| **Demand evidence** — "Who already wants this, and how do they solve it today?" | Grounds the spec in real need. If nobody is working around the absence, question priority. |

If the user's answers reveal the scope should shrink, update requirements accordingly before proceeding. Move deferred scope to **Out** with a note like "future spec."

### 8. Define Approaches (feature only)

**Feature specs require at least 2 approaches** before choosing one:

1. Draft Approach A and Approach B (optionally C)
2. Each approach: parts + mechanisms + brief tradeoff note
3. Present approaches side-by-side with tradeoffs via AskUserQuestion
4. User picks one → that becomes the spec's approach
5. Record rejected approaches in a collapsed `<details>` block under "Alternatives Considered"

```markdown
## Approach A: {title}
| Part | Mechanism | Flag |
|------|-----------|:----:|
| A1 | ... | |

**Tradeoff:** {1-2 sentences: what you gain, what you give up}

## Approach B: {title}
| Part | Mechanism | Flag |
|------|-----------|:----:|
| B1 | ... | |

**Tradeoff:** {1-2 sentences}
```

After selection, the chosen approach stays as `## Approach` and rejected ones collapse:

```markdown
<details>
<summary>Alternatives Considered</summary>

### Approach B: {title}
{tradeoff summary and why it was rejected}

</details>
```

Still flag if a meaningful alternative exists for any type.

6. Flag unknowns with ⚠️ in the chosen approach
7. Investigate ⚠️ before proceeding

### 9. Failure Modes (feature only)

For each approach part that touches error paths, data, or external systems, add a failure mode table:

```markdown
## Failure Modes
| Trigger | Detection | Response | Recovery |
|---------|-----------|----------|----------|
| {what goes wrong} | {how we know} | {immediate action} | {return to good state} |
```

Keep it proportional — a 2-part UI spec needs 0-1 rows. A 6-part backend spec might need 4-5.

### 10. Coverage Check (feature only)

Build R x A x C table:
- Approach column: parts (A1, A2) or ❌ if not covered
- Criteria column: criteria (C1, C2) or — if no verification

All `core` and `must-have` requirements need both approach parts and criteria.

### 11. Validate

**Principles:** Self-Containment, Language Independence, Decision Completeness, Explicit Non-Applicability — all must pass.

**All types:**
- All criteria are testable conditions
- No ⚠️ flags remain
- Scope clear (in/out)
- Loop Contract present with concrete values

**Feature only:**
- All core/must-have requirements have approach parts (no ❌)
- All core/must-have requirements have criteria (no —)
- Coverage matrix complete

Set `status: ready` only when all checks pass.

### 12. Self-Review (scored dimensions)

**Always runs** — not just for complex specs.

Score the draft on 5 dimensions (1-10). Any dimension below 7 must be fixed before proceeding.

| Dimension | Question | Threshold |
|-----------|----------|-----------|
| **Feasibility** | Can this be built with the current stack and codebase? Are there hidden prerequisites? | ≥ 7 |
| **Completeness** | Are failure modes, edge cases, and error paths covered? | ≥ 7 |
| **Scope Discipline** | Is there scope creep? Is this the narrowest wedge? | ≥ 7 |
| **Testability** | Can every criterion be verified mechanically (no "looks right" judgments)? | ≥ 7 |
| **Clarity** | Would an implementer unfamiliar with this codebase understand every statement? | ≥ 7 |

Present scores to user. For any below 7, explain the gap and fix it.

### 13. Independent Spec Review (subagent)

Launch a **spec-review subagent** that receives only:
- The draft spec markdown
- Ground knowledge files (`.mill/ground/**/*.md`)
- The project context (`.mill/context.md`)

The subagent does **not** see the drafting conversation. It reviews against:
1. Self-Containment — flag any statement that requires asking the author
2. Decision Completeness — flag any TBD, vague value, or unbound parameter
3. Coverage gaps — requirements without approach parts or criteria
4. Testability — criteria that can't be mechanically verified
5. Assumption surfacing — implicit assumptions that should be explicit

The subagent returns a structured review:
```markdown
## Spec Review

**Verdict:** Pass | Pass with notes | Needs revision

### Findings
| # | Severity | Section | Finding |
|---|----------|---------|---------|
| 1 | {block/warn/note} | {section} | {what's wrong and suggested fix} |

### Summary
{1-2 sentences}
```

**block** findings must be fixed before publishing. **warn** findings are presented to the user for decision. **note** findings are informational.

If the subagent returns "Needs revision," fix the blocking findings, update the draft, and re-run only the subagent (not the full self-review).

### 14. Confirm and Publish

**Open the draft as a rendered preview** using the [Preview Template](#preview-template):

1. Read `${CLAUDE_PLUGIN_ROOT}/templates/preview.html`
2. Read `.mill/spec/drafts/{slug}.md`
3. Replace `{{TITLE}}` → spec title, `{{CONTEXT}}` → "spec draft", `{{CONTENT}}` → raw markdown
4. Write to `.mill/.preview.html`
5. Open: `start`/`open`/`xdg-open .mill/.preview.html`

Present a brief validation summary inline (principles passed, coverage completeness). Then ask via AskUserQuestion: "Spec is open in your browser — create GitHub issue?" — Yes / Needs changes.

**Wait for explicit approval.**

### 15. Publish

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

## Preview Template

Shared template at `${CLAUDE_PLUGIN_ROOT}/templates/preview.html`. Renders markdown in the browser with GitHub-style CSS, syntax highlighting, and Mermaid diagram support. Replace `{{TITLE}}`, `{{CONTEXT}}`, `{{CONTENT}}` placeholders, write to `.mill/.preview.html`, and open with `start`/`open`/`xdg-open`.
