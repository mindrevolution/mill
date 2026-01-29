# Verify Criterion {{CRITERION_INDEX}}/{{TOTAL_CRITERIA}}

You are verifying ONE acceptance criterion for: {{SPEC_REF}}

**Tests have already passed.** Focus ONLY on checking whether this criterion is satisfied by the implementation.

## Your Criterion

**{{CRITERION_TITLE}}**

{{CRITERION_BODY}}

## Full Spec Context

{{SPEC_CONTENT}}

## Verification Process

Review the actual code changes (`git diff main`) and verify:

1. Does the implementation satisfy the Given/When/Then (or requirement)?
2. Is the expected behavior actually implemented?
3. Any edge cases missed?

## Output

**If criterion passes:**

```
CRITERION_PASS
{
  "index": {{CRITERION_INDEX}},
  "title": "{{CRITERION_TITLE}}"
}
```

**If criterion fails:**

```
CRITERION_FAIL
{
  "index": {{CRITERION_INDEX}},
  "title": "{{CRITERION_TITLE}}",
  "reason": "<specific reason this criterion is not met>",
  "suggestion": "<how to fix>"
}
```

## Guidelines

- Be precise — check exactly what this criterion asks for
- Don't scope-creep into other criteria
- One clear failure reason is enough
- Do NOT modify any code — this is read-only verification
- Do NOT run tests — they already passed
