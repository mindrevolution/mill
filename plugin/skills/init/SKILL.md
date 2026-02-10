---
name: init
description: "Initialize mill in your project • https://mill.mindrevolution.com/init"
allowed-tools: Read, Write, Glob, Bash(*mkdir *, *git *)
---

# Init

Initialize `.mill/` in the current git repository. Creates directory structure, config files, and copies templates.

## Preconditions

- Current directory must be a git repository
- `.mill/` must not already exist (or prompt to reinitialize)

## Workflow

### 1. Check Existing

Check if `.mill/` already exists:

```
Glob(".mill/project.json")
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
      description: "Recreate config and update templates"
```

If "Skip" → stop. If "Reinitialize" → continue (preserve ground/ and observations/).

### 2. Create Directory Structure

Create all directories via Bash:

```bash
mkdir -p .mill/ground/strategic .mill/ground/personas .mill/ground/rules .mill/ground/decisions .mill/ground/vocabulary .mill/ground/stack .mill/ground/schema .mill/ground/design .mill/ground/patterns .mill/ground/debt .mill/observations .mill/idea/active .mill/spec/drafts .mill/ship/work .mill/templates
```

### 3. Write project.json

Write default configuration using the Write tool:

Path: `.mill/project.json`

```json
{
  "version": "1.0",
  "initialized": "{ISO_DATE}",
  "test_command": "",
  "default_branch": "main"
}
```

Set `initialized` to current ISO date. Detect default branch from git:

```bash
git symbolic-ref refs/remotes/origin/HEAD 2>/dev/null || echo "refs/remotes/origin/main"
```

Parse the branch name (last segment) and use it for `default_branch`.

### 4. Write .gitignore

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
```

### 5. Copy Templates

Find the plugin's templates directory by searching for a known marker file:

```
Glob("**/templates/domains/backend.md")
```

This finds the plugin installation path (e.g., `~/.claude/plugins/mill@mindrevolution/templates/`). The source is whichever match is NOT inside `.mill/`.

If no plugin path found, check if templates exist in the same repo (for mill developing itself):

```
Glob("plugin/templates/domains/backend.md")
```

Once the source templates directory is found, copy all template subdirectories:

For each subdirectory (`archetypes/`, `stacks/`, `specs/`, `domains/`, `teammates/`):
1. `Glob("{source}/archetypes/*.md")` → Read each → Write to `.mill/templates/archetypes/{name}.md`
2. `Glob("{source}/stacks/*.md")` → Read each → Write to `.mill/templates/stacks/{name}.md`
3. `Glob("{source}/specs/*.md")` → Read each → Write to `.mill/templates/specs/{name}.md`
4. `Glob("{source}/domains/*.md")` → Read each → Write to `.mill/templates/domains/{name}.md`
5. `Glob("{source}/teammates/*.md")` → Read each → Write to `.mill/templates/teammates/{name}.md`

Create template directories first:

```bash
mkdir -p .mill/templates/archetypes .mill/templates/stacks .mill/templates/specs .mill/templates/domains .mill/templates/teammates
```

### 7. Report

Confirm initialization:

```
Initialized .mill/ in {repo_name}

  ground/        10 categories (empty)
  observations/  learning inbox
  templates/     {N} templates copied

Next: /mill:warmup to orient, or /mill:ground to define your project
```
