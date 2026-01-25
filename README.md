# MILL

**Spec-first AI delivery.** MILL turns conversations into verified specs, then executes them in bounded loops until tests pass — not until the AI thinks it's done.

```bash
mill spec          # chat → spec → GitHub issue
mill run 42       # loop until tests pass → PR
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
mill run 42           # execute loop
```

## Commands

| Command | Purpose |
|---------|---------|
| `mill init` | Initialize repo (labels, config, context) |
| `mill spec` | Interactive spec → GitHub issue |
| `mill personas` | Create or update user personas |
| `mill run` | List issues (sorted by impact) |
| `mill run --auto` | Autopick and execute best issue |
| `mill run N` | Execute loop on specific issue |

## Personas

Optional user personas (`.mill/personas.md`) improve spec elicitation by grounding questions and user stories in real user segments.

```bash
mill personas     # create, update, or manage personas
```

Personas are loaded during `mill spec` but **not** during `mill run`. They "compile down" into concrete user stories — the spec stands alone with no persona references. The run loop only sees the self-contained spec.

## Spec Types

| Type | When | Verified By |
|------|------|-------------|
| **Feature** | New capability | Acceptance criteria |
| **Bug** | Broken behavior | Regression test |
| **Security** | Vulnerability | Threat mitigated |
| **Task** | Technical work | Criteria pass |

Types are tracked via labels (not GitHub issue types) for portability across git platforms.

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
├── personas.md      # user personas (optional)
├── memory/          # learnings
├── drafts/          # in-progress specs
└── standards/       # human rules
```

Completed specs live in GitHub Issues.

## Requirements

Git repo + `gh` CLI (authenticated)

## License

[MIT](https://opensource.org/licenses/MIT)
