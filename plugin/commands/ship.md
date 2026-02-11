---
description: "Implement a spec → Pull Request • https://mill.mindrevolution.com/ship"
allowed-tools: Read, Write, Edit, Glob, Grep, Bash(*gh *, *git *, *rm *, *mkdir *)
argument-hint: "<issue-number> - GitHub issue to implement"
---

# Ship

Implement a spec as a team. You are the **lead** — you orchestrate, delegate, and verify. You never implement directly when teammates are available.

**IMPORTANT: `mill` is NOT a CLI tool. Never run `mill` as a shell command. All operations below use Claude Code's native tools (Read, Write, Edit, Glob, Grep, Bash) directly.**

## Interaction Pattern

Ship is mostly autonomous — the spec should be complete from `/mill:spec`. **Use AskUserQuestion only when:**

- Spec has ambiguity that blocks implementation
- Multiple valid approaches exist and choice matters
- Scope clarification needed before proceeding

Don't ask about implementation details you can decide autonomously.

---

## Workflow

### 1. Pick Issue

If no issue number was provided as argument:

```bash
gh issue list --state open --json number,title,labels --limit 100
```

Filter for issues with `spec` label. Present choices:

```yaml
AskUserQuestion:
  question: "Which spec do you want to ship?"
  header: "Spec"
  options:
    - label: "#42 — Add export feature"
      description: "feature, backend"
    - label: "#38 — Fix auth timeout"
      description: "bug, fullstack"
```

### 2. Load Spec

```bash
gh issue view {N} --json number,title,body,labels,state,createdAt
```

Parse the JSON. Confirm the spec is open and has a type label.

**Parse Loop Contract** from the spec body:
- Extract `max_iterations` — look for `**Max Iterations:**` first, fall back to `**Stop Conditions:**`. Default: `5` if missing or still a placeholder (`{{...}}`).
- Extract `test_command` — from `**Test Command:**`
- Extract `verification_commands` — from `**Verification Commands:**`

Store these values for use in steps 4, 7, 9, and 10.

### 3. Load Context + Domain Guidance

Check context freshness inline:
1. Read `.mill/context.md` — extract hash from first line
2. `git rev-parse HEAD` — compare
3. If missing or stale, run `/mill:warmup` first

Load domain guidance from the plugin:
```
Glob("**/templates/domains/{domain}.md")
```
Read the match. Where `{domain}` comes from the spec's labels (backend, application, website, platform).

### 4. Detect Project Settings

Detect default branch from git:
```bash
git symbolic-ref refs/remotes/origin/HEAD 2>/dev/null || echo "refs/remotes/origin/main"
```
Parse the branch name (last segment).

Detect test command. **Precedence order:**
1. **Loop Contract** `test_command` from the spec (if concrete, not a placeholder)
2. **Project files** — inspect `package.json` scripts, `Makefile`, `pyproject.toml`, CI workflows
3. **Ask the user** — if still ambiguous

Similarly, store `verification_commands` from the Loop Contract for use in implementer and verifier prompts. If empty or placeholder, set to `echo "no additional verification commands"`.

### 5. Create Worktree

```bash
git worktree add .mill/ship/work/issue-{N} -b issue-{N}
```

If the branch already exists (from a previous attempt):
```bash
git worktree add .mill/ship/work/issue-{N} issue-{N}
```

Copy context to worktree using the Read and Write tools (not `cp`):
1. Use the **Read** tool on `.mill/context.md`
2. Use the **Write** tool to save the content to `.mill/ship/work/issue-{N}/.mill/context.md`

### 6. Determine Team Size

Count the approach parts (A) in the spec and apply this table. Do not override based on coupling assessment — part count is the determinant:

| Approach Parts | Domain | Team Size |
|----------------|--------|-----------|
| 1–4 | Single | 1 implementer |
| 5–9 | Single | 2 implementers (split by concern) |
| Any | Fullstack | 2–3 implementers (one per layer) |
| 10+ | Any | 3–4 implementers |

Always +1 verifier (separate from implementers).

### 7. Spawn Implementers

For each implementer, use the Task tool to launch a teammate agent. Each gets:

1. **Their portion of the spec** — specific requirements and approach parts assigned to them
2. **Project context** — from `.mill/context.md`
3. **Domain guidance** — from plugin templates
4. **File ownership boundaries** — explicit: "you may ONLY edit files in src/api/ and src/models/"
5. **Working directory** — the worktree path `.mill/ship/work/issue-{N}/`
6. **Test command** — detected from project
7. **Teammate prompt template** — load from plugin via `Glob("**/templates/teammates/implementer.md")` and fill placeholders

Use `Task` tool with `subagent_type: "general-purpose"`. Build the prompt by reading the discovered implementer template and substituting:
- `{{ISSUE_NUMBER}}` → issue number
- `{{TASK_ASSIGNMENTS}}` → their specific tasks
- `{{FILE_BOUNDARIES}}` → their file ownership
- `{{SPEC_CONTENT}}` → full spec body
- `{{CONTEXT}}` → project context
- `{{DOMAIN_GUIDANCE}}` → domain template content
- `{{TEST_COMMAND}}` → detected test command
- `{{WORKTREE_PATH}}` → absolute path to worktree
- `{{ITERATION_FEEDBACK}}` → cumulative iteration log (see step 10). First run: `"First implementation pass — no prior iteration history."`
- `{{VERIFICATION_COMMANDS}}` → from Loop Contract (or `echo "no additional verification commands"` if none)

