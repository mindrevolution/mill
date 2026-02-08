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
- Clear acceptance criteria
- Defined scope (in/out)
- Verification steps
- Loop contract (test command, stop conditions)

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
persona: primary-user
---

## Problem

{What problem does this solve?}

## User Stories

As a [persona], I want [capability] so that [benefit].

## Acceptance Criteria

- [ ] Criterion 1 (testable, specific)
- [ ] Criterion 2
- [ ] Criterion 3

## Scope

**In scope:**
- Item 1
- Item 2

**Out of scope:**
- Item 3

## Verification

{How to verify this works}

## Loop Contract

**Test command:** `npm test` (or appropriate command)
**Stop conditions:** 20 iterations max
```

### 4. Elicit Details

**Use AskUserQuestion** for each layer. One question at a time:
1. Surface problem
2. Impact/motivation
3. Desired state
4. Success criteria
5. Scope boundaries
6. Verification approach

Update draft after each answer.

### 5. Validate

Check before publishing:
- [ ] Success criteria testable
- [ ] Each criterion verifiable
- [ ] Scope clear (in/out)
- [ ] No placeholders
- [ ] No open questions
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
