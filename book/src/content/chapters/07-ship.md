---
title: "Ship"
number: 7
subtitle: "From spec to pull request"
accent: "flame"
---

## The Execution Engine

Ship is where intent becomes reality. You point it at a crafted spec — hosted as a GitHub Issue — and it implements that spec through bounded, verified iterations.

This isn't "generate some code." This is structured execution: load the spec, load your project's context, plan slices, implement one at a time, test after each, verify against criteria, and create a PR only when everything passes.

## How It Works

### 1. Load the Spec

```
/mill:ship 47
```

mill reads GitHub Issue #47, parses the spec structure (requirements, approach, criteria, loop contract), and validates that it has everything needed to begin.

### 2. Load Context

Before writing a single line of code, mill loads:

- **Project context** from `.mill/context.md` — your codebase overview
- **Ground knowledge** — stack, patterns, rules, schema
- **Domain guidance** — execution template for the spec's domain (backend, application, etc.)

This is why ground matters. A ship run with rich ground knowledge produces dramatically better code than one without.

### 3. Plan Slices

Non-trivial specs are broken into slices by concern:

```markdown
## Slice Plan

1. Model — data structures and schema changes
2. Logic — business rules and service layer
3. Interface — API endpoints or UI components
4. Tests — verification coverage
```

Each slice is atomic. It can be implemented, tested, and committed independently. The plan is written to `.mill/ship/work/` for reference.

### 4. Execute

For each slice:

1. **Implement** — write the code for this concern
2. **Test** — run the loop contract's test command
3. **Commit** — save the work
4. **Signal** — tell mill what happened

Signals communicate iteration state:

**MILL_CONTINUE** — slice done, more work remains
```json
{ "done": "Implemented data models", "next": "Add business logic" }
```

**MILL_VERIFY** — all slices complete, ready for final verification
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

### 5. Verify

After `MILL_VERIFY`, an independent verification runs:

- Run the full test suite (not just the slice tests)
- Check each criterion from the spec
- Review the code for quality and consistency

If verification passes → proceed to PR.
If verification fails → iterate with the feedback.

### 6. Create PR

mill creates a Pull Request with:

- Title referencing the issue
- Description linking the spec
- Summary of changes
- Verification results

The PR connects back to the spec issue, creating full traceability from intent → spec → implementation → review.

### 7. Record History

After completion:

```bash
mill history add '{"date":"...","issue":47,"pr":48,...}'
```

History tracks everything: issue, PR, iterations, duration, outcome. Over time, this data shows trends — are specs getting smaller? Are ship runs getting faster? Where do failures cluster?

## Slicing Philosophy

The slice model is borrowed from the idea of separation of concerns, applied to time:

| Concern | What It Covers | Why It's Separate |
|---------|---------------|-------------------|
| **Model** | Data structures, schemas | Foundation that everything builds on |
| **Logic** | Business rules, services | Pure logic, testable in isolation |
| **Interface** | API/UI layer | Connects logic to users |
| **Tests** | Verification coverage | Proves everything works |

You wouldn't write a function that handles data, UI, and business logic in one blob. Similarly, you shouldn't implement all concerns in one iteration.

## Autonomy and Judgment

Ship is designed to be mostly autonomous. A good spec tells it everything it needs to know. But mill isn't reckless:

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

Iterate. The signal system means mill knows what failed and can address it in the next slice.

### Spec has gaps

If the spec is missing information that blocks implementation, mill uses `MILL_ABORT` with a clear reason. The spec goes back to the drafting stage.

### Too many iterations

If the 20-iteration limit is reached, the work stops for human review. This usually means the spec needs to be broken into smaller pieces.

### External dependencies missing

If a required service, library, or API isn't available, mill aborts with a description of what's missing rather than working around it.

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
