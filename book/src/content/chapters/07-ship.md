---
title: "Ship"
chapter: 7
part: "Process"
partNumber: 2
description: "Implementing specifications through bounded verification loops"
slug: "ship"
---

Ship is where specifications become code. It's the most visible stage — and the most constrained. That constraint is the point.

## The Bounded Loop

Ship doesn't just "implement a feature." It runs a structured loop:

1. **Read the specification** — understand requirements, approach, and criteria
2. **Implement** — write code following the specified approach
3. **Verify** — check each criterion independently
4. **Adjust** — fix what failed, leave what passed
5. **Repeat** — until all criteria pass or the iteration limit is reached

This is fundamentally different from asking an AI to "build this feature." The AI has a contract to fulfill, not a conversation to continue. It knows exactly what "done" means because the criteria define it.

## Running Ship

```
/mill:ship 42           # Ship GitHub issue #42
/mill:ship              # Ship will ask which issue to work on
```

Ship creates a git worktree for the implementation, keeping your main branch clean. When all criteria pass, it creates a Pull Request linked to the issue.

## Verification Is Not Testing

mill's verification is distinct from your test suite. Tests verify that code *behaves correctly*. Criteria verify that the *specification was fulfilled*.

A test might check that `resetPassword()` throws an error for invalid tokens. A criterion checks that "users can reset their password via email within the flows described in the approach." Criteria operate at a higher level — they verify the intent, not just the mechanics.

In practice, mill's verification uses two prompts:

- **Work prompt** — implements the specification
- **Verify prompt** — independently reviews the implementation against each criterion

The verify prompt has no access to the work prompt's reasoning. It evaluates the output purely against the criteria. This separation prevents the common failure where an AI convinces itself that its own work is correct.

## What Ship Produces

When ship completes successfully:

- A **Pull Request** is created with a clear description linking back to the spec
- **History** is recorded in `.mill/ship/history.json` — what was implemented, how many iterations it took, what failed and passed
- **Observations** are written — patterns discovered, concerns raised, suggestions for improvement

The PR goes through normal human review. Ship doesn't merge anything automatically. The human review is the final gate.

## Domain-Aware Execution

Ship adapts its execution guidance based on the specification's domain:

| Domain | Focus |
|--------|-------|
| `backend` | API design, error handling, performance |
| `application` | Component architecture, state management, UX |
| `website` | Visual quality, responsive design, performance |
| `platform` | Infrastructure, containers, observability |

The right guidance at the right time prevents the AI from applying backend patterns to frontend work, or vice versa.

> Ship doesn't iterate until the AI thinks it's done. It iterates until the criteria say it's done.
