---
title: "Ground"
number: 3
subtitle: "Your project's knowledge base"
accent: "electric"
---

## What Ground Is

Ground is the shared knowledge of your project — domain, users, conventions, stack. Every mill skill reads ground before acting: specs check it for consistency, ship loads it for implementation guidance, ideas reference it to connect sparks to real users.

```
.mill/ground/
├── strategic/      # Vision, mission, goals
├── personas/       # Who you build for
├── rules/          # Constraints and conventions
├── decisions/      # Architectural decisions
├── vocabulary/     # Domain terminology
├── stack/          # Technology stack
├── schema/         # Data structures
├── design/         # Visual language
├── patterns/       # Code patterns
└── debt/           # Known technical debt
```

## The Ten Categories

### Strategic

```markdown
# Product Vision
SaaS platform for construction project management.
Primary metric: time from bid submission to project kickoff.
North star: under 48 hours for standard residential projects.
```

Why does this project exist? Where is it going? This is the compass that helps mill understand which features matter.

### Personas

```markdown
# Site Foreman
**Job:** Coordinate subcontractors and track daily progress on-site.
**Pain:** Paper timesheets, no real-time visibility into material deliveries.
**Trigger:** Opens the app at 6am to check today's crew assignments.
```

Real users, not abstractions. When a spec says "As a *site foreman*...", it means something specific because the persona is defined.

### Rules

```markdown
# API Error Format
All API errors return `{code, message, trace?}`.
No naked strings. No stack traces in production.
```

Enforceable constraints. When a spec proposes something that conflicts with a rule, mill flags it. When an implementation violates a quality bar, verification catches it.

### Decisions

```markdown
# Database: PostgreSQL over MongoDB
Chose relational model for strong consistency on financial data.
Revisit if we add real-time collaboration (MongoDB's change streams).
```

Not just the choice — the reasoning. Documenting the *why* means future you can evaluate whether the reasoning still holds.

### Vocabulary

```markdown
# Invoice
A billing document tied to a single project phase.
States: draft → sent → paid → void.
Fields: amount, tax, lineItems[], dueDate, projectPhaseId.
```

Domain terms defined once, used consistently. Ambiguous terminology is one of the biggest sources of implementation bugs.

### Stack

```markdown
# Backend
Runtime: Node.js 20 LTS
Framework: Fastify 4.x
ORM: Prisma 5.x
Database: PostgreSQL 16
```

Explicitly documented technologies. When mill generates implementation guidance, it knows your tools.

### Schema

```markdown
# Project
Fields: id (uuid), name (string), status (enum: active|paused|completed),
        ownerId (FK → User), createdAt (timestamp).
Constraints: name unique per owner. Status transitions: active↔paused, active→completed.
```

Data structures and relationships — the structural truth that specs and implementations must respect.

### Design

```markdown
# Color System
Primary: #2563EB (blue-600). Surface: #FAFAFA. Error: #DC2626.
Border radius: 8px (cards), 4px (inputs).
Font: Inter 400/500/700.
```

Your visual language. For application and website domains, design ground ensures consistent UI.

### Patterns

```markdown
# Repository Pattern
All database access goes through repository classes.
Never call Prisma directly from route handlers.
Example: UserRepository.findById(id) — not prisma.user.findUnique().
```

Code idioms your team uses. These become implicit standards that mill follows during implementation.

### Debt

```markdown
# Missing ReportService Tests
ReportService has 0% test coverage. Added during sprint 12 rush.
Risk: PDF export changes will have no safety net.
```

Known issues tracked explicitly. When debt items affect new features, mill includes them in spec context.

## Building Ground Knowledge

### Direct Creation

You tell mill what you know:

```
/mill:ground
→ "Create knowledge"
→ Choose category: Personas
→ Answer questions about the user
→ mill writes the persona file
```

The process is conversational. mill asks one question at a time, offering options where sensible.

### Observation Review

When specs are drafted and features shipped, mill observes gaps. After ship runs, it also extracts process learnings — debugging insights, file couplings, commands that took trial and error.

```
/mill:ground
→ "Review observations"
→ See: "Unknown persona: Finance Admin (from spec #42)"
→ Choose: Curate to ground / Create spec / Add to debt / Dismiss
```

Ship learnings arrive with a `suggested:` routing hint (e.g., `suggested: ground/patterns/`) based on what was learned. You confirm or override the suggestion.

> **Tip** — This is the primary growth mechanism. You don't have to remember to document things — mill notices what's missing and asks you to fill the gaps.

### Codebase Sync

For technical categories (stack, patterns, schema), mill can extract knowledge directly from your code:

```
/mill:ground
→ "Sync codebase"
→ mill reads package.json, analyzes patterns, identifies entities
→ Creates or updates ground files
```

## Ground as Context

Every skill reads ground before acting:

- **Spec** checks personas, vocabulary, and rules to validate requirements
- **Ship** loads stack, patterns, and schema to guide implementation
- **Idea** references personas to connect ideas to real users

The richer your ground, the better your specs and implementations.

## Best Practices

> **Tip** — Start small. You don't need all ten categories on day one. Begin with personas and rules — they inform specs most directly. Add categories as they become relevant.

> **Tip** — Review observations regularly. The learning inbox is where ground grows organically. Make it a habit after ship runs.

> **Rule** — One persona per file. One decision per file. Granularity makes knowledge reusable across specs.
