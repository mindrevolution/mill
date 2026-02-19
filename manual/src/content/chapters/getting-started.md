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
/plugin marketplace add mindrevolution/mill
/plugin install mill@mindrevolution
```

That's it. The skills are ready to use.

## Initialize Your Project

In your project directory, run the init skill:

```
/mill:init
```

This creates the `.mill/` directory with the default structure:

```
.mill/
├── ground/               # Knowledge base (10 categories)
├── idea/active/          # Active ideas
├── spec/drafts/          # Spec drafts
└── ship/                 # Worktrees for implementation
```

## Project Context Is Automatic

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

mill [assembles a team](/ship) — a lead orchestrates, implementers build within assigned file boundaries, and a separate verifier checks every criterion independently. When verification passes, a PR is created. One skill, full pipeline.

## The Daily Rhythm

Once mill is set up, the workflow is simple:

- **Start of day:** `/mill:ground` — review observations from yesterday's ship runs, curate into knowledge
- **Anytime:** `/mill:idea "Quick thought"` — capture in 30 seconds, develop later
- **Ready to build:** `/mill:spec` → `/mill:ship 55` — spec to PR in one flow
- **Weekly:** `/mill:idea` — review active ideas, promote or drop the stale ones

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

Install → init → ground → spec → ship → review observations → repeat. Each cycle makes the next one sharper. Now go build something great.
