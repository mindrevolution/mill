---
title: "The Workflow"
number: 3
subtitle: "From intent to verified delivery"
accent: "gold"
---

## The Big Picture

mill's workflow is a cycle, not a pipeline. Knowledge feeds delivery. Delivery feeds knowledge. Each rotation makes the system sharper.

```
   Ground (knowledge)
     ↕         ↕
   Idea  →  Spec  →  Ship
                        ↓
                    learnings → Ground
```

Four skills, one cycle. Let's walk through the whole flow.

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

Ground isn't static. It grows with every cycle. When mill ships a feature, it observes what it learned and feeds it back for your review.

## Idea — The Spark

Not everything starts as a specification. Sometimes it's just a hunch. A thought in the shower. A feature request that *might* be worth pursuing.

**Idea** gives these sparks a home. Capture them quickly:

```
/mill:idea "Inline code review"
```

mill asks a few questions and saves the idea with a 30-day lifecycle. During that time, you develop it — add context, explore scope, answer open questions. When it's ready, it graduates to a spec. When it's not worth pursuing, you drop it — but you capture the *essence* of what you learned.

Nothing is wasted. Even dropped ideas contribute knowledge.

## Spec — The Contract

This is where intent becomes precision. A spec takes your idea and transforms it into an unambiguous contract:

- **Requirements** — what the solution must achieve
- **Approach** — how you'll build it, broken into parts
- **Criteria** — testable conditions that prove each requirement is met
- **Coverage** — the matrix proving requirements → approach → criteria

The spec workflow is conversational. mill asks you questions, one at a time, to tease out the details. It challenges ambiguity. It checks your spec against the principles (self-containment, decision completeness, language independence).

When the spec passes validation, it publishes as a GitHub Issue. That issue becomes the source of truth. No local files. No stale documents. One canonical location.

## Ship — The Execution

Ship takes a GitHub Issue and assembles a team of agents to implement it. One skill runs the full pipeline:

```
/mill:ship 47
```

**The team:**

1. **Lead** — reads the spec, determines team size, delegates work with explicit file ownership
2. **Implementer(s)** — 1-4 agents working within assigned boundaries, committing as they go
3. **Verifier** — a separate agent with clean context that reviews the full changeset against every criterion

**The process:**

1. **Isolate** — create a worktree on a dedicated branch (your main branch stays untouched)
2. **Load context** — the spec, your project's knowledge, domain guidance
3. **Delegate** — lead spawns implementers with specific task assignments and file boundaries
4. **Verify independently** — a separate agent checks the work (work can't grade its own homework)
5. **Ship** — create a Pull Request with full traceability
6. **Clean up** — remove the worktree

Ship is autonomous. The spec should be complete enough that implementation doesn't need human input. If the spec has gaps that block implementation, the lead escalates to you — the spec goes back to drafting.

During implementation, mill writes observations about what it discovered — patterns, gaps, concerns — which feed the learning loop.

## The Learning Loop

This is what makes mill different from every other tool.

Most systems are stateless. They forget everything between runs. mill remembers. Here's how:

During spec drafting, mill notices:
- Personas referenced that don't exist in ground
- Domain terms used without definitions
- Requirements that conflict with documented rules

During shipping, mill notices:
- Code patterns not documented in ground
- Dependencies not tracked in the stack
- Missing test coverage
- Undocumented APIs

These observations land in the **learning inbox** — a collection of markdown files in `.mill/observations/`. Each tagged with its type:

| Type | Meaning |
|------|---------|
| **extraction** | Auto-extracted from code |
| **discovery** | New information found |
| **concern** | Potential problem spotted |
| **suggestion** | Improvement idea |

When you run `/mill:ground`, you review these observations. Curate them into ground truth, create new specs from them, track them as debt, or dismiss them. The ones that matter become permanent knowledge that informs the next cycle.

## End to End

Here's a real example of the full cycle:

1. You have an idea: "Users should be able to export reports as PDF"
2. `/mill:idea` captures it with intent and persona
3. You develop the idea over a few days, answering open questions
4. `/mill:spec` transforms it into a spec with 4 requirements, 6 approach parts, and 8 criteria
5. The spec publishes as GitHub Issue #47
6. `/mill:ship 47` assembles a team — lead delegates to 2 implementers (backend + frontend), verifier checks independently
7. Verification passes — PR #48 is created, worktree cleaned up
8. During implementation, mill observed that "PDF export" isn't in the vocabulary. It also noticed the `ReportService` has no unit tests.
9. `/mill:ground` reviews these observations. You add "PDF export" to vocabulary and create a new idea for test coverage.
10. Next cycle starts sharper.

That's mill. Not a magic wand. A system that compounds. Every cycle, the specs are more precise. The implementations are more accurate. The knowledge is more complete.

And it's just getting started.
