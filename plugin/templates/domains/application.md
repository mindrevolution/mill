# Application Domain

Execution guidance for web apps, mobile apps, and interactive frontend code.

## Mindset

You're building what users touch. Every interaction shapes their experience. Code serves the interface, not the other way around.

## Design Thinking

Before coding, understand:

1. **User flow** — What's the journey? Where does this fit?
2. **State** — What data drives this? Where does it live? How does it change?
3. **Interactions** — What can users do? What feedback do they get?
4. **Edge states** — Loading, empty, error, partial — how does each look?

## Principles

### Component Architecture
- Single responsibility — one job per component
- Props down, events up
- Composition over configuration
- Colocate related code (styles, tests, types)

### State Management
- Local state first, lift only when needed
- Derived state over synchronized state
- Optimistic updates for responsiveness
- Clear loading/error/success states

### User Experience
- Immediate feedback on interactions
- Graceful degradation when things fail
- Keyboard navigation for accessibility
- Loading states that inform, not frustrate

### Visual Consistency
- Follow design system tokens (colors, spacing, typography)
- Consistent component patterns across the app
- Motion serves purpose — feedback, continuity, delight
- Respect platform conventions (web, iOS, Android)

### Performance
- Render only what's visible
- Lazy load heavy components
- Debounce expensive operations
- Measure actual user experience, not just metrics

## Quality Bar

- [ ] Component handles all states (loading, error, empty, success)
- [ ] Keyboard accessible where applicable
- [ ] No layout shift on load
- [ ] Responsive at specified breakpoints
- [ ] Tests cover user interactions
- [ ] Visual consistency with design system

## Anti-Patterns

- Prop drilling through many layers
- Monolithic components doing too much
- Inline styles for themeable properties
- Ignoring loading and error states
- Tightly coupling to data fetching
- Inconsistent spacing and typography
- Motion that blocks rather than enhances
