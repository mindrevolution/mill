# Slice Plan for #6

## Concerns
- [x] Model - add InstallMode enum, InstallCheck record, system location helpers
- [x] Logic - mode detection, install/copy logic, downgrade check, cleanup, update logic
- [x] Interface - add `mill install` and `mill install --check` commands, update help

## Slice Order
1. **Model** - Add data structures: InstallMode enum, InstallCheck record, system location helpers (GetSystemInstallPath, IsInstalledLocation)
2. **Logic** - Implement install logic: mode detection, install/copy behavior, downgrade warning, auto-cleanup, update behavior
3. **Interface** - Wire up CLI commands (`install`, `install --check`), update help text, remove old `update` command

## Current: Slice 3 (final)
