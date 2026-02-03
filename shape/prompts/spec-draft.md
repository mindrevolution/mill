# Spec Draft

Transform user intent into a complete, loop-ready specification.

**CRITICAL:** Do NOT use Claude Code's built-in `/plan` mode, plan files, or TodoWrite tool. Use ONLY MILL's draft system (`.mill/shape/drafts/`) as defined below.

## Context
Pre-loaded: `.mill/context.md`, `.mill/standards/*.md`, `.mill/memory/project.md`, `.mill/personas.md` (if exists), uncommitted changes.

**BEFORE ANYTHING ELSE, check if this prompt starts with a special section:**

1. **`# User Intent`** — If the message starts with this section (before this Spec Draft prompt), that IS the user's intent. Use it directly — do NOT ask what they want to build or offer to continue existing drafts. Proceed to Flow step 1 with this intent.

2. **`# Resume Mode`** — If present at the end with a file path, read that draft file and continue from where it left off. Skip warmup — go to Flow step 0.

3. **Neither section exists** — Interactive mode. User's next message will be their intent.

## Using Personas

If `.mill/personas.md` is loaded, use it to improve elicitation:

1. **Tailor questions** — reference specific personas when asking about users
   - Instead of: "who is this for?"
   - Ask: "is this primarily for ops engineers like Alex, or product leads like Sam?"

2. **Inform user stories** — write stories using persona context
   - Use their job, trigger, and pain points to frame the story
   - Example: "As an ops engineer on-call, I want to see test results inline so that I don't context-switch to the CI dashboard"

3. **DO NOT copy personas into spec** — personas inform the writing, but the spec stands alone
   - User stories should be self-contained
   - Don't reference persona names in the final spec
   - The run loop won't have personas — specs must be complete without them

4. **Validate against personas** — check if the spec addresses real pain points
   - Does this solve a job-to-be-done?
   - Would this reduce churn risk?

## Draft Persistence — Live Updates

**Create the draft file EARLY and update it CONTINUOUSLY.** The UI watches for file changes and displays the spec as it takes shape. Users should see their spec building in real-time.

### When to Write

| Trigger | Action |
|---------|--------|
| After classification (step 2) | CREATE draft with type, title, slug, status |
| After each user answer | UPDATE with new information |
| After each field is captured | UPDATE fields_complete/fields_pending |
| After status changes | UPDATE status field |
| After any refinement | UPDATE relevant sections |

**Write early, write often.** Don't wait for a complete section — write partial content as you learn it. A draft with "## Problem\n\nUsers can't X when Y..." is better than an empty section.

### File Location

`.mill/shape/drafts/{slug}.md`

### Format

```yaml
---
type: feature|bug|security|task
title: Human-readable title
slug: lowercase-hyphenated
summary: one-line (update as understanding deepens)
status: classifying|eliciting|reviewing|challenging|complete
persona: persona-slug (if applicable)
created: ISO8601
updated: ISO8601
fields_complete: [problem, users]
fields_pending: [acceptance_criteria, scope, verification, loop_contract]
---

## Problem/Opportunity
## Target Users
## User Stories
## Acceptance Criteria
## Scope
## Verification
## Loop Contract
```

### Update Pattern

After each interaction:
1. Read current draft (if exists)
2. Merge new information
3. Update `updated` timestamp
4. Update `fields_complete`/`fields_pending`
5. Write file

This ensures the UI always shows current progress, even if the session is interrupted.

## Flow

### 0. Check for Special Sections (FIRST!)

**Before doing anything else**, check if this message has special sections:

**If the message STARTS with `# User Intent`:**
1. Extract the intent text from that section (before the `# Spec Draft` heading)
2. Do NOT list existing drafts or ask what to build — you already have the intent
3. Proceed to **step 1 (Understand Context)** using this intent
4. Then continue to **step 2 (Classify)** with the intent

**If `# Resume Mode` exists at the END with a file path:**
1. **IMMEDIATELY read that file** using the Read tool — this is your draft
2. Do NOT search `.mill/shape/drafts/` or warm up context — the draft path is given
3. After reading, acknowledge: "resuming: {title from draft}" and ask what to refine
4. Skip to **step 3 (Elicit)** to continue

**If neither section exists** → Interactive mode, proceed to step 1 and await user input.

### 1. Understand Context
1. Read project instructions: check `AGENTS.md` (or `CLAUDE.md` if no AGENTS.md)
2. **Only if no `# User Intent` was provided:** Check `.mill/shape/drafts/*.md` for similar work — offer to continue if found
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

**CREATE DRAFT NOW.** As soon as you have type and a working title:
1. Generate slug from title
2. Write initial draft file with `status: classifying`
3. Continue elicitation — the draft will update as you learn more

### 3. Elicit

One question at a time. Layer by layer:
1. Surface problem
2. Impact/motivation
3. Desired state
4. Success criteria
5. Scope boundaries
6. Verification

