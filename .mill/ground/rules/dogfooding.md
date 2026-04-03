---
category: rules
id: dogfooding
---

# Dogfooding

mill is used to develop mill itself. Ground knowledge, context, and dropped ideas are committed — same as any project using mill. Local WIP (observations, active ideas, spec drafts, worktrees) is gitignored via `.mill/.gitignore`.

Dev workflow: `source activate-dev-plugin-location.sh` → `claude-mill` launches Claude Code with the live plugin (bypasses cache).
