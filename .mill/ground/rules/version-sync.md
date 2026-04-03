---
category: rules
id: version-sync
---

# Version Sync

Three files must stay in sync when bumping versions:

| File | Field | Example |
|------|-------|---------|
| `plugin/.claude-plugin/plugin.json` | `"version"` | `"0.7.3-beta"` |
| `manual/package.json` | `"version"` | `"0.7.3-beta"` |
| `manual/src/pages/index.astro` | Hero badge | `v0.7 beta` (minor only) |

The Astro hero badge uses minor-only (no patch) to avoid frequent visual churn.
