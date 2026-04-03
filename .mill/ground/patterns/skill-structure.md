---
category: patterns
id: skill-structure
---

# Skill Structure Pattern

Skills follow a consistent structure:

```
1. Entry point    — check state, route to the right flow
2. Context load   — read .mill/context.md, ground files, relevant codebase
3. Interaction    — AskUserQuestion loops (elicit, classify, confirm)
4. Execution      — Read/Write/Glob/Grep/Bash to produce artifacts
5. Observations   — write discoveries to .mill/observations/
6. Report         — confirm what was done
```

### State Checking

Skills check preconditions before starting:
- `/mill:warmup` checks context freshness (hash comparison)
- `/mill:spec` checks for existing drafts (resume vs. new)
- `/mill:ground` checks for pending observations
- `/mill:ship` loads spec from GitHub Issue, parses Loop Contract
- `/mill:idea` checks for active ideas (resume vs. new)

### File Operations

All I/O uses Claude Code tools directly:
- `Glob` for discovery (list drafts, observations, ground files)
- `Read` for loading content
- `Write` for creating/updating files
- `Bash` for `gh` CLI (GitHub) and `git` commands
- `rm` for cleanup after promotion/dismissal
