---
title: "Principles"
number: 2
subtitle: "The rules that make it work"
accent: "ember"
---

## Specs Drive Execution

The specification is law. Not a suggestion, not a rough outline — a precise contract that defines what gets built.

A good spec answers every question an implementer could ask. If someone unfamiliar with your system can execute the spec without asking for clarification, it's ready. If they can't, it's still a draft.

Every spec links three layers:

```
Requirements (R)     → what the solution must achieve
        ↓ implemented by
Approach (A)         → how we'll build it
        ↓ verified by
Criteria (C)         → testable conditions that prove it works
```

Here's what that looks like in practice:

```
R1: Failed API calls retry with backoff before surfacing an error.

A1: Add RetryHandler middleware that wraps HTTP calls with
    exponential backoff (base 200ms, max 3 retries, jitter ±50ms).

C1: Given a request that fails twice then succeeds on the third attempt,
    the response returns successfully with status 200.
C2: Given a request that fails 4 consecutive times,
    the caller receives a 503 after ~1.4s total elapsed time (±200ms).
```

Every requirement has an approach. Every approach has criteria. The **coverage matrix** proves the chain is complete.

## Contracts Over Conversation

"Done" is not a status. It's a verifiable state.

Nothing ships without passing its criteria — not because someone said "looks good," but because an independent verifier confirmed it meets the specification.

Here's what verification looks like:

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

The verifier sees only the spec and the code diff. It has no access to the implementer's reasoning or intent. Pass or fail, nothing in between.

## Team-Based Delivery

mill assembles a team for every ship run. The skill session becomes the **lead** — it orchestrates but never implements directly.

| Role | Purpose |
|------|---------|
| **Lead** | Reads the spec, determines team size, delegates work, manages contracts |
| **Implementer(s)** | 1-4 agents with explicit file ownership boundaries |
| **Verifier** | Separate agent with clean context — reviews the full changeset |

When verification rejects, the lead routes specific feedback to the responsible implementer. Fix, re-verify, repeat — governed by the spec's Loop Contract (default 5 cycles) before escalating to you.

> **Why a separate verifier?** — You proofread your own writing and miss the typos every time. The same applies to code. A verifier with no knowledge of the implementer's intent catches what self-review misses.

## Continuous Learning

Every skill writes **observations** during execution — patterns discovered, gaps noticed, conventions found. These land in a learning inbox that you review and curate into permanent knowledge.

Observations reach ground truth through three paths:

- `/mill:ground` — dedicated review sessions where you route observations to knowledge categories
- `/mill:spec` — nudges you when observations are pending before drafting
- `/mill:ship` — auto-tags learnings with routing suggestions after each run

The human always makes the final call on what becomes permanent knowledge.

## Humans Drive Direction

mill will never decide *what* to build. It helps you think through what you want, challenges assumptions, and finds gaps. But the decisions are yours.

```
mill can tell you:
  "Your retry spec is missing a jitter parameter.
   Without jitter, all clients retry simultaneously after an outage."

mill cannot tell you:
  "Retry is the right strategy here."
  That's your call. Maybe the answer is a circuit breaker. Maybe it's fail-fast.
```

AI is extraordinary at processing information, maintaining consistency, and handling tedious precision work. Humans are extraordinary at judgment, taste, and knowing what matters. mill puts each where they excel.

## Self-Containment

A specification is **self-contained** when every statement in it can be executed by someone unfamiliar with the system, without querying the author.

| Fails Self-Containment | Passes Self-Containment |
|------------------------|------------------------|
| "Configure the stream endpoint" | "Set `RTMP_INGEST=rtmp://ingest.example.com:1935/live` in `encoder/.env`" |
| "Use the appropriate codec" | "Encode with H.264 Main Profile, 1080p@30fps, 4500kbps CBR" |
| "Handle errors gracefully" | "On stream failure: retry 3x with exponential backoff, then emit `stream.failed` with payload `{streamId, error, timestamp}`" |

If a reader has to ask "which endpoint?" or "what codec?" — the spec has failed. mill's spec workflow catches these before they become implementation confusion.

## Decision Completeness

No TBDs. No "it depends." Every parameter bound to a concrete value.

When a decision genuinely depends on a condition, the spec uses the format:

```
default VALUE unless PREDICATE → ALTERNATIVE
```

For example:

```
Retry count: default 3 unless network is cellular → 5
Cache TTL: default 60s unless user is admin → 0 (no cache)
```

This forces you to think through the branches *before* implementation. The implementer never has to guess.

## The Sum of the Parts

These principles form an interlocking system:

- **Specs drive execution** tells you *what* to write
- **Self-containment** tells you *how well* to write it
- **Decision completeness** tells you *how thoroughly* to write it
- **Team-based delivery** tells you *how* to implement it
- **Contracts over conversation** tells you *when it's done*
- **Continuous learning** tells you *what to carry forward*
- **Humans drive direction** tells you *who decides*

Together, they create a delivery system where intent flows cleanly into outcome.
