---
category: patterns
id: template-loading
---

# Template Loading Pattern

Skills load supporting templates at runtime via relative paths from the skill's location. Two uses:

### 1. Spec Templates (output format)

`plugin/skills/spec/templates/{type}.md` — loaded during spec drafting to structure the output. Referenced in SKILL.md via relative links:

```markdown
Spec templates by type:
- [templates/feature.md](templates/feature.md)
```

### 2. Ship Templates (agent instructions + domain guidance)

```
plugin/skills/ship/templates/
├── domains/           # Loaded based on spec's domain label
│   ├── backend.md
│   ├── application.md
│   ├── website.md
│   └── platform.md
└── teammates/         # Loaded when spawning agents
    ├── implementer.md
    └── verifier.md
```

Teammate templates use placeholder substitution:

| Placeholder | Filled by lead |
|-------------|---------------|
| `{{SPEC_CONTENT}}` | Full spec body |
| `{{CONTEXT}}` | .mill/context.md content |
| `{{DOMAIN_GUIDANCE}}` | Domain template content |
| `{{TASK_ASSIGNMENTS}}` | Parts assigned to this implementer |
| `{{FILE_BOUNDARIES}}` | Explicit file ownership |
| `{{ITERATION_FEEDBACK}}` | Cumulative history from prior cycles |
| `{{TEST_COMMAND}}` | Detected test command |
| `{{WORKTREE_PATH}}` | Absolute worktree path |

### 3. Preview Template (shared)

`plugin/templates/preview.html` — renders markdown in the browser with GitHub-style CSS and Mermaid support. Used by spec and ground skills for visual review before publishing or curating.
