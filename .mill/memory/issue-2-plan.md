# Slice Plan for #2

## Concerns
- [x] Config - Version in csproj
- [x] Model - GitHub release API types, version info
- [x] Logic - Version reading, platform detection, GitHub API, download/replace
- [ ] Interface - CLI commands (`--version`, `update`, `update --check`)

## Slice Order
1. **Config + Model** - Add `<Version>` to csproj, add GitHub release API types (GhRelease, GhAsset). Foundation for everything else.
2. **Logic** - Implement version reading, platform detection, GitHub API fetch, download, and self-replacement. The core functionality.
3. **Interface** - Wire up CLI: `--version` flag, `update` command, `update --check` flag. Also add old binary cleanup on startup.

## Current: Slice 2 (complete)
