# Implement (MILL Iteration {{ITERATION}}/{{MAX_ITERATIONS}})

Execute ONE slice against the spec. Honor standards and Loop Contract.

## Spec
**Ref:** {{SPEC_REF}}

{{SPEC_CONTENT}}

## Workflow

1. Read project instructions: check `AGENTS.md` (or `CLAUDE.md` if no AGENTS.md)
2. Verify `.mill/context.md` exists (run warmup if not)
3. Read `.mill/standards/` and architecture docs
4. Verify Loop Contract present (stop if missing)

### Cleanup/Removal Tasks

For specs that involve removing files or cleaning up resources:
- **Check filesystem first** — use `ls`, `test -f`, or Read tool to verify files exist
- Don't rely solely on git history — files may be untracked or already deleted
- If the target doesn't exist → `MILL_ABORT: file/resource not found`

---

## Slicing (MANDATORY)

Work is sliced by concern. Each iteration implements ONE slice only.

### Concerns (slice boundaries)
- **Model** — data structures, types, schemas
- **Logic** — business rules, algorithms, core behavior
- **Interface** — API, CLI, UI changes
- **Integration** — wiring components together
- **Tests** — test coverage for the above
- **Config/Docs** — configuration, documentation updates

### Iteration 1: Plan + First Slice

1. Analyze the spec and identify which concerns it touches
2. Write a slice plan to `.mill/memory/issue-{{ISSUE_NUMBER}}-plan.md`:

```markdown
# Slice Plan for {{SPEC_REF}}

## Concerns
- [ ] Model — <what changes>
- [ ] Logic — <what changes>
- [ ] Tests — <what changes>

## Slice Order
1. <first slice — what and why first>
2. <second slice>
3. <third slice if needed>

## Current: Slice 1
```

3. Implement ONLY slice 1
4. Commit with message: `{{SPEC_REF}}: <slice description> (1/N)`
5. Signal `MILL_CONTINUE`

### Iteration 2+: Next Slice

1. Read `.mill/memory/issue-{{ISSUE_NUMBER}}-plan.md`
2. Update "Current" to next slice, check off completed concerns
3. Implement ONLY the next slice
4. Commit with message: `{{SPEC_REF}}: <slice description> (M/N)`
5. If more slices remain → `MILL_CONTINUE`
6. If final slice complete → `MILL_VERIFY`

### Single-Concern Exception

If the spec genuinely touches only ONE concern (e.g., "fix typo", "add one test"):
- Skip planning
- Implement directly
- Signal `MILL_VERIFY` after commit

Be honest: most features touch 2+ concerns. When in doubt, slice.

---

## Execute

For the current slice ONLY:
- Implement the changes
- Minimize unrelated changes
- Commit before signaling

### Verify Before Signaling
- Run the **Test Command** from the Loop Contract
- If tests fail → fix within this slice, don't proceed
- If tests pass → commit and signal

---

## Report

After each iteration:
- **Slice:** Which slice (M/N) and what it covered
- **Changed:** Files modified
- **Verification:** Test results
- **Next:** What the next slice will address (or "final" if done)

---

## GitHub ({{ISSUE_NUMBER}})

**If blocked:**
```bash
gh issue edit {{ISSUE_NUMBER}} --remove-label "in-progress" --add-label "blocked"
```

**Comments** — only for: blockers requiring input, discovered constraints.

**Do NOT create PR yourself.** Signal for verification; CLI creates the PR if approved.

---

## Memory

The slice plan lives in `.mill/memory/issue-{{ISSUE_NUMBER}}-plan.md`.

Write additional memory ONLY if you discover:
- Constraint not in spec
- Invalidated assumption
- Plan needs revision (update the plan file)

---

## Output

**Not complete (more slices remain):**
```
MILL_CONTINUE
{
  "done": "<1-line summary of what this slice accomplished>",
  "next": "<brief description of next slice>"
}
```

**Ready for verification (final slice complete, all tests pass):**
```
MILL_VERIFY
{
  "branch": "issue-{{ISSUE_NUMBER}}",
  "title": "{{SPEC_REF}}: <brief description>",
  "done": "<1-line summary of what this final slice accomplished>",
  "summary": "<what changed across all slices>",
  "verification": "<test results and checks performed>"
}
```

**Spec is invalid or impossible:**
```
MILL_ABORT: <reason the spec cannot be implemented>
```

Use MILL_ABORT when:
- Referenced files/resources don't exist
- Requirements are contradictory
- Spec describes already-completed work
- External dependencies are unavailable

The CLI will close the issue automatically with your reason.

Do NOT output `MILL_DONE` — only the verify step can authorize completion.
