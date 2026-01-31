# ground kickstart

## inputs
- name
- description
- archetype (template content)
- stack (template content)
- overrides (optional fields)
- codebase context (optional)

## rules
- Source product type, primary user, core loop, success metric, monetization from archetype.
- Source frontend, backend, data/storage, infra/deploy, testing, observability, avoid from stack.
- Apply overrides if present; otherwise keep template values.
- Use codebase context only to fill missing fields or resolve conflicts; never override explicit user inputs.
- Output only the four sections below, in order, with exact headings and fields.
- No extra categories, files, prose, or metadata.

## output (markdown only)

### product
```
# product
name: <name>
description: <description>

## core loop
<one sentence>

## success metric
<one line>

## monetization
<one line>
```

### primary user
```
# primary user
persona: <primary user>
job: <top job to be done>
pain points:
- <point 1>
- <point 2>
scenario: <top scenario>
```

### tech stack
```
# tech stack
frontend: <...>
backend: <...>
data/storage: <...>
infra/deploy: <...>
testing: <...>
observability: <...>
avoid: <...>
```

### quality bars
```
# quality bars
performance: <one line>
reliability: <one line>
ux: <one line>
security: <one line>
```
