---
title: "Getting Started"
number: 8
subtitle: "From zero to first ship"
accent: "electric"
---

## Requirements

Before you begin, make sure you have:

- **Claude Code** — mill is a Claude Code skill pack
- **GitHub CLI (`gh`)** — authenticated with your GitHub account
- **Git** — your project must be a Git repository on GitHub

## Install

Open Claude Code and run:

```
/install mill@mindrevolution
```

That's it. The skills and templates are ready to use.

## Initialize Your Project

In your project directory, run the init skill:

```
/mill:init
```

This creates the `.mill/` directory with the default structure:

```
.mill/
├── project.json          # Configuration
├── ground/               # Knowledge base (10 categories)
├── idea/active/          # Active ideas
├── spec/drafts/          # Spec drafts
├── templates/            # Copied from plugin
└── ship/                 # Work and history
```

## First Run Permissions

The first time you use mill skills, Claude Code will ask to approve certain commands (`gh`, `git`, `rm`, `mkdir`). Select **"Yes, and don't ask again"** to approve them permanently for the project.

## Context Is Automatic

When any skill needs project context, mill checks `.mill/context.md` and regenerates it automatically if it's missing or stale (default threshold: 25 commits).

If you ever want to force a refresh, you can run `/mill:warmup` — but you'll rarely need to.

## Build Your Ground

Start with the essentials:

```
/mill:ground
```

mill will ask what you want to work on. Start with:

1. **Personas** — who are your primary users?
2. **Rules** — what conventions does your team follow?
3. **Stack** — what technologies do you use?

You don't need all ten categories right away. Ground grows organically as you ship.

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

mill assembles a team — a lead orchestrates, implementers build within assigned file boundaries, and a separate verifier checks every criterion independently. When verification passes, a PR is created. One skill, full pipeline.

## The Daily Rhythm

Once mill is set up, here's what a typical workflow looks like:

### Morning

```
/mill:ground
→ Review observations from yesterday's ship runs
→ Curate new knowledge into ground
```

### During the Day

```
/mill:idea "Quick thought about caching"
→ Capture it in 30 seconds, develop later
```

### When Ready to Build

```
/mill:spec "Add response caching to API"
→ 10-minute conversation to produce a complete spec
→ Published as GitHub Issue #55
```

```
/mill:ship 55
→ Team assembled: 1 implementer + 1 verifier
→ Independent verification passed
→ PR #56 created, ready for review
```

### Weekly

```
/mill:idea
→ Review active ideas
→ Promote the ready ones, drop the stale ones
```

## Skills Reference

| Skill | Purpose |
|-------|---------|
| `/mill:init` | Initialize `.mill/` in your project |
| `/mill:ground` | Build and curate product knowledge |
| `/mill:idea` | Capture ideas with 30-day lifecycle |
| `/mill:spec` | Draft specs, publish as GitHub Issues |
| `/mill:ship` | Assemble team, implement, verify → PR |
| `/mill:warmup` | Orient Claude to your codebase |

## Tips for Success

**Invest in ground early.** The richer your knowledge base, the better your specs and implementations. Even 30 minutes of initial setup pays back immediately.

**Write precise specs.** The time you spend making a spec self-contained is time you don't spend debugging misimplementations.

**Review observations.** This is how mill learns. Make it a regular habit, not an afterthought.

**Trust the process.** Team-based delivery with independent verification feels slower on the first run. By the third run, you'll wonder how you shipped without it.

**Start small.** Pick a straightforward feature for your first ship run. Build confidence with the workflow before tackling complex specs.

## What's Next?

You've learned the full mill workflow. Here's your path forward:

1. **Install and initialize** — get set up
2. **Build ground** — start with personas and rules
3. **Draft a spec** — turn an idea into a contract
4. **Ship it** — run the full cycle and get a PR
5. **Review and learn** — check observations, refine ground

Each cycle makes the next one sharper. That's the promise. Now go build something great.
