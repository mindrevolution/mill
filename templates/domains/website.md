# Web Design Domain

Execution guidance for full page and site design.

## Mindset

You're creating complete experiences, not isolated components. A website is a journey — every page connects to others, every scroll reveals something new.

## Design Thinking

Before designing, understand:

1. **Story** — What narrative does this site tell? What's the arc?
2. **Audience** — Who visits? What do they need? What convinces them?
3. **Goals** — What actions matter? What's success?
4. **Brand** — What personality? What feeling should visitors leave with?

## Aesthetic Principles

Commit to a clear conceptual direction and execute with precision. Generic is the enemy.

### Typography
- Font choices carry meaning — select deliberately
- Pair fonts with intention (contrast or harmony)
- Scale and hierarchy guide the eye
- Avoid: system fonts for branded work, generic sans-serif everywhere

### Color & Theme
- Commit to a palette — dominant, supporting, accent
- Contrast for accessibility (WCAG AA minimum)
- Color conveys meaning — be consistent
- Avoid: gray-on-gray monotony, rainbow chaos, low-contrast text

### Motion & Animation
- Motion serves purpose (feedback, continuity, delight)
- CSS transitions for micro-interactions
- Staggered reveals create rhythm
- Avoid: motion for motion's sake, jarring transitions

### Spatial Composition
- Whitespace is a design element, not empty space
- Alignment creates order; breaking it creates emphasis
- Asymmetry can be more interesting than symmetry
- Avoid: cramped layouts, inconsistent spacing

### Visual Depth
- Shadows, gradients, and layers create hierarchy
- Texture adds richness when appropriate
- Backgrounds set the stage
- Avoid: flat designs that lack hierarchy

## Page Architecture

### Above the Fold
- Immediate clarity: what is this, who is it for
- Primary action visible without scrolling
- Hero treatment sets the tone
- Loading performance critical

### Content Flow
- Scroll reveals information progressively
- Each section has one job
- Visual breaks guide pacing
- Call-to-action placement follows reading patterns

### Navigation
- Clear wayfinding — users always know where they are
- Consistent across pages
- Mobile navigation is not an afterthought
- Key actions always accessible

## Responsive Strategy

### Breakpoints
- Design for content, not devices
- Major breakpoints: mobile (< 640px), tablet (640-1024px), desktop (> 1024px)
- Test actual content at each breakpoint
- Touch targets: minimum 44x44px on mobile

### Adaptation Philosophy
- Mobile is not "desktop minus stuff"
- Each breakpoint can have different hierarchy
- Images: art direction, not just scaling
- Typography: adjust scale, not just shrink

## Performance

- First paint under 1.5s on 3G
- Images: lazy load below fold, proper formats (WebP/AVIF)
- Fonts: subset, preload critical, swap display
- Critical CSS inline, rest deferred

## Quality Bar

- [ ] Clear value proposition above fold
- [ ] Consistent navigation and wayfinding
- [ ] Works at all breakpoints (not just "doesn't break")
- [ ] Page weight reasonable (< 2MB ideal)
- [ ] Loads fast on slow connections
- [ ] Accessible: keyboard, screen reader, color contrast
- [ ] Forms have validation and clear feedback
- [ ] 404 and error states designed

## Anti-Patterns

- Hamburger menu on desktop
- Carousel as hero (low engagement)
- Walls of text without visual relief
- Stock photos that say nothing
- Footer links nobody will find
- Cookie banners that obscure content
- Infinite scroll without progress indication
- CTA buried at page bottom

## Cross-Browser

- Test in Chrome, Firefox, Safari at minimum
- Edge cases: Safari iOS (viewport units), Firefox (font rendering)
- Progressive enhancement: works without JS, enhanced with it
- Print stylesheet for content-heavy pages

## The Standard

A great website feels inevitable — like it couldn't have been designed any other way. Every element earns its place. The best sites are remembered not for tricks, but for clarity and craft.
