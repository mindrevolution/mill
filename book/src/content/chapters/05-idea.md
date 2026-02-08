---
title: "Idea"
chapter: 5
part: "Process"
partNumber: 2
description: "Capturing and time-boxing raw ideas before they become specs"
slug: "idea"
---

Ideas are the intake valve. They capture the raw, unrefined thoughts that eventually become specifications — or don't.

## Why Ideas Exist

Not every thought deserves a specification. Some ideas need time to mature. Others seem brilliant at 2 AM but irrelevant by morning. Ideas give you a lightweight holding pen where thoughts can sit without polluting your backlog.

The key design decision: **ideas expire**. Every idea has a 30-day time-box. If it doesn't graduate to a specification within 30 days, it drops automatically. This isn't a bug — it's pressure. If an idea is worth building, 30 days is enough time to refine it. If it's not, letting it go keeps your focus clean.

## Capturing Ideas

```bash
# Create a new idea interactively
mill idea create --human

# List active ideas
mill idea list --human

# View a specific idea
mill idea get my-feature-idea --human

# Drop an idea manually
mill idea drop my-feature-idea --human
```

The `/mill:idea` skill provides a conversational workflow. You describe what you're thinking, and it helps you capture the intent, the motivation, and any constraints — just enough structure to be useful later, without the overhead of a full specification.

## From Idea to Spec

When an idea is ready, you promote it to a specification:

```
/mill:idea       →  captures rough intent
/mill:spec       →  refines into precise specification
```

The spec skill reads the idea and uses it as a starting point. Your original intent is preserved and expanded into requirements, approach, and criteria.

## What Makes a Good Idea

An idea doesn't need to be complete. It needs three things:

1. **Intent** — What do you want to happen?
2. **Motivation** — Why does this matter?
3. **Context** — What's the situation that triggered this thought?

That's it. The specification stage handles the precision. The idea stage handles the spark.

> Backlogs don't die from neglect. They die from accumulation. The 30-day time-box keeps yours alive.
