---
title: "Principles"
chapter: 2
part: "Philosophy"
partNumber: 1
description: "The five beliefs that shape how mill works"
slug: "principles"
---

mill is opinionated. It makes deliberate choices about how software should be built with AI assistance. These aren't arbitrary constraints — they're hard-won lessons from watching AI-assisted projects succeed and fail.

## Specs Drive Execution

Every piece of work in mill starts with a specification. Not a task description. Not a Jira ticket with a one-line summary. A *specification* — with requirements, an approach, and testable criteria.

This isn't paperwork. It's the single source of truth that tells both humans and AI what "done" looks like. Without it, you're navigating without a map.

The specification lives as a GitHub Issue. It's versioned, commentable, and linked to the code that implements it. There's no separate documentation system to maintain. The spec *is* the documentation.

## Contracts Over Conversation

When you tell an AI to "make the button blue," you're having a conversation. When you write a criterion that says "the primary action button uses the brand color `#2563EB` with WCAG AA contrast ratio against its background," you're creating a contract.

Contracts are verifiable. Conversations are not.

mill pushes you to express your intent as contracts — testable conditions that can be checked mechanically. This isn't about mistrusting the AI. It's about creating a shared definition of success that doesn't depend on interpretation.

> A conversation can go anywhere. A contract goes exactly where you specified.

## Bounded Iterations

mill doesn't let work expand indefinitely. Every implementation runs in a bounded loop: implement, verify, adjust. If the verification passes, you're done. If it fails, you iterate — but only on what failed.

This prevents the two most common failure modes of AI-assisted development:

1. **Endless refinement** — the AI keeps "improving" things that were already correct, often introducing new bugs
2. **Scope creep** — without boundaries, a simple task balloons into a rewrite

Bounded iterations mean work has a clear start, clear checkpoints, and a clear end. The specification defines the boundaries. Verification enforces them.

## Skills + CLI

mill splits its intelligence between two components:

**Skills** are LLM-powered workflows. They handle the fuzzy, creative, context-dependent work — understanding your intent, drafting specifications, making implementation decisions, writing observations.

**The CLI** handles structured data operations. It reads and writes files, manages the project state, integrates with GitHub, and provides the scaffolding that skills build on.

This separation is deliberate. LLMs are excellent at reasoning and generation. They're terrible at reliable data operations. By giving each component the work it's suited for, mill stays both intelligent and dependable.

## Humans Drive Direction

mill amplifies human judgment. It never replaces it.

You decide what to build. You review and approve specifications. You choose when to ship. The AI helps you think more clearly, implement more quickly, and verify more thoroughly — but the direction is always yours.

This isn't a philosophical stance about AI safety. It's practical: the humans closest to the problem have context that no model can fully absorb. mill's job is to make that human context actionable, not to bypass it.
