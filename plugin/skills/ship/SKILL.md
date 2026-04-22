---
description: "Implement a spec → Pull Request • https://mill.mindrevolution.com/ship"
disable-model-invocation: true
allowed-tools: Read, Write, Edit, Glob, Grep, Bash(*gh *, *git *, *rm *, *mkdir *)
argument-hint: "<issue-number> - GitHub issue to implement"
---

# Ship

Implement a spec as a team. You are the **lead** — you orchestrate, delegate, and verify. You never implement directly when teammates are available.

## Interaction Pattern

Ship is mostly autonomous — the spec should be complete from `/mill:spec`. **Use AskUserQuestion only when:**

- Spec has ambiguity that blocks implementation
- Multiple valid approaches exist and choice matters
- Scope clarification needed

---

## Workflow

### 1. Pick Issue

If no issue number provided:

```bash
gh issue list --state open --json number,title,labels --limit 100
```

Filter for `spec` label. Present choices via AskUserQuestion.

### 2. Load Spec

```bash
gh issue view {N} --json number,title,body,labels,state,createdAt
```

Parse JSON. Confirm open and has type label.

**Parse Loop Contract** from spec body:
- `max_iterations` — from `**Max Iterations:**`, fall back to `**Stop Conditions:**`. Default: `5`.
- `test_command` — from `**Test Command:**`
- `verification_commands` — from `**Verification Commands:**`

### 3. Load Context + Domain Guidance + Ground

Check context freshness:
1. Read `.mill/context.md` — extract hash
2. `git rev-parse HEAD` — compare
3. If missing or stale → run `/mill:warmup`

Load domain template matching spec's domain label:
- [templates/domains/backend.md](templates/domains/backend.md)
- [templates/domains/application.md](templates/domains/application.md)
- [templates/domains/website.md](templates/domains/website.md)
- [templates/domains/platform.md](templates/domains/platform.md)

Load implementation-relevant ground knowledge:
`Glob(".mill/ground/{rules,patterns,decisions}/**/*.md")` → read each file. Concatenate as `ground_knowledge`. If no files found, set to `"No ground knowledge established yet."`

### 4. Detect Project Settings

Default branch:
```bash
git symbolic-ref refs/remotes/origin/HEAD 2>/dev/null || echo "refs/remotes/origin/main"
```

Test command precedence:
1. Loop Contract `test_command` (if concrete)
2. Project files (`package.json`, `Makefile`, `pyproject.toml`, CI workflows)
3. Ask the user

Store `verification_commands` from Loop Contract. If empty: `echo "no additional verification commands"`.

### 5. Create Worktree

```bash
git worktree add .mill/ship/work/issue-{N} -b issue-{N}
```

If branch exists: `git worktree add .mill/ship/work/issue-{N} issue-{N}`

Copy context: Read `.mill/context.md` → Write to `.mill/ship/work/issue-{N}/.mill/context.md`.

### 6. Determine Team Size

Count approach parts (A). Part count determines team size — no coupling-based override:

| Approach Parts | Domain | Team Size |
|----------------|--------|-----------|
| 1–4 | Single | 1 implementer |
| 5–9 | Single | 2 implementers (split by concern) |
| Any | Fullstack | 2–3 implementers (one per layer) |
| 10+ | Any | 3–4 implementers |

Always +1 verifier (separate from implementers).

### 7. Spawn Implementers

For each implementer, launch via `Task` (`subagent_type: "general-purpose"`). Load [templates/teammates/implementer.md](templates/teammates/implementer.md) and substitute:

| Placeholder | Value |
|-------------|-------|
| `{{ISSUE_NUMBER}}` | Issue number |
| `{{TASK_ASSIGNMENTS}}` | Assigned approach parts |
| `{{FILE_BOUNDARIES}}` | Explicit file ownership (e.g., "only edit files in src/api/") |
| `{{SPEC_CONTENT}}` | Full spec body |
| `{{CONTEXT}}` | `.mill/context.md` content |
| `{{DOMAIN_GUIDANCE}}` | Domain template content |
| `{{TEST_COMMAND}}` | Detected test command |
| `{{WORKTREE_PATH}}` | Absolute worktree path |
| `{{ITERATION_FEEDBACK}}` | Cumulative iteration log (step 11). First run: `"First implementation pass — no prior iteration history."` |
| `{{VERIFICATION_COMMANDS}}` | From Loop Contract, or `echo "no additional verification commands"` |
| `{{GROUND_KNOWLEDGE}}` | Concatenated rules, patterns, decisions from `.mill/ground/` |

Team-of-1: single implementer gets full spec and all files.

Parallel when work doesn't overlap (e.g., backend + frontend). Sequential when dependent (e.g., API contract needed by frontend).

### 8. Monitor Progress

Watch task completion. When implementers report:
- **Need clarification** → Lead resolves or relays to user
- **Cross-team dependency** → Lead relays contracts between teammates
- **Stuck** → Lead redirects with specific guidance
- **Done** → Proceed to polish pass when all complete

### 9. Polish Pass

Re-launch each implementer once with all original placeholders, but set `{{ITERATION_FEEDBACK}}` to:

```
## Polish Pass

Implementation is complete. Before independent verification, one pass to:

1. **Polish** — `git diff {default_branch}...HEAD`. Only polish code in the diff — don't refactor adjacent code.
   - Simplify, improve naming, remove dead code. Reduce nesting (early returns, guard clauses).
   - Clean dead comments: TODOs from implementation, debugging breadcrumbs, comments restating the obvious.
   - Behavior must not change — only clarity and structure.
   - Don't over-consolidate: clarity over cleverness, no nested ternaries or dense one-liners.
   - Check ground rules — verify naming, conventions, patterns are honored.
   - If a polish change doesn't clearly improve readability, revert it.
2. **Review** — check every spec criterion against the diff. Fix gaps found during review.
3. **Test** — run test + verification commands. Failures here mean polish went too far.
4. **Commit** — commit polish changes referencing the issue.

Last pass before independent verification.
```

