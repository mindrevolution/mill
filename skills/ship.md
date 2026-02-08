---
description: "Implement a spec → Pull Request • https://mill.mindrevolution.com/ship"
allowed-tools: Read, Write, Edit, Glob, Grep, Bash
argument-hint: "<issue-number> - GitHub issue to implement"
---

# Ship

Execute bounded work loops against specs. The CLI orchestrates the full loop — this skill handles pre-flight checks.

## Interaction Pattern

Ship is mostly autonomous — the spec should be complete from `/mill:spec`. **Use AskUserQuestion only when:**

- Spec has ambiguity that blocks implementation
- Multiple valid approaches exist and choice matters
- Scope clarification needed before proceeding

Don't ask about implementation details you can decide autonomously.

## Workflow

### 1. Pick Issue

If no issue number was provided as argument:

```bash
mill spec list --human
```

Use AskUserQuestion to let the user pick which spec to ship.

### 2. Validate Spec

```bash
mill spec get <number> --human
```

Confirm the spec is open and has a type label.

### 3. Ensure Context

```bash
mill context status
```

If missing or stale, run `/mill:warmup` before proceeding.

### 4. Run Ship

The CLI handles the entire work loop — worktree creation, iterations, verification, PR creation, and cleanup:

```bash
mill ship <number> --human
```

The CLI will:
1. Create a worktree at `.mill/ship/work/issue-<N>/`
2. Iterate: implement slice → test → commit → signal
3. Run independent verification on MILL_VERIFY
4. Create PR on success
5. Record history
6. Clean up worktree

### 5. Report Result

After `mill ship` completes, report the outcome to the user:
- On success: share the PR URL
- On failure: explain what happened and suggest next steps

## Commands Reference

```bash
mill spec list --human          # List open specs
mill spec get <N> --human       # Get spec details
mill context status             # Check context freshness
mill ship <N> --human           # Run the full ship loop
mill history --human            # View run history
```
