---
title: "Principles"
number: 2
subtitle: "The rules that make it work"
accent: "ember"
---

## Specs Drive Execution

In mill, the specification is law. Not a suggestion, not a rough outline — a precise contract that defines what gets built.

A good spec answers every question an implementer could ask. If someone unfamiliar with your system can execute the spec without asking for clarification, it's ready. If they can't, it's still a draft.

This isn't bureaucracy. This is precision. The time you invest in a clear spec pays back tenfold in implementation that doesn't need rework.

```
Requirements (R)     → what the solution must achieve
        ↓ implemented by
Approach (A)         → how we'll build it
        ↓ verified by
Criteria (C)         → testable conditions that prove it works
```

Every requirement has an approach. Every approach has criteria. The coverage matrix proves the chain is complete. No gaps. No handwaving.

## Contracts Over Conversation

"Done" is not a status. It's a verifiable state.

In mill, nothing ships without passing its criteria. Not because someone said "looks good" — because automated tests confirmed it meets the specification. This is what separates a delivery system from a project manager.

Think of it like this:

| Old Way | mill Way |
|---------|----------|
| "I think it's done" | Tests pass against criteria |
| "Looks good to me" | Coverage matrix shows completeness |
| "Should be fine" | Verification loop confirms behavior |

## Bounded Iterations

mill works in slices. Each slice addresses one concern — a model change, a logic implementation, an interface update, a test suite. After each slice, you verify.

This prevents the most common failure mode in software: the big-bang merge. When you implement everything at once and test at the end, failures compound. When you implement in slices and test after each, failures are isolated and obvious.

**The loop:**

1. Read the plan
2. Implement one slice
3. Run verification
4. Signal: continue, verify, or abort
5. Repeat

Maximum 20 iterations by default. If you can't ship in 20 slices, the spec needs refinement.

## Skills + CLI

mill has two halves, each doing what it does best:

**Skills** (the LLM side) handle intelligence — asking questions, drafting specs, making implementation decisions, observing patterns. They understand context and nuance.

**CLI** (the structured side) handles data — reading files, writing JSON, managing drafts, publishing issues, tracking history. It's deterministic and reliable.

This separation is deliberate. You want your AI to be creative and intelligent. You want your data layer to be predictable and exact. Mixing them creates tools that are neither.

## Humans Drive Direction

mill will never decide *what* to build. It will help you think through what you want, challenge your assumptions, find gaps in your reasoning. But the decisions are yours.

This isn't a limitation — it's the design. AI is extraordinary at processing information, maintaining consistency, and handling tedious precision work. Humans are extraordinary at judgment, taste, and knowing what matters.

mill puts each where they excel.

> The best systems don't replace human judgment. They give it better inputs and handle the follow-through.

## Self-Containment

This principle deserves special attention because it's the most violated in practice.

A specification is self-contained when every statement in it can be executed by someone unfamiliar with the system, without querying the author.

| Fails Self-Containment | Passes Self-Containment |
|------------------------|------------------------|
| "Configure the stream endpoint" | "Set `RTMP_INGEST=rtmp://ingest.example.com:1935/live` in `encoder/.env`" |
| "Use the appropriate codec" | "Encode with H.264 Main Profile, 1080p@30fps, 4500kbps CBR" |
| "Handle errors gracefully" | "On failure: retry 3x with exponential backoff, then emit `stream.failed` event" |

If a reader has to ask "which endpoint?" or "what codec?" — your spec has failed. mill's spec workflow is designed to catch these failures before they become implementation confusion.

## Decision Completeness

No TBDs. No "it depends." Every parameter bound to a concrete value.

When a decision genuinely depends on a condition, the spec uses the format:

```
default VALUE unless PREDICATE → ALTERNATIVE
```

This forces you to think through the branches *before* implementation, not during. The implementer never has to guess.

## The Sum of the Parts

These principles aren't arbitrary rules. They form an interlocking system:

- **Specs drive execution** tells you *what* to write
- **Self-containment** tells you *how well* to write it
- **Decision completeness** tells you *how thoroughly* to write it
- **Bounded iterations** tells you *how* to implement it
- **Contracts over conversation** tells you *when it's done*
- **Humans drive direction** tells you *who decides*

Together, they create a delivery system where intent flows cleanly into outcome. No loss in translation. No drift. No surprises.
