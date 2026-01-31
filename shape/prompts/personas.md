# Personas

Create or update user personas grounded in codebase evidence and human input.

**CRITICAL:** Personas must be evidence-backed. Never invent details not supported by research or user input.

## Purpose

Personas created here will be loaded during `mill spec` to improve elicitation:
- Better questions tailored to specific user types
- User stories grounded in real segments
- Acceptance criteria that reflect actual workflows

Personas are **NOT** loaded during `mill run` — they "compile down" into concrete specs.

## Context
Pre-loaded: `.mill/context.md`, `README.md`, `AGENTS.md`, existing `.mill/personas.md` (if exists).

## Flow

### 1. Check Existing Personas

If `.mill/personas.md` exists, show what's there:

```
found {n} personas:

1. [Name] — [Role]
2. [Name] — [Role]

what would you like to do? (update, add, remove, review, or start fresh)
```

Accept numbered shortcuts or natural language. Examples:
- "2" or "add" → Add Flow
- "update alex" → Update Flow for Alex
- "remove the pm one" → confirm → remove
- "start over" → confirm → Create Flow

If no existing personas, go directly to Create Flow.

---

### Create Flow (new personas from scratch)

#### 1a. Gather Evidence

Collect signals about who uses this product:

```bash
# Fetch recent issues for user intent signals
gh issue list --limit 30 --state all --json title,body,labels
```

Read and analyze:
- `README.md` — intended audience, use cases
- `docs/` or `documentation/` — onboarding, tutorials
- `examples/` — who are examples written for?
- CLI help text or API surface — complexity signals technical level
- Issue titles/bodies — what do users ask for?

Summarize findings:
```
evidence summary:
- product type: [CLI tool / library / service / app]
- domain: [what problem space]
- technical signals: [complexity level, languages, integrations]
- user intent signals: [from issues, docs]
```

#### 1b. Hypothesize Segments

From evidence, propose 2-4 user segments:

```
based on the codebase, i see these user types:

1. [segment] — [evidence]
2. [segment] — [evidence]
3. [segment] — [evidence]

does this look right? (yes / add / remove / change)
```

Accept natural responses. Iterate until confirmed.

#### 1c. Elicit Per Segment

For EACH segment, collect these fields (one at a time, with numbered options):

1. **Role** — job title
2. **Level** — novice / intermediate / expert
3. **Context** — startup / enterprise / solo / team
4. **Job** — what they hire this product to do
5. **Trigger** — when they reach for it
6. **Pain** — current workflow problem
7. **Success** — how they know it worked
8. **Churn risk** — what makes them leave

Example prompt:
```
[segment]: what job are they hiring this product to do?

1. [inferred from codebase]
2. [inferred from issues]
3. [common for domain]
4. other (describe)
```

Keep answers to one line. No narratives.

#### 1d. Generate & Validate

Generate personas using template, then show summary:

```
ready:

- Alex — Platform Engineer (ship verified changes without babysitting CI)
- Sam — Product Lead (prioritize work based on impact)

save? (yes / edit / add another / start over)
```

---

### Update Flow (modify existing persona)

Show current persona, ask what to change:

```
Alex — Platform Engineer:
- job: ship verified changes without babysitting CI
- pain: context-switching between code and CI dashboard
...

what would you like to change?
```

User can say "change the pain point" or "job should be X" — flexible input.

After changes: "save? (yes / change more / discard)"

---

### Add Flow (add new persona to existing set)

Skip evidence gathering (already have context):

```
adding a new persona.

what user segment? (or describe who they are)

1. [gap from existing personas]
2. [suggested from issues]
```

Elicit details, then:
```
new persona:

Jordan — Junior Developer
- job: learn codebase patterns from AI suggestions
...

add this? (yes / edit / cancel)
```

---

## Persona Template

Compact, facts-only. No narrative fluff.

```markdown
## [Name] — [Role]

- **Segment:** [label]
- **Level:** [novice / intermediate / expert]
- **Context:** [startup / enterprise / solo / team]
- **Job:** [what they're trying to accomplish]
- **Trigger:** [when they reach for this product]
- **Pain:** [current workflow problem]
- **Success:** [how they know it worked]
- **Churn risk:** [what makes them leave]
- **Evidence:** [source]
```

Example:
```markdown
## Alex — Platform Engineer

- **Segment:** ops
- **Level:** expert
- **Context:** startup, on-call rotation
- **Job:** ship verified changes without babysitting CI
- **Trigger:** PR ready, needs to pass tests before merge
- **Pain:** context-switching between code and CI dashboard
- **Success:** PR merged, tests green, no manual intervention
- **Churn risk:** flaky tests, slow feedback loops
- **Evidence:** README mentions "bounded loops", issues #12 #18
```

No names like "Sarah the Marketing Manager". No age, hobbies, or stock photos. Just facts that inform specs.

---

## File Format

`.mill/personas.md` structure:

```markdown
<!-- mill-personas: 3 -->
<!-- updated: 2025-01-20T14:30:00Z -->

# Personas

## Alex — Platform Engineer
- **Segment:** ops
- **Level:** expert
...

## Sam — Product Lead
- **Segment:** pm
- **Level:** intermediate
...
```

No intro paragraphs. Just the personas.

---

## Save Flow

After save:
```
✓ saved .mill/personas.md ({n} personas)

anything else? (update / add / done)
```

On "done":
```
done. run `mill spec` to use personas in spec elicitation.
```

---

## Rules

1. **Evidence-first** — every attribute needs a source (codebase, docs, issues, or user input)
2. **Numbered options where helpful** — offer shortcuts, but always accept free-form input
3. **One question at a time** — don't overwhelm
4. **Compact answers** — one line per field, no narratives
5. **No fluff** — no demographics, hobbies, or filler unless product-relevant
6. **2-4 personas max** — warn if adding 5th
7. **Confirm destructive actions** — removing or replacing needs explicit y/n

## Anti-patterns

- "Sarah is a 32-year-old marketing manager who loves coffee" — fluff
- "Alex is passionate about clean code and enjoys..." — narrative filler
- Multi-sentence field values — keep to one line
- Personas without jobs — useless for specs
- Rigid numbered menus only — allow natural language
- More than 4 personas — diminishing returns
- Assumptions without evidence — cite the source
