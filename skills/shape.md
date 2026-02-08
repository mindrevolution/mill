---
description: Transform ideas into complete, loop-ready specifications
allowed-tools: Read, Write, Glob, Grep, Bash(mill draft*, mill ground list, mill template get specs*, git *)
argument-hint: "[intent] - what you want to build, or [draft-slug] to resume"
---

# Shape

Transform user intent into a complete, loop-ready specification.

## Interaction Pattern

**Always use the AskUserQuestion tool** for elicitation. Present 2-4 options plus free text ("Other"). One question at a time.

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

## Overview

Shape takes an idea and produces a GitHub Issue with:
- **Requirements (R)** — what the solution must achieve
- **Approach (A)** — how we'll build it (with parts)
- **Coverage** — proof that approach satisfies requirements
- **Acceptance Criteria** — testable verification conditions
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

Create draft file at `.mill/shape/drafts/{slug}.md`:

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

## Coverage (R × A)
| Req | A |
|-----|---|
| R1 | ✅ |
| R2 | ✅ |

## Acceptance Criteria
- [ ] {testable condition for R1}
- [ ] {testable condition for R2}

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

Build coverage table (R × A):
- ✅ = covered
- ❌ = not covered (explain why)

All `core` and `must-have` requirements need ✅.

### 5. Validate

Check before publishing:
- [ ] All core/must-have requirements covered (✅)
- [ ] No ⚠️ flags remain in approach
- [ ] Acceptance criteria testable
- [ ] Scope clear (in/out)
- [ ] No placeholders or open questions
- [ ] Loop Contract present

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

## Rules

1. One question at a time
2. Create draft early, update often
3. No placeholders in final spec
4. Security always wins classification
5. Scope creep → separate spec
6. Never publish without explicit approval
7. GitHub is source of truth — no local spec files after publish
