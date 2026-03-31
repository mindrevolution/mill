---
description: "Define who you build for and how • https://mill.mindrevolution.com/ground"
disable-model-invocation: true
allowed-tools: Read, Write, Glob, Grep, Bash(*rm *, *git *, *start *, *open *, *xdg-open *)
argument-hint: "[category] - personas, rules, decisions, vocabulary, or review"
---

# Ground

Build and manage product knowledge in `.mill/ground/`. Review observations from the learning inbox.

## Interaction Pattern

**Always use AskUserQuestion** — 2-4 options plus free text. One question at a time.

## Entry Point

Check observations first: `Glob(".mill/observations/*.md")` — read each for title and count.

Ask via AskUserQuestion: Review observations ({N} pending) / Create knowledge / Sync codebase.

---

## Observation Review Flow

### 1. List Observations

`Glob(".mill/observations/*.md")` → read each for title and type from frontmatter.

### 2. For Each Observation

Read full observation, then **present content before asking for a decision**:

- **≤25 lines:** Print the full observation content inline in the terminal so the user can read it without leaving the conversation.
- **>25 lines:** Ask the user via AskUserQuestion: "This observation is {N} lines — open in your editor, or print here anyway?" If the user chooses to open, run the platform-appropriate command:
  - Windows: `start {filepath}`
  - macOS: `open {filepath}`
  - Linux: `xdg-open {filepath}`

After the user has seen the content, ask via AskUserQuestion:
- **Curate to ground** — add to ground truth
- **Create spec** — write to idea/active/
- **Add to debt** — track in ground/debt/
- **Dismiss** — delete, not actionable

### 3. Handle Each Action

**Curate to ground:**
1. Infer target folder:
   - `type: discovery` + persona → `ground/personas/`
   - `type: discovery` + term → `ground/vocabulary/`
   - `type: extraction` + deps → `ground/stack/`
   - `type: concern` → often `ground/debt/`
   - `type: discovery` + decision → `ground/decisions/`
   - `type: learning` + file couplings/architecture → `ground/patterns/`
   - `type: learning` + conventions/commands/config → `ground/rules/`
   - If observation has `suggested:` frontmatter (from ship auto-tag), use as routing hint but confirm with user
2. Ask clarifying questions to gather details
3. `Write(".mill/ground/{category}/{id}.md", content)`
4. `rm .mill/observations/{id}.md`

**Create spec:**
1. Transform to idea → `Write(".mill/idea/active/{slug}.md", ...)` with frontmatter: title, stage: spark, intent, created
2. `rm .mill/observations/{id}.md`

**Add to debt:**
1. Read existing `ground/debt/{topic}.md`, append, Write back
2. `rm .mill/observations/{id}.md`

**Dismiss:**
1. `rm .mill/observations/{id}.md`

### 4. Continue

`Glob(".mill/observations/*.md")` — if more exist, continue. Otherwise exit or offer other actions.

---

## Categories

| Category | Purpose |
|----------|---------|
| **strategic** | Vision, mission, goals |
| **personas** | Who you build for |
| **rules** | Constraints and conventions |
| **decisions** | Architectural decisions (why X over Y) |
| **vocabulary** | Domain terminology |
| **stack** | Technology stack |
| **schema** | Data structures and relationships |
| **design** | Visual language (colors, typography) |
| **patterns** | Code patterns and idioms |
| **debt** | Known issues, future work |

## Create Knowledge Flow

When creating new knowledge (not from observation):

### 1. Ask Category

Ask via AskUserQuestion: Personas / Rules / Decisions / Vocabulary (extend to other categories as needed).

### 2. Elicit Details

Ask category-appropriate questions via AskUserQuestion:

| Category | Elicit |
|----------|--------|
| **Personas** | User type (end user/admin/operator), job-to-be-done, pain points, triggers |
| **Rules** | Kind (tech policy/quality bar/convention), specifics |
| **Decisions** | What was decided (technology/architecture/process), choice, rationale, alternatives rejected |
| **Vocabulary** | Term, definition, context, related terms |

### 3. Write Knowledge Files

Write to `.mill/ground/{category}/{id}.md` with frontmatter (`category`, `id`) and structured content.

### 4. Verify

`Glob(".mill/ground/**/*.md")` — confirm written correctly.

## Integration with Specs

- Personas → user stories reference real users
- Rules → acceptance criteria align with quality bars
- Vocabulary → specs use consistent terminology
- Design → UI specs reference design tokens

## Kickstart (New Projects)

Ask via AskUserQuestion: product type (SaaS / API-Platform / Marketing site / Internal tool). Then elicit stack, conventions, key personas. Create initial ground files from conversation.
