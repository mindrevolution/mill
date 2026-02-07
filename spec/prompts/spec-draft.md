# Spec Draft

Transform user intent into a complete, loop-ready specification.

**CRITICAL:** Do NOT use Claude Code's built-in `/plan` mode, plan files, or TodoWrite tool. Use ONLY MILL's draft system (`.mill/drafts/`) as defined below.

## Structured Questions

Use the `AskUserQuestion` tool for multiple-choice questions. This provides cleaner UX with keyboard navigation and built-in "Other" option.

**When to use AskUserQuestion:**
- Classification (feature/bug/security/task)
- Scope narrowing (which of these options?)
- Challenge phase (how to handle edge case?)
- Persona selection (if multiple personas exist)
- Final approval (approve/revise)

**When NOT to use AskUserQuestion:**
- Open-ended elicitation ("describe the problem")
- Follow-up clarifications

**Format:**
```typescript
AskUserQuestion({
  questions: [{
    header: "Type",           // Short label (max 12 chars)
    question: "What kind of change is this?",
    options: [
      { label: "Feature", description: "New capability" },
      { label: "Bug", description: "Something broken" }
    ],
    multiSelect: false        // true only when choices aren't mutually exclusive
  }]
})
```

## Context
Pre-loaded: `.mill/context.md`, `.mill/standards/*.md`, `.mill/memory/project.md`, `.mill/personas.md` (if exists), uncommitted changes.

**Resume Mode:** If `# Resume Mode` section exists, continue from that draft.
**New Session:** User's first message IS their intent. Proceed directly — don't ask "what would you like to build?"

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

## Draft Persistence

Save to `.mill/drafts/{slug}.md` after EACH field captured.

```yaml
---
type: feature|bug|security|task
title: Human-readable title
slug: lowercase-hyphenated
summary: one-line
status: classifying|eliciting|reviewing|challenging|complete
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

Use `AskUserQuestion` to confirm type and narrow scope:

```typescript
AskUserQuestion({
  questions: [
    {
      header: "Type",
      question: "This sounds like a feature. Is that correct?",
      options: [
        { label: "Yes, feature", description: "New capability or behavior" },
        { label: "Bug", description: "Something existing is broken" },
        { label: "Security", description: "Risk or vulnerability" },
        { label: "Task", description: "Technical work, no user impact" }
      ],
      multiSelect: false
    },
    {
      header: "Scope",
      question: "What specifically do you want?",
      options: [
        { label: "[option from code]", description: "Based on codebase analysis" },
        { label: "[option 2]", description: "Alternative approach" },
        { label: "[option 3]", description: "Another possibility" }
      ],
      multiSelect: false
    }
  ]
})
```

Populate options based on codebase analysis. User can always select "Other" for custom input.

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
[ ] Loop Contract present with:
    - test command (or explicit "none: <reason>")
    - success criteria
    - stop conditions
```

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
2. Ask ONE question using `AskUserQuestion`, wait for response
3. If answer reveals missing criteria → add to spec, return to step 5 (Validate)
4. If answer confirms no gap → continue to next question
5. After all gaps addressed → proceed to Confirm

Use `AskUserQuestion` for each challenge:

```typescript
AskUserQuestion({
  questions: [{
    header: "Edge case",
    question: "What happens if the database is unavailable during sync?",
    options: [
      { label: "Queue and retry", description: "Buffer locally, sync when available" },
      { label: "Fail fast", description: "Show error immediately" },
      { label: "Not a concern", description: "Availability is guaranteed" }
    ],
    multiSelect: false
  }]
})
```

**Do not invent problems.** If the spec is solid, acknowledge it and move on. Challenge is not a gate to add scope — it surfaces genuine risks.

### 7. Confirm

**MANDATORY GATE — DO NOT PROCEED WITHOUT EXPLICIT USER APPROVAL**

Present the spec with validation summary, then use `AskUserQuestion` for approval:

```
spec ready for review:

[spec]

---
validation:
  ✓ {n} testable criteria
  ✓ verification defined
  ✓ scope clear
```

```typescript
AskUserQuestion({
  questions: [{
    header: "Confirm",
    question: "Spec is ready. Create GitHub issue?",
    options: [
      { label: "Approve", description: "Create issue and finalize" },
      { label: "Revise", description: "Make changes, show again" }
    ],
    multiSelect: false
  }]
})
```

**STOP HERE.** Do not call `gh issue create` or proceed to step 8 until user explicitly approves.

- If user selects "Approve" → proceed to step 8
- If user selects "Revise" or "Other" with feedback → incorporate changes and repeat step 7

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
3. Use `AskUserQuestion` for multiple-choice; conversation for open-ended
4. Save draft after EVERY answer — to `.mill/drafts/`, NOT `/plan` or any other location
5. No placeholders
6. Security always wins classification
7. Force decisions — no "it depends"
8. Scope creep → separate spec
9. End cleanly — after finalize, stop
10. Never use built-in plan mode, TodoWrite, or other Claude Code features — only MILL workflow
11. **NEVER create GitHub issue without explicit user approval** — step 7 confirmation is mandatory
