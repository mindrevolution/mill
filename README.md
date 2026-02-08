# mill

Turning intent into verified deliverables, continuously.

mill is a specification-first delivery system that integrates with Claude Code. It provides structure and intelligence for the entire delivery workflow — from capturing ideas to shipping verified code.

## Quick Start

### Install

```bash
# macOS / Linux
curl -fsSL https://raw.githubusercontent.com/mindrevolution/mill/main/install.sh | bash

# Windows
irm https://raw.githubusercontent.com/mindrevolution/mill/main/install.ps1 | iex
```

### Initialize

```bash
cd your-project
mill init
```

### Use in Claude Code

```
/mill:warmup              # Generate project context
/mill:spec               # Draft a specification
/mill:ship 42             # Implement issue #42
```

## How It Works

mill has two components:

1. **CLI** — Data operations and GitHub integration
2. **Skills** — LLM-powered workflows for Claude Code

### Workflow

```mermaid
flowchart LR
    subgraph Knowledge
        G[Ground]
    end

    subgraph Delivery
        B[Brief] --> S[Spec] --> X[Ship]
    end

    G -.->|informs| B
    G -.->|validates| S
    G -.->|guides| X

    X -.->|learnings| G
```

| | |
|-|-|
| **Ground** | Build product knowledge — personas, standards, concepts |
| **Brief** | Capture ideas with intent — 30-day time-box |
| **Spec** | Refine into verified specs — publish to GitHub Issues |
| **Ship** | Execute bounded loops — until tests pass |

### Skills

| Skill | Purpose |
|-------|---------|
| `/mill:ground` | Manage product knowledge |
| `/mill:brief` | Capture and develop ideas |
| `/mill:spec` | Draft specifications → GitHub Issues |
| `/mill:ship` | Implement specs with verification |
| `/mill:warmup` | Generate codebase context |
| `/mill:question` | Answer questions (no changes) |

### CLI Commands

```bash
mill init                    # Initialize .mill/
mill ground list --human     # List knowledge items
mill draft list --human      # List spec drafts
mill issue list --human      # List GitHub issues
mill history --human         # View run history
```

## Requirements

- Claude Code
- `gh` CLI (authenticated)
- Git repository on GitHub

## Documentation

See [AGENTS.md](AGENTS.md) for full documentation.

## License

[MIT](https://opensource.org/licenses/MIT)
