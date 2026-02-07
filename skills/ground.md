---
description: Manage product knowledge - personas, standards, concepts, design
allowed-tools:
  - Read
  - Write
  - Glob
  - Grep
  - Bash(mill ground*, git *)
model: sonnet
argument-hint: "[category] - personas, standards, concepts, or design"
---

# Ground

Build and manage product knowledge in `.mill/ground/`.

## Categories

| Category | Purpose | Examples |
|----------|---------|----------|
| **personas** | Who you build for | Primary user, admin, operator |
| **standards** | How you build | Tech stack, quality bars, conventions |
| **concepts** | Domain vocabulary | Key terms, business entities |
| **design** | Visual language | Colors, typography, components |

## Commands

```bash
# List all knowledge items
mill ground list --human

# List by category
mill ground list personas --human

# Get item content
mill ground get standards tech-stack --human

# Create item (content from stdin)
echo "content" | mill ground create personas primary-user -
```

## Workflow

### 1. Check Current State

```bash
mill ground list --human
```

### 2. Create or Update Items

For each category, help the user define their product knowledge.

**Personas** — Ask about users:
- Who are the primary users?
- What are their jobs-to-be-done?
- What pain points do they have?
- What triggers them to use the product?

**Standards** — Ask about conventions:
- What tech stack is used?
- What quality bars apply?
- What coding conventions matter?

**Concepts** — Ask about domain:
- What are the key business terms?
- What entities exist in the domain?
- What relationships matter?

**Design** — Ask about visuals:
- What colors are used?
- What typography applies?
- What component patterns exist?

### 3. Write Knowledge Files

Format for knowledge items:

```markdown
---
category: personas
id: primary-user
---

# Primary User

{Description of the persona}

## Job to be Done
{What they're trying to accomplish}

## Pain Points
{Current frustrations}

## Triggers
{What causes them to use the product}
```

### 4. Verify

```bash
mill ground list --human
```

## Integration with Specs

Knowledge items inform spec drafting:
- Personas → user stories reference real users
- Standards → acceptance criteria align with quality bars
- Concepts → specs use consistent terminology
- Design → UI specs reference design tokens

## Kickstart (New Projects)

For new projects, use templates to bootstrap:

```bash
# List available archetypes
mill template list archetypes --human

# List available stacks
mill template list stacks --human
```

Then create initial ground files based on archetype and stack.
