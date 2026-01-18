---
allowed-tools: Read, Write, Edit, Glob, Grep, Bash
description: Generate a Ralph-ready implementation plan with a loop contract and store it in .mill/plans/.
argument-hint: [user prompt]
model: claude-opus-4-5
---

# Preprocessing
If "USER_PROMPT" matches ^#\d+ (e.g., #42), then before planning:
1. Extract the number and store as "GITHUB_ISSUE_NUMBER".
2. Run `gh issue view <number> --json body,title`.
3. Replace USER_PROMPT with the retrieved issue content.
4. Continue with the normal "Plan" flow.

# Plan
Construct a technically precise, loop-ready implementation plan based on USER_PROMPT.
The plan must be sized for a single GitHub issue and include a Loop Contract that a Ralph loop can execute.

## Variables
USER_PROMPT: $1
PLAN_DIRECTORY: "./.mill/plans/"
GITHUB_ISSUE_NUMBER: [github issue number if provided in USER_PROMPT]

## Instructions
- If USER_PROMPT is not provided, request it and stop.
- Inspect the codebase to identify existing patterns and conventions.
- Identify task type (chore|feature|refactor|bug|enhancement) and complexity (simple|medium|complex).
- Enforce a single-issue feasibility gate:
  - If the work is not feasible as a single issue, stop and ask to split.
  - Provide a suggested decomposition list with candidate issue titles.
- Produce a plan adhering strictly to the Plan Format below.
- Derive a descriptive kebab-case filename capturing the plan scope.
- Save to `PLAN_DIRECTORY/<descriptive-name>.md`. Append `#<issue>` if provided.
- The plan must include explicit success criteria and a completion promise token.
- Include validation commands that are executable and unambiguous.

## Plan Format
Use the exact structure below:

```md
# Plan: <task name><#GITHUB_ISSUE_NUMBER if provided>

## Task Type & Size
- Type: <chore|feature|refactor|bug|enhancement>
- Complexity: <simple|medium|complex>

## Task Description
<Describe the task in detail based on the prompt.>

## Objective
<State what will be accomplished when this plan is complete.>

## Scope & Non-Goals
<Define scope boundaries and explicit exclusions.>

## Feasibility Gate
- Single-issue feasible: <yes|no>
- Rationale: <short explanation>
- If "no", list the minimal split into multiple issues and stop.

## Solution Approach
<Describe the approach and how it meets the objective.>

## Relevant Files
<List files relevant to the task with brief reasons.>

## Step by Step Tasks
IMPORTANT: Execute every step in order, top to bottom.

### 1. <First Task Name>
- <specific action>
- <specific action>

### 2. <Second Task Name>
- <specific action>
- <specific action>

<Continue with additional tasks as needed. The last step must validate the work.>

## Testing Strategy
<Describe tests and edge cases appropriate to the task.>

## Acceptance Criteria
<List specific, measurable criteria for completion.>

## Loop Contract
- Success Criteria: <machine-checkable criteria aligned to Acceptance Criteria>
- Completion Promise: <exact token string to output when done, e.g. RALPH_DONE>
- Verification Commands: <commands and expected signals>
- Stop Conditions: <max iterations, time, or budget>
- Progress Reporting: <where and how to report each iteration, e.g. GitHub issue update format>

## Validation Commands
<List exact commands to validate the work.>

## Notes
<Optional context, dependencies, or constraints.>
```

## Report
After saving the plan, provide a brief summary:

```
? Implementation Plan Created

File: PLAN_DIRECTORY/<filename><#GITHUB_ISSUE_NUMBER>.md
Topic: <brief description>
Key Components:
- <main component 1>
- <main component 2>
- <main component 3>
```
