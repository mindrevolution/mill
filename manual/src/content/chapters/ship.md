---
title: "Ship"
number: 7
subtitle: "From spec to pull request"
accent: "flame"
---

## The Execution Engine

Ship is where intent becomes reality. You point it at a crafted spec — hosted as a GitHub Issue — and it assembles a team of agents to implement that spec, verify it independently, and open a Pull Request.

This isn't "generate some code." This is **team-based delivery**: a lead agent reads your spec, determines the right team size, delegates work with explicit file ownership boundaries, and coordinates until an independent verifier confirms every criterion is met.

## How It Works

### 1. Launch

```
/mill:ship 47
```

That's it. mill reads GitHub Issue #47, parses the spec structure, and validates that everything needed is present. If no issue number is given, mill shows open spec issues for you to choose from.

### 2. Isolate

mill creates a **worktree** — an isolated copy of your repo on a dedicated branch:

```
.mill/ship/work/issue-47/
```

Your main branch stays untouched. All implementation happens in isolation. If anything goes wrong, there's nothing to clean up on main.

### 3. Load Context

Before assembling the team, the lead loads:

- **The spec** — parsed from the GitHub Issue
- **Project context** from `.mill/context.md` — your codebase overview
- **Domain guidance** — execution template for the spec's domain (backend, application, etc.)

This is why ground matters. A ship run with rich ground knowledge produces dramatically better code than one without.

### 4. Determine Team Size

The lead analyzes the spec's requirements and approach to determine how many implementers are needed:

| Spec Shape | Implementers |
|------------|-------------|
| Single domain, 1-4 approach parts | 1 |
| Single domain, 5+ approach parts | 2 (split by concern) |
| Fullstack domain | 2-3 (one per layer) |
| Complex, 10+ parts | 3-4 |

Even a simple spec gets a team-of-1. The lead always delegates — it never implements directly.

### 5. Delegate

The lead spawns implementer agents. Each gets:

- Their portion of the spec (specific task assignments)
- Project context and domain guidance
- **Explicit file ownership boundaries** — "you may only edit files in `src/api/` and `src/models/`"
- The worktree path as working directory

File ownership prevents conflicts when multiple implementers work in parallel. The lead manages cross-team contracts — if a backend implementer defines an API shape, the lead communicates that contract to the frontend implementer.

### 6. Verify Independently

This is the key insight: **work can't grade its own homework.**

After all implementers complete their tasks, the lead spawns a **verifier** — a separate agent with clean context that never saw the implementer's reasoning. The verifier:

- Reviews the full `git diff` for the changeset
- Runs the test suite
- Checks every acceptance criterion from the spec
- Looks for quality issues, security concerns, and scope creep

If verification passes → proceed to PR.
If verification rejects → the lead routes specific feedback to the responsible implementer(s).

### 7. Rejection Cycles

When the verifier rejects, the cycle is precise:

1. Verifier reports specific blockers with suggestions
2. Lead identifies which implementer owns the affected code
3. That implementer receives the feedback and fixes the issues
4. Verifier re-checks with clean context

Maximum **3 rejection cycles** before escalating to you. This prevents infinite loops while giving honest attempts to resolve issues.

### 8. Create PR

mill creates a Pull Request with:

- Title referencing the issue
- Description linking the spec
- Summary of changes
- Verification results

The PR connects back to the spec issue, creating full traceability from intent → spec → implementation → review.

### 9. Clean Up

After completion, mill removes the worktree — clean slate. Your git log and GitHub PRs are the history.

## Structural Independence

The verifier is always a separate agent. This isn't a stylistic choice — it's the only way to get genuine review.

When you review your own work, you see what you *intended* to write, not what you *actually* wrote. A fresh pair of eyes catches what self-review misses: edge cases, inconsistencies, scope creep, subtle bugs.

mill enforces this structurally. The verifier has no access to the implementer's reasoning, planning, or intermediate thoughts. It only sees the spec and the resulting code.

## Autonomy and Judgment

Ship is autonomous. The spec is the complete instruction set — implementers don't have a line back to you during execution.

- **Implementation details** — decided by the implementer within spec boundaries
- **Scope creep** — the verifier catches anything that wasn't in the spec
- **Spec gaps** — if something blocks implementation, the lead escalates and the spec goes back to drafting

The rule is simple: honor the spec. Don't add what wasn't asked for. Don't skip what was specified. Build exactly what was contracted.

## Observations During Ship

While implementing, mill observes:

- Missing test coverage in existing code
- Undocumented APIs being used
- Code patterns not tracked in ground
- Dependencies not in the stack inventory

These observations are written to `.mill/observations/` without interrupting the flow. They'll be reviewed later in the ground review cycle.

## When Things Go Wrong

### Tests fail

Implementers receive test failures and address them as part of their task. Each implementer commits after getting tests to pass.

### Verification rejects

The verifier found issues. The lead routes rejection feedback to the responsible implementer(s) for another round. Maximum 3 cycles.

### Spec has gaps

If the spec is missing information that blocks implementation, the lead escalates to you with a clear description of what's needed. The spec goes back to the drafting stage.

### Fallback mode

If agent teams aren't available (experimental feature disabled), ship falls back to single-session mode: the lead implements directly, then does an explicit self-review phase against the spec. Degraded but functional.

