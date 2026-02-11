---
description: "Define who you build for and how • https://mill.mindrevolution.com/ground"
disable-model-invocation: true
allowed-tools: Read, Write, Glob, Grep, Bash(*rm *, *git *)
argument-hint: "[category] - personas, rules, decisions, vocabulary, or review"
---

# Ground

Build and manage product knowledge in `.mill/ground/`. Review observations from the learning inbox.

## Interaction Pattern

**Always use the AskUserQuestion tool** for gathering knowledge. Present 2-4 options plus free text. One question at a time.

## Entry Point

Check observations first:

```
Glob(".mill/observations/*.md")
```

Read each observation file to get titles and count.

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

---

## Observation Review Flow

### 1. List Observations

```
Glob(".mill/observations/*.md")
```

Read each file to display title and type from frontmatter.

### 2. For Each Observation

Read the full observation:

```
Read(".mill/observations/{id}.md")
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

3. Write to appropriate ground folder using the Write tool:
   ```
   Write(".mill/ground/{category}/{id}.md", content)
   ```

4. Delete observation file:
   ```bash
   rm .mill/observations/{id}.md
   ```

**Create spec:**
1. Transform observation into idea — write directly:
   ```
   Write(".mill/idea/active/{slug}.md", content)
   ```
   Use frontmatter: title, stage: spark, intent, created date.

2. Delete observation file:
   ```bash
   rm .mill/observations/{id}.md
   ```

**Add to debt:**
1. Read existing `ground/debt/{topic}.md` (if any), append content, Write back
2. Delete observation file:
   ```bash
   rm .mill/observations/{id}.md
   ```

**Dismiss:**
1. Delete observation file:
   ```bash
   rm .mill/observations/{id}.md
   ```

### 4. Continue

After processing, check for more observations:

```
Glob(".mill/observations/*.md")
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

## File Operations

```
# List all knowledge items
Glob(".mill/ground/**/*.md") → Read each for frontmatter

# List by category
Glob(".mill/ground/{category}/*.md") → Read each

# Get item content
Read(".mill/ground/{category}/{id}.md")

# Create item
Write(".mill/ground/{category}/{id}.md", content)

# List observations
Glob(".mill/observations/*.md") → Read each

# Get observation
Read(".mill/observations/{id}.md")
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

Write using the Write tool to `.mill/ground/{category}/{id}.md`:

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

```
Glob(".mill/ground/**/*.md")
```

Read and confirm the new file was written correctly.

## Integration with Specs

Knowledge items inform spec drafting:
- Personas → user stories reference real users
- Rules → acceptance criteria align with quality bars
- Vocabulary → specs use consistent terminology
- Design → UI specs reference design tokens

## Kickstart (New Projects)

For new projects, ask the user directly:

```yaml
AskUserQuestion:
  question: "What kind of product is this?"
  header: "Product"
  options:
    - label: "SaaS"
      description: "Web app with subscriptions"
    - label: "API / Platform"
      description: "Developer-facing service"
    - label: "Marketing site"
      description: "Content, landing pages"
    - label: "Internal tool"
      description: "Team-facing utility"
```

Then ask about their stack, conventions, and key personas. Create initial ground files from the conversation — no templates needed.
