---
title: "Ship"
number: 7
subtitle: "From spec to pull request"
accent: "flame"
---

## The Execution Engine

Ship is where intent becomes reality. You point it at a spec — a GitHub Issue — and it assembles a team of agents to implement, verify, and open a Pull Request.

This isn't "generate some code." This is **team-based delivery**: a lead reads your spec, sizes the team, delegates work with file ownership boundaries, and coordinates until an independent verifier confirms every criterion.

## How It Works

### 1. Launch

```
/mill:ship 47
```

mill reads Issue #47, parses the spec structure, and validates everything needed is present. No issue number? mill shows open specs for you to choose from.

### 2. Isolate

mill creates a **worktree** — an isolated copy of your repo on a dedicated branch:

```
.mill/ship/work/issue-47/
```

Your main branch stays untouched. If anything goes wrong, there's nothing to clean up.

### 3. Load Context

Before assembling the team, the lead loads the spec, project context from `.mill/context.md`, and domain guidance matching the spec's domain. This is why ground matters — rich ground knowledge produces dramatically better code.

### 4. Size the Team

The lead analyzes the spec's approach to determine how many implementers are needed:

| Spec Shape | Implementers |
|------------|-------------|
| Single domain, 1-4 parts | 1 |
| Single domain, 5+ parts | 2 (split by concern) |
| Fullstack | 2-3 (one per layer) |
| Complex, 10+ parts | 3-4 |

Even a simple spec gets a team-of-1. The lead always delegates — it never implements directly.

### 5. Delegate

Each implementer gets their portion of the spec, project context, domain guidance, and **explicit file ownership** — "you may only edit files in `src/api/` and `src/models/`." This prevents conflicts when multiple implementers work in parallel.

The lead manages cross-team contracts. If a backend implementer defines an API shape, the lead communicates that contract to the frontend implementer.

### 6. Polish

After all implementers complete their tasks, each gets one bounded pass to self-review: clean up rough edges, check every spec criterion against their diff, run tests. This raises the floor before the independent verifier sees the code.

### 7. Verify Independently

**Work can't grade its own homework.**

The lead spawns a **verifier** — a separate agent with clean context that never saw the implementer's reasoning. The verifier reviews the full `git diff`, runs the test suite, checks every acceptance criterion, and looks for quality issues, security concerns, and scope creep.

Pass → PR. Reject → feedback routed to the responsible implementer.

### 8. Iteration Cycles

When the verifier rejects:

1. Verifier reports specific blockers with suggestions
2. Lead identifies which implementer owns the affected code
3. That implementer gets the feedback plus cumulative history of past fixes
4. Verifier re-checks with clean context (no memory of previous rounds)

Every spec includes a **Loop Contract** that governs how many cycles are allowed (default 5). After that, mill escalates to you with options: continue iterating, PR as-is, or abort.

### 9. Create PR

mill creates a Pull Request linking back to the spec issue — full traceability from intent to implementation.

### 10. Extract Learnings

After every ship run, the lead pauses to ask: **"How can we do better next time?"**

It reviews the session — iteration history, implementer notes, orchestration decisions — and writes a learnings observation. Not a formality: these capture the non-obvious stuff that makes the next ship smarter:

- Files that turned out to be coupled in ways you wouldn't guess from the directory structure
- Error messages that were misleading and what they actually meant
- Commands or configs that took trial and error to get right

All learnings land in the **learning inbox** with a suggested routing hint. You decide what becomes permanent knowledge — either via `/mill:ground` or when `/mill:spec` nudges you before drafting. mill suggests where each learning belongs (patterns, rules, etc.) but the human makes the final call.

### 11. Clean Up

Worktree removed. Your git log and GitHub PRs are the history.

## Structural Independence

The verifier is always a separate agent. When you review your own work, you see what you *intended* to write, not what you *actually* wrote. A fresh pair of eyes catches what self-review misses.

mill enforces this structurally. The verifier has no access to the implementer's reasoning or planning. It only sees the spec and the resulting code.

## Autonomy and Judgment

Ship is autonomous. The spec is the complete instruction set. The rule: honor the spec. Don't add what wasn't asked for. Don't skip what was specified.

If something blocks implementation, the lead escalates and the spec goes back to drafting.

## When Things Go Wrong

**Tests fail** — Implementers address test failures as part of their task, committing only after tests pass.

**Verification rejects** — The lead routes feedback to the responsible implementer. Iteration continues up to the Loop Contract limit.

**Spec has gaps** — The lead escalates with a clear description of what's missing. The spec goes back to drafting.

**Fallback mode** — If agent teams aren't available, ship falls back to single-session mode: the lead implements directly, then self-reviews against the spec. Degraded but functional.
