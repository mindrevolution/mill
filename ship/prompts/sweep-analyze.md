# Sweep Analysis

Analyze this codebase against its standards and surface findings for potential cleanup.

**Output:** Structured JSON with findings by category.

## Context

- **Standards:** `.mill/standards/code.md` (loaded below)
- **Config:** `.mill/project.json` (source filtering)
- **Project:** `.mill/context.md` (codebase overview)

## Standards to Check Against

{{STANDARDS_CONTENT}}

## Analysis Scope

Scan project source code only. Exclude:
- `node_modules/`, `vendor/`, `.git/`
- Build outputs, generated files
- Test fixtures and mock data
- Third-party code

## Finding Categories

Organize findings into these categories:

1. **DRY Violations** - Duplicated code, copy-paste patterns
2. **Complexity** - Methods too long, deep nesting, high cyclomatic complexity
3. **Naming** - Inconsistent naming, unclear identifiers
4. **Architecture** - Layer violations, circular dependencies, misplaced code
5. **Documentation** - Missing/stale docs, TODO/FIXME comments
6. **Deprecated** - Old patterns, outdated APIs, legacy code

## Output Format

Return a JSON object with this structure:

```json
{
  "categories": [
    {
      "name": "DRY Violations",
      "description": "Code duplicated across files",
      "findings": [
        {
          "id": "dry-001",
          "title": "Duplicate validation logic",
          "description": "Same email validation pattern in UserService and AuthController",
          "file": "src/services/UserService.ts",
          "line": 42,
          "severity": "medium"
        }
      ]
    }
  ]
}
```

### Field Definitions

- **id**: Unique identifier (category-prefix + number, e.g., `dry-001`, `complexity-003`)
- **title**: Short description (max 60 chars)
- **description**: Full context (max 200 chars)
- **file**: Path relative to repo root (null if N/A)
- **line**: Line number (null if N/A)
- **severity**: `low`, `medium`, or `high`

## Guidelines

1. **Evidence-based** - Only report findings you can point to in code
2. **Actionable** - Each finding should have a clear fix
3. **Standards-aligned** - Reference which standard is violated
4. **De-duplicate** - Group similar issues, don't list every instance
5. **Prioritize** - Report most impactful findings first within category
6. **Skip empty categories** - Don't include categories with no findings

## Analysis Process

1. Read `.mill/standards/code.md` for rules to check
2. Sample source files (`git ls-files | grep -E '\.(cs|ts|js|py|go)$'`)
3. For each category:
   - Identify patterns that violate standards
   - Find specific examples in code
   - Assess severity based on impact
4. Output structured JSON

## Output

Wrap your analysis in a code block:

```
SWEEP_ANALYSIS
{
  "categories": [...]
}
```

If no findings in any category, return:

```
SWEEP_ANALYSIS
{
  "categories": []
}
```
