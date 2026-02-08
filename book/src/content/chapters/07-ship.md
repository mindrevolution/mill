---
title: "Ship"
number: 7
subtitle: "From spec to pull request"
accent: "flame"
---

## The Execution Engine

Ship is where intent becomes reality. You point it at a crafted spec — hosted as a GitHub Issue — and it implements that spec through bounded, verified iterations.

This isn't "generate some code." This is **CLI-orchestrated execution**: the `mill ship` command runs the entire loop — worktree isolation, bounded iterations, independent verification, PR creation, and cleanup. One command, full pipeline.

## How It Works

### 1. Launch

```bash
mill ship 47
```

That's it. The CLI takes over. It reads GitHub Issue #47, parses the spec structure, and validates that everything needed is present. You can also start from the skill (`/mill:ship 47`) which handles pre-flight checks before delegating to the CLI.

### 2. Isolate

mill creates a **worktree** — an isolated copy of your repo on a dedicated branch:

```
.mill/ship/work/issue-47/
```

Your main branch stays untouched. All implementation happens in isolation. If anything goes wrong, there's nothing to clean up.

### 3. Load Context

Before writing a single line of code, the CLI loads:

- **The spec** — parsed from the GitHub Issue
- **Project context** from `.mill/context.md` — your codebase overview
- **Domain guidance** — execution template for the spec's domain (backend, application, etc.)

This is why ground matters. A ship run with rich ground knowledge produces dramatically better code than one without.

### 4. Iterate

The CLI invokes Claude with the spec and context, one iteration at a time. Each iteration implements one slice:

1. **Implement** — write the code for this concern
2. **Test** — run the loop contract's test command
3. **Commit** — save the work
4. **Signal** — tell the CLI what happened

Signals drive the loop:

**MILL_CONTINUE** — slice done, more work remains
```json
{ "done": "Implemented data models", "next": "Add business logic" }
```

**MILL_VERIFY** — all slices complete, ready for verification
```json
{
  "branch": "issue-47",
  "title": "#47: Add PDF export",
  "summary": "Full export flow with template rendering",
  "verification": "All 14 tests passing"
}
```

**MILL_ABORT** — spec can't be implemented as written
```
MILL_ABORT: Required dependency not available
```

The CLI manages up to 20 iterations. Each iteration gets the full context plus any rejection feedback from previous verification attempts.

### 5. Verify Independently

This is the key insight: **work can't grade its own homework.**

After `MILL_VERIFY`, the CLI spins up a *separate* Claude instance — one that didn't write the code — to verify the implementation against the spec:

- Run the full test suite
- Check every acceptance criterion
- Review the diff for quality, security, and scope creep

If verification passes → proceed to PR.
If verification rejects → feed the blockers back to the implementation loop and iterate again.

### 6. Create PR

The CLI creates a Pull Request with:

- Title referencing the issue
- Description linking the spec
- Summary of changes
- Verification results

The PR connects back to the spec issue, creating full traceability from intent → spec → implementation → review.

### 7. Clean Up and Record

After completion, the CLI:

- Removes the worktree (clean slate)
- Records the run in `.mill/ship/history.json`

History tracks everything: issue, PR, iterations, duration, outcome. Over time, this data shows trends — are specs getting smaller? Are ship runs getting faster? Where do failures cluster?

## Slicing Philosophy

The slice model is borrowed from the idea of separation of concerns, applied to time:

| Concern | What It Covers | Why It's Separate |
|---------|---------------|-------------------|
| **Model** | Data structures, schemas | Foundation that everything builds on |
| **Logic** | Business rules, services | Pure logic, testable in isolation |
| **Interface** | API/UI layer | Connects logic to users |
| **Tests** | Verification coverage | Proves everything works |

You wouldn't write a function that handles data, UI, and business logic in one blob. Similarly, you shouldn't implement all concerns in one iteration. The plan is written to the worktree for reference across iterations.

## Autonomy and Judgment

Ship is designed to be mostly autonomous. The spec should be complete enough that implementation doesn't need constant human input. But the system isn't reckless:

- **Genuine ambiguity** → ask the user (e.g., "spec says 'handle errors gracefully' — which approach?")
- **Implementation details** → decide autonomously (e.g., variable names, internal structure)
- **Scope creep** → flag it, don't add unrequested features

The rule is simple: honor the spec. Don't add what wasn't asked for. Don't skip what was specified. Build exactly what was contracted.

## Observations During Ship

While implementing, mill observes:

- Missing test coverage in existing code
- Undocumented APIs being used
- Code patterns not tracked in ground
- Dependencies not in the stack inventory

These observations are written to `.mill/observations/ship-{issue}-{slug}.md` without interrupting the flow. They'll be reviewed later in the ground review cycle.

## When Things Go Wrong

### Tests fail

The CLI feeds test failures back into the next iteration. The signal system means the implementation knows exactly what failed and can address it.

### Verification rejects

The independent verifier found issues. The CLI pipes the rejection blockers back to the implementation loop for another round. This cycle continues until verification passes or the iteration limit is hit.

### Spec has gaps

If the spec is missing information that blocks implementation, the signal `MILL_ABORT` fires with a clear reason. The spec goes back to the drafting stage.

### Too many iterations

If the 20-iteration limit is reached, the CLI stops and reports what was accomplished. This usually means the spec needs to be broken into smaller pieces.

### External dependencies missing

If a required service, library, or API isn't available, the implementation aborts with a description of what's missing rather than working around it.

## History and Trends

```bash
mill history --human
```

History tells the story of your project's delivery:

- How many iterations does a typical feature take?
- Which types of specs succeed most reliably?
- Where are the failure patterns?
- Is delivery getting more efficient over time?

This data, combined with ground knowledge, makes each cycle more predictable.
