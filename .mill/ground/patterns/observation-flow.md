---
category: patterns
id: observation-flow
---

# Observation Flow Pattern

Observations are the learning mechanism. Skills write them; `/mill:ground` curates them.

### Write Points

| Skill | When | Type |
|-------|------|------|
| `/mill:warmup` | Discovers conventions, empty ground, implied personas | `discovery`, `concern` |
| `/mill:spec` | Notices unknown personas, new terms, conflicting rules | `discovery` |
| `/mill:ship` | Extracts process learnings after PR creation | `learning` |

### Format

```markdown
---
source: {skill}
type: {discovery|concern|suggestion|learning|extraction}
issue: {N}              # if from ship
created: {ISO_DATE}
suggested: ground/{category}/   # routing hint for /mill:ground
---

# Title

Content describing the observation.
```

### Review Paths

```
.mill/observations/*.md
    │
    ├── /mill:ground (dedicated review — curate, dismiss, or ban)
    ├── /mill:spec pre-flight (count + nudge before drafting)
    └── /mill:ship auto-tag (suggested: routing hint)
    │
    ▼
.mill/ground/* (curated truth)
```

### `suggested:` Routing

Ship auto-tags learnings with a routing hint:
- File couplings, architecture → `ground/patterns/`
- Conventions, commands, config → `ground/rules/`
- Mixed/uncertain → omit `suggested:`

Human decides final placement via `/mill:ground`.
