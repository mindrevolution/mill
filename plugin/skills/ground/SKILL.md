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

Ask via AskUserQuestion: Review observations ({N} pending) / Create knowledge / Verify ground (validate ground truth against code).

---

## Observation Review Flow

### 1. List Observations

`Glob(".mill/observations/*.md")` → read each for title and type from frontmatter.

### 1b. Batch Preview

Before diving into individual decisions, print a numbered summary of all pending observations so the user sees the full picture:

```
Pending observations:

  1. Technology decisions — rationale for MassTransit, Orleans, Kamal (discovery)
  2. Stack details — full NuGet versions and local dev stack (discovery)
  3. Missing vocabulary — 8 terms used but not in glossary (discovery)
```

For observations that contain lists of items (e.g. vocabulary terms, schema entities, stack entries), expand the items inline:

```
  3. Missing vocabulary — 8 terms:
     Grain, Silo, Reminder, Saga, Outbox, Backplane, Sidecar, Tombstone
```

This lets the user see what they're about to review and decide on batch actions ("curate all", "dismiss 2 and 5") before walking through individually.

### 2. For Each Observation

Read full observation, then **present content before asking for a decision**:

- **≤25 lines:** Print the full observation content inline in the terminal so the user can read it without leaving the conversation.
- **>25 lines:** Ask the user via AskUserQuestion: "This observation is {N} lines — open rendered preview, or print here anyway?" If the user chooses to open, use the [Preview Template](#preview-template) to render it in the browser.

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

## Verify Ground Flow

Check that ground truth is still grounded in reality. Auto-fix trivial drift; ask the user for anything requiring judgment.

### 1. Inventory

`Glob(".mill/ground/**/*.md")` → group files by category. Print a summary:

```
Ground truth sync — 23 files across 8 categories:
  stack (5) · vocabulary (6) · decisions (3) · rules (4) · patterns (2) · schema (2) · debt (1)
```

### 2. Category-Specific Validation

Process each category with targeted checks. For every file, read its content, then validate:

| Category | Validation Strategy |
|----------|-------------------|
| **stack** | Grep for each technology/package name in project files, lock files, and configs. Check version claims against lock files. |
| **vocabulary** | Grep each defined term across the codebase. Flag terms with zero references. |
| **decisions** | Check that the chosen option's artifacts exist (packages, files, patterns). Check that rejected alternatives haven't crept in. |
| **rules** | Spot-check stated conventions against actual code. For naming rules, Grep for violations. For structural rules, Glob for counterexamples. |
| **patterns** | Grep/Glob for the described pattern. Flag if the pattern no longer appears or has clearly changed shape. |
| **schema** | Compare described entities/fields against actual model definitions (Grep for class/type/table names). |
| **debt** | Check if the referenced problem still exists. Flag items that appear resolved (file deleted, code changed, test added). |
| **personas** | Skip — personas are business context, not code-verifiable. |
| **strategic** | Skip — vision/mission are not code-verifiable. |
| **design** | Grep for referenced design tokens, color values, font names in stylesheets/configs. Flag missing references. |

### 3. Classify Each Finding

Every discrepancy gets one of two classifications:

**Auto-fix** (apply silently, report after) — only when ALL of these are true:
- The correct value is unambiguous from the codebase (e.g., version `8.0.1` → lock file shows `8.2.0`)
- The change is a single fact update, not a rewrite
- No judgment needed — a machine diff would reach the same conclusion

Examples of auto-fixes:
- Version number in a stack file doesn't match the lock file → update to lock file version
- A term in vocabulary has a minor casing change in the codebase → update the ground file
- A debt item references a file that no longer exists and the fix is noted in git log → mark resolved

**Ask user** — everything else:
- Technology listed in stack but not found in codebase (removed? renamed? optional?)
- Decision's chosen option has no artifacts but rejected alternative does (reversed?)
- Rule appears widely violated (outdated rule? or widespread non-compliance?)
- Vocabulary term has zero hits (removed concept? or just not in code?)
- Pattern described but code uses a different approach now
- Any ambiguity at all

### 4. Execute

Process all files. Collect results into three buckets:

1. **Auto-fixed** — changes already applied
2. **Needs input** — discrepancies requiring user judgment
3. **Verified** — ground truth confirmed accurate

Print the auto-fixed items as a batch summary:

```
Auto-fixed (3):
  ✓ stack/dotnet.md — updated EF Core version 8.0.1 → 8.2.0 (from packages.lock.json)
  ✓ debt/legacy-auth.md — marked resolved (auth/ directory removed in commit abc1234)
  ✓ vocabulary/terms.md — updated casing "WorkItem" → "Workitem" (matches codebase)
```

Then walk through each "needs input" item one at a time via AskUserQuestion:

```
stack/messaging.md says "MassTransit 8.x" but no MassTransit references found.
Found "Wolverine" in 12 files — possible replacement?

→ Update ground file (replace MassTransit with Wolverine)
→ Remove ground file (no longer relevant)
→ Keep as-is (still accurate, just not referenced in code)
→ Create observation (needs deeper investigation)
```

### 5. Report

After all items are processed, print a short console summary:

```
Ground verified — 23 files checked
  ✓ 14 verified · 3 auto-fixed · 4 updated · 2 removed · 1 observation created
```

If there were auto-fixes, list them in one line each:

```
Auto-fixed:
  stack/dotnet.md — EF Core 8.0.1 → 8.2.0
  debt/legacy-auth.md — marked resolved
  vocabulary/terms.md — casing "WorkItem" → "Workitem"
```

## Integration with Specs

- Personas → user stories reference real users
- Rules → acceptance criteria align with quality bars
- Vocabulary → specs use consistent terminology
- Design → UI specs reference design tokens

## Kickstart (New Projects)

Ask via AskUserQuestion: product type (SaaS / API-Platform / Marketing site / Internal tool). Then elicit stack, conventions, key personas. Create initial ground files from conversation.

## Preview Template

To open a markdown file as a rendered preview in the browser:

1. `Read` the template from `plugin/templates/preview.html` (resolve via `${CLAUDE_PLUGIN_ROOT}/templates/preview.html`)
2. `Read` the markdown file to preview
3. Replace placeholders in the template:
   - `{{TITLE}}` → document title (from frontmatter or first heading)
   - `{{CONTEXT}}` → brief context string, e.g. "observation review" or "spec draft"
   - `{{CONTENT}}` → the raw markdown content (the template renders it client-side via marked.js)
4. `Write` the result to `.mill/.preview.html`
5. Open in browser:
   - Windows: `start .mill/.preview.html`
   - macOS: `open .mill/.preview.html`
   - Linux: `xdg-open .mill/.preview.html`
