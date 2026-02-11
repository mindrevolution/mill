---
description: "Capture a rough idea (30-day lifecycle) • https://mill.mindrevolution.com/idea"
disable-model-invocation: true
allowed-tools: Read, Write, Glob, Bash(*rm *)
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

## File Operations

```
# List active ideas
Glob(".mill/idea/active/*.md") → Read each for frontmatter

# Get idea details
Read(".mill/idea/active/{slug}.md")

# Create new idea
Write(".mill/idea/active/{slug}.md", content)

# Drop idea
Read idea → Bash(rm .mill/idea/active/{slug}.md) → Read+append+Write .mill/idea/dropped.json

# View dropped ideas
Read(".mill/idea/dropped.json")
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

After gathering responses, generate a slug from the title (lowercase, hyphens, no special chars) and write:

```
Write(".mill/idea/active/{slug}.md", content)
```

With frontmatter:
```markdown
---
title: Feature Name
stage: spark
intent: Enable users to X so they can Y
created: {ISO_DATE}
---
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

Read the existing idea file, update it with new content, and Write back:

```markdown
---
title: Feature Name
stage: developing
intent: Enable users to X so they can Y
persona: primary-user
created: {ISO_DATE}
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
1. Read the idea file from `.mill/idea/active/{slug}.md`
2. Transform content into spec draft format
3. Write to `.mill/spec/drafts/{slug}.md`
4. Delete the idea: `rm .mill/idea/active/{slug}.md`
5. Continue with `/mill:spec` to complete the spec

### 4. Or Drop with Essence

If the idea isn't worth pursuing:
- Capture what was learned
- The essence goes into `dropped.json` for team knowledge

1. Read the idea file
2. Read existing `.mill/idea/dropped.json` (or start with `[]` if missing)
3. Append entry:
   ```json
   {
     "slug": "my-idea",
     "title": "My Idea",
     "essence": "Learned that users prefer Y instead",
     "dropped": "{ISO_DATE}"
   }
   ```
4. Write updated `.mill/idea/dropped.json`
5. Delete the idea file: `rm .mill/idea/active/{slug}.md`

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
