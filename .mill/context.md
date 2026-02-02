<!-- mill-context-hash: 037c008765b68b3a3771291d857388f901d491b4 -->
# Project Context

## Summary

mill is a specification-first AI delivery system that transforms user intent into verified deliverables through bounded, test-driven execution loops. It consists of a React workbench UI hosted in a Photino desktop app, backed by an ASP.NET Minimal API that orchestrates LLM providers (Claude Code CLI, with OpenCode planned).

The system is organized into four workspaces: **Ground** (product knowledge), **Brief** (time-boxed ideas), **Shape** (spec elicitation), and **Ship** (bounded execution loops). Specs publish to GitHub Issues; completed work creates PRs.

## Standards

- **Branding:** Always "mill" in lowercase
- **Enum serialization:** Always as strings, never integers (`JsonStringEnumConverter`)
- **Shell commands:** PowerShell on Windows, `/bin/sh` on macOS/Linux — never `cmd.exe`
- **Diagrams:** Must use Mermaid, no ASCII art
- **Package manager:** pnpm for workbench (not npm/yarn)
- **Cross-platform:** Windows, Linux, macOS (x64/arm64)

## Architecture

```
mill/
├── workbench/     # React 19 + Vite + Tailwind 4 (primary UI)
├── api/           # ASP.NET Minimal API (.NET 10) — endpoint/service layer
├── cli/           # Photino desktop shell (deprecated standalone CLI)
├── ground/        # Ground workspace assets (prompts, templates)
├── brief/         # Brief workspace assets
├── shape/         # Shape workspace assets (prompts, templates)
└── ship/          # Ship workspace assets (prompts)
```

### LLM Runtime Modes

| Mode | Path | Use Cases |
|------|------|-----------|
| **Non-interactive** | Workbench → API → CLI (`--print`) | Context warmup, verification, observations |
| **Interactive** | Workbench → Photino + PTY → CLI | Spec elicitation, work loops |

### Desktop Stack

- **Photino.NET 3.2.3** — Native webview wrapper
- **Pty.Net 1.0.3** — Cross-platform PTY
- **xterm.js 6.0** — Terminal UI in browser

## Recent Changes

- CLI improvements: fixed MILL_HOME resolution, updated prompt paths
- Ship workspace: iteration tracking, stderr streaming, live elapsed time, PR creation
- Ground: observation extraction from ship runs
- Shape: PTY-based spec input, close confirmation for terminal sessions
- PTY improvements: high-level spec-session endpoint, terminal stability

## Tech Stack

### Backend (api/)
- **.NET 10** (net10.0)
- **ASP.NET Core Minimal API**
- **Markdig** 0.44.0 — Markdown parsing

### Desktop (cli/)
- **.NET 10** (net10.0)
- **Photino.NET** 3.2.3 — Desktop webview
- **Pty.Net** 1.0.3 — PTY support
- **Markdig** 0.44.0

### Frontend (workbench/)
- **React** 19.2.0
- **TypeScript** 5.9.3
- **Vite** 7.2.4
- **Tailwind CSS** 4.1.18
- **Zustand** 5.0.10 — State management
- **Radix UI** — Component primitives
- **xterm.js** 6.0 — Terminal emulator
- **Lucide React** — Icons

---
*Generated: 2026-02-02*
