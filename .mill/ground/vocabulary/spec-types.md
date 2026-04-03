---
category: vocabulary
id: spec-types
---

# Spec Types (Intent Types)

Every spec is classified by intent type, which determines its structure and verification strategy:

| Type | When | Key Fields | Verified By |
|------|------|------------|-------------|
| **Feature** | New behavior or capability | User stories, acceptance criteria, scope | Acceptance criteria |
| **Bug** | Existing behavior is broken | Reproduction steps, expected vs actual | Regression test |
| **Security** | Risk, vulnerability, compliance | STRIDE category, attack vector, mitigation | Threat mitigated |
| **Task** | Technical work, no user-facing change | Rationale, scope, regression guardrails | Criteria pass |
