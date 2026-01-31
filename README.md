# mill

**Knowledge-first AI delivery.** mill builds project ground — personas, standards, concepts — then uses that knowledge to craft well-verified specs and execute them in bounded loops until tests pass.

![mill workbench](docs/workbench.png)

| | |
|-|-|
| **Ground** | Build project knowledge — personas, standards, concepts; review AI observations |
| **Shape** | Draft and refine specs — chat-to-spec elicitation; publish to GitHub Issues |
| **Ship** | Execute and verify — bounded loops until tests pass; review PRs |

## How It Works

1. **Ground** — Curate the knowledge that guides AI decisions
2. **Shape** — Interactive elicitation turns intent into verifiable specs
3. **Ship** — AI iterates until success criteria pass (bounded loops)

Every spec includes a Loop Contract:

```markdown
## Loop Contract
- Success Criteria: <machine-checkable>
- Test Command: <must pass before PR>
- Stop Conditions: <max iterations>
```

The contract decides completion, not the agent.

## Spec Types

| Type | When | Verified By |
|------|------|-------------|
| **Feature** | New capability | Acceptance criteria |
| **Bug** | Broken behavior | Regression test |
| **Security** | Vulnerability | Threat mitigated |
| **Task** | Technical work | Criteria pass |

## Structure

```
.mill/
├── project.json     # config
├── context.md       # auto-generated project context
├── memory/          # machine learnings
├── drafts/          # in-progress specs
└── standards/       # human-authored rules
```

Completed specs live in GitHub Issues.

## Requirements

- Git repository
- `gh` CLI (authenticated)
- Windows, Linux or macOS

## License

[MIT](https://opensource.org/licenses/MIT)
