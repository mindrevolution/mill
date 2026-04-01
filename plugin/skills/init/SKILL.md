---
description: "Initialize mill in your project • https://mill.mindrevolution.com/init"
disable-model-invocation: true
allowed-tools: Read, Write, Glob, Bash(*mkdir *, *git *)
---

# Init

Initialize `.mill/` in the current git repository. Creates directory structure and gitignore.

## Preconditions

- Current directory must be a git repository
- `.mill/` must not already exist (or prompt to reinitialize)

## Workflow

### 1. Check Existing

Check if `.mill/` already exists:

```
Glob(".mill/ground")
```

If it exists, ask:

```yaml
AskUserQuestion:
  question: "This project already has .mill/ initialized. Reinitialize?"
  header: "Exists"
  options:
    - label: "Skip"
      description: "Keep existing setup"
    - label: "Reinitialize"
      description: "Recreate config"
```

If "Skip" → stop. If "Reinitialize" → continue (preserve ground/ and observations/).

### 2. Create Directory Structure

Create all directories via Bash:

```bash
mkdir -p .mill/ground/strategic .mill/ground/personas .mill/ground/rules .mill/ground/decisions .mill/ground/vocabulary .mill/ground/stack .mill/ground/schema .mill/ground/design .mill/ground/patterns .mill/ground/debt .mill/observations .mill/idea/active .mill/spec/drafts .mill/ship/work
```

### 3. Write .gitignore

Write `.mill/.gitignore` using the Write tool:

```
# Learning inbox — local, reviewed via /mill:ground
observations/

# Ideas — personal WIP
idea/active/

# Spec drafts — local until published to GitHub Issues
spec/drafts/

# Ship worktrees — ephemeral execution sandboxes
ship/work/

# Prompt scratchpad
.prompt

# Preview render
.preview.html
```

### 4. Report

Confirm initialization:

```
Initialized .mill/ in {repo_name}

  ground/        10 categories (empty)
  observations/  learning inbox

Next: /mill:warmup to orient, or /mill:ground to define your project
```
