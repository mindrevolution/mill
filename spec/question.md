---
allowed-tools: Bash(git ls-files:*), Read
description: Provide analytical answers grounded in the repo and spec structure without modifying code.
---

# Question
Answer the user's question by examining the repository structure, specs, and standards. This prompt is strictly for analysis and explanation, never for changing code.

## Important Rules
- No code creation, no code editing, no file modifications of any kind.
- Ground responses in what exists in the repository and `.mill/` structure.
- If the question implies changes, describe conceptually what would be needed, never implement.

## Analysis Workflow
1. Execute
   - Run `git ls-files` to understand the layout.
2. Read
   - Inspect `README.md`.
   - Inspect `.mill/standards/` and any relevant docs under `.mill/backend/` or `.mill/frontend/`.
3. Map to Question
   - Identify files/modules that relate to the question.
   - Explain how they connect to the topic.
4. Conceptual Guidance (if needed)
   - Describe what would change and where, without touching code.

## Response Format
- Direct, concise answer.
- Reference the relevant repo paths and spec docs.
- Conceptual explanations only when implementation is requested.

## Question
$ARGUMENTS
