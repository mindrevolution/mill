# {{TITLE}}

> {{ONE_SENTENCE_DESCRIPTION}}

## Status
Draft | Ready | In Progress | Verified | Disclosed

## Severity
Critical | High | Medium | Low

## STRIDE Category
Spoofing | Tampering | Repudiation | Information Disclosure | Denial of Service | Elevation of Privilege

---

## Threat Description

### Summary
{{Clear description of the security concern}}

### Threat Type
- [ ] Vulnerability (exploitable weakness)
- [ ] Missing Control (protection not implemented)
- [ ] Compliance Requirement (regulatory/policy driven)

---

## Attack Vector

### Entry Point
{{API endpoint, UI element, network layer, etc.}}

### Preconditions
- {{What attacker needs: authentication, specific role, etc.}}

### Exploitation Steps
1. {{Step 1}}
2. {{Step 2}}
3. {{Step 3}}
4. {{Result: security breach}}

### Skill Level Required
Script Kiddie | Intermediate | Advanced | Expert

---

## Risk Assessment

### Likelihood Factors
| Factor | Score (1-10) | Rationale |
|--------|--------------|-----------|
| Ease of Discovery | {{score}} | {{why}} |
| Ease of Exploitation | {{score}} | {{why}} |
| Attacker Motivation | {{score}} | {{why}} |

### Impact Factors
| Factor | Score (1-10) | Rationale |
|--------|--------------|-----------|
| Confidentiality | {{score}} | {{data exposed}} |
| Integrity | {{score}} | {{data/system modified}} |
| Availability | {{score}} | {{service disruption}} |
| Business Impact | {{score}} | {{reputation, compliance, financial}} |

### Overall Risk
**{{CRITICAL/HIGH/MEDIUM/LOW}}** — Likelihood: {{H/M/L}} × Impact: {{H/M/L}}

---

## Affected Surface

### Components
- {{Affected endpoint/service}}
- {{Affected endpoint/service}}

### Data at Risk
- {{Data type and sensitivity}}

### Users Affected
- {{User population}}

### Connected Systems
- {{Downstream systems that could be affected}}

---

## Current State

### Exploitable Now?
- [ ] Yes, actively exploitable in production
- [ ] Yes, but requires specific conditions
- [ ] No, only in certain environments
- [ ] Theoretical/not yet exploitable

### Existing Mitigations
{{Any partial protections already in place}}

### Evidence of Exploitation
{{Any indicators this has been exploited}}

---

## Mitigation Strategy

### Root Cause
{{What's the underlying weakness?}}

### Proposed Fix
{{Specific technical mitigation}}

### Mitigation Type
- [ ] Input Validation
- [ ] Output Encoding
- [ ] Authentication
- [ ] Authorization
- [ ] Encryption
- [ ] Rate Limiting
- [ ] Audit Logging
- [ ] Security Headers
- [ ] Other: {{specify}}

### Defense in Depth
{{Additional layers of protection to add}}

---

## Verification

### Security Test
```
Test: {{test name}}
Attack: {{attempt exploitation}}
Expected: {{attack fails, appropriate response}}
```

### Proof of Fix
- [ ] Attack vector no longer exploitable
- [ ] Security test passes
- [ ] No regression in functionality
- [ ] Security scan clean

### Verification Commands
```bash
{{Security test commands}}
```

---

## Compliance & Disclosure

### Compliance Implications
- [ ] GDPR
- [ ] SOC2
- [ ] HIPAA
- [ ] PCI-DSS
- [ ] Other: {{specify}}
- [ ] None

### Disclosure Required
- [ ] Internal stakeholders
- [ ] Customers
- [ ] Regulators
- [ ] Public disclosure

### Disclosure Timeline
{{When and how to disclose}}

---

## Loop Contract

- **Success Criteria:** Attack vector closed; security test passes; no regressions
- **Test Command:** `{{test runner command}}` — must pass before PR
- **Verification Commands:** {{security test commands}}
- **Max Iterations:** 5
- **Escalation:** Immediately if Critical/High and blocked

---

## Notes

{{Additional context, references to CVEs, related issues}}

---

*Built with [mill](https://mill.mindrevolution.com)*
