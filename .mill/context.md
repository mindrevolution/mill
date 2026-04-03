<!-- mill-context-hash: 14e46e7c49abece1d27bcc5178419c17d99470e4 -->
# Project Context

## Summary

mill is a Claude Code skill pack (v0.7.3-beta) that turns intent into verified Pull Requests. It provides 6 markdown-based skills that orchestrate Claude Code's native tools for the full delivery workflow: knowledge capture, idea development, spec drafting, team-based implementation, and independent verification.

This repo contains the skill pack itself (not a target project). It ships as a Claude Code plugin via the marketplace.

## Architecture

### Layers

- **Plugin** (`plugin/`) — Skill definitions (SKILL.md) + templates, distributed via Claude Code plugin system
- **Manual** (`manual/`) — Documentation site (Astro + Tailwind), deployed to mill.mindrevolution.com
- **Marketplace** (`.claude-plugin/`) — Plugin discovery metadata

### Entry Points

- `plugin/.claude-plugin/plugin.json` — Plugin manifest (name, version, description)
- `.claude-plugin/marketplace.json` — Marketplace listing, points to `./plugin`
- `plugin/skills/{name}/SKILL.md` — Each skill's definition and entry point
- `manual/src/pages/index.astro` — Manual site homepage
- `activate-dev-plugin-location.sh` — Dev alias (`claude-mill`) for live plugin testing

### Key Abstractions

- **SKILL.md** — Each skill is a self-contained markdown file with YAML frontmatter (description, allowed-tools, argument-hint). Claude Code loads and executes them directly.
- **Templates** — Reusable markdown fragments loaded by skills:
  - `plugin/skills/spec/templates/` — Spec output format by type (feature, bug, security, task)
  - `plugin/skills/ship/templates/domains/` — Domain-specific guidance (backend, application, website, platform)
  - `plugin/skills/ship/templates/teammates/` — Agent role instructions (implementer, verifier)
  - `plugin/templates/preview.html` — HTML preview template for rendering markdown in browser

### Data Flow

- User invokes `/mill:{skill}` → Claude Code loads SKILL.md → skill orchestrates via Read/Write/Glob/Grep/Bash
- Spec phase: elicit → draft to `.mill/spec/drafts/` → publish via `gh issue create` → GitHub Issue
- Ship phase: load spec from GitHub Issue → create worktree → spawn implementer + verifier agents → iterate → `gh pr create`
- Learning loop: skills write observations to `.mill/observations/` → `/mill:ground` curates into `.mill/ground/`

## Recent Changes

| Commit | Description |
|--------|-------------|
| 14e46e7 | chore: bump version to 0.7.3-beta |
| 73445c9 | feat(ground): add verify ground flow to detect and fix knowledge drift |
| dca7160 | chore: bump version to 0.7.2-beta |
| c1dffa0 | feat: spec quality overhaul and HTML preview template |
| 3746095 | chore: bump version to 0.7.1-beta |
| 38faf1a | feat(ground): add batch preview with expanded items before review |
| f83e8d4 | feat(spec): open draft in editor before publish confirmation |
| 661b0ea | feat(ground): show observation content before review decision |
| d04e7fb | docs(manual): merge why-mill and principles into single page |
| 2940370 | feat(manual): link workflow step cards to their chapter pages |
| f55030b | docs(manual): rewrite all 8 chapters for CS-tutor quality |
| eecdd0c | docs: update manual and README for learning pipeline |
| e67601d | feat: learning pipeline with auto-tag, spec nudge, and observation reporting |
| b7cef78 | refactor: tighten prompts, optimize and tune precision |
| 4652cc4 | feat(ship): extract process learnings after every PR creation |
| 7536cba | chore: remove redundant "not a CLI" warnings from all skills |
| bf05e5e | feat(idea): add codebase-aware develop phase |
| bac5d21 | fix(manual): hero install commands visible on all MacBook screen sizes |

## Tech Stack

### Plugin
- Pure markdown (SKILL.md files with YAML frontmatter)
- No runtime dependencies — Claude Code's native tools are the runtime
- Templates: markdown (specs, domain guidance, teammates) + HTML (preview)

### Manual (manual/)
- Astro 5.x
- Tailwind CSS
- pnpm
- Deployed via Kamal + Docker + Nginx to mill.mindrevolution.com

## Key Files

| File | Purpose |
|------|---------|
| `AGENTS.md` | Full project reference — architecture, skills, ship details |
| `README.md` | Public-facing overview |
| `plugin/.claude-plugin/plugin.json` | Plugin manifest (version lives here) |
| `plugin/skills/spec/SKILL.md` | Spec skill — RAC framework, self-review, independent review |
| `plugin/skills/ship/SKILL.md` | Ship skill — agent teams, iteration loop, learning extraction |
| `plugin/skills/ground/SKILL.md` | Ground skill — observation review, knowledge curation, verify flow |
| `plugin/skills/idea/SKILL.md` | Idea skill — 30-day lifecycle, develop phase |
| `plugin/skills/init/SKILL.md` | Init skill — `.mill/` directory setup |
| `plugin/skills/warmup/SKILL.md` | Warmup skill — context generation |
| `plugin/skills/ship/templates/teammates/implementer.md` | Implementer agent instructions |
| `plugin/skills/ship/templates/teammates/verifier.md` | Verifier agent instructions |
| `plugin/templates/preview.html` | HTML preview template (markdown rendering) |
| `manual/src/content/chapters/` | Manual chapters (7 files) |
| `activate-dev-plugin-location.sh` | Dev alias for live plugin testing |

---
*Updated: 2026-04-03T13:00:00Z*
