---
description: Execute bounded work loops against specs until verification passes
allowed-tools: Read, Write, Edit, Glob, Grep, Bash
argument-hint: "<issue-number> - GitHub issue to implement"
---

# Ship

Execute bounded work loops against specs. Work in slices until verification passes.

## Interaction Pattern

Ship is mostly autonomous — the spec should be complete from `/mill:spec`. **Use AskUserQuestion only when:**

- Spec has ambiguity that blocks implementation
- Multiple valid approaches exist and choice matters
- Scope clarification needed before proceeding

```yaml
AskUserQuestion:
  question: "Spec says 'handle errors gracefully'. Which approach?"
  header: "Approach"
  options:
    - label: "Toast notifications"
      description: "Show inline error messages"
    - label: "Error page"
      description: "Redirect to error view"
```

Don't ask about implementation details you can decide autonomously.

## Overview

Ship takes a GitHub issue (spec) and implements it through bounded iterations:
1. Read the spec
2. Plan slices
3. Implement one slice per iteration
4. Verify after final slice
5. Create PR on success

## Commands

```bash
# List open issues
mill issue list --human

# Get issue details
mill issue get 42 --human

# View run history
mill history --human
```

## Workflow

### 1. Load Spec

```bash
mill issue get <number> --human
```

Read the issue body as the spec.

### 2. Check Context

```bash
mill context --human
```

Ensure `.mill/context.md` exists. Run `/mill:warmup` if not.

### 2b. Load Domain Guidance

Based on spec's `domain` field, load execution guidance:

```bash
mill template get domains {domain} --human
```

| Domain | Focus |
|--------|-------|
| `backend` | API design, data modeling, error handling, performance |
| `application` | Component architecture, state management, UX |
| `website` | Page architecture, aesthetics, responsive, performance |
| `platform` | IaC, containers, reliability, observability, security |
| `fullstack` | Load both backend and application guidance |

**Apply this mindset throughout implementation.** Domain guidance sets quality expectations and anti-patterns to avoid.

For `website`: Design references (Figma, screenshots) in the spec are source of truth. Match them precisely.

### 3. Plan Slices

For non-trivial specs, plan the work in slices:

```markdown
# Slice Plan

## Concerns
- [ ] Model — data structures
- [ ] Logic — business rules
- [ ] Interface — API/UI changes
- [ ] Tests — test coverage

## Slice Order
1. Model changes (foundation)
2. Logic implementation
3. Interface updates
4. Tests
```

Write plan to `.mill/ship/work/issue-{number}-plan.md`.

### 4. Execute Slice

For each iteration:
1. Read the plan
2. Implement ONE slice only
3. Run tests (from Loop Contract)
4. Commit changes
5. Update plan (mark slice complete)

### 5. Signals

After each iteration, signal status:

**More slices remain:**
```
MILL_CONTINUE
{
  "done": "Implemented data models",
  "next": "Add business logic"
}
```

**Final slice complete, ready for verification:**
```
MILL_VERIFY
{
  "branch": "issue-42",
  "title": "#42: Add user authentication",
  "done": "Added integration tests",
  "summary": "Full auth flow with JWT tokens",
  "verification": "All 12 tests passing"
}
```

**Spec cannot be implemented:**
```
MILL_ABORT: Referenced file does not exist
```

### 6. Verification

After `MILL_VERIFY`, an independent verification runs:
- Tests are run again
- Acceptance criteria are checked
- Code is reviewed

If verification passes → PR is created
If verification fails → continue iterating with feedback

### 7. Record History

After completion:
```bash
mill history add '{"date":"2024-01-15","issue":42,"pr":43,"title":"Add auth",...}'
```

## Slicing Rules

1. **One slice per iteration** — don't bundle unrelated changes
2. **Slice by concern** — Model, Logic, Interface, Tests
3. **Tests verify each slice** — run before committing
4. **Commit before signaling** — changes must be committed
5. **Single-concern exception** — trivial fixes can skip slicing

## Iteration Limits

Default: 20 iterations max (from Loop Contract)

If limit reached without completion → signal for human review

## Integration

- Specs come from GitHub Issues (created by `/mill:spec`)
- Context from `.mill/context.md` (created by `/mill:warmup`)
- Standards from `.mill/ground/standards/`
- History tracked in `.mill/ship/history.json`

## Rules

1. Honor the spec — don't add unrequested features
2. Minimize unrelated changes
3. Test before signaling
4. Commit before signaling
5. One slice per iteration
6. Don't create PR yourself — signal for verification
7. MILL_ABORT for impossible specs, not for difficulties
