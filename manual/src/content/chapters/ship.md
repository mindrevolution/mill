---
title: "Ship"
number: 6
subtitle: "From spec to pull request"
accent: "flame"
---

## What Ship Does

Ship takes a GitHub Issue (your spec) and assembles a team of agents to implement it, verify it independently, and open a Pull Request. One skill, full pipeline.

```
/mill:ship 47
```

## The Team

| Role | Count | Purpose |
|------|-------|---------|
| **Lead** | 1 | Reads spec, sizes team, delegates work, manages cross-team contracts |
| **Implementer(s)** | 1-4 | Build within assigned file ownership boundaries |
| **Verifier** | 1 | Separate agent, clean context, reviews full changeset against every criterion |

Even a simple spec gets a team-of-1. The lead always delegates — it never implements directly.

Team size is driven by the spec's approach:

| Approach Parts | Domain | Implementers |
|----------------|--------|-------------|
| 1-4 | Single domain | 1 |
| 5-9 | Single domain | 2 (split by concern) |
| Any | Fullstack | 2-3 (one per layer) |
| 10+ | Any | 3-4 |

## How It Works

### Phase 1: Setup

1. **Launch** — mill reads the issue, parses the spec structure, validates everything needed is present
2. **Isolate** — creates a worktree on a dedicated branch (`issue-47`). Your main branch stays untouched
3. **Load context** — the spec, project context from `.mill/context.md`, ground knowledge, and domain guidance

### Phase 2: Implement

4. **Delegate** — each implementer gets their portion of the spec, project context, domain guidance, and explicit file ownership:

```
Backend implementer:  src/api/, src/models/, src/services/
Frontend implementer: src/components/, src/pages/, src/hooks/
```

File ownership prevents conflicts when multiple implementers work in parallel. The lead manages cross-team contracts — if the backend implementer defines an API shape, the lead communicates that contract to the frontend implementer.

5. **Build** — implementers work within their boundaries, committing as they go
6. **Polish** — after completion, each implementer gets one bounded pass to clean up rough edges, check every spec criterion against their diff, and run tests. This raises the floor before the verifier sees the code.

### Phase 3: Verify

7. **Independent verification** — the lead spawns a verifier with **clean context**: no access to the implementer's reasoning, planning, or iteration history. The verifier sees only the spec and the resulting code diff. It reviews the full changeset, runs the test suite, checks every acceptance criterion, and looks for quality issues, security concerns, and scope creep.

> **What "clean context" means** — The verifier is a separate agent that starts fresh. It doesn't know what was hard to implement, what trade-offs the implementer considered, or what was tried and reverted. This is deliberate — it catches what self-review misses.

8. **Iteration cycles** — when the verifier rejects:
   1. Verifier reports specific blockers with suggestions
   2. Lead identifies which implementer owns the affected code
   3. That implementer gets the feedback plus cumulative history of past fixes
   4. Verifier re-checks with clean context (no memory of previous rounds)

> **Rule** — Iteration cycles are governed by the spec's [Loop Contract](/spec#the-loop-contract). The default is 5 cycles. After that, mill escalates to you with options: continue iterating, create PR as-is, or abort.

### Phase 4: Finalize

9. **Create PR** — mill creates a Pull Request linking back to the spec issue. Full traceability from intent to implementation
10. **Extract learnings** — the lead reviews the session — iteration history, implementer notes, orchestration decisions — and writes a learnings observation:

```markdown
---
source: ship
type: learning
issue: 47
suggested: ground/patterns/
---
# Ship #47 Learnings
- ReportService and PdfRenderer are tightly coupled — changes to one
  require changes to the other
- Puppeteer needs `--no-sandbox` flag in the CI Docker container
- The PDF rendering timeout should be configurable per-report, not global
```

Learnings are auto-tagged with a `suggested:` routing hint. You decide what becomes permanent knowledge during `/mill:ground` review.

11. **Clean up** — worktree removed. Git log and GitHub PRs are the history.

## When Things Go Wrong

**Tests fail** — Implementers address test failures as part of their task, committing only after tests pass.

**Verification rejects** — The lead routes feedback to the responsible implementer. Iteration continues up to the Loop Contract limit.

**Spec has gaps** — The lead escalates with a clear description of what's missing. The spec goes back to drafting.

**Fallback mode** — If agent teams aren't available (experimental feature disabled), ship falls back to single-session mode: the lead implements directly, then does an explicit self-review phase against the spec. Iteration limit still governed by the Loop Contract. Degraded but functional.
