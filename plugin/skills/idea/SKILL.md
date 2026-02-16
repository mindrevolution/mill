---
description: "Capture a rough idea (30-day lifecycle) • https://mill.mindrevolution.com/idea"
disable-model-invocation: true
allowed-tools: Read, Write, Glob, Grep, Bash(*rm *)
argument-hint: "[title] - start capturing a new idea"
---

# Idea

Capture ideas with intent. Ideas have a 30-day lifecycle — develop them into specs or drop them with learned essence.

**IMPORTANT: `mill` is NOT a CLI tool. Never run `mill` as a shell command. All operations use Claude Code's tools directly.**

## Interaction Pattern

**Always use the AskUserQuestion tool** for gathering information. Present 2-4 options plus free text. Before each question round, share a brief perspective — what you think, what you noticed, what you'd recommend. This is a design conversation, not a survey.

## Lifecycle

```
spark → develop → ready → [promote to draft | drop with essence]
```

| Stage | Description |
|-------|-------------|
| **spark** | Quick capture — intent + type, nothing more |
| **develop** | Orient in codebase, 3-5 dialogue rounds, decisions crystallized |
| **ready** | Scope clear, approach sketched, decisions documented |

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

## Entry

Every invocation starts here. Check what exists and route:

1. `Glob(".mill/idea/active/*.md")` — check for existing ideas
2. If the user provided a title/description as an argument → **Capture the Spark** (step 1)
3. If no argument given, read all active idea files and display them:

```
Active Ideas:
  - auth-token-rotation (spark, 3 days) — "Enable seamless token refresh"
  - batch-export (developing, 12 days) — "Let users export data in bulk"
```

Show title, stage, age (days since created), and intent for each. Then ask:

```
AskUserQuestion:
  question: "What would you like to do?"
  header: "Action"
  options:
    - label: "New idea"
      description: "Capture a fresh spark"
    - label: "Develop"
      description: "Continue working on an existing idea"
    - label: "Promote"
      description: "Move a ready idea to spec draft"
    - label: "Drop"
      description: "Drop an idea, keep the essence"
```

If they pick an existing idea, read it and route to the appropriate step based on its stage.

4. If no active ideas and no argument → ask what they want to capture (step 1).

---

## Workflow

### 1. Capture the Spark

Quick capture — get the intent down and stop. Don't over-discuss.

**Use the AskUserQuestion tool:**

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

Generate a slug from the title (lowercase, hyphens, no special chars) and write:

```markdown
---
title: Feature Name
stage: spark
intent: Enable users to X so they can Y
created: {ISO_DATE}
---
```

To `.mill/idea/active/{slug}.md`. Done — spark captured.

### 2. Develop the Idea

When the user returns to develop a spark, run three sub-steps: orient, dialogue, update.

#### 2a. Orient

Targeted codebase exploration before asking anything. Use `Glob` and `Grep` to find files, modules, and patterns relevant to the idea. Read key files to understand existing structure. Do NOT do a full scan — focus on what's relevant to this idea.

Share what you found: which parts of the codebase matter, what patterns exist, any constraints.

#### 2b. Dialogue (3-5 rounds)

Each round: share your perspective first, then ask 1-3 questions via AskUserQuestion with options informed by the codebase. Cover these topics (adapt order to what matters most):

- **Scope** — What's in, what's out?
- **Approach** — What are the main building blocks?
- **Decisions** — What are the key design choices? What are the alternatives?
- **Trade-offs** — What are we optimizing for? What are we accepting?

Guidelines:
- Options should be concrete and derived from what you found in the codebase and prior rounds.
- If answers reveal new relevant code, explore it before the next round.
- Move on when a topic is clear. Don't over-discuss.
- After 3 rounds, assess if more are needed. Wrap up by round 5.

#### 2c. Update

Read the existing idea file and write back with everything captured:

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

High-level building blocks and how they fit together. Include relevant file paths.

## Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| what was decided | the choice | why |

## Trade-offs

- Optimizing for X at the cost of Y

## Open Questions

- Anything unresolved
```

### 3. Ready to Promote

An idea is ready when:
- Scope is defined (in and out)
- Approach is sketched with file paths
- Key decisions are documented with rationale
- No blocking open questions remain

Promote to draft:
1. Read the idea file from `.mill/idea/active/{slug}.md`
2. Transform content into spec draft format
3. Write to `.mill/spec/drafts/{slug}.md`
4. Delete the idea: `rm .mill/idea/active/{slug}.md`
5. Continue with `/mill:spec` to complete the spec

### 4. Or Drop with Essence

If the idea isn't worth pursuing — capture what was learned.

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
- Ready ideas become **drafts** in spec (carrying scope, approach, decisions)
- Dropped essences inform future decisions

## Rules

1. One idea per file
2. Intent is mandatory — why does this matter?
3. 30-day soft limit — develop or drop
4. Dropped ideas capture learning, not just deletion
5. Ideas are local (gitignored) — they're personal WIP
6. Perspective before questions — never just survey
