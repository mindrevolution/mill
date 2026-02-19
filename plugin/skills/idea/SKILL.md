---
description: "Capture a rough idea (30-day lifecycle) • https://mill.mindrevolution.com/idea"
disable-model-invocation: true
allowed-tools: Read, Write, Glob, Grep, Bash(*rm *)
argument-hint: "[title] - start capturing a new idea"
---

# Idea

Capture ideas with intent. 30-day lifecycle — develop into specs or drop with learned essence.

## Interaction Pattern

**Always use AskUserQuestion** — 2-4 options plus free text. Before each question, share a brief perspective: what you think, what you noticed, what you'd recommend. Design conversation, not survey.

## Lifecycle

```
spark → develop → ready → [promote to draft | drop with essence]
```

| Stage | Description |
|-------|-------------|
| **spark** | Quick capture — intent + type |
| **develop** | Orient in codebase, 3-5 dialogue rounds, decisions crystallized |
| **ready** | Scope clear, approach sketched, decisions documented |

## File Operations

```
Glob(".mill/idea/active/*.md")              # list active
Read(".mill/idea/active/{slug}.md")         # get idea
Write(".mill/idea/active/{slug}.md", ...)   # create/update
Read+Write ".mill/idea/dropped.json"        # drop log
```

## Entry

1. `Glob(".mill/idea/active/*.md")`
2. Route:

| Condition | Action |
|-----------|--------|
| Argument provided | Capture the Spark (step 1) |
| Active ideas exist | List (title, stage, age, intent), ask: New / Develop / Promote / Drop |
| No ideas, no argument | Ask what to capture (step 1) |

If user picks an existing idea, read it and route to the step matching its stage.

---

## Workflow

### 1. Capture the Spark

Quick capture — get the intent down and stop.

Ask via AskUserQuestion:
1. "What type?" — New feature / Improvement / Exploration
2. "What's the core idea?" — free text
3. "Why does this matter?" — options from loaded personas if available, plus free text

Generate slug (lowercase, hyphens). Write to `.mill/idea/active/{slug}.md`:

```markdown
---
title: Feature Name
stage: spark
intent: Enable users to X so they can Y
created: {ISO_DATE}
---
```

### 2. Develop the Idea

Three sub-steps: orient, dialogue, update.

#### 2a. Orient

Targeted codebase exploration before asking anything. `Glob` and `Grep` for relevant files, modules, patterns. Read key files. Share findings: which code matters, what patterns exist, constraints discovered.

Not a full scan — focused on this idea.

#### 2b. Dialogue (3-5 rounds)

Each round: share perspective first, then ask 1-3 questions with codebase-informed options.

Topics (adapt order to priority):
- **Scope** — in/out
- **Approach** — main building blocks
- **Decisions** — key design choices and alternatives
- **Trade-offs** — optimizing for what, accepting what

If answers reveal new relevant code, explore before next round. After 3 rounds, assess if more needed. Wrap by round 5.

#### 2c. Update

Read existing idea file, write back with everything captured:

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

## Scope
**In:** what this covers
**Out:** what this explicitly does not cover

## Approach
High-level building blocks. Include relevant file paths.

## Decisions
| Decision | Choice | Rationale |
|----------|--------|-----------|
| what | choice | why |

## Trade-offs
- Optimizing for X at the cost of Y

## Open Questions
- Anything unresolved
```

### 3. Ready to Promote

Ready when: scope defined, approach sketched with file paths, decisions documented, no blocking open questions.

1. Read `.mill/idea/active/{slug}.md`
2. Transform to spec draft format → Write to `.mill/spec/drafts/{slug}.md`
3. `rm .mill/idea/active/{slug}.md`
4. Continue with `/mill:spec`

### 4. Drop with Essence

Capture what was learned, don't just delete.

1. Read idea file
2. Read `.mill/idea/dropped.json` (or start with `[]`)
3. Append: `{ "slug", "title", "essence": "Learned that...", "dropped": "{ISO_DATE}" }`
4. Write updated `dropped.json`
5. `rm .mill/idea/active/{slug}.md`

## Rules

1. One idea per file
2. Intent is mandatory
3. 30-day soft limit — develop or drop
4. Dropped ideas capture learning, not just deletion
5. Ideas are local (gitignored)
6. Perspective before questions — never just survey
