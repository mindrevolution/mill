# Platform Domain

Execution guidance for infrastructure, orchestration, and system architecture.

## Mindset

You're building the foundation everything else runs on. Reliability beats cleverness. Automation beats manual. Reproducibility is non-negotiable.

## Design Thinking

Before building, understand:

1. **Topology** — What components exist? How do they communicate?
2. **Scale** — What load is expected? What's the growth trajectory?
3. **Failure** — What breaks? How do we detect it? How do we recover?
4. **Operations** — Who runs this? How do they troubleshoot?

## Principles

### Infrastructure as Code
- Everything versioned, nothing manual
- Idempotent operations — run twice, same result
- Environment parity — dev mirrors prod (scaled down)
- Secrets never in code — use vaults, env injection

### Container Design
- One process per container
- Minimal base images (Alpine, distroless)
- Health checks that actually verify health
- Graceful shutdown handling

### Orchestration
- Declarative over imperative
- Rolling updates with rollback capability
- Resource limits prevent noisy neighbors
- Horizontal scaling over vertical

### Reliability
- Health endpoints: liveness vs readiness
- Circuit breakers for external dependencies
- Retry with exponential backoff
- Graceful degradation over hard failure

### Observability
- Logs: structured, contextual, not excessive
- Metrics: RED (Rate, Errors, Duration) at minimum
- Traces: correlation IDs across services
- Alerts: actionable, not noisy

### Security
- Principle of least privilege
- Network policies restrict traffic
- Secrets rotated, never logged
- Images scanned, base images updated

## Quality Bar

- [ ] Infrastructure defined in code (Terraform, Pulumi, K8s manifests)
- [ ] Changes are reproducible — delete and recreate works
- [ ] Health checks verify actual functionality
- [ ] Secrets managed properly (not in env files or code)
- [ ] Rollback path exists and is tested
- [ ] Monitoring covers failure scenarios
- [ ] Documentation covers operations runbook

## Anti-Patterns

- Manual configuration that isn't captured in code
- "Works on my machine" without container parity
- Health checks that just return 200
- Secrets in environment files committed to repo
- No resource limits (memory leaks take down cluster)
- Logs that don't include correlation context
- Alerts for things nobody can act on
- Scripts that fail silently
- Hardcoded IPs, ports, or hostnames

## Scripting Standards

When writing glue code and automation:

- Fail fast, fail loud — `set -e` in bash
- Idempotent — safe to run multiple times
- Log what you're doing — operators need to understand
- Validate inputs before acting
- Clean up on failure when possible
- Exit codes matter — 0 is success, non-zero is failure
