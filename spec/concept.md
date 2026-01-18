---
allowed-tools: Read, Write, Edit, Glob, Grep, Bash
description: Develop a product or feature concept and store it under .mill/concepts/.
argument-hint: [initial idea prompt]
model: claude-opus-4-5
---

# Preprocessing
If "USER_PROMPT" matches ^#\d+ (e.g., #42), then:
1. Extract the number and store as "GITHUB_ISSUE_NUMBER".
2. Run `gh issue view <number> --json body,title`.
3. Replace USER_PROMPT with the retrieved issue content.
4. Continue with the normal "Concept" flow.

# Concept
Use USER_PROMPT as the starting point to develop a concept for SaaS, mobile, or software platforms.
- First determine if this is a new product, a new product area within an existing product, or a feature inside an existing product.
- If it is a feature inside an existing product, run `/ralph:warmup` before continuing.
- Guide the user with structured questions to expand the concept.
- Avoid technical implementation details.
- Do not propose improvements early; gather user details first.
- After several rounds, summarize the concept.
- Introduce sparse, optional suggestions only after the user's direction is clear.
- Save the final concept to `.mill/concepts/<concept_shortname>.md`.
- If GITHUB_ISSUE_NUMBER is provided, append `#<number>` to the filename.

## Variables
USER_PROMPT: $1
CONCEPTS_DIRECTORY: "./.mill/concepts/"
GITHUB_ISSUE_NUMBER: [issue number if extracted]

## Instructions
- If USER_PROMPT is empty, request it.
- Identify: target users, core problem, intended outcomes, main functional blocks.
- Drive an iterative loop: ask -> receive -> deepen -> summarize.
- Keep focus on user experience and outcomes; avoid architecture or stack.
- Derive a concise kebab-case `<concept_shortname>` from the central theme.
- Write a complete concept document to `CONCEPTS_DIRECTORY/<concept_shortname>.md`.

## Concept Format
Produce the markdown file exactly in this structure:

```md
# Concept: <concept title><#GITHUB_ISSUE_NUMBER if applicable>

## Problem & Target Audience
<Define the key pain point and who experiences it.>

## Core Idea
<High-level description of the proposed feature or product.>

## Functional Goals
<List what users should be able to accomplish.>

## Key Features
- <feature>
- <feature>
- <feature>

## User Journey (High-Level)
<Describe how a typical user interacts with the concept.>

## Optional Enhancements
<Only include elements suggested after the main idea was clarified.>

## Constraints & Non-Goals
<Clarify what the concept intentionally avoids or excludes.>

## Summary
<Short, consolidated recap of the concept.>
```
