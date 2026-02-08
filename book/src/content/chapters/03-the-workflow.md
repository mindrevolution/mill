---
title: "The Workflow"
chapter: 3
part: "Process"
partNumber: 2
description: "From raw intent to verified deliverables in four stages"
slug: "the-workflow"
---

mill organizes work into four stages. Each stage transforms your intent into something more concrete, more verified, more useful. Together, they form a continuous loop that gets smarter with every cycle.

## The Four Stages

**Ground** is your product's knowledge base. It holds everything the system needs to understand about your project — who your users are, what conventions you follow, what technology you use, what decisions you've made and why. Ground is the context that makes specifications precise and implementations correct.

**Idea** is the intake. When you have a thought — a feature, a fix, a refactoring — you capture it as an idea. Ideas are deliberately lightweight. They have a 30-day time-box: if an idea doesn't graduate to a specification within 30 days, it drops. This prevents the backlog from becoming a graveyard.

**Spec** is the specification stage. Here, a rough idea gets refined into a precise, complete specification with requirements, an approach, and testable criteria. The output is a GitHub Issue — the single source of truth for what needs to be built.

**Ship** is the implementation stage. It takes a specification and turns it into code through bounded iteration loops. Implement, verify against criteria, adjust. When all criteria pass, a Pull Request is created for human review.

## How They Connect

The stages aren't independent — they form a knowledge cycle:

```
Ground ─── informs ───→ Idea
  ↑                       │
  │                       ↓
  │        validates ──── Spec
  │                       │
  │                       ↓
  └──── learnings ─────── Ship
```

Ground *informs* your ideas by providing context about what exists and what's needed. It *validates* your specs by checking them against established patterns, constraints, and vocabulary. And after shipping, *learnings* flow back into Ground — new patterns discovered, decisions made, debt accumulated.

This feedback loop is what sets mill apart from a simple task runner. The system accumulates knowledge. Each cycle makes the next one better.

## Observations: The Learning Inbox

As skills execute, they write **observations** — things they notice during the work. An observation might be:

- **An extraction**: "This project uses NextAuth for authentication"
- **A discovery**: "There's a user persona not documented in Ground"
- **A concern**: "The API endpoint has no rate limiting"
- **A suggestion**: "These three components share duplicated logic"

Observations are raw, unfiltered notes. They accumulate in an inbox. When you run Ground, you review and curate them — accepting insights into your product knowledge, dismissing false positives, and acting on concerns.

This is how mill learns without hallucinating. Observations are always reviewed by a human before they become part of the system's knowledge.

## Entry Points

You don't have to use every stage every time. Common entry points:

- **Start from an idea**: `/mill:idea` → `/mill:spec` → `/mill:ship`
- **Start from a spec**: Write the issue manually → `/mill:ship`
- **Build knowledge**: `/mill:ground` (review observations, add context)
- **Quick implementation**: `/mill:ship #42` (jump straight to shipping an existing issue)

The stages are designed to be composable, not ceremonial. Use what you need.
