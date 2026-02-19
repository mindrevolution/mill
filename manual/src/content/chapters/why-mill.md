---
title: "Why mill"
number: 1
subtitle: "What it does and how it thinks"
accent: "flame"
---

## The Problem

You describe what you want. What gets built is close — but not quite right.

```
You: "Add a retry mechanism for failed API calls"

What you meant:
  Retry 3x with exponential backoff, circuit-break after 5 failures,
  log each attempt, emit a metric on final failure.

What got built:
  A while loop that retries forever with no backoff.
```

The gap between what you *meant* and what got *built* is where quality dies. Most tools try to make building faster. mill focuses on making building **accurate**.

## How mill Solves This

mill turns intent into a spec, then executes the spec with a team of agents:

```
/mill:idea   "Retry mechanism for API calls"        → capture the spark
/mill:spec   "Retry with backoff and circuit-break"  → precise contract
/mill:ship 47                                        → team implements + verifies
                                                     → PR ready for review
```

Before anything gets built, mill helps you think through what you actually want — challenging assumptions, finding gaps, asking the questions you'd skip. Once your intent is precise, it assembles a team: a lead orchestrates, implementers build within file boundaries, and a separate verifier checks every criterion independently.

## The Core Idea: Specs as Contracts

The key insight is simple: if you can describe *exactly* what you want, verify it was built correctly, and prove the chain is complete — most implementation problems disappear.

Every mill spec links three layers:

```
Requirements (R)     → what the solution must achieve
        ↓ implemented by
Approach (A)         → how we'll build it
        ↓ verified by
Criteria (C)         → testable conditions that prove it works
```

Here's what that looks like:

```
R1: Failed API calls retry with backoff before surfacing an error.

A1: Add RetryHandler middleware that wraps HTTP calls with
    exponential backoff (base 200ms, max 3 retries, jitter ±50ms).

C1: Given a request that fails twice then succeeds on the third attempt,
    the response returns successfully with status 200.
C2: Given a request that fails 4 consecutive times,
    the caller receives a 503 after ~1.4s total elapsed time (±200ms).
```

Every requirement has an approach. Every approach has criteria. A **coverage matrix** proves the chain is complete — no gaps, no untested requirements.

The [spec](/spec) chapter covers how to write these in detail.

## Independent Verification

You proofread your own writing and miss the typos every time. The same applies to code.

mill enforces this structurally. After implementers finish, a separate **verifier** agent reviews the full changeset. The verifier has no access to the implementer's reasoning — it sees only the spec and the resulting code. Pass or fail, nothing in between.

```
Verifier checks C1:
  → Simulates two failures, then success
  → Confirms status 200 returned
  → PASS

Verifier checks C2:
  → Simulates four consecutive failures
  → Measures elapsed time: 1.35s
  → Confirms 503 returned
  → PASS
```

The [ship](/ship) chapter covers the full team model: lead, implementers, and verifier.

## Knowledge Compounds

Every cycle, mill writes **observations** — patterns discovered, gaps noticed, conventions found. These land in a learning inbox. You review them, decide what matters, and curate it into permanent project knowledge.

Next cycle, specs are more precise because the knowledge base is richer. Implementations are more accurate because mill knows your stack, your patterns, your rules.

> **What mill is NOT** — not a code generator, not a project manager. It's a delivery system that uses specification as the interface between intent and implementation. You decide what to build. mill handles the precision.

## Who It's For

If you've ever:

- Rewritten a feature because the requirements were ambiguous
- Merged a PR that broke something nobody tested
- Lost architectural decisions to the fog of Slack threads
- Wished your AI assistant understood your *project*, not just your prompt

...then mill was built for you.
