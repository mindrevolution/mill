---
allowed-tools: Read, Write, Edit, Glob, Grep, Bash
description: Guide architecture design and store it under spec/backend/ or spec/frontend/.
argument-hint: [system or subsystem description]
model: claude-opus-4-5
---

# Preprocessing
If "USER_PROMPT" matches ^#\d+ (e.g., #42), then:
1. Extract the number and store it as GITHUB_ISSUE_NUMBER.
2. Run: `gh issue view <number> --json title,body`.
3. Replace USER_PROMPT with combined title and body.
4. Proceed with the normal architecture workflow.

# Architect
Using USER_PROMPT, help the user architect a system or subsystem. Guide them section-by-section and build a complete architecture spec.

Ensure the documentation is technically precise, implementation-aware, and free of fluff.

## Variables
USER_PROMPT: $1
SPEC_DIRECTORY: "./spec/"
TARGET_AREA: [backend|frontend|fullstack; ask user, default backend]
ARCHITECTURE_FILENAME: "architecture.md"
GITHUB_ISSUE_NUMBER: [optional GitHub issue number]
DEFAULT_STACK: ".NET, EF Core, PostgreSQL, Hangfire, Valkey, S3 (Valet Key), Kamal deploy; optionally MS Orleans"
STACK_CHOICE: [prompt user, fallback to DEFAULT_STACK]
ARCHITECTURE_STYLE: [default: Modular Monolith, override only if user specifies]

## Instructions
1. Restate USER_PROMPT and confirm intent.
2. Ask for TARGET_AREA and stack confirmation.
3. Assume Modular Monolith unless explicitly changed.
4. Proceed section-by-section, asking targeted questions.
5. Save to:
   - `./spec/backend/architecture.md` if TARGET_AREA is backend.
   - `./spec/frontend/architecture.md` if TARGET_AREA is frontend.
   - `./spec/architecture.md` if TARGET_AREA is fullstack.
6. After all sections, output a brief report with the resolved file path and summary.

## Sections (in order)
1. Philosophy
2. Core Principles
3. System Context
4. Architecture Overview (include state management)
5. Data Flows
6. Database Strategy (if backend or fullstack)
7. Security Model
8. Configuration Strategy
9. Observability
10. Deployment Model
11. External Dependencies
12. Failure Modes
13. Scaling Model
14. Trade-offs
15. Summary

## Final Report
```text
? Architecture Document Created

File: <resolved path>
System: <brief system description>
Stack: <STACK_CHOICE>
Architecture: <ARCHITECTURE_STYLE>
```
