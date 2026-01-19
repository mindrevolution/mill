# MILL

**Spec-first AI delivery.** MILL turns conversations into verified specs, then executes them in bounded loops until tests pass — not until the AI thinks it's done.

```bash
mill spec          # chat → spec → GitHub issue
mill run #42       # loop until tests pass → PR
```

## How It Works

1. **Spec** — Interactive elicitation turns intent into a verifiable contract
2. **Loop** — AI iterates until success criteria pass (max 20 iterations)
3. **PR** — Only created after tests pass

Every spec includes a Loop Contract:

```markdown
## Loop Contract
- Success Criteria: <machine-checkable>
- Test Command: <must pass before PR>
- Stop Conditions: <max iterations>
```

The contract decides completion, not the agent.

## Quick Start

```bash
mill init              # setup repo (once)
mill spec              # create spec → GitHub issue #N
mill run               # list issues
mill run --auto        # autopick best issue
mill run #42           # execute loop
```

## Commands

| Command | Purpose |
|---------|---------|
| `mill init` | Initialize repo (labels, config, context) |
| `mill spec` | Interactive spec → GitHub issue |
| `mill run` | List issues (sorted by impact) |
| `mill run --auto` | Autopick and execute best issue |
| `mill run #N` | Execute loop on specific issue |

## Spec Types

| Type | When | Verified By |
|------|------|-------------|
| **Feature** | New capability | Acceptance criteria |
| **Bug** | Broken behavior | Regression test |
| **Security** | Vulnerability | Threat mitigated |
| **Task** | Technical work | Criteria pass |

## Trajectory

Autonomy increases incrementally — each step validates before the next.

```
Today:     Human creates spec → Human triggers loop
Tomorrow:  MILL drafts specs → Human approves → MILL executes
Future:    Multiple sources → MILL prioritizes → Human reviews PRs
```

Regardless of autonomy level, humans drive product direction — deciding *what* gets built and *when* it ships.

## Structure

```
.mill/
├── config.json      # scoring, excludes
├── context.md       # auto-generated
├── memory/          # learnings
├── drafts/          # in-progress specs
└── standards/       # human rules
```

Completed specs live in GitHub Issues.

## Requirements

Git repo + `gh` CLI (authenticated)

## License

[MIT](https://opensource.org/licenses/MIT)
