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
- **Node.js** — only needed for the plugin installer (auto-installs the CLI binary)

## Install

### Skills + CLI (Claude Code Plugin)

The complete package — skills, CLI, and MCP integration. Open Claude Code and run:

```
/plugin marketplace add mindrevolution/claude-plugins
/plugin install mill@mindrevolution
```

The CLI binary is automatically installed when you first enable the plugin. No manual setup needed.

### CLI Only

The CLI can run independently — without the Claude Code plugin. This is useful if you want to execute `mill ship` against specs that are already crafted as GitHub Issues, without the full skill pack.

```bash
# macOS / Linux
curl -fsSL https://mill.mindrevolution.com/install.sh | bash

# Windows
irm https://mill.mindrevolution.com/install.ps1 | iex
```

You get the execution engine (`mill ship`, `mill ground`, `mill history`, etc.) but not the conversational skills (`/mill:spec`, `/mill:idea`, `/mill:ground`) that help you author specs and manage knowledge interactively.

## Initialize Your Project

Navigate to your project directory and run:

```bash
mill init
```

This creates the `.mill/` directory with the default structure:

```
.mill/
├── project.json          # Configuration
├── ground/               # Knowledge base
├── idea/active/          # Active ideas
├── spec/drafts/          # Spec drafts
└── ship/                 # Work and history
```

## First Run Permissions

The first time you use mill skills in Claude Code, you'll be asked to approve certain commands. Select **"Yes, and don't ask again"** to approve them permanently for the project.

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

```bash
mill ship 42
```

The CLI takes over — it creates an isolated worktree, iterates through slices, runs independent verification, and creates a PR. One command, full pipeline. You can also start from the skill (`/mill:ship 42`) which handles pre-flight checks before delegating to the CLI.

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

```bash
mill ship 55
→ Worktree created, 4 slices implemented
→ Independent verification passed
→ PR #56 created, ready for review
```

### Weekly

```
mill idea list --human
→ Review active ideas
→ Promote the ready ones, drop the stale ones
```

## CLI Quick Reference

```bash
mill init                          # Initialize .mill/
mill ground list --human           # View knowledge base
mill ground list personas --human  # View specific category
mill observations list --human     # View learning inbox
mill idea list --human             # View active ideas
mill idea dropped --human          # View dropped ideas + learnings
mill draft list --human            # View spec drafts
mill history --human               # View ship history
mill context status                # Check context freshness
mill template list archetypes      # View available archetypes
```

## Tips for Success

**Invest in ground early.** The richer your knowledge base, the better your specs and implementations. Even 30 minutes of initial setup pays back immediately.

**Write precise specs.** The time you spend making a spec self-contained is time you don't spend debugging misimplementations.

**Review observations.** This is how mill learns. Make it a regular habit, not an afterthought.

**Trust the process.** Bounded iterations and verified slices feel slower on the first run. By the third run, you'll wonder how you shipped without them.

**Start small.** Pick a straightforward feature for your first ship run. Build confidence with the workflow before tackling complex specs.

## What's Next?

You've learned the full mill workflow. Here's your path forward:

1. **Install and initialize** — get the tools set up
2. **Build ground** — start with personas and rules
3. **Draft a spec** — turn an idea into a contract
4. **Ship it** — run the full cycle and get a PR
5. **Review and learn** — check observations, refine ground

Each cycle makes the next one sharper. That's the promise. Now go build something great.
