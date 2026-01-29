# Standards

Create or update codebase standards by detecting config files and inferring patterns from code.

**CRITICAL:** Standards must be evidence-backed. Infer from actual codebase patterns and config files, not generic templates.

## Purpose

Standards created here are loaded during `mill run` to guide implementation:
- Consistent formatting and style
- Naming conventions aligned with existing code
- Architecture patterns the codebase follows
- Build/test requirements

## Context
Pre-loaded: `.mill/context.md`, `.mill/standards/code.md` (if exists), config files detected.

## Flow

### 1. Check Existing Standards

If `.mill/standards/code.md` exists, show summary:

```
found existing standards:

sections:
- [section name] ({n} rules)
- [section name] ({n} rules)

what would you like to do? (update, add, regenerate, or review)
```

Accept natural language. Examples:
- "update" → Update Flow
- "add naming rules" → Add specific section
- "regenerate" → confirm → Infer Flow
- "review" → show full content

If no existing standards, go directly to Infer Flow.

---

### Infer Flow (detect and generate standards)

#### 1a. Detect Config Files

Search for standard config files:

```bash
# List files in root that might contain standards
ls -la | grep -E '\.(editorconfig|eslintrc|prettierrc|stylecop)' || true
ls -la *.json 2>/dev/null | grep -E '(tsconfig|stylecop|biome)' || true
```

Also check for:
- `.editorconfig` — indentation, line endings
- `.eslintrc.*`, `eslint.config.*` — JS/TS linting
- `.prettierrc*`, `prettier.config.*` — formatting
- `stylecop.json`, `.stylecop` — C# style
- `tsconfig.json` — TypeScript config
- `biome.json` — Biome linter/formatter
- `.rubocop.yml` — Ruby style
- `pyproject.toml`, `setup.cfg` — Python config
- `go.mod` — Go module info
- `Makefile`, `justfile` — build conventions

Read each found config and extract relevant rules.

#### 1b. Analyze Codebase Patterns

Sample code files to infer patterns:

```bash
# Find main source files
git ls-files | grep -E '\.(cs|ts|js|py|go|rb|rs)$' | head -10
```

For each language detected, analyze:
- **Indentation** — tabs vs spaces, width
- **Naming** — PascalCase, camelCase, snake_case for different constructs
- **Imports** — order, grouping
- **Comments** — style, documentation patterns
- **Architecture** — directory structure, module patterns

#### 1c. Summarize Findings

Present detected standards:

```
detected from config files:
- [source]: [rule summary]
- [source]: [rule summary]

inferred from codebase:
- [pattern]: [evidence]
- [pattern]: [evidence]

anything to add or change? (yes / proceed / skip [category])
```

Accept modifications before generating.

#### 1d. Generate Standards

Create `.mill/standards/code.md` with discovered rules.

---

### Update Flow (modify existing standards)

Show current section, ask what to change:

```
[section name]:
- rule 1
- rule 2

what would you like to change? (add rule / remove rule / reword)
```

After changes: "save? (yes / change more / discard)"

---

### Add Flow (add new section or rules)

```
what kind of standards? (naming, formatting, architecture, testing, or describe)
```

Elicit specifics, then:
```
new rules:

[section]:
- [rule]
- [rule]

add these? (yes / edit / cancel)
```

---

## Standards Template

Organized by category, evidence-backed.

```markdown
<!-- mill-standards: {hash} -->
<!-- updated: {timestamp} -->
<!-- sources: {list of config files} -->

# Code Standards

## Formatting
{Rules about indentation, line length, etc.}

## Naming
{Naming conventions for types, methods, variables, files}

## Architecture
{Directory structure, module patterns, dependencies}

## Documentation
{Comment style, when to document}

## Testing
{Test file naming, coverage expectations}

---
*Detected from: {sources}*
```

---

## Detection Priority

When configs conflict with code patterns, trust configs over inferred patterns.

1. **Explicit config files** — `.editorconfig`, `.eslintrc`, etc.
2. **Project conventions** — `AGENTS.md`, `README.md` style notes
3. **Inferred from code** — patterns observed in existing files

---

## File Format

`.mill/standards/code.md` structure:

```markdown
<!-- mill-standards: abc123 -->
<!-- updated: 2025-01-20T14:30:00Z -->
<!-- sources: .editorconfig, tsconfig.json -->

# Code Standards

## Formatting

- Use 4 spaces for indentation (from .editorconfig)
- Max line length: 120 characters
- UTF-8 encoding, LF line endings

## Naming

- **Types:** PascalCase (`UserService`, `HttpRequest`)
- **Methods:** PascalCase for C#, camelCase for JS/TS
- **Variables:** camelCase
- **Constants:** UPPER_SNAKE_CASE
- **Files:** kebab-case for scripts, PascalCase for classes

## Architecture

- CLI commands in `cli/Program.cs`
- Prompts in `spec/prompts/` and `run/prompts/`
- Templates in `spec/templates/`

## Documentation

- Use `///` XML docs for public APIs in C#
- Minimal comments — code should be self-documenting
- Diagrams must use Mermaid, not ASCII art

---
*Detected from: .editorconfig, tsconfig.json, cli/Program.cs patterns*
```

---

## Save Flow

After save:
```
saved .mill/standards/code.md

detected: {n} rules from config
inferred: {n} rules from code

anything else? (update / done)
```

On "done":
```
done. standards will be loaded during `mill run`.
```

---

## Rules

1. **Evidence-first** — every rule needs a source (config file, code pattern, or user input)
2. **Config wins** — explicit config files override inferred patterns
3. **Be specific** — "use 4 spaces" not "use consistent indentation"
4. **Language-aware** — group by language if multi-language project
5. **No aspirational rules** — only document what the codebase actually does
6. **Preserve existing** — update mode merges, doesn't replace
7. **Confirm destructive actions** — regenerate needs explicit y/n

## Anti-patterns

- "Follow best practices" — too vague, cite specific rules
- Rules the codebase doesn't follow — standards document reality
- Long explanations — keep rules to one line
- Duplicate rules from different sources — dedupe and cite primary
- Inventing conventions — only document what exists
