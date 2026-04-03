---
category: decisions
id: skills-only
---

# Decision: Skills-Only Architecture

## Choice

mill operates as Claude Code skills — no desktop app, no web UI, no API server.

## Context

mill previously had a Photino desktop app (React workbench + ASP.NET API + Pty.Net for interactive terminals). This was abandoned in favor of running entirely as Claude Code plugin skills.

## Rationale

- **Complexity overhead** — The desktop app (Photino + React + ASP.NET + xterm.js + PTY management) added significant surface area for the value it provided. Most of the real work happened in prompts, not UI.
- **Iteration speed** — Skills-only lets the product iterate faster without maintaining a separate application layer. Changes to prompts and workflows ship immediately.
- **Natural fit** — Claude Code skills are the right interface for a specification-first delivery system. The LLM session *is* the interaction — no intermediary UI needed.

## Rejected

- **Photino desktop app** — React + ASP.NET + Pty.Net. Directories `workbench/`, `api/`, `cli/` are legacy and slated for removal.

## Implications

- All user interaction happens through Claude Code skill invocations (`/mill:warmup`, `/mill:ground`, `/mill:shape`, etc.)
- Non-interactive operations (verification, autopick, observations) use Claude Code's `--print` mode via prompts
- Interactive operations (spec elicitation, work loops) run as normal Claude Code sessions
- No custom UI to maintain — Claude Code's terminal *is* the UI
