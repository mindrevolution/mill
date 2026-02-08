---
title: "Spec"
chapter: 6
part: "Process"
partNumber: 2
description: "Turning fuzzy intent into precise, verifiable specifications"
slug: "spec"
---

The specification is mill's most important artifact. It's the contract between what you want and what gets built. Everything upstream (ground, ideas) feeds into it. Everything downstream (ship) is driven by it.

## Anatomy of a Spec

Every mill specification has three sections that form a verification chain:

**Requirements (R)** define *what* the solution must achieve. These are outcomes, not implementation details. "Users can reset their password via email" is a requirement. "Add a POST endpoint to /api/reset" is not.

**Approach (A)** describes *how* the requirements will be met. This is the implementation strategy — which components to create, which patterns to follow, which trade-offs to accept. The approach is broken into parts, each addressing one or more requirements.

**Criteria (C)** are testable conditions that *verify* the approach satisfies the requirements. Each criterion is specific, objective, and mechanically verifiable. "The reset email is sent within 5 seconds" is a criterion. "The feature works well" is not.

## The Verification Chain

The three sections are linked:

```
Requirements  → implemented by →  Approach Parts
Approach Parts → verified by →    Criteria
```

This creates a **coverage matrix (R x A x C)**. Every requirement has approach parts that implement it, and every approach part has criteria that verify it. If the coverage is incomplete, the spec has gaps.

## Writing Specs

The `/mill:spec` skill handles the hard work:

```
/mill:spec              # Start from scratch or from an idea
/mill:spec --draft my-feature   # Resume a draft
```

The workflow is conversational but structured:

1. You describe your intent
2. mill drafts requirements based on your description and ground knowledge
3. You review and refine the requirements
4. mill proposes an approach
5. You review the approach
6. mill generates criteria that cover the R x A matrix
7. You approve and publish to GitHub Issues

At every step, you have full control. mill proposes; you decide.

## Specs as GitHub Issues

Published specs live as GitHub Issues — the single source of truth. This means:

- They're versioned and commentable
- They integrate with your existing workflow (branches, PRs, labels)
- They're accessible to everyone on the team
- They survive beyond any single tool

```bash
# List spec drafts
mill draft list --human

# Validate a draft before publishing
mill draft validate my-feature --human

# Publish to GitHub
mill draft publish my-feature --human
```

> A specification isn't overhead. It's the cheapest way to prevent rework.
