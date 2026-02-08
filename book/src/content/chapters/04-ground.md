---
title: "Ground"
chapter: 4
part: "Process"
partNumber: 2
description: "Building and curating your product's knowledge base"
slug: "ground"
---

Ground is the foundation everything else builds on. It's your project's shared understanding — codified, versioned, and available to every skill that runs.

## What Ground Contains

Ground organizes knowledge into ten categories:

| Category | What It Holds |
|----------|---------------|
| **Strategic** | Vision, mission, goals — why this project exists |
| **Personas** | Who you build for — their needs, behaviors, context |
| **Rules** | Constraints and conventions — what you must and must not do |
| **Decisions** | Architectural decisions and their rationale |
| **Vocabulary** | Domain terminology — the language of your project |
| **Stack** | Technology choices and their configuration |
| **Schema** | Data structures and their relationships |
| **Design** | Visual language — colors, typography, components |
| **Patterns** | Code patterns and idioms — how things are done here |
| **Debt** | Technical debt — known problems and planned remediation |

You don't need to fill in everything upfront. Ground grows organically as you work. Each time a skill discovers something new, it writes an observation. Each time you run `/mill:ground`, you review those observations and curate them into lasting knowledge.

## Using Ground

```bash
# List all ground knowledge
mill ground list --human

# View a specific item
mill ground get personas/power-user --human

# Create new knowledge
mill ground create --human
```

The `/mill:ground` skill provides an interactive workflow: it reviews pending observations, suggests what to add or update, and asks you to confirm every change.

## The Curation Loop

Ground is never written by AI alone. The loop works like this:

1. Skills (spec, ship) write observations during execution
2. Observations accumulate in `.mill/observations/`
3. You run `/mill:ground` to review them
4. Good observations become ground knowledge
5. Knowledge improves the next spec and ship cycle

This human-in-the-loop design is deliberate. The AI notices things. You decide what matters.

> Ground is not documentation about your code. It's knowledge about your product.
