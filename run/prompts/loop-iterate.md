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
- If tests fail → fix and retry
- If all criteria met → signal ready for verification (see Output)
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

**Do NOT create PR yourself.** When ready, signal for verification (see Output). An independent verify step will review, and CLI creates the PR if approved.

## Memory

Write to `.mill/memory/issue-{{ISSUE_NUMBER}}#iter-{{ITERATION}}.md` **only** if discovered:
- Constraint not in spec
- Invalidated assumption
- Required decomposition

**Never** for: failed attempts, typos, debug details, slice planning/progress.

Most iterations produce no memory. Skip if nothing genuine.

## Output

**Ready for verification** — when all criteria met and tests pass:

```
MILL_VERIFY
{
  "branch": "issue-{{ISSUE_NUMBER}}",
  "title": "{{SPEC_REF}}: <brief description>",
  "summary": "<what changed>",
  "verification": "<test results and checks performed>"
}
```

An independent verify step will then review and either approve (PR created) or reject (you'll see feedback and can fix).

**Not complete:** `NOT_DONE` on its own line

**Blocked:** report error, prompt user for help

Do NOT output `MILL_DONE` — only the verify step can authorize completion.