Single bounded pass — not a loop. When done, proceed to verifier.

### 10. Spawn Verifier

Spawn a separate teammate with clean context. Load [templates/teammates/verifier.md](templates/teammates/verifier.md) and substitute same placeholders as step 7 (minus `{{ITERATION_FEEDBACK}}`), plus `{{DEFAULT_BRANCH}}`.

The verifier receives NO iteration history — clean context preserved across all cycles.

The verifier:
- Runs tests
- Reviews `git diff {default_branch}...HEAD` in the worktree
- Checks each spec criterion
- Reports: pass or reject with specific blockers

### 11. Handle Verification Result

**Pass** → Step 12.

**Reject** → Loop Contract-driven iteration:

Track cumulative state: `cycle`, `max_iterations`, `history: [{ cycle, blockers, fixes_attempted, what_passed }]`

For each rejection:
1. Parse blockers, match to responsible implementer
2. Append to history
3. Build `{{ITERATION_FEEDBACK}}`:
   ```
   ## Iteration History
   ### Cycle 1
   **Blockers:** {list}
   **Fixes attempted:** {list}
   **What passed:** {list}
   ### Cycle 2
   ...
   **IMPORTANT:** Address all current blockers. Do NOT regress on items that already passed.
   ```
4. Re-launch responsible implementer(s) with updated feedback
5. Re-run verifier with clean context (no iteration history)
6. Increment cycle

**On exhaustion** (cycle > max_iterations):

Report to user via AskUserQuestion: Continue iterating (ask how many more) / Create PR as-is / Abort.

If "Continue" → resume loop. If "PR as-is" → step 12 with issues noted. If "Abort" → skip to step 14.

### 12. Create PR

```bash
git -C .mill/ship/work/issue-{N} push -u origin issue-{N}
```

Build PR body → `Write(".mill/.prompt", pr_body)`:

```markdown
## Summary
{1-3 bullet points}

Closes #{N}

## Verification
- All spec criteria verified by independent reviewer
- Test command: `{test_command}` — passing
{if cycles > 1:}
## Iteration Summary
- **Cycles:** {cycle} of {max_iterations}
- {brief summary of fixes}
{end if}

🤖 Claude
```

```bash
gh pr create --title "#{N}: {spec title}" --body-file .mill/.prompt --head issue-{N}
```

Parse PR URL. Clean up: `rm .mill/.prompt`

### 13. Extract Learnings

After every ship session, extract process knowledge. The question: **"How can we do better next time?"**

Review the session — iteration history, implementer process notes, orchestration decisions. Extract only **non-obvious** learnings:

| Category | What to capture |
|----------|----------------|
| **Hidden relationships** | Files/modules that must change together, not apparent from imports |
| **Debugging breakthroughs** | Error messages that pointed elsewhere — what was actually wrong |
| **Trial-and-error commands** | Build flags, CLI invocations that took multiple attempts |
| **Architectural constraints** | Invariants or coupling discovered only by breaking things |
| **Divergent execution paths** | Runtime behavior that differs from how code reads statically |

**Default: no learnings.** Most sessions produce nothing worth recording. Only write if a learning passes the bar: *"Would a developer reading this change how they work on this codebase next time?"* If nothing passes, write nothing — don't fabricate.

All learnings go to the observation inbox with a `suggested:` routing hint for `/mill:ground`:

- Path: `.mill/observations/ship-{N}-learnings.md`
- Frontmatter: `source: ship`, `type: learning`, `issue: {N}`, `created: {date}`, `suggested: ground/{category}/`

| Learning about | `suggested:` value |
|----------------|--------------------|
| File couplings, architecture patterns | `ground/patterns/` |
| Conventions, commands, config | `ground/rules/` |
| Mixed or uncertain | omit `suggested:` |

Human decides final routing via `/mill:ground`.

**Report observation count:**

`Glob(".mill/observations/*.md")` — if observations exist, report: "{N} observations in the learning inbox. Review via `/mill:ground` or they'll surface at next `/mill:spec`."

### 14. Cleanup

```bash
git worktree remove --force .mill/ship/work/issue-{N}
```

Report the PR URL to the user.

---

## Fallback: Single-Session Mode

If agent teams unavailable (Task tool limited or experimental features disabled):

1. Lead implements directly in the worktree
2. Approach parts sequentially: implement → test → commit
3. Explicit verification phase: re-read spec, `git diff`, run tests, check each criterion
4. Fix and re-verify (up to `max_iterations`, default 5)
5. Create PR
6. Extract learnings (same as step 13)

Report: "Running in single-session mode (agent teams not available)"

---

## Observations

Teammates write observations during implementation:
- Path: `.mill/observations/ship-{N}-{slug}.md`
- Frontmatter: `source: ship`, `type: concern|discovery|suggestion`, `issue: {N}`

After PR creation, the lead extracts learnings (step 13):
- All learnings → `.mill/observations/ship-{N}-learnings.md` with `suggested:` routing hint
- Human decides final routing via `/mill:ground`

All observations reviewed via `/mill:ground` or during `/mill:spec` pre-flight.

## Rules

1. **Lead never implements** when teammates available — always delegate
2. **Verifier is always separate** — clean context, never saw implementation reasoning
3. **Spec drives everything** — implement what's specified, nothing more
4. **Test before PR** — all tests must pass
5. **Loop Contract governs iterations** — max cycles from spec, then escalate
6. **Always clean up** — worktree removed after PR creation
7. **Commits reference issue** — every message includes `#{N}`
