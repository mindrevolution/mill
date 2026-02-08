---
title: "Ground"
number: 4
subtitle: "Your project's knowledge base"
accent: "electric"
---

## What Ground Is

Ground is the shared brain of your project. Everything mill knows about your domain, your users, your conventions, your stack — it lives here.

Unlike documentation that rots in a wiki, ground is *active*. It's read by specs to ensure consistency. It's referenced by ship for implementation guidance. It's enriched by observations from every cycle.

```bash
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

The big picture. Why does this project exist? Where is it going?

This isn't a business plan. It's the compass that helps mill understand which features matter and why. When a spec references "improving user onboarding," strategic context tells mill what good onboarding means for *your* product.

### Personas

Real users, not abstractions. Each persona has:

- **Job to be done** — what they're trying to accomplish
- **Pain points** — current frustrations
- **Triggers** — what causes them to use your product

When you write specs, requirements reference these personas. "As a *finance admin*..." means something specific because the persona is defined.

### Rules

The constraints your team has agreed on. Technology policies. Quality bars. Naming conventions. Performance budgets.

Rules are enforceable. When a spec proposes something that conflicts with a rule, mill flags it. When an implementation doesn't meet a quality bar, verification catches it.

### Decisions

Why you chose X over Y. Not just the choice — the reasoning.

Decisions have a half-life. Some are permanent ("we use PostgreSQL"). Some may be revisited ("we chose REST over GraphQL because..."). Documenting the *why* means future you can evaluate whether the reasoning still holds.

### Vocabulary

Domain terms defined once, used consistently everywhere. When your spec says "invoice," everyone knows exactly what that means — the fields, the states, the lifecycle.

This is subtle but powerful. Ambiguous terminology is one of the biggest sources of implementation bugs. Ground vocabulary eliminates it.

### Stack

Your technology stack, explicitly documented. Languages, frameworks, dependencies, versions. When mill generates implementation guidance, it knows your tools.

### Schema

Data structures and their relationships. Entities, fields, types, constraints. This is the structural truth that both specs and implementations must respect.

### Design

Your visual language. Colors, typography, spacing, component patterns. For website and application domains, design ground ensures consistent UI without a 200-page style guide.

### Patterns

Code idioms your team uses. Error handling patterns. Logging conventions. API response structures. These become implicit standards that mill follows during implementation.

### Debt

Known issues, technical compromises, future work. Tracked explicitly so they don't get lost. When debt items affect new features, mill includes them in spec context.

## Building Ground Knowledge

Ground is built in three ways:

### 1. Direct Creation

You tell mill what you know:

```
/mill:ground
→ "Create knowledge"
→ Choose category: Personas
→ Answer questions about the user
→ mill writes the persona file
```

The process is conversational. mill asks one question at a time, offering options where sensible. You provide the substance; mill handles the structure.

### 2. Observation Review

When specs are drafted and features are shipped, mill observes gaps in ground truth. These observations land in the learning inbox:

```
/mill:ground
→ "Review observations"
→ See: "Unknown persona: Finance Admin (from spec #42)"
→ Choose: Curate to ground / Create spec / Add to debt / Dismiss
```

This is the primary growth mechanism. You don't have to remember to document things. mill notices what's missing and asks you to fill the gaps.

### 3. Codebase Sync

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

This is why ground matters. It's not documentation for humans — it's context for every mill operation. The richer your ground, the better your specs and implementations.

## Commands

```bash
# List all knowledge items
mill ground list --human

# List by category
mill ground list personas --human

# Get a specific item
mill ground get decisions auth-strategy --human

# Create an item
mill ground create vocabulary invoice "# Invoice\n\nA billing document..."
```

## Best Practices

**Start small.** You don't need all ten categories on day one. Begin with personas and rules. Add categories as they become relevant.

**Review observations regularly.** The learning inbox is where ground grows organically. Make it a habit.

**Keep entries focused.** One persona per file. One decision per file. Granularity makes knowledge reusable.

**Update, don't hoard.** If a decision changes, update the file. Ground should reflect current truth, not historical record.
