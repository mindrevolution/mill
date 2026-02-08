---
description: Transform ideas into complete, loop-ready specifications
allowed-tools: Read, Write, Glob, Grep, Bash(mill *, git *)
argument-hint: "[intent] - what you want to build, or [draft-slug] to resume"
---

# Spec

Transform user intent into a complete, loop-ready specification.

## Interaction Pattern

**Always use the AskUserQuestion tool** — never ask questions as raw text. Present 2-4 options plus free text ("Other"). One question per tool call.

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

Never output raw numbered lists and ask "pick one" — use the tool.

## Principles

A specification that requires clarifying questions is a draft, not a specification.

| Principle | Definition |
|-----------|------------|
| **Self-Containment** | ∀ statement σ ∈ spec: an implementer unfamiliar with the system can execute σ without querying the author. Test: "Could someone unfamiliar execute this without asking me anything?" |
| **Language Independence** | Specs describe WHAT and WHY at an abstraction level invariant under language transformation. Implementation hints are explicitly marked as language-specific. |
| **Decision Completeness** | Every parameter bound to a concrete value. No TBD. For conditionals: `default UNLESS predicate → alternative`. |
| **Explicit Non-Applicability** | Silent omission conflates "rejected" with "overlooked." When N/A, state: `N/A: {reason}`. |
| **Binary State** | Document is either **draft** (requires clarification) or **ready** (self-contained). No intermediate states. |

### Clarifying Question Failures

If a reader asks any of these, the spec has failed:

| ❌ Fails | ✅ Passes |
|----------|-----------|
| "Configure the stream endpoint" | "Set `RTMP_INGEST=rtmp://ingest.example.com:1935/live` in `encoder/.env`" |
| "Use the appropriate codec" | "Encode with H.264 Main Profile, 1080p@30fps, 4500kbps CBR" |
| "The API returns video metadata" | "The API returns `{streamId, resolution, bitrate, codec, status, viewerCount}`" |
| "Handle transcoding errors" | "On transcode failure: retry 3×, then emit `stream.failed` event with `{streamId, error, timestamp}`" |
| "Update the content status" | "Set `content.status = 'published'` and `content.publishedAt = NOW()` in `cms_entries` table" |

## Overview

Spec takes an idea and produces a GitHub Issue with:
- **Requirements (R)** — what the solution must achieve
- **Approach (A)** — how we'll build it (parts + mechanisms)
- **Criteria (C)** — testable verification conditions
- **Coverage (R × A × C)** — proof that approach implements requirements and criteria verify them
- **Loop Contract** — test command, stop conditions

## Commands

```bash
# List drafts
mill draft list --human

# Get draft content
mill draft get my-feature --human

# Validate draft (check for issues)
mill draft validate my-feature --human

# Publish to GitHub
mill draft publish my-feature --human
```

## Workflow

### 1. Understand Context

Read project context:
```bash
mill context --human
mill ground list --human
```

Search codebase for relevant files.

### 2. Classify Intent

| Type | Signals | Label |
|------|---------|-------|
| Security | risk, vulnerability, threat | `security` |
| Bug | broken, error, wrong | `bug` |
| Feature | add, create, new | `feature` |
| Task | refactor, update, migrate | `task` |

### 2b. Classify Domain

| Domain | Focus |
|--------|-------|
| `backend` | APIs, services, data layer |
| `application` | Interactive apps (web, mobile, desktop) |
| `website` | Pages (landing, marketing, content) |
| `platform` | Infrastructure, containers, CI/CD |
| `fullstack` | Multiple layers |

```yaml
AskUserQuestion:
  question: "What part of the system does this affect?"
  header: "Domain"
  options:
    - label: "Backend"
      description: "APIs, services, data layer"
    - label: "Application"
      description: "Interactive apps — web, mobile, desktop"
    - label: "Website"
      description: "Pages — landing, marketing, content"
    - label: "Platform"
      description: "Infrastructure, containers, CI/CD, scripts"
    - label: "Full-stack"
      description: "Touches multiple layers"
```

### 3. Create Draft Early

Create draft file at `.mill/spec/drafts/{slug}.md`:

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

