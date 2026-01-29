# Slice Plan for #3

> Enable MILL to auto-generate codebase standards during init and refine them interactively via a new `mill standards` command.

## Concerns
- [ ] Model - N/A (no new data structures needed)
- [ ] Logic - Standards inference logic (detecting config files, analyzing codebase patterns)
- [ ] Interface - CLI command `mill standards`, Init integration
- [ ] Integration - Hook standards inference into `mill init` after context warmup
- [ ] Tests - Build verification
- [ ] Config/Docs - Prompt file for standards inference

## Slice Order
1. **Prompt** - Create `spec/prompts/standards-infer.md` with inference logic
   - Detects `.editorconfig`, `.eslintrc`, `stylecop.json`, `tsconfig.json`, etc.
   - Analyzes codebase for patterns
   - Outputs structured `.mill/standards/code.md`
   - Why first: This is the core logic, CLI just invokes it

2. **CLI** - Add `mill standards` command + integrate into `mill init`
   - New command routing for `["standards"]`
   - New `RunStandards()` method (similar to `RunPersonas()`)
   - Modify `Init()` to call standards inference after context warmup
   - Idempotent: preserve existing content, merge new inferences

3. **Build Verification** - Run `dotnet build` to verify compilation

## Current: Slice 1
Creating standards-infer.md prompt file.
