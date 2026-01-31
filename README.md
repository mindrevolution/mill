# mill

**Knowledge-first AI delivery.** mill builds project ground — personas, standards, concepts — then uses that knowledge to craft well-verified specs and execute them in bounded loops until tests pass.

## Workbench

The workbench is a cross-platform desktop app with three workspaces:

| Workspace | Purpose | Key Actions |
|-----------|---------|-------------|
| **Ground** | Build project knowledge | Curate personas, standards, concepts, design tokens; review AI observations |
| **Shape** | Draft and refine specs | Chat-to-spec elicitation; manage drafts; publish to GitHub Issues |
| **Ship** | Execute and verify | Run bounded loops; watch progress; review PRs |

```bash
mill workbench    # launch the desktop app
```

The workbench provides real-time visibility into AI work, visual knowledge management, and a streamlined spec-to-PR flow.

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

## CLI

For scripting or terminal workflows:

```bash
mill spec         # chat → spec → GitHub issue
mill run 42       # loop until tests pass → PR
mill run --auto   # autopick best issue
```

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
- Windows, macOS, or Linux

## License

[MIT](https://opensource.org/licenses/MIT)
