# Ship — Work Iteration {{ITERATION}}/{{MAX_ITERATIONS}}

You are implementing a spec. Work autonomously — implement one slice, test, commit, signal.

## Spec

**Issue:** #{{ISSUE_NUMBER}} ({{SPEC_REF}})

{{SPEC_CONTENT}}

## Project Context

{{CONTEXT}}

## Domain Guidance

{{DOMAIN_GUIDANCE}}

{{REJECTION_CONTEXT}}

## Instructions

1. Read the spec above carefully
2. If this is iteration 1, plan slices and write plan to `.mill/ship/work/issue-{{ISSUE_NUMBER}}-plan.md`
3. If iteration > 1, read the plan and continue from where you left off
4. Implement ONE slice only
5. Run tests to verify your changes work
6. Commit changes with a descriptive message referencing #{{ISSUE_NUMBER}}
7. Update the plan (mark completed slices)
8. Signal your status (see below)

## Slicing Rules

- One slice per iteration — don't bundle unrelated changes
- Slice by concern: Model, Logic, Interface, Tests
- Tests verify each slice — run before committing
- Commit before signaling — changes must be committed

## Observations

During implementation, note discoveries for later review. Write observation files:
- Path: `.mill/observations/ship-{{ISSUE_NUMBER}}-{slug}.md`
- Frontmatter: source: ship, type: (concern|discovery|suggestion), issue: {{ISSUE_NUMBER}}
- Don't interrupt the ship flow — observations are reviewed later

## Signals

After completing your slice, you MUST output exactly one of these signals at the END of your response.

**More slices remain:**
```
MILL_CONTINUE
{"done": "what you completed", "next": "what comes next"}
```

**All slices complete, ready for verification:**
```
MILL_VERIFY
{"branch": "issue-{{ISSUE_NUMBER}}", "title": "#{{ISSUE_NUMBER}}: <concise title>", "done": "what you completed", "summary": "overall summary of changes", "verification": "test results summary"}
```

**Cannot proceed (spec is impossible, not just difficult):**
```
MILL_ABORT: <reason>
```

## Rules

- Honor the spec — don't add unrequested features
- Minimize unrelated changes
- Test before signaling
- Commit before signaling
- One slice per iteration
- MILL_ABORT only for impossible specs, not difficulties
- The signal MUST be the last thing you output