For a team-of-1: single implementer gets the full spec and all files.

Launch implementers in parallel when their work doesn't overlap (e.g., backend + frontend). Launch sequentially when one depends on another's output (e.g., API contract needed by frontend).

### 8. Monitor Progress

Watch task completion. When implementers report:
- **Need clarification** → Lead resolves or relays to user
- **Cross-team dependency** → Lead relays contracts between teammates (e.g., "backend teammate says the API shape is `{...}`, use this")
- **Stuck** → Lead redirects with specific guidance
- **Done** → Proceed to verification when all implementers complete

### 9. Spawn Verifier

After all implementation tasks complete, spawn a verifier — a separate teammate with clean context.

Discover verifier template via `Glob("**/templates/teammates/verifier.md")`, read it, and substitute:
- `{{ISSUE_NUMBER}}` → issue number
- `{{SPEC_CONTENT}}` → full spec body
- `{{WORKTREE_PATH}}` → absolute path to worktree
- `{{TEST_COMMAND}}` → detected test command
- `{{DEFAULT_BRANCH}}` → detected from git
- `{{VERIFICATION_COMMANDS}}` → from Loop Contract (or `echo "no additional verification commands"` if none)

The verifier receives NO iteration history — clean context is preserved across all cycles.

Use `Task` tool with `subagent_type: "general-purpose"`.

The verifier:
- Runs tests
- Reviews `git diff {default_branch}...HEAD` in the worktree
- Checks each spec criterion
- Reports: pass or reject with specific blockers

### 10. Handle Verification Result

**Pass** → Proceed to step 11 (Create PR).

**Reject** → Loop Contract-driven iteration:

Track cumulative iteration state:
- `cycle` — current cycle number (starts at 1)
- `max_iterations` — from Loop Contract (default 5)
- `history` — list of `{ cycle, blockers, fixes_attempted, what_passed }`

For each rejection cycle:
1. Parse verifier's blockers
2. Match each blocker to the implementer who owns those files
3. Append to iteration history: `{ cycle, blockers, fixes_attempted: [], what_passed: [criteria that passed] }`
4. Build `{{ITERATION_FEEDBACK}}` from cumulative history — a formatted log of all previous cycles:
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
5. Re-launch the responsible implementer(s) with updated `{{ITERATION_FEEDBACK}}`
6. After fix, re-run verifier (always with clean context — no iteration history)
7. Increment cycle counter

**On exhaustion** (cycle > max_iterations):
Report to user with full iteration log and offer options:

```yaml
AskUserQuestion:
  question: "Reached {max_iterations} iteration cycles. How to proceed?"
  header: "Iterations"
  options:
    - label: "Continue iterating"
      description: "Grant {N} more cycles"
    - label: "Create PR as-is"
      description: "Open PR with known issues documented"
    - label: "Abort"
      description: "Remove worktree and stop"
```

If user chooses "Continue iterating," ask how many additional cycles and resume the loop. If "Create PR as-is," proceed to step 11 with issues noted. If "Abort," skip to step 12 (cleanup only).

### 11. Create PR

From the worktree directory:

```bash
git -C .mill/ship/work/issue-{N} push -u origin issue-{N}
```

Build PR body and write to temp file:
```
Write(".mill/.prompt", pr_body)
```

PR body format:
```markdown
## Summary
{1-3 bullet points summarizing what changed}

Closes #{N}

## Verification
- All spec criteria verified by independent reviewer
- Test command: `{test_command}` — passing
{if cycles > 1:}
## Iteration Summary
- **Cycles:** {cycle} of {max_iterations}
- {brief summary of what was fixed across iterations}
{end if}

🤖 Claude
```

Create PR:
```bash
gh pr create --title "#{N}: {spec title}" --body-file .mill/.prompt --head issue-{N}
```

Parse the PR URL from output. Clean up temp file:
```bash
rm .mill/.prompt
```

### 12. Cleanup

```bash
git worktree remove --force .mill/ship/work/issue-{N}
```

Report the PR URL to the user.

---

## Fallback: Single-Session Mode

If agent teams are not available (Task tool limited or experimental features disabled), fall back to single-session mode:

**Detection:** If the first Task tool call fails or returns an error about agent capabilities, switch to fallback.

**In fallback mode:**
1. Lead implements directly (no teammates) in the worktree
2. Work through approach parts sequentially: implement → test → commit for each
3. After all parts complete, enter explicit "verification phase":
   - Re-read the full spec with fresh eyes
   - Run `git diff {default_branch}...HEAD` and review the full changeset
   - Run test command
   - Check each criterion
4. If issues found, fix and re-verify (up to Loop Contract `max_iterations`, default 5)
5. Create PR as normal

Report to user: "Running in single-session mode (agent teams not available)"

---

## Observations

During implementation, teammates write observations to `.mill/observations/`:
- Path: `.mill/observations/ship-{N}-{slug}.md`
- Frontmatter: `source: ship`, `type: concern|discovery|suggestion`, `issue: {N}`

These are reviewed later via `/mill:ground`. Don't interrupt the ship flow.

## Rules

1. **Lead never implements** when teammates are available — always delegate
2. **Verifier is always separate** — clean context, never saw implementation reasoning
3. **Spec drives everything** — implement what's specified, nothing more
4. **Test before PR** — all tests must pass
5. **Loop Contract governs iterations** — max cycles from spec (default 5), then escalate to user
6. **Always clean up** — worktree removed after PR creation
7. **Commits reference issue** — every commit message includes `#{N}`
