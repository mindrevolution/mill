# Implement (MILL Iteration {{ITERATION}}/{{MAX_ITERATIONS}})

Execute one iteration against the spec. Honor standards and Loop Contract.

## Spec
**Ref:** {{SPEC_REF}}

{{SPEC_CONTENT}}

## Workflow

1. Read project instructions: check `AGENTS.md` (or `CLAUDE.md` if no AGENTS.md). If neither exists and you need to create one: create `AGENTS.md` with content, and `CLAUDE.md` containing only `@AGENTS.md`
2. Verify `.mill/context.md` exists (run warmup if not)
3. Read `.mill/standards/` and architecture docs
4. Verify Loop Contract present (stop if missing)
5. Identify success criteria and verification commands

### Focus
State intent for THIS iteration:
- **Slice:** Smallest piece that advances the contract
- **Done:** How you'll know slice is complete (before verification)
- **Risk:** One blocker, if any

Trivial issues: one line. Complex work: be explicit.

### Execute
- Implement the slice. Minimize unrelated changes.
- Check: slice complete? Note deferrals.

### Verify
- Run the **Test Command** from the Loop Contract — tests must pass
- Run additional verification commands
- If tests fail → fix and retry (do NOT proceed to PR)
- If all criteria met → output `{{COMPLETION_TOKEN}}`
- If not → output progress report + next intent

## Report
- **Slice:** What attempted
- **Changed:** What and why
- **Verification:** Results
- **Remaining:** Gaps or next slice

## GitHub ({{ISSUE_NUMBER}})

**If blocked:**
```bash
gh issue edit {{ISSUE_NUMBER}} --remove-label "in-progress" --add-label "blocked"
```

**Comments** — only for: blockers requiring input, discovered constraints.
Never for: routine progress, failed attempts.

**PR on completion (required for MILL_DONE):**
```bash
git push -u origin issue-{{ISSUE_NUMBER}}
gh pr create --title "{{SPEC_REF}}: <description>" --body "Fixes {{SPEC_REF}}

## Summary
<changes>

## Verification
<results>"
```

If push or PR creation fails, do NOT output `{{COMPLETION_TOKEN}}`. Report the error and prompt user for help.

## Memory

Write to `.mill/memory/issue-{{ISSUE_NUMBER}}#iter-{{ITERATION}}.md` **only** if discovered:
- Constraint not in spec
- Invalidated assumption
- Required decomposition

**Never** for: failed attempts, typos, debug details, slice planning/progress.

Most iterations produce no memory. Skip if nothing genuine.

## Output
- Complete: `{{COMPLETION_TOKEN}}` on its own line — **only after PR created successfully**
- Not complete: `NOT_DONE` on its own line
- Blocked: report error, prompt user for help (do not exit)
