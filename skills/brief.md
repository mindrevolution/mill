---
description: Capture and develop ideas with 30-day lifecycle
allowed-tools:
  - Read
  - Write
  - Bash(mill brief*, mill ground list)
model: sonnet
argument-hint: "[title] - start capturing a new idea"
---

# Brief

Capture ideas with intent. Briefs have a 30-day lifecycle — develop them into specs or drop them with learned essence.

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
# List active briefs
mill brief list --human

# Get brief details
mill brief get my-idea --human

# Create new brief
mill brief create "My Idea" "Enable users to X so they can Y" --human

# Drop with learned essence
mill brief drop my-idea "Users don't actually need X because Z" --human

# View dropped briefs (learnings)
mill brief dropped --human
```

## Workflow

### 1. Capture the Spark

Ask the user:
- What's the idea?
- What's the intent (why does this matter)?
- Who is this for?

```bash
mill brief create "Feature Name" "Intent statement"
```

### 2. Develop the Brief

Help the user flesh out the idea:
- What problem does this solve?
- What would success look like?
- What's definitely in/out of scope?
- What questions need answers?

Update the brief file at `.mill/brief/active/{slug}.md`:

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

When the brief is ready to become a spec:
- All open questions resolved
- Intent is clear
- Scope is defined

Promote to draft:
1. Move content to `.mill/shape/drafts/{slug}.md`
2. Delete the brief
3. Continue with `/mill:shape` to complete the spec

### 4. Or Drop with Essence

If the idea isn't worth pursuing:
- Capture what was learned
- The essence goes into `dropped.json` for team knowledge

```bash
mill brief drop my-idea "Learned that users prefer Y instead"
```

## Integration

- Briefs reference **personas** from ground
- Briefs use **concepts** from ground
- Ready briefs become **drafts** in shape
- Dropped essences inform future decisions

## Rules

1. One idea per brief
2. Intent is mandatory — why does this matter?
3. 30-day soft limit — develop or drop
4. Dropped briefs capture learning, not just deletion
5. Briefs are local (gitignored) — they're personal WIP
