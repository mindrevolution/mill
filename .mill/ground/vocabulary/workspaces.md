---
category: vocabulary
id: phases
---

# Phases

mill organizes work into four phases, each a distinct step in the delivery pipeline:

```
Ground → Idea → Spec → Ship
```

| Phase | Skill | Purpose |
|-------|-------|---------|
| **Ground** | `/mill:ground` | Product knowledge — personas, standards, concepts, design. The shared understanding that keeps AI output coherent. |
| **Idea** | `/mill:idea` | Ideas with intent — captured with a 30-day lifecycle. Ideas that aren't promoted decay and get dropped. Essence preserved. |
| **Spec** | `/mill:spec` | Spec elicitation — interactive chat-to-spec that refines intent into complete, loop-ready specifications. Published to GitHub Issues. |
| **Ship** | `/mill:ship` | Bounded execution — agent teams implement specs in worktrees with independent verification. Produces PRs. |

Two utility skills support the pipeline:

| Skill | Purpose |
|-------|---------|
| `/mill:init` | Initialize `.mill/` in a git repository — creates directory structure and ground categories |
| `/mill:warmup` | Orient Claude to the codebase — generates or refreshes `.mill/context.md` |
