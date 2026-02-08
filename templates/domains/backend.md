# Backend Domain

Execution guidance for backend/API work.

## Mindset

You're building the foundation. Backend code is consumed by other systems — clarity and reliability matter more than cleverness.

## Design Thinking

Before coding, understand:

1. **Contracts** — What interfaces does this expose? What promises does it make?
2. **Data** — What's the shape? How does it flow? Where does it persist?
3. **Failure modes** — What can go wrong? How do we recover gracefully?
4. **Scale** — What happens at 10x load? 100x?

## Principles

### API Design
- Consistent naming (verb-noun for actions, nouns for resources)
- Predictable response shapes
- Clear error responses with actionable messages
- Versioning strategy if breaking changes possible

### Data Modeling
- Normalize until it hurts, denormalize until it works
- Explicit over implicit (no magic strings, typed IDs)
- Audit fields where relevant (created, updated, deleted)

### Error Handling
- Fail fast, fail loud in development
- Fail gracefully, fail informatively in production
- Never swallow exceptions silently
- Log with context (correlation IDs, request details)

### Performance
- Measure before optimizing
- Database queries are usually the bottleneck
- Batch operations where possible
- Cache deliberately, invalidate correctly

## Quality Bar

- [ ] All public methods have clear contracts
- [ ] Error cases are handled explicitly
- [ ] No N+1 query patterns
- [ ] Logging provides debugging context
- [ ] Tests cover happy path and key failure modes

## Anti-Patterns

- God services that do everything
- Stringly-typed code (magic strings everywhere)
- Silent failures (catch and ignore)
- Premature optimization
- Over-abstraction for one use case
