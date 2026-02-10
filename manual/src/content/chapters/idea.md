---
title: "Idea"
number: 5
subtitle: "Capture sparks before they fade"
accent: "violet"
---

## The 30-Day Rule

Ideas are fragile. They appear in the middle of a code review, in the shower, during a conversation about something else entirely. Most of them vanish before they can be evaluated.

mill's Idea skill gives sparks a safe landing zone. Capture them fast. Develop them at your own pace. And give them a deadline: 30 days to become a spec or get dropped.

This isn't arbitrary pressure. It's a forcing function against idea hoarding. We've all seen backlogs with 200 items where the bottom 180 will never be touched. mill says: decide or let go.

## The Lifecycle

```
spark → developing → ready → spec (or drop with essence)
```

### Spark

The minimum viable idea. A title and an intent — nothing more:

```
/mill:idea "Inline code review"
```

mill asks a couple of quick questions, and the idea is saved. No requirements. No approach. No criteria. Just enough to not forget.

### Developing

When you have time, flesh it out. mill asks questions to help you think:

- What type of idea is this? (Feature / Improvement / Exploration)
- What problem does this solve?
- Who experiences this problem? (references personas from ground)
- What would success look like?
- What's the rough scope?

Each answer enriches the idea file. Open questions are tracked explicitly — they're the gaps that need closing before promotion.

### Ready

An idea is ready when:

- The intent is clear and specific
- Open questions are resolved
- You can articulate the scope
- A spec could reasonably be drafted from it

### Promote or Drop

**Promote:** The idea graduates to a draft spec. mill carries over the context — title, intent, persona, notes — so the spec workflow starts with everything you've already thought through.

**Drop:** The idea isn't worth pursuing. But the learning isn't lost. mill asks you to capture the *essence* — why it was considered and why it was dropped.

That essence goes into `dropped.json`. Future ideas can reference past learnings. Next time someone suggests "inline code review," you know what happened last time and why.

## Nothing Is Wasted

This is the philosophy behind the drop mechanism. Even rejected ideas contain knowledge:

- "Users don't actually need X because Z"
- "This would require changing the data model in ways that conflict with..."
- "Explored this and found that the real problem is Y, not X"

These learnings prevent your team from circling the same ideas. And they inform ground truth — a dropped idea might reveal a new persona or a rule that wasn't documented.

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

Ideas don't exist in isolation:

- They reference **personas** from ground — connecting sparks to real users
- They use **vocabulary** from ground — keeping terminology consistent
- When promoted, they become **drafts** in the spec workflow
- When dropped, their **essences** inform future decisions

The idea stage is intentionally lightweight. Its job is to be the fastest possible bridge between "I had a thought" and "should we build this?" — without losing anything in between.

## Tips

**Capture immediately.** Don't wait until you can think it through. The spark is enough.

**One idea per idea.** If you're cramming multiple features into one idea, split them. Each should have a single, clear intent.

**Be honest about drops.** Dropping isn't failure — it's curation. A well-managed idea list with 5 focused items beats a backlog of 50 vague ones.

**Review weekly.** Spend 10 minutes reviewing your active ideas. Develop the promising ones. Drop the stale ones. Keep the list fresh.