## Coverage (R × A × C)
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
**Test command:** `npm test`
**Stop conditions:** 20 iterations max
```

### 4. Elicit Requirements

**Use AskUserQuestion** for each layer. One question at a time:
1. Problem — what pain exists?
2. Core goal — what must this solve?
3. Constraints — what rules apply?
4. Nice-to-haves — what's bonus?

Add each requirement to the table with status (`core`, `must-have`, `nice-to-have`, `out`).

### 4b. Define Approach

Once requirements are clear, define approach:
1. Each part = mechanism (what we build/change)
2. Flag unknowns with ⚠️
3. Investigate ⚠️ before proceeding

### 4c. Coverage Check

Build coverage table (R × A × C):
- Approach column: list parts (A1, A2) or ❌ if not covered
- Criteria column: list criteria (C1, C2) or — if no verification

All `core` and `must-have` requirements need both approach parts and criteria.

### 5. Validate

**Principles check:**
- [ ] Self-Containment — no statement requires clarification to execute
- [ ] Language Independence — describes WHAT/WHY, not language-specific HOW
- [ ] Decision Completeness — no TBD, no "it depends," all values concrete
- [ ] Explicit Non-Applicability — omitted sections marked `N/A: {reason}`

**Structure check:**
- [ ] All core/must-have requirements have approach parts (no ❌)
- [ ] All core/must-have requirements have criteria (no —)
- [ ] No ⚠️ flags remain in approach
- [ ] All criteria are testable conditions
- [ ] Scope clear (in/out)
- [ ] Loop Contract present

**Status:** Set `status: ready` only when all checks pass. Otherwise remains `status: draft`.

### 6. Challenge (for complex specs)

Probe for gaps:
- Assumptions — what if X isn't available?
- Edge cases — empty input? concurrent access?
- Failure modes — what if service is down?
- Integration — conflicts with existing features?

### 7. Confirm and Publish

Present the spec, show validation summary, then **use AskUserQuestion** for approval:

```yaml
AskUserQuestion:
  question: "Spec ready. Create GitHub issue?"
  header: "Approve"
  options:
    - label: "Yes, create issue"
      description: "Publish spec to GitHub Issues"
    - label: "No, needs changes"
      description: "I'll provide feedback"
```

**Wait for explicit approval before publishing.**

### 8. Publish

```bash
mill draft publish my-feature --human
```

This creates a GitHub issue and deletes the draft.

## Templates

Get spec templates:
```bash
mill template list specs --human
mill template get specs feature --human
```

## Observations

During drafting, notice gaps in ground truth using LLM judgment:
- Unknown personas mentioned (e.g., "finance admin" not in ground/personas/)
- New domain terms used (not in ground/vocabulary/)
- Requirements conflicting with ground/rules/
- Features implying new entities (not in ground/schema/)

### High-confidence gaps (auto-write)

When clearly a gap (e.g., "As a finance admin..." with no matching persona):

1. Write observation immediately using the Write tool:
   - Path: `.mill/observations/spec-{slug}-{gap}.md`
   - Include frontmatter with source, type, created
   - Describe what was discovered

2. Continue drafting without interruption

Example observation:

```markdown
---
source: spec
type: discovery
created: 2025-02-08
---

# Unknown Persona: "Finance Admin"

User referenced "finance admin" during spec drafting.

## Context

- Spec: Export Monthly Reports
- Quote: "The finance admin should be able to export monthly reports"

## Current Personas

- developer
- admin
- operator

## Suggested Action

Add persona to ground/personas/ or clarify if alias for existing.
```

### Uncertain gaps (collect and ask)

When unsure if it's a gap, collect during drafting and ask at the end:

```yaml
# At end of spec drafting, before final confirmation:
AskUserQuestion:
  question: "Found potential gaps in ground truth. Note any for review?"
  header: "Observations"
  multiSelect: true
  options:
    - label: "'Finance Admin' — possible persona"
      description: "Not found in ground/personas/"
    - label: "'Invoice' — domain term"
      description: "Not defined in ground/vocabulary/"
```

For selected items, write observation files. For unselected, ignore.

## Rules

**Process:**
1. One question at a time — always use AskUserQuestion tool, never raw text prompts
2. Create draft early, update often
3. Security always wins classification
4. Scope creep → separate spec
5. Never publish without explicit approval
6. GitHub is source of truth — no local spec files after publish
7. Write observations for ground truth gaps — don't interrupt flow

**Quality (non-negotiable):**
8. Self-Containment — if reader must ask, spec has failed
9. Language Independence — WHAT/WHY universal, HOW marked as language-specific
10. Decision Completeness — no TBD, no placeholders, all values concrete
11. Explicit Non-Applicability — state `N/A: {reason}`, never silently omit
12. Binary State — `status: draft` until all principles pass, then `status: ready`
