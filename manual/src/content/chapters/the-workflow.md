---
title: "The Workflow"
number: 2
subtitle: "From intent to verified delivery"
accent: "gold"
---

## The Big Picture

mill's workflow is a cycle, not a pipeline. Knowledge feeds delivery. Delivery feeds knowledge.

```
   Ground (knowledge)
     ↕         ↕
   Idea  →  Spec  →  Ship
                        ↓
                    learnings → Ground
```

> **Why a cycle?** — Each rotation makes the system sharper. The spec you write after three ship runs is more precise than your first because ground knowledge has grown with every delivery.

## Ground — The Knowledge Layer

Before you build anything, you need to know your world. **Ground** is mill's shared product knowledge — everything the system knows about your project.

It's organized into ten categories:

| Category | What It Holds |
|----------|--------------|
| **strategic** | Vision, mission, goals |
| **personas** | Who you build for — real users, not abstractions |
| **rules** | Constraints and conventions your team follows |
| **decisions** | Why you chose X over Y — the reasoning, not just the result |
| **vocabulary** | Domain terms so everyone speaks the same language |
| **stack** | Technologies, frameworks, dependencies |
| **schema** | Data structures and relationships |
| **design** | Visual language — colors, typography, components |
| **patterns** | Code idioms your team uses |
| **debt** | Known issues and future work |

> **Tip** — Start with personas and rules. The other eight categories grow naturally as you ship.

Ground isn't static. When mill ships a feature, it observes what it learned and feeds it back for your review.

## Idea — The Spark

Not everything starts as a specification. Sometimes it's a hunch, a feature request, a thought mid-code-review.

**Idea** gives sparks a home with a 30-day lifecycle:

```
spark → developing → ready → spec (or drop with essence)
```

Capture fast (`/mill:idea "Inline code review"`), develop at your pace. During the **developing** stage, mill orients in your codebase — finding relevant files and patterns — then walks you through scope, approach, and open questions informed by what it found.

When an idea is ready, it graduates to a spec. When it's not worth pursuing, you drop it — but capture the *essence* of why, so future ideas can reference past learnings.

## Spec — The Contract

This is where intent becomes precision. A spec links three layers into a provable chain:

```
Requirements (R)     → what the solution must achieve
        ↓ implemented by
Approach (A)         → how we'll build it (parts + mechanisms)
        ↓ verified by
Criteria (C)         → testable conditions
```

The **coverage matrix** proves every core requirement has an approach implementing it and criteria verifying it. No gaps.

The spec workflow is conversational — mill asks one question at a time, challenges ambiguity, and [validates](/spec#validation) the result (self-containment, decision completeness, language independence). When the spec passes, it publishes as a GitHub Issue. That issue is the source of truth.

## Ship — The Execution

Ship takes a GitHub Issue and assembles a team to implement it:

```
/mill:ship 47
```

| Role | Count | Purpose |
|------|-------|---------|
| **Lead** | 1 | Reads spec, sizes team, delegates with file ownership boundaries |
| **Implementer(s)** | 1-4 | Build within assigned boundaries, commit as they go |
| **Verifier** | 1 | Separate agent, clean context, reviews full changeset against every criterion |

The process in four phases:

1. **Setup** — create an isolated worktree, load spec + domain guidance + ground knowledge (rules, patterns, decisions)
2. **Implement** — lead delegates to implementers with explicit file ownership and ground rules; after completion, each implementer gets a bounded **polish pass** scoped to their diff — simplify, reduce nesting, clean dead comments, check ground rules, review every criterion
3. **Verify** — an independent verifier checks the full diff against every criterion; rejects route back through the lead for iteration (governed by the spec's [Loop Contract](/spec#the-loop-contract), default 5 cycles)
4. **Finalize** — create a PR linking to the spec issue, extract process learnings, clean up the worktree

Ship is autonomous. If the spec has gaps that block implementation, the lead escalates — the spec goes back to drafting.

## The Learning Loop

During spec drafting, mill notices:
- Personas referenced that don't exist in ground
- Domain terms used without definitions
- Requirements that conflict with documented rules

During shipping, mill notices:
- Code patterns not documented in ground
- Dependencies not tracked in the stack
- Debugging insights and file couplings discovered through implementation

These observations land in the **learning inbox** (`.mill/observations/`), each tagged with a type:

| Type | Meaning |
|------|---------|
| **extraction** | Auto-extracted from code |
| **discovery** | New information found |
| **concern** | Potential problem spotted |
| **suggestion** | Improvement idea |
| **learning** | Process knowledge from ship (debugging insights, file couplings, workarounds) |

Observations reach ground truth through three paths:

- `/mill:ground` — dedicated review where you route each observation to a knowledge category, create a spec from it, add it to debt, or dismiss it
- `/mill:spec` — nudges you when observations are pending before drafting
- `/mill:ship` — auto-tags learnings with a `suggested:` routing hint (e.g., `suggested: ground/patterns/`)

> **Rule** — The human always decides what becomes permanent knowledge. mill suggests routing, you confirm or override.

## End to End

Here's a full cycle:

1. You have an idea: "Users should be able to export reports as PDF"
2. `/mill:idea` captures it with intent and persona
3. You develop the idea over a few days, answering open questions
4. `/mill:spec` transforms it into a spec with 4 requirements, 6 approach parts, and 8 criteria
5. The spec publishes as GitHub Issue #47
6. `/mill:ship 47` assembles a team — lead delegates to 2 implementers (backend + frontend), verifier checks independently
7. Each implementer runs a polish pass, then the verifier reviews
8. Verification passes — PR #48 is created, worktree cleaned up
9. The lead extracts process learnings: "The `ReportService` and `PdfRenderer` are tightly coupled — changes to one require changes to the other"
10. During implementation, mill also observed that "PDF export" isn't in the vocabulary
11. `/mill:ground` reviews these observations — you add "PDF export" to vocabulary, route the coupling insight to patterns, and create a new idea for `ReportService` test coverage
12. Next cycle starts sharper
