---
name: analyze-components-report
description: "Creates a report app from the component-analysis skill artifacts and performs full artifact validation."
argument-hint: "Path to the Xperience by Kentico project folder or the .kenticopilot/component-analysis folder"
compatibility: "PowerShell Test-Json command or node.js + npx to run ajv-cli package"
---

You are tasked with validating previously generated component-analysis JSON artifacts and deploying a static SPA report shell.

This skill does not perform fresh code discovery and uses the JSON artifacts as they exist. If an artifact for a component type is missing, report it to the user. This skill's primary input is the output of the `analyze-components` skill.

This skill must fail fast when required artifacts are missing or schema-invalid.

## Input parameters

- **Project folder path or analysis output folder** - Required.
- **Validation mode** - Optional. Must be one of:
   - `test-json` (PowerShell `Test-Json`)
   - `ajv-cli` (`npx ajv-cli validate`)
   - `skip` (no schema validation)

If validation mode is omitted, ask the user to choose one of the three modes before proceeding.

Resolve categories from existing artifacts in this order:

1. `analysis/component-analysis-summary.json` -> `sourceArtifacts.includedCategoryArtifacts`
2. `analysis/analysis-index.json` -> `availableCategoryArtifacts`

If neither source provides category artifacts, fail with an actionable error.

Resolve the analysis output root to:

- `<project folder>/.kenticopilot/component-analysis`, or
- the provided folder if the user already points to `.kenticopilot/component-analysis`

## Required files

Read the following from the **analysis output root** (`<project>/.kenticopilot/component-analysis`):

1. `analysis/analysis-index.json`
2. All category files referenced by resolved artifact paths under `analysis/`
3. `analysis/component-analysis-summary.json`

Read the following from the **report skill's folder**:

4. `../analyze-components/references/schemas/analysis-index.schema.json`
5. `../analyze-components/references/schemas/category-analysis.schema.json`
6. `../analyze-components/references/schemas/report-summary.schema.json`
7. `references/spa-shell/index.html`
8. `references/spa-shell/app.js`
9. `references/spa-shell/styles.css`
10. `references/spa-shell/tokens.css`
11. `references/report-summary-schema.md`

Important: do not resolve `references/...` from the analysis output root; resolve them from the skill folder path above.

Do not infer missing required artifacts.
If any required file is missing, stop and return actionable errors.

## Required validation workflow

Prompt the user to select a validation mode from the options below.

### Mode: `test-json`

Use PowerShell `Test-Json` for each instance/schema pair as separate commands.

Example commands:

- `Test-Json -Json (Get-Content analysis/analysis-index.json -Raw) -Schema (Get-Content ../analyze-components/references/schemas/analysis-index.schema.json -Raw)`
- `Test-Json -Json (Get-Content analysis/component-analysis-summary.json -Raw) -Schema (Get-Content ../analyze-components/references/schemas/report-summary.schema.json -Raw)`
- `Test-Json -Json (Get-Content analysis/<file>.json -Raw) -Schema (Get-Content ../analyze-components/references/schemas/category-analysis.schema.json -Raw)`

### Mode: `ajv-cli`

Use Node via `npx ajv-cli` for each instance/schema pair as separate commands.

Example commands:

- `npx ajv-cli validate -s ../analyze-components/references/schemas/analysis-index.schema.json -d analysis/analysis-index.json`
- `npx ajv-cli validate -s ../analyze-components/references/schemas/report-summary.schema.json -d analysis/component-analysis-summary.json`
- `npx ajv-cli validate -s ../analyze-components/references/schemas/category-analysis.schema.json -d analysis/<file>.json`

Use this for:

- `analysis/analysis-index.json` with `analysis-index.schema.json`
- `analysis/component-analysis-summary.json` with `report-summary.schema.json`
- each resolved file under `analysis/` with `category-analysis.schema.json`

### Mode: `skip`

Skip schema validation.

- Continue to copy SPA assets.
- Return a clear warning that no validation guarantees were applied and report rendering may fail with invalid data.

### Failure behavior

If mode is `test-json` or `ajv-cli` and any validation fails:

- Stop immediately.
- Do not generate or overwrite output files.
- Return a concise error list with file path and reason.

## Output files

Write files only under the analysis output root:

- `report/index.html`
- `report/app.js`
- `report/styles.css`
- `report/tokens.css`

Copy these files from `references/spa-shell/` with identical content.
Use the existing analysis JSON artifacts as data inputs. Do not regenerate them in this skill.

## Deployment requirements

The SPA must remain zero-build and static:

- Bootstrap 5.3 and Alpine.js loaded from CDN.
- Data loaded at runtime from relative JSON paths under `.kenticopilot/component-analysis`.
- Visual direction and styling defined by `references/spa-shell/tokens.css` and `references/spa-shell/styles.css`.

Include:

- executive summary cards
- analyzed category coverage summary
- category-by-category findings table
- inconsistency severity and AI-risk summary
- prioritized action plan
- category drill-down sections
- filters for category, severity, AI risk, and free-text search

The deployed UI should clearly distinguish:

- categories that were analyzed and have findings
- categories that were analyzed and not present in the project
- categories that have not yet been analyzed

The deployed UI should remain highly scannable for large projects:

- keep executive summary and coverage visible quickly
- provide drill-down per category via expandable/detail sections
- keep finding and action tables comparable across categories
- avoid layouts that collapse under long recommendation lists

## Copy workflow

After validation succeeds:

1. Ensure `<analysis output root>/report` exists.
2. Copy all files from `references/spa-shell/` into `<analysis output root>/report`.
3. Do not write or overwrite files under `analysis/` or the output root JSON artifacts.

## Final response format

Return:

1. Path to `report/index.html`
2. A concise summary of:
   - validation mode used (`test-json`, `ajv-cli`, or `skip`)
   - validation result for required artifacts
   - categories resolved from existing artifacts and included in the deployed report shell
   - any missing prerequisite JSON files
   - warning that rendering is not guaranteed when mode is `skip`
