---
description: Capture and develop ideas with 30-day lifecycle
allowed-tools: Read, Write, Bash(mill idea*, mill ground list)
argument-hint: "[title] - start capturing a new idea"
---

# Idea

Capture ideas with intent. Ideas have a 30-day lifecycle — develop them into specs or drop them with learned essence.

## Interaction Pattern

**Always use the AskUserQuestion tool** for gathering information. Present 2-4 options plus free text. One question at a time — don't overwhelm.

Example:
```
AskUserQuestion:
  question: "What type of idea is this?"
  header: "Idea type"
  options:
    - label: "New feature"
      description: "Something users can't do today"
    - label: "Improvement"
      description: "Make existing behavior better"
    - label: "Exploration"
      description: "Not sure yet, need to think it through"
```

## Lifecycle

```
spark → developing → ready → [promote to draft | drop with essence]
```

| Stage | Description |
|-------|-------------|
| **spark** | Initial idea, minimal detail |
| **developing** | Adding context, exploring scope |
| **ready** | Clear enough to become a spec |

## Commands

```bash
# List active ideas
mill idea list --human

# Get idea details
mill idea get my-idea --human

# Create new idea
mill idea create "My Idea" "Enable users to X so they can Y" --human

# Drop with learned essence
mill idea drop my-idea "Users don't actually need X because Z" --human

# View dropped ideas (learnings)
mill idea dropped --human
```

## Workflow

### 1. Capture the Spark

**Use the AskUserQuestion tool** to gather the idea. Present options where sensible:

```
Question 1: "What type of idea is this?"
Options:
  - "New feature" — something users can't do today
  - "Improvement" — make existing behavior better
  - "Exploration" — not sure yet, need to think it through

Question 2: "What's the core idea?" (free text — user types)

Question 3: "Why does this matter?"
Options based on loaded personas (if available):
  - "[Persona A] needs this because [pain point]"
  - "[Persona B] would benefit by [outcome]"
  - "Different reason..." (free text)
```

After gathering responses:
```bash
mill idea create "Feature Name" "Intent statement"
```

### 2. Develop the Idea

**Use AskUserQuestion** to flesh out the idea layer by layer:

```
Question: "What problem does this solve?"
Options:
  - "Users can't do X" — missing capability
  - "X is too slow/hard" — friction
  - "X breaks when Y" — reliability
  - (free text)

Question: "What would success look like?"
Options:
  - "Users can X without Y"
  - "X takes <N seconds/clicks"
  - (free text)

Question: "What's the scope?"
Options (multiSelect: true):
  - "Just the happy path for now"
  - "Include error handling"
  - "Include edge cases"
  - "Full production-ready"
```

Update the idea file at `.mill/idea/active/{slug}.md`:

```markdown
---
title: Feature Name
stage: developing
intent: Enable users to X so they can Y
persona: primary-user
concepts:
  - key-concept
---

## Notes

{Captured thoughts, questions, explorations}

## Open Questions

- Question 1?
- Question 2?
```

### 3. Ready to Promote

When the idea is ready to become a spec:
- All open questions resolved
- Intent is clear
- Scope is defined

Promote to draft:
1. Move content to `.mill/spec/drafts/{slug}.md`
2. Delete the idea
3. Continue with `/mill:spec` to complete the spec

### 4. Or Drop with Essence

If the idea isn't worth pursuing:
- Capture what was learned
- The essence goes into `dropped.json` for team knowledge

```bash
mill idea drop my-idea "Learned that users prefer Y instead"
```

## Integration

- Ideas reference **personas** from ground
- Ideas use **concepts** from ground
- Ready ideas become **drafts** in spec
- Dropped essences inform future decisions

## Rules

1. One idea per file
2. Intent is mandatory — why does this matter?
3. 30-day soft limit — develop or drop
4. Dropped ideas capture learning, not just deletion
5. Ideas are local (gitignored) — they're personal WIP
