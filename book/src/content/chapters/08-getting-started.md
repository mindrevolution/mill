---
title: "Getting Started"
chapter: 8
part: "Practice"
partNumber: 3
description: "Installation, setup, and your first delivery cycle"
slug: "getting-started"
---

Getting mill running takes about two minutes. Here's everything you need.

## Prerequisites

Before installing mill, make sure you have:

- **Claude Code** (1.0.33 or later) — mill's skills run inside Claude Code
- **GitHub CLI** (`gh`) — authenticated with your GitHub account
- **Git** — your project must be a Git repository hosted on GitHub
- **Node.js** — for the MCP installer

## Install

The fastest way to install mill is via the Claude Code plugin:

```bash
/plugin marketplace add mindrevolution/mill-plugin
/plugin install mill@mindrevolution-mill-plugin
```

The CLI is automatically installed when the plugin is first enabled.

If you prefer a standalone CLI installation:

```bash
# macOS / Linux
curl -fsSL https://raw.githubusercontent.com/mindrevolution/mill/main/install.sh | bash

# Windows
irm https://raw.githubusercontent.com/mindrevolution/mill/main/install.ps1 | iex
```

## Initialize Your Project

Navigate to your project directory and run:

```bash
mill init
```

This creates the `.mill/` directory with the project configuration. You'll be asked a few questions about your project — name, description, and repository URL.

## First Run Permissions

The first time you use mill skills in Claude Code, you'll be prompted to approve `mill` and `git` commands. Select **"Yes, and don't ask again"** to grant permanent permission for the project. After this one-time approval, all skills run without interruption.

## Your First Cycle

Here's a complete delivery cycle to get familiar with mill:

### 1. Orient yourself

```
/mill:warmup
```

This generates a project context file that helps all subsequent skills understand your codebase.

### 2. Capture an idea

```
/mill:idea
```

Describe something small — a bug fix, a minor feature, a refactoring. Keep it focused for your first run.

### 3. Write a specification

```
/mill:spec
```

mill will find your idea and help you refine it into a specification with requirements, approach, and criteria. Review each section carefully. Publish it as a GitHub Issue when you're satisfied.

### 4. Ship it

```
/mill:ship <issue-number>
```

Watch mill implement your specification through bounded verification loops. When all criteria pass, review the Pull Request it creates.

### 5. Build knowledge

```
/mill:ground
```

Review the observations that spec and ship wrote during their execution. Curate the useful ones into your project's ground knowledge.

That's the full cycle. Each subsequent cycle gets faster as ground knowledge accumulates and the system learns your project's patterns.
