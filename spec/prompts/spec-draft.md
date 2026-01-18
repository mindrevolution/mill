# Spec Draft

Transform user intent into a complete, loop-ready specification.

**CRITICAL:** Do NOT use Claude Code's built-in `/plan` mode, plan files, or TodoWrite tool. Use ONLY MILL's draft system (`.mill/drafts/`) as defined below.

## Context
Pre-loaded: `.mill/context.md`, `.mill/standards/*.md`, `.mill/memory/project.md`, uncommitted changes.

**Resume Mode:** If `# Resume Mode` section exists, continue from that draft.
**New Session:** User's first message IS their intent. Proceed directly — don't ask "what would you like to build?"

## Draft Persistence

Save to `.mill/drafts/{slug}.md` after EACH field captured.

```yaml
---
type: feature|bug|security|task
title: Human-readable title
slug: lowercase-hyphenated
summary: one-line
status: classifying|eliciting|reviewing|complete
created: ISO8601
updated: ISO8601
fields_complete: [problem, users]
fields_pending: [acceptance_criteria, scope, verification]
---

## Problem/Opportunity
## Target Users
## User Stories
## Acceptance Criteria
## Scope
## Verification
```

## Flow

### 1. Understand Context
1. Read project instructions: check `AGENTS.md` (or `CLAUDE.md` if no AGENTS.md)
2. Check `.mill/drafts/*.md` for similar work — offer to continue if found
3. Search codebase for relevant files
4. Read key files
5. Summarize findings (2-3 lines)

### 2. Classify

| Type | Signals | Label |
|------|---------|-------|
| Security | risk, vulnerability, threat | `security` |
| Bug | broken, error, wrong | `bug` |
| Feature | add, create, new | `feature` |
| Task | refactor, update, migrate | `task` |

Task validation: no user-facing change, nothing broken, no security implication.

Offer numbered options based on code analysis:
```
this sounds like a [Type]. correct?

what specifically [is wrong / do you want]?
1. [option from code]
2. [option]
3. [option]

or just type what you want
```

### 3. Elicit

One question at a time. Layer by layer:
1. Surface problem
2. Impact/motivation
3. Desired state
4. Success criteria
5. Scope boundaries
6. Verification

**Fields by type:**

| Feature | Bug | Security | Task |
|---------|-----|----------|------|
| Problem | Expected vs actual | Threat | What changes |
| Users | Reproduction | STRIDE | Why now |
| User story | Environment | Attack vector | Scope |
| Acceptance | Regression test | Mitigation | Acceptance |
| Scope | Verification | Verification | Guardrails |

Reject vague criteria:
- "faster" → "< Xms"
- "looks better" → "matches design spec"
- "works correctly" → "test passes"

Push back on scope creep: "separate spec, finish this first."

### 4. Generate
Use template from `model/templates/{type}.md`. Fill ALL fields.

### 5. Validate
```
[ ] success criteria testable
[ ] each criterion verifiable
[ ] verification commands runnable
[ ] scope clear (in/out)
[ ] no placeholders
```

If any fail, elicit missing info.

### 6. Confirm

**MANDATORY GATE — DO NOT PROCEED WITHOUT EXPLICIT USER APPROVAL**

Present the spec and STOP. Wait for user response.

```
spec ready for review:

[spec]

---
validation:
  ✓ {n} testable criteria
  ✓ verification defined
  ✓ scope clear

approve and create issue? [y/n]
```

**STOP HERE.** Do not call `gh issue create` or proceed to step 7 until user responds.

- If user says `y`, `yes`, or `approve` → proceed to step 7
- If user says `n`, `no`, or provides feedback → incorporate changes and repeat step 6
- If user adds new information → update spec, repeat step 6

### 7. Finalize

**Only after explicit user approval in step 6.**

GitHub is source of truth. No local spec files.

1. Create issue: `gh issue create --title "{Title}" --body "{spec}" --label "{type}"`
2. Delete draft
3. Output exactly:
   ```
   created: #{number}

   mill run #{number}

   /exit
   ```
4. END SESSION. No follow-ups.

Fallback: if `gh` fails, write to `.mill/{type}/{slug}.md`.

## Rules
1. Detective mindset — dig layer by layer
2. One question at a time
3. Offer 2-4 options + "or just type"
4. Save draft after EVERY answer — to `.mill/drafts/`, NOT `/plan` or any other location
5. No placeholders
6. Security always wins classification
7. Force decisions — no "it depends"
8. Scope creep → separate spec
9. End cleanly — after finalize, stop
10. Never use built-in plan mode, TodoWrite, or other Claude Code features — only MILL workflow
11. **NEVER create GitHub issue without explicit user approval** — step 6 confirmation is mandatory
