---
name: analyze-components-report
description: "Reads component-analysis JSON artifacts from .kenticopilot/component-analysis and generates an aggregated HTML report."
argument-hint: "Path to the Xperience by Kentico project folder or the .kenticopilot/component-analysis folder"
compatibility: "Does not require MCP"
---

You are tasked with generating a readable HTML report from previously generated component-analysis JSON artifacts.

This skill does not perform fresh code discovery and uses the JSON artifacts as they exist. If an artifact for a component type is missing, report it to the user. This skill's primary input is the output of the `analyze-components` skill.

## Input parameters

- **Project folder path or analysis output folder** - Required.
- **Included categories** - Optional. If omitted, include all available category JSON files found in the analysis output folder.

Resolve the analysis output root to:

- `<project folder>/.kenticopilot/component-analysis`, or
- the provided folder if the user already points to `.kenticopilot/component-analysis`

## Required files

Read:

1. `analysis-index.json`
2. All selected files under `categories/`
3. `references/report-template.html`
4. `references/report-summary-schema.md`
5. `references/branding.md`

If `analysis-index.json` is missing, infer available categories from the `categories` directory and say so in the final response.

## Output files

Write artifacts only under the analysis output root:

- `reports/component-analysis-report.html`
- `reports/component-analysis-summary.json`

The summary JSON is a downstream handoff artifact for other agents and external presentation systems.
Treat it as a stable, machine-consumable document, not a narrative supplement.

## Report requirements

Generate a professional, readable HTML document with embedded CSS and no external dependencies.
Use `references/report-template.html` as the structural baseline and adapt it to actual data volume.
Apply branding and design direction from `references/branding.md` and the linked Kentico brand pages.

Include:

- executive summary cards
- analyzed category coverage summary
- category-by-category findings table
- inconsistency severity and AI-risk summary
- best-practice and consistency matrix
- prioritized action plan
- docs references by category

The report should clearly distinguish:

- categories that were analyzed and have findings
- categories that were analyzed and not present in the project
- categories that have not yet been analyzed

The report should remain highly scannable for large projects:

- keep executive summary and coverage visible quickly
- provide drill-down per category via expandable/detail sections
- keep finding and action tables comparable across categories
- avoid layouts that collapse under long recommendation lists

## Aggregation rules

- Preserve the category-level findings and recommendations from the source JSON files.
- Do not invent findings that are not supported by the category artifacts.
- When combining actions across categories, merge only clearly duplicate actions.
- Preserve `confidence`, `falsePositiveRisk`, and `estimatedAgentEffort` in the summary JSON and surface them in the HTML where helpful.
- If scoring fields are `null` or absent in the source JSON, do not fabricate them.

## Predictability and determinism

Use deterministic output rules for both HTML and summary JSON:

- category display order must always be: `admin-ui`, `page-builder`, `email-builder`, `form-builder`, `global-extensibility`
- apply stable sorting for findings and actions as defined in `references/report-summary-schema.md`
- when combining duplicate actions, use stable grouping keys and deterministic action IDs
- preserve unknowns explicitly rather than inventing values

Do not introduce arbitrary wording or section changes between runs when source data is unchanged.
When the same input artifacts are used, output structure and ordering should remain predictably similar.

## Final response format

Return:

1. Path to `reports/component-analysis-report.html`
2. Path to `reports/component-analysis-summary.json`
3. A concise summary of:
   - the highest-risk cross-category consistency problems
   - categories included in the report
   - any missing prerequisite JSON files
