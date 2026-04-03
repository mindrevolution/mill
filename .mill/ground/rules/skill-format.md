---
category: rules
id: skill-format
---

# SKILL.md Format

Every skill is a single markdown file at `plugin/skills/{name}/SKILL.md` with YAML frontmatter:

```yaml
---
description: "Short description • https://mill.mindrevolution.com/{name}"
disable-model-invocation: true
allowed-tools: Read, Write, Glob, Grep, Bash(*gh *, *git *)
argument-hint: "[arg] - what it means"
---
```

| Field | Required | Purpose |
|-------|----------|---------|
| `description` | Yes | One-liner shown in skill list. Include manual URL. |
| `disable-model-invocation` | Yes | Always `true` — skills use Claude Code's native tools, not model API calls |
| `allowed-tools` | Yes | Whitelist of Claude Code tools. Bash uses glob patterns for command restrictions. |
| `argument-hint` | No | Shown when user types the skill name — describes expected argument |

### Bash Tool Restrictions

Bash commands are restricted via glob patterns in `allowed-tools`:

```yaml
# Allow only gh and git commands
Bash(*gh *, *git *)

# Allow rm for cleanup
Bash(*rm *)

# Allow mkdir for directory creation
Bash(*mkdir *)

# Allow open/start/xdg-open for preview
Bash(*start *, *open *, *xdg-open *)
```

### Interaction Pattern

All skills use `AskUserQuestion` for structured input — never raw text questions. 2-4 options plus free text. One question at a time.
