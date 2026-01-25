<!-- mill-context-hash: 3d4003c4ee6c16f509dc0033001855f9d74beec7 -->
# Project Context

## Summary
MILL is a specification-first AI delivery system that transforms user intent into verified deliverables. It provides two main workflows: **Spec Process** (chat-to-spec) and **Work Loop** (bounded iteration until verification passes). The system creates GitHub issues from specs and executes work in isolated git worktrees.

## Standards
- Diagrams must use Mermaid (no ASCII art)
- CLI output style: minimal, uniform, hacker style (all lowercase, small symbols only)
- Do not run `dotnet publish` after each change
- Keep markdown files concise and scannable

## Architecture
- Single .NET 9 CLI application (`cli/Program.cs`)
- AOT-compiled native binary
- Prompts stored in `spec/prompts/` and `run/prompts/`
- Templates in `spec/templates/`
- Per-project state in `.mill/` directory (context, memory, drafts, standards)
- Work executed in isolated git worktrees (`.mill/work/`)
- GitHub Issues as single source of truth for completed specs

## Recent Changes
- fix(install): detect ARM64 architecture on Windows
- feat(ci): add Windows ARM64 build using native runner
- fix(ci): update macos runners (13 retired, use 15-intel and latest)
- fix(release): allow untracked files, warn on non-main branch
- chore: remove publish scripts (use install or release instead)
- feat: add release workflow and local scripts
- fix(update): handle 404 as missing releases instead of connection error
- #2: add CLI commands (--version, update, update --check) and startup cleanup (3/3)
- #2: add version reading, platform detection, and updater logic (2/3)
- #2: add version property and github release api types (1/3)

## Tech Stack
- **Language:** C# 13, .NET 9
- **Build:** AOT native compilation (`PublishAot`)
- **External CLIs:** `claude` (or MILL_CLI), `gh` (GitHub CLI), `git`
- **CI/CD:** GitHub Actions (release.yml)
- **Platforms:** Windows (x64, ARM64), macOS (x64, ARM64), Linux (x64, ARM64)

## Key Files
- `cli/Program.cs` - Main CLI entry point and all logic
- `cli/mill-cli.csproj` - Project configuration
- `spec/prompts/context-warmup.md` - Context generation prompt
- `spec/prompts/spec-draft.md` - Interactive spec elicitation
- `run/prompts/loop-iterate.md` - Work loop iteration prompt
- `run/prompts/loop-verify.md` - Verification prompt
- `run/prompts/run-autopick.md` - Issue selection prompt

## Commands
| Command | Purpose |
|---------|---------|
| `mill init` | Initialize repo for MILL |
| `mill spec` | Create specification → GitHub issue |
| `mill personas` | Create, update, or manage user personas |
| `mill run` | List available issues |
| `mill run --auto` | Autopick best issue (health check + scoring) |
| `mill run N` | Execute work loop on GitHub issue |
| `mill update` | Update mill to latest version |
| `mill update --check` | Check for updates without installing |
| `mill --version` | Show version |

---
*Generated: 2026-01-25*
