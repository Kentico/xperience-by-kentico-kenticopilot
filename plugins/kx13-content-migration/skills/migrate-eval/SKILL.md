---
name: migrate-eval
description: Evaluates migration results across 12 categories by gathering data from logs, databases, and migration plan. Generates a self-contained HTML report with per-category pass/fail/warn status, prioritized remediation guidance, and next-step routing to close the migration loop.
compatibility: Requires sqlcmd CLI and network access to KX13 and XbyK databases. Requires parse-migration-logs.ps1 script.
argument-hint: "[migration-plan-detail-path] [appsettings-path?]"
---

# Migration Evaluation

Gathers data from three sources, evaluates migration results across 12 categories, generates an HTML report, and routes the user to next steps:

1. **Log analysis** — what the tool attempted and reported (parse script → YAML)
2. **Database verification** — actual state in KX13 and XbyK (SQL queries)
3. **Plan cross-reference** — expected vs actual (migration plan detail)
4. **HTML report** — self-contained report with remediation guidance
5. **Console summary** — prioritized action items and next-step routing

Does NOT validate configuration — that is handled upstream by `migrate-appsettings` and `migrate-run` skills before migration runs.

## Prerequisites

1. At least one migration run completed
2. Protocol log (`protocol*.txt`) and console log (`migration-run*.log`) in the directory from `Settings.MigrationProtocolPath`
3. Migration plan detail markdown file
4. `sqlcmd` CLI available with network access to both databases
5. `appsettings.json` with connection strings

## Workflow

### Phase 0: Resolve Inputs

1. **Migration plan detail**: Use the first argument. If not provided, prompt the user for the path.
2. **appsettings.json**: Use the optional second argument. If not provided, default to `xperience-by-kentico-kentico-migration-tool-master/Migration.Tool.CLI/appsettings.json`.
3. **Log directory**: Read `Settings.MigrationProtocolPath` from appsettings.json. Use its parent directory as the log directory for all inputs and outputs. If not set, prompt the user for the path.

Verify required files exist before proceeding.

### Phase 1: Gather Data

#### 1a. Parse Logs

Run the log parsing script, passing the log directory. The script discovers all `protocol*.txt` and `migration-run*.log` files, processes them chronologically, and merges results across runs (later runs override earlier failures per entity):
```
& skills/migrate-eval/scripts/parse-migration-logs.ps1 -LogDir <log-dir>
```
Read output at `<log-dir>/parsed-log-summary.yaml`.

If script fails, mark all log-based checks as **N/A** — do not attempt manual regex parsing.

#### 1b. Establish Database Connections

Read `appsettings.json`. Extract connection strings:
- **KX13**: `Settings.KxConnectionString`
- **XbyK**: `Settings.XbyKApiSettings.ConnectionStrings.CMSConnectionString`

Parse server (`-S`) and database (`-d`). Handle both `Data Source`/`Server` and `Initial Catalog`/`Database` variants.

Validate: `sqlcmd -S <server> -d <database> -Q "SELECT 1" -h -1`

If a connection fails, note which categories will have partial data. Do NOT stop — other sources can still proceed.

#### 1c. Parse Migration Plan

Read the migration plan detail. Split by `## ` (H2) and `### ` (H3) headings. Parse markdown tables (split by `|`, trim cells). Extract expected entities:

| Plan Section | What to Extract |
|---|---|
| Target Content Model > Webpage Content Types | Class names, created by (tool/manual) |
| Target Content Model > Content Hub Types | Class names, created by |
| Target Content Model > Reusable Field Schemas | Schema names, field names |
| Target Content Model > Taxonomies | Taxonomy names, tags, created by |
| Content Model Mapping > Exclusions | Source classes to exclude |
| Field Mappings | Source → target field pairs per class |
| Widget Transformations | Source → target widget types |

If a section is not found, mark that evaluation category as **N/A**.

#### 1d. Run All SQL Queries

Read [eval-sql-queries.md](references/eval-sql-queries.md). Execute all queries for the 12 categories. Log all `sqlcmd` output to `<log-dir>/eval-queries.log`.

Use parseable output: `sqlcmd -S <server> -d <database> -Q "<query>" -W -s "|" -h -1`

### Phase 2: Evaluate 12 Categories

Read [evaluation-categories.md](references/evaluation-categories.md) for the full per-category evaluation logic. For each category, compare gathered data against plan expectations and assign status:

