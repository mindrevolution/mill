# Observation Extraction

Analyze this completed ship run and extract valuable observations for the project's knowledge base.

## Spec Content

{{SPEC_CONTENT}}

## Run Outcome

- **Success:** {{RUN_SUCCESS}}
- **Iterations:** {{ITERATIONS}}

## Iteration History

{{ITERATION_HISTORY}}

## Existing Ground Items

These are already in the knowledge base — do NOT suggest duplicates:

{{EXISTING_GROUND}}

## Existing Observations

These are pending observations — if you find the same pattern, mark it as a reinforcement:

{{EXISTING_OBSERVATIONS}}

## Banned Terms

Never suggest observations containing these terms — they were explicitly rejected:

{{BANLIST}}

---

## Instructions

Extract observations that would help future development. Focus on:

- **personas**: User types or roles referenced in the spec (who the feature is for)
- **standards**: Patterns, practices, or quality bars worth standardizing (how to build)
- **concepts**: Domain terms that should be documented (shared vocabulary)
- **design**: UI/UX patterns used (visual language)

For each potential observation:

1. **Check duplicates** — If it matches an existing ground item, skip it
2. **Check reinforcement** — If it matches an existing observation, note the ID in `matchesExisting`
3. **Check banlist** — If it contains a banned term, skip it
4. **Assess confidence** (0.0–1.0):
   - 0.9+ = Clearly defined, explicitly mentioned multiple times
   - 0.7–0.9 = Clear mention, would add value to document
   - 0.5–0.7 = Implicit or mentioned once, may be useful
   - <0.5 = Speculative, probably not worth tracking

Format suggestions as: `Name — brief description of what this is`

Output JSON:

```json
{
  "observations": [
    {
      "category": "concepts",
      "suggestion": "Tournament — recurring structure for organizing competitive matches",
      "confidence": 0.85,
      "matchesExisting": null
    },
    {
      "category": "personas",
      "suggestion": "League Admin — manages tournaments, schedules, and player disputes",
      "confidence": 0.72,
      "matchesExisting": "obs_abc123"
    }
  ]
}
```

Be selective — only extract observations with confidence ≥ 0.6. Quality over quantity.
