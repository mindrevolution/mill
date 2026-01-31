# Draft Relevance Validation

Validate whether a draft specification is still relevant to the current codebase state.

## Context

Pre-loaded: `.mill/context.md`

## Draft

{{DRAFT_CONTENT}}

## Purpose

Drafts can become stale as codebases evolve. This validation checks:
1. **Implementation status** — Has this already been built (fully or partially)?
2. **Architecture alignment** — Does the proposed approach still fit current patterns?
3. **Reference validity** — Do mentioned files, APIs, and dependencies still exist?
4. **Problem relevance** — Is the problem/opportunity still real?

## Validation Process

### 1. Quick Orientation

Read `.mill/context.md` to understand:
- Project structure and tech stack
- Recent changes (last 10 commits)
- Current architecture patterns

### 2. Extract Key References

From the draft, identify:
- **Files mentioned** — paths, components, modules
- **APIs/interfaces** — function names, endpoints, class names
- **Patterns** — architectural approaches, libraries, frameworks
- **Problem indicators** — error messages, behaviors, symptoms

### 3. Targeted Verification

For each reference type, verify against codebase:

| Check | Method | Finding if Failed |
|-------|--------|-------------------|
| Files exist | `Glob` for paths | "referenced file moved/deleted" |
| APIs exist | `Grep` for names | "API renamed/removed" |
| Pattern used | `Grep`/`Read` | "architecture changed" |
| Problem exists | `Grep` for symptoms | "issue may be resolved" |

**Type-specific checks:**

| Draft Type | Additional Checks |
|------------|-------------------|
| **Feature** | Similar feature exists? Duplicate functionality? |
| **Bug** | Error still reproducible? Fix already merged? |
| **Security** | Vulnerability still present? Data flow changed? |
| **Task** | Refactor already done? Dependency removed? |

### 4. Score Relevance

Based on findings, assign a score:

| Score | Meaning | Condition |
|-------|---------|-----------|
| **10** | Fully relevant | All references valid, problem/opportunity intact |
| **8-9** | Mostly relevant | Minor references stale, core still applies |
| **6-7** | Partially relevant | Some sections need update, core valid |
| **4-5** | Questionable | Significant changes affect approach |
| **2-3** | Mostly obsolete | Major assumptions invalid |
| **0-1** | Discard | Fully implemented, or problem no longer exists |

### 5. Determine Verdict

| Score Range | Verdict | Action |
|-------------|---------|--------|
| **8-10** | `current` | Proceed with draft |
| **4-7** | `review` | User should review findings before proceeding |
| **0-3** | `discard` | Draft is obsolete, recommend archiving |

## Output

Output ONLY valid JSON (no markdown, no explanation):

```json
{
  "score": <0-10>,
  "verdict": "<current|review|discard>",
  "findings": [
    {
      "category": "<implemented|moved|renamed|changed|resolved|obsolete>",
      "severity": "<info|warning|critical>",
      "reference": "<what was checked>",
      "expected": "<what draft assumed>",
      "actual": "<what was found>",
      "impact": "<how this affects the draft>"
    }
  ],
  "summary": "<1-2 sentence human-readable assessment>",
  "recommendation": "<specific next step>"
}
```

**Example outputs:**

Feature already implemented:
```json
{
  "score": 1,
  "verdict": "discard",
  "findings": [
    {
      "category": "implemented",
      "severity": "critical",
      "reference": "user authentication flow",
      "expected": "no auth exists",
      "actual": "AuthService with JWT implemented in src/services/auth.ts",
      "impact": "feature request is complete"
    }
  ],
  "summary": "This feature has been fully implemented. The AuthService matches all acceptance criteria.",
  "recommendation": "Archive this draft. Verify implementation covers all acceptance criteria."
}
```

Bug already fixed:
```json
{
  "score": 0,
  "verdict": "discard",
  "findings": [
    {
      "category": "resolved",
      "severity": "critical",
      "reference": "null pointer in UserController.Get()",
      "expected": "crash on missing user",
      "actual": "null check added in commit abc123",
      "impact": "bug no longer reproducible"
    }
  ],
  "summary": "Bug was fixed in a previous commit. The null check prevents the crash.",
  "recommendation": "Close this draft. Add regression test if not present."
}
```

Partially stale but relevant:
```json
{
  "score": 7,
  "verdict": "review",
  "findings": [
    {
      "category": "renamed",
      "severity": "warning",
      "reference": "UserService.fetchUser()",
      "expected": "method exists at src/services/user.ts",
      "actual": "renamed to UserService.getById() in recent refactor",
      "impact": "acceptance criteria references wrong method name"
    },
    {
      "category": "moved",
      "severity": "info",
      "reference": "components/Button.tsx",
      "expected": "at root components folder",
      "actual": "moved to components/ui/Button.tsx",
      "impact": "file path in scope section outdated"
    }
  ],
  "summary": "Core feature request is valid but references are stale from recent refactoring.",
  "recommendation": "Update method names and file paths before proceeding to implementation."
}
```

## Rules

1. **JSON only** — Output must be parseable JSON, nothing else
2. **Be specific** — Include file paths, method names, commit hashes where relevant
3. **Check, don't assume** — Use tools to verify, don't guess based on naming
4. **Focus on blockers** — Minor staleness (typos, formatting) doesn't lower score
5. **Partial implementation matters** — If 50% built, score reflects that
6. **Recent commits are key** — Check if recent changes address the draft's concerns