| # | Category | Primary Sources |
|---|----------|----------------|
| 1 | Configuration & Run Overview | YAML `run` section |
| 2 | Content Types | XbyK `CMS_Class` + protocol log + plan |
| 3 | Reusable Field Schemas | XbyK `CMS_ContentItemCommonData` + plan |
| 4 | Taxonomies & Tags | XbyK `CMS_Taxonomy`/`CMS_Tag` + plan |
| 5 | Content Item Counts & Orphans | KX13 `View_CMS_Tree_Joined` + XbyK counts |
| 6 | Field Verification | XbyK `INFORMATION_SCHEMA.COLUMNS` + plan |
| 7 | Page Migration Issues | YAML errors + protocol log + Page Builder data |
| 8 | Users & Roles | Protocol log + KX13 vs XbyK counts |
| 9 | Media & Attachments | YAML media stats + KX13 vs XbyK counts |
| 10 | Forms | KX13 vs XbyK `CMS_Form` names |
| 11 | Custom Modules | Protocol log `ResourceInfo` + XbyK data |
| 12 | Overall Health | Aggregate from all categories |

Status values: **PASS** (matches), **FAIL** (critical mismatch), **WARN** (non-critical), **N/A** (data unavailable).

### Phase 3: Generate HTML Report

Read these references:
- [report-generation.md](references/report-generation.md) for template filling instructions and per-category HTML fragment structure
- [actionable-suggestions.md](references/actionable-suggestions.md) for issue → remediation mapping with fix types and skill routing
- [LOG_ANALYSIS_REPORT_TEMPLATE.html](assets/LOG_ANALYSIS_REPORT_TEMPLATE.html) for the HTML template

For each of the 12 categories, build an HTML fragment replacing the corresponding `{{CAT_N_BODY}}` token. Build prioritized action items (`{{ACTION_ITEMS_BODY}}`): FAIL items first with remediation type badge, then WARN items. Deduplicate across categories.

Save to `<log-dir>/migrate-eval-report.html`.

### Phase 4: Console Summary & Routing

Display:

1. **Summary table** — all 12 categories with status
2. **FAIL items** — with remediation type and specific fix
3. **WARN items** — with recommended action

Then route next steps based on results:

| Result | Action |
|--------|--------|
| **All PASS** | Migration verified. Proceed to UAT / manual content review. |
| **FAIL (Config)** | Re-run `migrate-appsettings` skill to fix config → re-run migration with `--bypass-dependency-check` → re-evaluate. |
| **FAIL (Code)** | Re-run the relevant extension skill (`migrate-classes`, `migrate-widgets`, `migrate-content-items`, or `migrate-fields`) → rebuild extensions → re-run migration → re-evaluate. |
| **FAIL (Manual)** | Document manual steps for user. These cannot be automated — user must act in XbyK Administration. |
| **WARN only** | Present warnings to user for acceptance decision. If acceptable → migration complete. If not → apply fixes and re-evaluate. |
| **Mixed FAIL + WARN** | Address FAILs first (config before code). WARNs can wait until FAILs are resolved. |

Be specific when routing. Examples:
- "Re-run `migrate-classes` skill for `MedioClinic.DoctorProfile` to fix taxonomy JSON casing"
- "Re-run `migrate-appsettings` skill — `EntityConfigurations` keys use dots instead of underscores"
- "Manually delete orphaned content type `MedioClinic.DayOfWeek` in XbyK Administration > Content types"

## Rules

- **Read-only only** — all SQL is `SELECT`. Never modify logs, plan, config, or source code.
- **Always save query output** to `<log-dir>/eval-queries.log`.
- **Graceful degradation** — missing logs → log checks N/A. Failed DB → DB checks N/A. Always produce partial results.
- **Deduplicate cascading errors** — count unique issues, not total log lines.
- **Filter noise** — system resource skips, root node skips, admin user skips, terminal `InvalidOperationException` are informational. Do not count them as errors.
- **Self-contained HTML** — inline CSS, no external deps, no JavaScript.
- **Every FAIL / WARN must have a remediation** classified as: Config, Code, or Manual.
- **Every remediation must route to a specific skill or manual action** — never say "verify manually" without specifying what and where.

## Gotchas

- Protocol log entries are multi-line — the parse script handles this. Do not parse manually.
- Console log may have ANSI escape codes (`\[\d+m`) — strip before parsing.
- Both protocol log and console log live in the same directory — the parent of `Settings.MigrationProtocolPath`. Protocol files are `protocol*.txt`, console logs are `migration-run*.log`.
- `EntityConfigurations` keys must use underscores (`CMS_Class`), not dots. Misspelling makes `ExcludeCodeNames` silently fail.
- `--page-types` creates `DataClassInfo` for ALL source classes regardless of `ExcludeCodeNames`. Excluded classes replaced by taxonomy → orphaned content type (WARN).
- Taxonomy tag JSON: `"identifier"` (lowercase) produces no log error but tags don't render. DB query is the only detection method.
- Table names use underscores: `MedioClinic.Doctor` → `MedioClinic_Doctor` in SQL.
- Exclude linked pages from KX13 source counts: `WHERE NodeLinkedNodeID IS NULL`.
- Content Hub items use `ClassContentTypeType = 'Content'`, not `'Website'`.
- Connection string format varies — handle `Data Source`/`Server` and `Initial Catalog`/`Database`.
- Reusable schema fields are on `CMS_ContentItemCommonData`, NOT per-content-type data tables.
