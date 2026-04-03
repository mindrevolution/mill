---
category: vocabulary
id: delivery
---

# Delivery Vocabulary

## Spec

A complete, loop-ready specification that drives execution. Starts as a **Draft** during elicitation, then gets **published** to a GitHub Issue (single source of truth). Every spec includes a Loop Contract and acceptance criteria.

The spec is what the work prompt implements and what the verify prompt reviews against. It's the model — not conversation — that drives delivery.

## Draft

A spec in progress, stored locally at `.mill/shape/drafts/{slug}.md`. Drafts are gitignored (local WIP) and only the author can see them. A draft becomes a spec when published to GitHub Issues via `/mill:spec`.

Drafts go through interactive elicitation: classify intent → gather requirements → challenge assumptions → generate spec → user approval → publish.

## Idea

A rough idea captured via `/mill:idea`. Ideas have a 30-day lifecycle across three stages:

- **Spark** — initial capture, just the intent
- **Develop** — dialogue-based development, adding context and validation
- **Ready** — mature enough to promote to a spec draft

Ideas that aren't promoted decay and get dropped. Dropped ideas have their essence preserved — a searchable log of "whys that didn't survive," not a backlog to manage.

## Loop Contract

The verifiable contract embedded in every spec:

```markdown
## Loop Contract
- Success Criteria: <machine-checkable conditions>
- Test Command: <must pass before PR>
- Stop Conditions: <max iterations, e.g. 5>
```

The contract decides completion, not the agent. No spec ships without its contract being satisfied. Specs missing a parseable Loop Contract skip verification with a warning.

## Run

A bounded execution of a spec in Ship. A run:
1. Creates an isolated worktree branch (`issue-{N}`)
2. Iterates through work→verify cycles with agent teams
3. Stops when verification passes (PR created) or max iterations hit (aborted)

Each run is tracked in `.mill/ship/history.json` with outcome, iterations, duration, and git user.

## Iteration

One work→verify cycle within a run. Each iteration:
- **Work prompt** (`loop-iterate.md`) implements ONE slice of the spec, commits, and signals
- **Verify prompt** (`loop-verify.md`) reviews as a principal engineer — runs tests, checks criteria

Signals flow: `MILL_CONTINUE` (more slices) → `MILL_VERIFY` (ready for review) → `MILL_DONE` (approved) or `MILL_REJECTED` (try again).

## Slice

A single concern addressed in one iteration. Work is sliced by concern boundaries:
- **Model** — data structures, types, schemas
- **Logic** — business rules, algorithms
- **Interface** — API, CLI, UI changes
- **Integration** — wiring components together
- **Tests** — test coverage
- **Config/Docs** — configuration, documentation

Most specs touch 2+ concerns. Each iteration implements ONE slice only. The slice plan lives in `.mill/memory/issue-{N}-plan.md`.

## Observation

An AI-generated learning surfaced during a completed ship run. Extracted by the observation prompt (`run-observations.md`) which analyzes the spec, run outcome, and iteration history.

Observations are **pending** until a human reviews them via `/mill:ground`. They can be:
- **Curated** into ground knowledge (personas, rules, vocabulary, etc.)
- **Dismissed** (not actionable)
- **Banned** (never suggest again)

This is the learning loop: Ship → Observations → Ground → better specs → better shipping.

## Kickstart

Bootstrap initial ground files for a new project. Uses archetype templates (SaaS, marketplace, API platform, etc.) and stack templates (React+Node, Django+Postgres, etc.) as starting points, then an LLM generates tailored ground files.

## Autopick

Intelligent issue selection for Ship. Scores open issues by:
- **Type priority** — security (400) > bug (300) > feature (200) > task (100)
- **Impact** — critical (100) > high (75) > normal (50) > low (0)
- **Age factor** — older issues score slightly higher (configurable)
- **Health gates** — CI must be green, WIP must be under limit

Blocked by: CI failure on main (if `blockOnCiFailure` is true), too many in-progress issues.

## Signals

Machine-readable signals that flow between work and verify prompts:

| Signal | Emitted By | Meaning |
|--------|-----------|---------|
| `MILL_CONTINUE` | Work prompt | More slices remain, continue iterating |
| `MILL_VERIFY` | Work prompt | Final slice complete, ready for verification |
| `MILL_DONE` | Verify prompt | All criteria met, create PR |
| `MILL_REJECTED` | Verify prompt | Criteria not met, iterate again with feedback |
| `MILL_ABORT` | Work prompt | Spec is invalid or impossible to implement |