**UPDATE DRAFT AFTER EACH ANSWER.** When the user responds:
1. Incorporate their answer into the appropriate section
2. Update `status: eliciting` and `updated` timestamp
3. Update `fields_complete`/`fields_pending` lists
4. Write the file — the UI will refresh automatically

**Fields by type:**

| Feature | Bug | Security | Task |
|---------|-----|----------|------|
| Problem | Expected vs actual | Threat | What changes |
| Users | Reproduction | STRIDE | Why now |
| User story | Environment | Attack vector | Scope |
| Acceptance | Regression test | Mitigation | Acceptance |
| Scope | Verification | Verification | Guardrails |
| Loop Contract | Loop Contract | Loop Contract | Loop Contract |

**If personas exist:** When eliciting "Users" and "User story", reference loaded personas to ground the conversation. Write user stories that reflect persona jobs and pain points, but don't name personas in the output.

Reject vague criteria:
- "faster" → "< Xms"
- "looks better" → "matches design spec"
- "works correctly" → "test passes"

Push back on scope creep: "separate spec, finish this first."

### 4. Generate
Use template from `spec/templates/{type}.md`. Fill ALL fields.

### 5. Validate
```
[ ] success criteria testable
[ ] each criterion verifiable
[ ] scope clear (in/out)
[ ] no placeholders
[ ] no open questions (resolve before finalizing)
[ ] Loop Contract present with:
    - test command (or explicit "none: <reason>")
    - success criteria
    - stop conditions
```

**Open questions block finalization.** If the spec contains "Open Questions", "Open Topics", or similar unresolved sections, they MUST be resolved before creating a GitHub Issue. Drafts may have open questions; issues may not.

**Loop Contract is required.** Ask: "What command runs the tests?" Common: `npm test`, `dotnet test`, `pytest`, `go test ./...`. If no tests exist, require explicit reason. Stop conditions default to 20 iterations.

If any fail, elicit missing info.

### 6. Challenge

**When to challenge:** Skip this step for `bug` and `task` types with ≤3 acceptance criteria. Always challenge `feature` and `security` types, or any spec with >3 acceptance criteria.

**Purpose:** Adversarial review to surface gaps before implementation. Elicitation builds the spec; challenge tries to break it.

Update draft status to `challenging`. Then probe these areas one question at a time:

| Area | Example Questions |
|------|-------------------|
| **Assumptions** | "You assume X is always available — what if it's not?" |
| **Edge cases** | "What happens with empty input? Concurrent access? 10x expected load?" |
| **Failure modes** | "If the database/API/service is down, what should happen?" |
| **Integration** | "How does this interact with [existing feature]? Any conflicts?" |
| **Rollback** | "If this breaks in production, how do we recover or disable it?" |

**Flow:**
1. Identify 2-4 potential gaps from the areas above
2. Ask ONE question, wait for response
3. If answer reveals missing criteria → add to spec, return to step 5 (Validate)
4. If answer confirms no gap → continue to next question
5. After all gaps addressed → proceed to Confirm

```
challenging spec for gaps...

[area]: [specific question about this spec]

1. [option if applicable]
2. [option]
3. not a concern because [user explains]
```

**Do not invent problems.** If the spec is solid, acknowledge it and move on. Challenge is not a gate to add scope — it surfaces genuine risks.

### 7. Confirm

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
  ✓ no open questions

approve and create issue? [y/n]
```

**If open questions exist**, show validation failure and resolve them first:
```
⚠ cannot finalize — open questions remain:

- [question 1]
- [question 2]

resolve these before creating issue. which should we address first?
```

**STOP HERE.** Do not call `gh issue create` or proceed to step 8 until user responds.

- If user says `y`, `yes`, or `approve` → proceed to step 8
- If user says `n`, `no`, or provides feedback → incorporate changes and repeat step 7
- If user adds new information → update spec, repeat step 7

### 8. Finalize

**Only after explicit user approval in step 7.**

GitHub is source of truth. No local spec files.

1. Create issue: `gh issue create --title "{Title}" --body "{spec}" --label "{type}"`
2. Delete draft
3. Output exactly:
   ```
   created: #{number}

   mill run {number}

   /exit
   ```
4. END SESSION. No follow-ups.

Fallback: if `gh` fails, write to `.mill/{type}/{slug}.md`.

## Rules
1. Detective mindset — dig layer by layer
2. One question at a time
3. Offer 2-4 options + "or just type"
4. **Save draft EARLY and OFTEN** — create file after classification, update after every user answer
5. Write to `.mill/shape/drafts/` ONLY — never use `/plan`, TodoWrite, or other locations
6. No placeholders in final spec (partial content during elicitation is fine)
7. Security always wins classification
8. Force decisions — no "it depends"
9. Scope creep → separate spec
10. End cleanly — after finalize, stop
11. Never use built-in plan mode, TodoWrite, or other Claude Code features — only MILL workflow
12. **NEVER create GitHub issue without explicit user approval** — step 7 confirmation is mandatory
