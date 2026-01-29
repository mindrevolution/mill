# Slice Plan for #4

## Concerns
- [x] Model - data structures for sweep findings and categories
- [x] Logic - sweep prompt, issue creation, dismissal tracking
- [x] Interface - `mill sweep` command, interactive prompts

## Slice Order
1. **Model** - Define data structures: `SweepFinding`, `SweepCategory`, JSON contexts. Add `sweep` and `impact:low` labels to `RequiredLabels`. This is first because other slices depend on the data model.
2. **Logic** - Create `sweep-analyze.md` prompt for Claude to analyze codebase against standards. Implement helper methods for: fetching existing sweep issues, reading dismissed findings from `project.md`, creating issues via `gh`.
3. **Interface** - Implement `RunSweep()` command with interactive flow: display findings by category, prompt for selection (`1,2,3`, `all`, `none`), dismissal confirmation, issue creation.

## Current: Complete
