---
title: "Why mill"
number: 1
subtitle: "The gap between intent and outcome"
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

## How mill Works

mill turns intent into a spec, then executes the spec with a team of agents:

```
/mill:idea   "Retry mechanism for API calls"     → capture the spark
/mill:spec   "Retry with backoff and circuit-break" → precise contract
/mill:ship 47                                     → team implements + verifies
                                                  → PR ready for review
```

Before anything is written, mill helps you think through what you actually want — challenging assumptions, finding gaps, asking the questions you'd skip. Once your intent is precise, it assembles a team: a lead orchestrates, implementers build within file boundaries, and a separate verifier checks every criterion independently.

Every cycle, mill observes what it learned — patterns, gaps, terminology — and feeds it back into your project's knowledge base.

> **What mill is NOT** — not a code generator, not a project manager. It's a delivery system that uses specification as the interface between intent and implementation.

## Who It's For

If you've ever:

- Rewritten a feature because the requirements were ambiguous
- Merged a PR that broke something nobody tested
- Lost architectural decisions to the fog of Slack threads
- Wished your AI assistant understood your *project*, not just your prompt

...then mill was built for you.

## Five Principles

1. **Specs drive execution** — the spec is the complete instruction set, not a rough outline
2. **Contracts over conversation** — an independent verifier confirms criteria, not "looks good"
3. **Team-based delivery** — a lead orchestrates, implementers build, a verifier checks
4. **Continuous learning** — every cycle feeds observations back into project knowledge
5. **Humans drive direction** — mill handles precision, you make the decisions
