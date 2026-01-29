# Model

Build a complete, executable model for a feature before implementation.

## Preconditions
- `.mill/context.md` must exist
- Read project instructions: check `AGENTS.md` (or `CLAUDE.md` if no AGENTS.md)
- Read `.mill/features/`, `.mill/standards/`, architecture docs

## Rules
- **One question at a time** — don't batch questions
- Integrate architecture, standards, constraints, success criteria
- If ambiguous, stop and request clarification

## Flow

### 1. Understand Intent

```
what are we modeling?

{inferred from USER_PROMPT or context}

is this correct? [y/edit]
```

### 2. Elicit Section by Section

Walk through each section one at a time:

| Section | Key Question |
|---------|--------------|
| Intent | "what should this do and why?" |
| Inputs & Outputs | "what goes in, what comes out?" |
| Constraints | "what technical/product/security limits apply?" |
| Assumptions | "what are we assuming to be true?" |
| Success Criteria | "how do we know it works? (must be testable)" |
| Verification | "what commands prove success?" |
| Limits | "max iterations, time budget, cost budget?" |
| Non-Goals | "what are we explicitly NOT doing?" |
| Risks | "what could go wrong, and how do we mitigate?" |

For each section:
```
[{n}/9] {section}

{question}

based on context:
{inferred answer or options}

approve? [y/edit/skip]
```

Wait for response before continuing to next section.

### 3. Validate & Save

```
model complete:

{brief summary}

sections: {n}/9 filled
skipped: {list if any}

save to .mill/model/{name}.md? [y/n]
```

## Output Template

```md
# Model: <title>

## Intent
<what and why>

## Inputs & Outputs
<boundaries>

## Constraints
<technical, product, security, operational>

## Assumptions

## Success Criteria
<machine-checkable, aligned to tests>

## Verification
<commands + expected outcomes>

## Limits
- Max iterations: <n>
- Time budget: <min>
- Cost budget: <optional>

## Non-Goals

## Risks
<known risks and mitigations>
```

## Input
{{USER_PROMPT}}
