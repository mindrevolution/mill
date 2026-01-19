# Autopick Next Issue

Select the highest-value issue based on project health and priorities.

## Inputs

- `{{LABEL_MAP}}` — Contents of `.mill/label-map.yaml` (or empty if not exists)
- `{{OPEN_ISSUES}}` — Open issues JSON

## Workflow

### 1. Health Check (GitHub Actions)

```bash
# Get recent workflow runs
gh run list --limit 5 --json status,conclusion,name,createdAt,headBranch
```

Evaluate:

| Condition | Status | Action |
|-----------|--------|--------|
| Latest run on default branch failed | **BLOCK** | "ci failing on main — fix before new work" |
| Latest run on default branch passing | **OK** | continue |
| No workflow runs found | **OK** | skip CI check, repo may not use Actions |

Also check WIP:
```bash
gh issue list --state open --label in-progress --json number,title
gh issue list --state open --label blocked --json number,title
```

| Condition | Status | Action |
|-----------|--------|--------|
| WIP > 2 | **WARN** | "N issues in-progress — consider finishing first" |
| Blocked > 0 | **WARN** | "N issues blocked — may need attention" |

If **BLOCK**: stop, report reason, output `BLOCKED`. Do not pick.
If **WARN**: display warnings, continue.

### 2. Score Candidates

Exclude issues with labels: `in-progress`, `blocked`, `ready-for-review`, or any in `{{LABEL_MAP}}.exclude`.

For each remaining issue:

#### a) Type Score (100-400)

Detect from labels (using `{{LABEL_MAP}}.type_mapping`) or infer from title/body:

| Type | Score | Signals |
|------|-------|---------|
| security | 400 | vulnerability, CVE, auth bypass, injection, XSS |
| bug | 300 | broken, error, crash, fails, doesn't work |
| feature | 200 | add, new, implement, create, support |
| task | 100 | refactor, update, migrate, cleanup, docs |

#### b) Impact Score (0-100)

From labels (using `{{LABEL_MAP}}.impact_mapping`) or infer:

| Impact | Score | Signals |
|--------|-------|---------|
| critical | 100 | blocks release, data loss, security, production down |
| high | 75 | significant user impact, key workflow broken |
| normal | 50 | standard priority, unlabeled |
| low | 0 | nice-to-have, cosmetic, minor |

#### c) Age Score (0-50)

```
age_score = min(50, days_since_created × 0.5)
```

Older issues gradually bubble up.

#### d) Complexity Penalty (0 to -50)

Estimate from issue body:

| Signals | Complexity | Penalty |
|---------|------------|---------|
| 1-2 acceptance criteria, single file | 1 | -10 |
| 3-4 criteria, few files | 2 | -20 |
| 5+ criteria, multiple components | 3 | -30 |
| Architectural, cross-cutting | 4 | -40 |
| Major refactor, unknown scope | 5 | -50 |

Also check `{{LABEL_MAP}}.complexity_mapping` for size labels.

#### e) Blocker Bonus (+30)

Search other open issues for references to this one:
- "blocked by #N"
- "depends on #N"
- "after #N"
- "needs #N"

If found, this issue unblocks others → +30.

### 3. Calculate & Rank

```
total = type + impact + age + complexity_penalty + blocker_bonus
```

Sort descending. Select top candidate.

### 4. Present Recommendation

```
health:
  ✓ ci passing (main, 2h ago)
  ! 1 issue blocked (#34 — waiting on API key)

---

recommendation: #47

  "fix Safari login redirect loop"

  type:       bug         +300
  impact:     high        +75
  age:        12 days     +6
  complexity: small       -10
  blocker:    no          +0
                          ───
  total:                  371

rationale: high-impact bug, small scope.
should complete in 1-2 iterations.

runners up:
  2. #52 "add password reset flow" (feature, 296)
  3. #41 "update dependencies" (task, 165)

---

run #47? [y/n/NUMBER]
```

### 5. Handle Response

| Input | Output | Effect |
|-------|--------|--------|
| y, yes | `PICK:#47` | CLI runs `mill run #47` |
| n, no | `SKIP` | Exit cleanly |
| 52 | `PICK:#52` | User overrides selection |
| ? | Show full score breakdown for all candidates |

## Edge Cases

**No open issues:**
```
no open issues found

create one with: mill spec
```
Output: `EMPTY`

**All issues excluded:**
```
all N issues are in-progress, blocked, or ready-for-review

in-progress:
  #34 — "auth refactor"
  #38 — "api caching"

blocked:
  #29 — "needs design review"
```
Output: `FULL`

**No label map:**
Proceed without it. Infer types from issue content. Note:
```
  ! no label map — run `mill init` to improve detection
```

## Output Tokens

| Token | Meaning |
|-------|---------|
| `PICK:#N` | User approved, run issue N |
| `SKIP` | User declined |
| `BLOCKED` | Health check failed |
| `EMPTY` | No issues to pick from |
| `FULL` | All issues already have workflow labels |
