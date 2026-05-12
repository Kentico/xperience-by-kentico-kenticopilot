# Report Generation

Instructions for generating the HTML evaluation report from findings and the HTML template.

## Inputs

1. Phase 2 evaluation results (12 categories with status, summary, evidence, and remediation)
2. [LOG_ANALYSIS_REPORT_TEMPLATE.html](../assets/LOG_ANALYSIS_REPORT_TEMPLATE.html) — HTML template with placeholder tokens
3. [actionable-suggestions.md](actionable-suggestions.md) — issue → remediation mapping with fix types and skill routing

## Template Placeholders

| Token | Source |
|---|---|
| `{{PROJECT_NAME}}` | Derive from plan path or appsettings |
| `{{TIMESTAMP}}` | Current date/time |
| `{{PROTOCOL_LOG_PATH}}` | Protocol log path from Phase 1 |
| `{{CONSOLE_LOG_PATH}}` | Console log path from Phase 1 |
| `{{PLAN_PATH}}` | Migration plan detail path from Phase 0 |
| `{{PASS_COUNT}}` | Count of PASS categories |
| `{{FAIL_COUNT}}` | Count of FAIL categories |
| `{{WARN_COUNT}}` | Count of WARN categories |
| `{{NA_COUNT}}` | Count of N/A categories |
| `{{SUMMARY_ROWS}}` | One `<tr>` per category with #, name, status badge, summary |
| `{{CAT_1_BODY}}` ... `{{CAT_12_BODY}}` | Per-category HTML fragment |
| `{{ACTION_ITEMS_BODY}}` | Prioritized action list |

## Per-Category HTML Fragment Structure

For each of the 12 categories, build an HTML fragment to replace `{{CAT_N_BODY}}`:

1. **Status badge** — colored pill (PASS / FAIL / WARN / INFO / N/A)
2. **Summary sentence** — one-line category result
3. **Log Evidence** subsection — relevant log observations
4. **Database Verification** subsection — SQL query results
5. **Plan Cross-Reference** — expected vs actual comparison
6. **Entity details table** — per-entity rows for FAIL/WARN categories
7. **Action Required callout** — for FAIL/WARN, use actionable-suggestions.md for remediation type and skill routing

## Prioritized Action Items

Build `{{ACTION_ITEMS_BODY}}`:

1. FAIL items first, each with remediation type badge (Config / Code / Manual)
2. WARN items next
3. Each with specific next step and skill to run
4. Deduplicate — if the same root cause spans multiple categories, list the fix once

## Output Rules

- **Self-contained HTML** — inline CSS, no external deps, no JavaScript
- Save to `<log-dir>/migrate-eval-report.html`
- Do not modify the template footer
