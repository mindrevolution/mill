---
description: Manage product knowledge - personas, standards, concepts, design
allowed-tools: Read, Write, Glob, Grep, Bash(mill ground*, mill observations*, git *, rm *)
argument-hint: "[category] - personas, rules, decisions, vocabulary, or review"
---

# Ground

Build and manage product knowledge in `.mill/ground/`. Review observations from the learning inbox.

## Interaction Pattern

**Always use the AskUserQuestion tool** for gathering knowledge. Present 2-4 options plus free text. One question at a time.

## Entry Point

```yaml
AskUserQuestion:
  question: "What do you want to work on?"
  header: "Ground"
  options:
    - label: "Review observations"
      description: "{N} observations to review"
    - label: "Create knowledge"
      description: "Add personas, rules, decisions"
    - label: "Sync codebase"
      description: "Extract changes from code"
```

Check observations first:
```bash
mill observations list --human
```

If observations exist, show count in "Review observations" option.

---

## Observation Review Flow

### 1. List Observations

```bash
mill observations list --human
```

### 2. For Each Observation

Read the full observation:
```bash
mill observations get {id} --human
```

Then present action options:

```yaml
AskUserQuestion:
  question: "{id}: {title}"
  header: "Action"
  options:
    - label: "Curate to ground"
      description: "Add to ground truth"
    - label: "Create spec"
      description: "Write to idea/active/"
    - label: "Add to debt"
      description: "Track in ground/debt/"
    - label: "Dismiss"
      description: "Delete, not actionable"
```

### 3. Handle Each Action

**Curate to ground:**
1. Infer target folder from observation content:
   - `type: discovery` + mentions persona → `ground/personas/`
   - `type: discovery` + mentions term → `ground/vocabulary/`
   - `type: extraction` + about deps → `ground/stack/`
   - `type: concern` → often `ground/debt/`
   - `type: discovery` + about decisions → `ground/decisions/`

2. Ask clarifying question to gather details:
   ```yaml
   AskUserQuestion:
     question: "What does '{term}' mean?"
     header: "Define"
     options:
       - label: "{option1}"
       - label: "{option2}"
       - label: "{option3}"
   ```

3. Write to appropriate ground folder:
   ```bash
   mill ground create {category} {id} -
   ```

4. Delete observation file:
   ```bash
   rm .mill/observations/{id}.md
   ```

**Create spec:**
1. Transform observation into idea:
   ```bash
   mill idea create "{title}" "{intent from observation}"
   ```
2. Delete observation file

**Add to debt:**
1. Append to `ground/debt/{topic}.md`
2. Delete observation file

**Dismiss:**
1. Delete observation file:
   ```bash
   rm .mill/observations/{id}.md
   ```

### 4. Continue

After processing, check for more observations:
```bash
mill observations list --human
```

If more exist, continue. Otherwise, exit or offer other actions.

---

## Categories

| Category | Purpose | Examples |
|----------|---------|----------|
| **strategic** | Vision, mission, goals | Product direction, business objectives |
| **personas** | Who you build for | Primary user, admin, operator |
| **rules** | Constraints and conventions | Tech policies, quality bars |
| **decisions** | Architectural decisions | Why we chose X over Y |
| **vocabulary** | Domain terminology | Key terms, business entities |
| **stack** | Technology stack | Languages, frameworks, dependencies |
| **schema** | Data structures | Entities, relationships, types |
| **design** | Visual language | Colors, typography, components |
| **patterns** | Code patterns | Common solutions, idioms |
| **debt** | Technical debt | Known issues, future work |

## Commands

```bash
# List all knowledge items
mill ground list --human

# List by category
mill ground list personas --human

# Get item content
mill ground get rules tech-stack --human

# Create item (content from stdin)
echo "content" | mill ground create personas primary-user -

# List observations
mill observations list --human

# Get observation
mill observations get ship-42-test-gaps --human
```

## Create Knowledge Flow

When creating new knowledge (not from observation):

### 1. Ask Category

```yaml
AskUserQuestion:
  question: "What category of knowledge?"
  header: "Category"
  options:
    - label: "Personas"
      description: "Who you build for"
    - label: "Rules"
      description: "Constraints and conventions"
    - label: "Decisions"
      description: "Architectural decisions"
    - label: "Vocabulary"
      description: "Domain terminology"
```

### 2. Elicit Details

For each category, ask appropriate questions:

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

**Rules** — Ask about constraints:
```yaml
AskUserQuestion:
  question: "What kind of rule?"
  header: "Rule"
  options:
    - label: "Tech policy"
      description: "Languages, frameworks, tools"
    - label: "Quality bar"
      description: "Testing, coverage, performance"
    - label: "Convention"
      description: "Naming, structure, patterns"
```

**Decisions** — Ask about choices made:
```yaml
AskUserQuestion:
  question: "What decision are you documenting?"
  header: "Decision"
  options:
    - label: "Technology choice"
      description: "Why we chose X over Y"
    - label: "Architecture pattern"
      description: "How we structure X"
    - label: "Process decision"
      description: "How we do X"
```

**Vocabulary** — Ask about domain terms.

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
- Rules → acceptance criteria align with quality bars
- Vocabulary → specs use consistent terminology
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
