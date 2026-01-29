# Spec Refine

Analyze and update an existing specification to align with current codebase reality.

**CRITICAL:** Do NOT use Claude Code's built-in `/plan` mode, plan files, or TodoWrite tool. Use only MILL's workflow.

## Context
Pre-loaded: GitHub issue content, `.mill/context.md`, `.mill/standards/*.md`, `.mill/memory/project.md`, uncommitted changes.

Issue: `{{ISSUE_NUMBER}}`

## Purpose

Specs can become stale as codebases evolve. This prompt helps surface:
1. **Outdated assumptions** — APIs renamed, files moved, patterns changed
2. **Missing context** — conventions, gotchas, constraints that would improve AI execution
3. **Ambiguities** — vague criteria that could cause rework

## Flow

### 1. Analyze Current State

Read the existing spec and compare against current codebase:

1. **Check referenced files** — Do they still exist? Have they moved?
2. **Check APIs/interfaces** — Are they still the same? Renamed? Deprecated?
3. **Check patterns** — Does the spec assume patterns that have since changed?
4. **Check dependencies** — Are referenced dependencies still used?

Use tools to verify:
- `Glob` to find files
- `Grep` to search for patterns
- `Read` to examine current implementations

### 2. Identify Gaps

For each finding, categorize as:

| Category | Example | Impact |
|----------|---------|--------|
| **Breaking** | Referenced file deleted | Blocks execution |
| **Misleading** | API renamed but not updated in spec | Causes wrong approach |
| **Missing** | Convention not documented | Suboptimal implementation |
| **Ambiguous** | "Make it faster" without metric | Unclear success criteria |

### 3. Propose Updates

Present findings one by one:

```
found {n} potential updates:

1. [category]: [finding]
   current: [what the spec says]
   reality: [what the codebase shows]
   suggested update: [proposed text]

apply? [y/n/edit]
```

For each:
- **y** — Accept suggestion as-is
- **n** — Skip this update
- **edit** — User provides alternative text

### 4. Add Missing Context

After addressing gaps, ask:

```
any additional context to add?
(conventions, gotchas, constraints that would help execution)

1. add context
2. done
```

If user adds context, help structure it into the spec's appropriate section.

### 5. Validate Updated Spec

Run the same validation as spec-draft:

```
[ ] success criteria testable
[ ] each criterion verifiable
[ ] scope clear (in/out)
[ ] no placeholders
[ ] Loop Contract present
```

### 6. Iterate

After initial analysis and proposed changes, the user may want to:
- Discuss findings further
- Add more context through conversation
- Refine acceptance criteria
- Clarify scope boundaries

**Stay in conversation.** Don't rush to finalize. The user drives when the spec is ready.

```
anything else to refine?

1. review another section
2. add more context
3. finalize and update issue
```

### 7. Confirm and Update

When user chooses to finalize, present the complete updated spec:

```
updated spec ready:

[full spec with changes highlighted]

---
changes:
  • [change 1]
  • [change 2]

apply to issue #{{ISSUE_NUMBER}}? [y/n]
```

**STOP HERE.** Do not update the issue without explicit approval.

### 8. Apply

After explicit user approval:

1. Update issue: `gh issue edit {{ISSUE_NUMBER}} --body "{updated spec}"`
2. Output exactly:
   ```
   updated: #{{ISSUE_NUMBER}}

   mill run {{ISSUE_NUMBER}}

   /exit
   ```

## Rules

1. **Read-only until approved** — Never update the issue without explicit user confirmation
2. **One finding at a time** — Don't overwhelm; let user process each change
3. **Preserve intent** — Updates should align spec with reality, not change scope
4. **Scope creep → separate spec** — If gaps reveal new work, suggest `mill spec` instead
5. **Human approves all changes** — AI identifies gaps, human decides what to update
6. **Conversational** — User can chat, ask questions, and refine iteratively before finalizing
7. **End cleanly** — After applying update, stop
