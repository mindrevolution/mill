---
title: "Spec"
number: 5
subtitle: "Turn intent into precision"
accent: "cyan"
---

## The Bar

A specification that requires clarifying questions has failed.

This single test determines whether a spec is ready. The bar is high because the payoff is high: a spec that passes this test can be implemented without back-and-forth, without "what did you mean by...?", without rework.

## The Structure

Every mill spec has four sections that form a provable chain:

### Requirements (R)

What the solution must achieve. Each requirement gets an ID, a description, and a priority:

| Priority | Meaning |
|----------|---------|
| **core** | Must be implemented. No ship without it. |
| **must-have** | Required but could be deferred to a follow-up. |
| **nice-to-have** | Valuable but not blocking. |
| **out** | Explicitly excluded. Saying "no" is a decision too. |

### Approach (A)

How you'll build it. Broken into parts, each describing a specific mechanism — a model change, an API endpoint, a UI component, a migration script.

Approach parts can be flagged with a warning when uncertainty exists. These flags must be resolved before the spec is ready.

### Criteria (C)

Testable conditions that prove each requirement is met. Good criteria are:

- **Binary** — they pass or fail, no "kind of"
- **Automated** — they can be verified by a test command
- **Independent** — each criterion tests one thing

### Coverage (R x A x C)

The proof matrix. Every core and must-have requirement must have at least one approach part implementing it and at least one criterion verifying it.

---

### A Complete Example

Here's a small spec for adding PDF export to a reporting system:

**Requirements:**

| ID | Description | Priority |
|----|-------------|----------|
| R1 | Users can export any saved report as a PDF file | core |
| R2 | The PDF preserves the report's table formatting and charts | core |
| R3 | Export works for reports up to 500 rows without timeout | must-have |

**Approach:**

| ID | Mechanism |
|----|-----------|
| A1 | Add `GET /api/reports/:id/pdf` endpoint that accepts `format=pdf` query param |
| A2 | Use Puppeteer to render the report HTML template server-side and print to PDF |
| A3 | Stream the PDF response with `Content-Disposition: attachment` header |
| A4 | Add a 30-second timeout; return 504 if rendering exceeds it |

**Criteria:**

| ID | Condition |
|----|-----------|
| C1 | `GET /api/reports/1/pdf` returns a valid PDF with status 200 and `Content-Type: application/pdf` |
| C2 | The PDF contains all table rows and chart images from the source report |
| C3 | A 500-row report completes export in under 25 seconds |
| C4 | A 1000-row report returns 504 after 30 seconds |

**Coverage:**

| | A1 | A2 | A3 | A4 |
|---|---|---|---|---|
| **R1** | x | x | x | |
| **R2** | | x | | |
| **R3** | | | | x |

| | C1 | C2 | C3 | C4 |
|---|---|---|---|---|
| **R1** | x | | | |
| **R2** | | x | | |
| **R3** | | | x | x |

Every requirement has approach parts implementing it. Every requirement has criteria verifying it. No gaps.

## The Workflow

### Starting Fresh

```
/mill:spec "Add PDF export for monthly reports"
```

mill checks your project context, loads ground knowledge, and begins the conversation. If observations are pending in the learning inbox, mill nudges you — a reminder to run `/mill:ground` before drafting so your spec benefits from the latest learnings.

### The Conversation

mill asks one question at a time, using structured options where sensible:

1. **What type of change?** Feature / Bug / Task / Security
2. **What domain?** Backend / Application / Website / Platform / Full-stack
3. **What's the core problem?** (your words)
4. **What must the solution achieve?** (requirements emerge)
5. **How should we build it?** (approach forms)
6. **How do we prove it works?** (criteria crystallize)

A draft is saved early and updated as you go. You can stop mid-conversation and resume later.

### Validation

Before publishing, mill validates your spec against these checks:

- **Self-Containment** — no statement requires external clarification
- **Language Independence** — describes *what* and *why*, not language-specific *how*
- **Decision Completeness** — no TBDs, all parameters bound
- **Explicit Non-Applicability** — omitted sections marked N/A with reason
- **Coverage** — all core/must-have requirements have approach and criteria
- **No open flags** — all uncertainties resolved

The first three deserve examples — they're the ones most often violated.

**Self-containment** means every statement can be executed by someone unfamiliar with the system:

| Fails | Passes |
|-------|--------|
| "Configure the stream endpoint" | "Set `RTMP_INGEST=rtmp://ingest.example.com:1935/live` in `encoder/.env`" |
| "Use the appropriate codec" | "Encode with H.264 Main Profile, 1080p@30fps, 4500kbps CBR" |
| "Handle errors gracefully" | "On failure: retry 3x with exponential backoff, then emit `stream.failed` with payload `{streamId, error, timestamp}`" |

If a reader has to ask "which endpoint?" or "what codec?" — the spec isn't ready.

**Language independence** means "validate the email format" (what), not "use a regex to validate the email" (how). The implementer chooses the mechanism based on the stack.

**Decision completeness** means every parameter is bound to a concrete value. When a decision depends on a condition:

```
Retry count: default 3 unless network is cellular → 5
Cache TTL: default 60s unless user is admin → 0 (no cache)
```

No TBDs. No "it depends." The implementer never has to guess.

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
| **fullstack** | Combined guidance from all relevant layers |

> **Why domain matters** — A backend spec loads API design patterns. A website spec loads Core Web Vitals guidance. The domain shapes what the verifier checks and what implementation patterns ship follows.

## The Loop Contract

Every spec ends with a loop contract that governs how [ship](/ship) iterates:

```markdown
## Loop Contract
**Test Command:** `npm test`
**Max Iterations:** 5
**Verification Commands:** `npm run lint`
```

| Field | Purpose | Default |
|-------|---------|---------|
| **Test Command** | Must pass before PR | Detected from project |
| **Max Iterations** | Maximum implement-verify cycles before escalating | 5 |
| **Verification Commands** | Additional checks run by implementer and verifier | None |

After `Max Iterations` cycles, ship escalates to you with options: continue iterating, create PR as-is, or abort.

## Observations During Drafting

As you draft, mill watches for gaps in ground knowledge:

- A persona referenced but not defined
- A domain term used without a vocabulary entry
- A requirement that conflicts with a documented rule
- An entity implied but not in the schema

High-confidence gaps get written as observations automatically. Uncertain ones are collected and shown at the end for your review. Drafting a spec doesn't just produce a spec — it enriches the knowledge base.

## Common Patterns

### Bug Specs

Bug specs are surgical. They focus on reproduction and verification:

```
R1: The checkout total no longer double-counts tax on discounted items.

Approach: Fix the tax calculation in OrderService.calculateTotal() to apply
tax after discount, not before.

C1: Given a $100 item with 10% discount and 8% tax, total is $97.20
    (not $98.00).
```

### Feature Specs

The most common type. Full R→A→C chain with coverage proving completeness. The [example above](#a-complete-example) is a feature spec.

### Task Specs

Refactoring, migration, cleanup. Requirements are often simpler ("migrate from X to Y without regression"), but approach and before/after criteria are detailed.

### Security Specs

Security always wins classification. If a change has security implications, it's a security spec regardless of what else it does. These include threat model, attack vectors, and security-specific criteria.
