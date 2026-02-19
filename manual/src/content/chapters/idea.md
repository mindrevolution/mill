---
title: "Idea"
number: 4
subtitle: "Capture sparks before they fade"
accent: "violet"
---

## The 30-Day Rule

Ideas appear in the middle of a code review, in the shower, during a conversation about something else entirely. Most vanish before they can be evaluated.

mill's Idea skill gives sparks a safe landing zone. Capture them fast. Develop them at your own pace. And give them a deadline: 30 days to become a spec or get dropped.

> **Why 30 days?** — Ideas that survive 30 days of casual development have signal. Ideas that don't weren't worth a spec. This is a forcing function against the 200-item backlog where the bottom 180 will never be touched.

## The Lifecycle

```
spark → developing → ready → spec (or drop with essence)
```

### Spark

The minimum viable idea — a title and an intent:

```
/mill:idea "Inline code review"
```

mill asks a couple of quick questions, and the idea is saved:

```yaml
---
title: Inline Code Review
stage: spark
type: feature
persona: developer
created: 2025-07-15
expires: 2025-08-14
---
Let developers leave review comments directly on code lines
in the editor, without switching to the GitHub PR view.
```

No requirements. No approach. No criteria. Just enough to not forget.

### Developing

When you have time, run `/mill:idea` and pick an idea to develop. mill starts by **orienting in your codebase** — reading relevant files, modules, and patterns — then walks you through 3-5 rounds of dialogue informed by what it found:

```yaml
---
title: Inline Code Review
stage: developing
type: feature
persona: developer
created: 2025-07-15
expires: 2025-08-14
---
Let developers leave review comments directly on code lines
in the editor, without switching to the GitHub PR view.

## Scope
- In: Line-level comments on open PRs, reply threads, resolve/unresolve
- Out: Full PR approval flow, CI status display

## Approach
- WebSocket connection for real-time comment sync
- CodeMirror decoration API for inline rendering
- Backend: new `ReviewComment` entity linked to PR + file + line

## Open Questions
- [ ] How to handle comments on lines that changed since the review started?
- [x] Use existing auth or separate review permissions? → Use existing auth.
```

Each round sharpens scope, documents decisions with rationale, and tracks open questions explicitly. The open questions are the gaps that need closing before promotion.

### Ready

An idea is ready when:

- The intent is clear and specific
- Open questions are resolved
- Scope is articulated (what's in, what's out)
- A spec could reasonably be drafted from it

### Promote or Drop

**Promote:** The idea graduates to a draft spec. mill carries over the context — title, intent, persona, scope, approach notes — so the spec workflow starts with everything you've already thought through.

```
/mill:idea
→ Select "Inline Code Review"
→ "Promote to spec"
→ Draft spec created with title, persona, scope, and approach pre-filled
→ Continue with /mill:spec to refine requirements, criteria, and coverage
```

**Drop:** The idea isn't worth pursuing — but the learning isn't lost. mill asks you to capture the *essence*: why it was considered and why it was dropped.

```yaml
# dropped.json entry
{
  "title": "Voice Commands for Editor",
  "essence": "Speech recognition latency too high for real-time editing.
              Revisit when browser APIs support streaming transcription.",
  "dropped": "2025-08-01"
}
```

Future ideas can reference past learnings. Next time someone suggests voice commands, you know what happened last time.

## Working with Ideas

### Capture

```
/mill:idea "Dark mode support"
```

mill asks a few quick questions and creates the idea file in `.mill/idea/active/`.

### Browse and Develop

```
/mill:idea
```

mill shows your active ideas with their stage and age. Pick one to develop — mill picks up where you left off, asking about the gaps. You can also review dropped ideas and their captured learnings.

## Integration with the Cycle

Ideas connect to the broader mill workflow:

- They reference **personas** from ground — connecting sparks to real users
- They use **vocabulary** from ground — keeping terminology consistent
- When promoted, they become **drafts** in the [spec](/spec) workflow
- When dropped, their **essences** inform future decisions

> **Tip** — Capture immediately. Don't wait until you can think it through. The spark is enough — developing happens later.

> **Tip** — One idea per idea. If you're cramming multiple features into one, split them. Each should have a single, clear intent.

> **Common mistake** — Treating the idea stage as spec-lite. Ideas don't need requirements or criteria. They need just enough structure to evaluate whether a spec is worth drafting.
