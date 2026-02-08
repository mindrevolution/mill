---
description: Manage product knowledge - personas, standards, concepts, design
allowed-tools: Read, Write, Glob, Grep, Bash(mill ground*, git *)
argument-hint: "[category] - personas, standards, concepts, or design"
---

# Ground

Build and manage product knowledge in `.mill/ground/`.

## Interaction Pattern

**Always use the AskUserQuestion tool** for gathering knowledge. Present 2-4 options plus free text. One question at a time.

```yaml
AskUserQuestion:
  question: "What category do you want to work on?"
  header: "Category"
  options:
    - label: "Personas"
      description: "Who you build for"
    - label: "Standards"
      description: "How you build"
    - label: "Concepts"
      description: "Domain vocabulary"
    - label: "Design"
      description: "Visual language"
```

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

For each category, **use AskUserQuestion** to gather knowledge:

**Personas** — Ask about users:
```yaml
AskUserQuestion:
  question: "What type of user is this?"
  header: "User type"
  options:
    - label: "End user"
      description: "Primary product user"
    - label: "Admin"
      description: "Manages settings/users"
    - label: "Operator"
      description: "Runs/maintains the system"
```

Then elicit: job-to-be-done, pain points, triggers.

**Standards** — Ask about conventions:
```yaml
AskUserQuestion:
  question: "What kind of standard?"
  header: "Standard"
  options:
    - label: "Tech stack"
      description: "Languages, frameworks, tools"
    - label: "Quality bar"
      description: "Testing, coverage, performance"
    - label: "Conventions"
      description: "Naming, structure, patterns"
```

**Concepts** — Ask about domain vocabulary.

**Design** — Ask about visual language.

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
