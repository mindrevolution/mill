---
title: "Getting Started"
number: 7
subtitle: "From zero to first ship"
accent: "electric"
---

## Requirements

Before you begin:

- **Claude Code** — mill is a Claude Code skill pack
- **GitHub CLI (`gh`)** — authenticated with your GitHub account
- **Git** — your project must be a Git repository on GitHub

> **Tip** — Run `gh auth status` to verify GitHub CLI is authenticated. If it says "not logged in," run `gh auth login` first.

## Install

Open Claude Code and run:

```
/plugin marketplace add mindrevolution/mill
/plugin install mill@mindrevolution
```

The skills are ready to use.

## Initialize Your Project

In your project directory:

```
/mill:init
```

This creates the `.mill/` directory — the local workspace where mill stores knowledge, ideas, spec drafts, and worktrees:

```
.mill/
├── context.md            # Auto-generated project context
├── ground/               # Knowledge base (10 categories)
├── idea/active/          # Active ideas
├── spec/drafts/          # Spec drafts before publishing
└── ship/work/            # Worktrees for implementation
```

## Project Context

When any skill needs project context, mill checks `.mill/context.md` and regenerates it if it's missing or stale (default: 25 commits since last generation).

Context includes your project's architecture, entry points, key dependencies, and recent changes. You can force a refresh with `/mill:warmup`, but you'll rarely need to.

## Build Your Ground

Start with the essentials:

```
/mill:ground
```

mill asks what you want to work on. Begin with:

1. **Personas** — who are your primary users?
2. **Rules** — what conventions does your team follow?
3. **Stack** — what technologies do you use?

> **Why these three first?** — Personas and rules inform specs most directly. They're what mill checks when validating requirements and flagging conflicts. Stack tells ship which implementation patterns to follow. The other seven categories grow naturally as you ship.

You don't need all ten categories right away.

## Capture an Idea

Have something you want to build? Capture it:

```
/mill:idea "Add user authentication"
```

mill asks a few questions about the type, the problem, and the scope. The idea lands in `.mill/idea/active/` with a 30-day clock.

## Draft a Spec

When an idea is ready (or if you want to go straight to spec):

```
/mill:spec "JWT-based authentication for API endpoints"
```

mill walks you through:

1. Type classification (feature, bug, task, security)
2. Domain selection (backend, application, etc.)
3. Requirements elicitation
4. Approach design
5. Criteria definition
6. Validation and publishing

The result: a GitHub Issue with a complete, self-contained specification.

## Ship It

Point ship at the issue:

```
/mill:ship 42
```

mill assembles a team — a lead orchestrates, implementers build within assigned file boundaries, and a separate verifier checks every criterion independently. When verification passes, a PR is created.

## The Daily Rhythm

Once mill is set up, a suggested workflow:

- **Start of day:** `/mill:ground` — review observations from yesterday's ship runs, curate into knowledge
- **Anytime:** `/mill:idea "Quick thought"` — capture in 30 seconds, develop later
- **Ready to build:** `/mill:spec` then `/mill:ship 55` — spec to PR in one flow
- **Weekly:** `/mill:idea` — review active ideas, promote or drop the stale ones

This is a suggested rhythm, not a requirement. Adapt it to how you work.

## Skills Reference

| Skill | Purpose |
|-------|---------|
| `/mill:init` | Initialize `.mill/` in your project |
| `/mill:ground` | Build and curate product knowledge |
| `/mill:idea` | Capture ideas with 30-day lifecycle |
| `/mill:spec` | Draft specs, publish as GitHub Issues |
| `/mill:ship` | Assemble team, implement, verify, create PR |
| `/mill:warmup` | Orient Claude to your codebase |

## Tips

> **Tip** — Invest in ground early. Even 30 minutes of initial personas and rules pays back on your first spec.

> **Tip** — Start small. Pick a straightforward feature for your first ship run. Build confidence with the workflow before tackling complex specs.

> **Common mistake** — Writing vague specs and expecting ship to fill in the blanks. The spec is the complete instruction set. Time spent on spec precision is time saved on implementation rework.

## What's Next

Install, init, ground, spec, ship, review observations, repeat. Each cycle makes the next one sharper.
