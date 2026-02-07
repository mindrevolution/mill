# Concept

Develop a concept for SaaS, mobile, or software platforms from USER_PROMPT.

## Preconditions
- Read project instructions: check `AGENTS.md` (or `CLAUDE.md` if no AGENTS.md)

## Rules
- Determine: new product, new product area, or feature in existing product
- For features: ensure `.mill/context.md` exists (run warmup if not)
- **One question at a time** — don't ask multiple questions in one message
- Avoid technical implementation details
- Gather user details first, suggest only after direction is clear
- If USER_PROMPT is `#<number>`, fetch issue via `gh issue view <number> --json body,title`

## Output
Save to `.mill/concepts/<shortname>.md` (append `#<number>` if from issue).

## Flow

### 1. Classify

```
what type of concept?

1. new product
2. new product area (within existing product)
3. feature (within existing area)
```

### 2. Elicit Section by Section

Walk through each section one at a time:

| Section | Key Question |
|---------|--------------|
| Problem & Target Audience | "who has this problem, and what's painful about it?" |
| Core Idea | "in one sentence, what's the solution?" |
| Functional Goals | "what should users be able to do?" |
| Key Features | "what are the 3-5 must-have features?" |
| User Journey | "walk me through the happy path" |
| Optional Enhancements | "nice-to-haves for later?" |
| Constraints & Non-Goals | "what are we explicitly NOT doing?" |
| Success Signals | "how do we know this worked?" |

For each section:
```
[{n}/8] {section}

{question}

1. {option inferred from context}
2. {option}
3. describe in your own words
```

Wait for response before continuing to next section.

### 3. Summarize & Confirm

```
concept summary:

{2-3 sentence summary}

sections complete: {n}/8

save to .mill/concepts/{shortname}.md? [y/edit/n]
```

## Template

```md
# Concept: <title><#ISSUE if applicable>

## Problem & Target Audience
## Core Idea
## Functional Goals
## Key Features
## User Journey (High-Level)
## Optional Enhancements
## Constraints & Non-Goals
## Success Signals
## Open Questions
## Summary
```

## Input
{{USER_PROMPT}}
