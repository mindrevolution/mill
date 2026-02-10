---
title: "Spec"
number: 6
subtitle: "Turn intent into precision"
accent: "cyan"
---

## The Art of Specification

A specification that requires clarifying questions is a draft, not a specification.

This single test separates mill specs from every other requirements document you've written. The bar is high because the payoff is high: a spec that passes this test can be implemented without back-and-forth, without "what did you mean by...?", without rework.

## The Structure

Every mill spec has four sections that form a provable chain:

### Requirements (R)

What the solution must achieve. Each requirement gets an ID, a description, and a status:

| Status | Meaning |
|--------|---------|
| **core** | Must be implemented. No ship without it. |
| **must-have** | Required but could be deferred to a follow-up. |
| **nice-to-have** | Valuable but not blocking. |
| **out** | Explicitly excluded. Saying "no" is a decision too. |

### Approach (A)

How you'll build it. Broken into parts, each describing a specific mechanism — a model change, an API endpoint, a UI component, a migration script.

Approach parts can be flagged with ⚠️ when uncertainty exists. These flags must be resolved before the spec is ready. You can't ship ambiguity.

### Criteria (C)

Testable conditions that prove each requirement is met. Not "it should work" — but "given X input, the system returns Y with status Z in under 500ms."

Good criteria are:
- **Binary** — they pass or fail, no "kind of"
- **Automated** — they can be verified by a test command
- **Independent** — each criterion tests one thing

### Coverage (R × A × C)

The proof matrix. Every core and must-have requirement must have:
- At least one approach part implementing it
- At least one criterion verifying it

If a row in the matrix has ❌ in the approach column or — in the criteria column, the spec isn't complete.

## The Workflow

### Starting Fresh

```
/mill:spec "Add PDF export for monthly reports"
```

mill checks your project context, loads ground knowledge, and begins the conversation.

### The Conversation

mill asks one question at a time, using structured options where sensible:

1. **What type of change?** Feature / Bug / Task / Security
2. **What domain?** Backend / Application / Website / Platform / Full-stack
3. **What's the core problem?** (your words)
4. **What must the solution achieve?** (requirements emerge)
5. **How should we build it?** (approach forms)
6. **How do we prove it works?** (criteria crystalize)

A draft is saved early and updated as you go. You can stop mid-conversation and resume later — mill picks up where you left off.

### Validation

Before publishing, mill validates your spec against the principles:

- [ ] **Self-Containment** — no statement requires external clarification
- [ ] **Language Independence** — describes what/why, not language-specific how
- [ ] **Decision Completeness** — no TBDs, all parameters bound
- [ ] **Explicit Non-Applicability** — omitted sections marked N/A with reason
- [ ] **Coverage** — all core/must-have requirements have approach and criteria
- [ ] **No ⚠️ flags** — all uncertainties resolved

### Publishing

When validation passes, mill creates a GitHub Issue. The issue becomes the canonical spec. The local draft is deleted. One source of truth.

## Domain Awareness

Specs include a domain that shapes execution guidance:

| Domain | mill Pays Attention To |
|--------|----------------------|
| **backend** | API design, error handling, data modeling, performance |
| **application** | Component architecture, state management, UX patterns |
| **website** | Page architecture, responsive design, Core Web Vitals |
| **platform** | Infrastructure-as-code, containers, observability |
| **fullstack** | All of the above |

Domain guidance isn't just decorative. When `/mill:ship` implements a backend spec, it follows backend patterns. When it implements a website spec, it optimizes for Web Vitals and visual fidelity.

## Observations During Drafting

As you draft, mill watches for gaps in your ground knowledge:

- A persona referenced but not defined ("As a *finance admin*..." — who?)
- A domain term used without a vocabulary entry
- A requirement that conflicts with a documented rule
- An entity implied but not in the schema

High-confidence gaps get written as observations automatically. Uncertain ones are collected and shown at the end for your review.

This is the learning loop in action. Drafting a spec doesn't just produce a spec — it enriches the knowledge base.

## The Loop Contract

Every spec ends with a loop contract:

```markdown
## Loop Contract
**Test command:** `npm test`
**Stop conditions:** 20 iterations max
```

This tells ship exactly how to verify and when to stop. No ambiguity about what "passing" means.

## Common Patterns

### Bug Specs

Bug specs are surgical: what's broken, what's expected, how to reproduce, how to verify the fix.

### Feature Specs

Feature specs are the most common. Requirements → approach → criteria, with coverage proving completeness.

### Task Specs

Refactoring, migration, cleanup. Task specs focus on approach and before/after verification. Requirements are often simpler ("migrate from X to Y without regression").

### Security Specs

Security always wins classification. If a change has security implications, it's a security spec regardless of what else it does. Security specs include threat model, attack vectors, and security-specific criteria.
