---
name: analyze-components
description: "Audits one or more Xperience by Kentico component categories for consistency and writes structured JSON analysis files under .kenticopilot/component-analysis."
argument-hint: "Path to the Xperience by Kentico project folder to analyze. Optionally include categories to limit the audit."
compatibility: "Requires Kentico Docs MCP"
---

You are tasked with auditing component consistency in an Xperience by Kentico project to improve maintainability and AI-assisted development outcomes.

The primary goal of this skill is not to produce abstract best-practice commentary. The primary goal is to identify patterns that make the codebase inconsistent and therefore harder for developers and AI agents to extend safely.

This skill is the entry point for component analysis. It writes structured JSON artifacts for each requested component category. It does not generate the final HTML report. Use the separate `analyze-components-report` skill to synthesize HTML from the generated JSON files.

This skill must remain project-agnostic. Do not assume sample-project-specific file paths, naming conventions, or architecture unless discovered in the current workspace.

## Input parameters

- **Project folder path** - Required. Root folder of the Xperience by Kentico project to analyze.
- **Categories** - Optional. A subset of categories to analyze. If omitted, analyze all supported categories.
- **Output root** - Optional. Default to `<project folder>/.kenticopilot/component-analysis`.

Treat category requests case-insensitively. Map user input to the following category keys:

- `admin-ui`
- `page-builder`
- `email-builder`
- `form-builder`
- `global-extensibility`

If the user requests a subset, load and analyze only those categories.

## Required reference files

Before auditing code, read these files from this skill folder:

1. `references/output-schema.md`
2. `references/docs-manifest.md`
3. The selected category files under `references/component-types/`

Do not read category files that are outside the selected audit scope.

## Required docs workflow

For each selected category:

1. Start with the fixed documentation links listed in `references/docs-manifest.md` for that category.
2. Use Kentico Docs MCP to retrieve the current official guidance for those links.
3. Perform one final MCP pass for the category to check whether there are additional relevant current docs pages or subtopics not already covered by the fixed links.
4. If you find new relevant docs pages, use them and record them in the output.

Use the docs to distill concrete, auditable checks. If Kentico docs guidance and existing code differ, report both and call out the drift explicitly.

## Output location

Create and update artifacts only under:

`<project folder>/.kenticopilot/component-analysis`

Use this structure:

```text
.kenticopilot/
  component-analysis/
    analysis-index.json
    categories/
      admin-ui.json
      page-builder.json
      email-builder.json
      form-builder.json
      global-extensibility.json
    reports/
```

Only write category files for categories included in the current run.

## Analysis workflow

### 1. Prepare the output folder

- Ensure the `.kenticopilot/component-analysis` folder exists at the project root.
- Ensure the `categories` and `reports` subfolders exist.

### 2. Discover implementations

For each selected category, find relevant implementations and build an inventory with:

- component category
- component type
- component identifier or name
- registration mechanism and metadata
- primary source files
- related files such as properties models, Razor views, client modules, styles, scripts, tests

### 3. Run consistency analysis

For each selected category, use the category reference file plus current docs guidance to evaluate:

- naming and identifier consistency
- registration and discoverability consistency
- properties and editing experience consistency
- rendering and composition consistency
- service and dependency usage consistency
- reliability and safety consistency
- documentation and testability consistency

Prioritize checks that reveal whether the codebase follows one repeatable pattern or several conflicting patterns.

### 4. Produce category JSON

Write one JSON file per selected category into the `categories` folder. Follow the core contract defined in `references/output-schema.md`.

The schema must have a strict shared core and may be extended with category-specific data under `extensions`.

### 5. Produce or update the analysis index

Create or update `analysis-index.json` in the output root. This file must summarize:

- project path
- a single top-level `generatedAtUtc` timestamp for the index
- selected categories for the current run
- available category artifact paths
- docs references used by category

Do not generate HTML in this skill.

## Category expectations

Use the corresponding category reference file to guide discovery and checks:

- `references/component-types/admin-ui.md`
- `references/component-types/page-builder.md`
- `references/component-types/email-builder.md`
- `references/component-types/form-builder.md`
- `references/component-types/global-extensibility.md`

## Important rules

- Consistency is the primary evaluation metric. Best-practice checks support the analysis, but do not let them overshadow consistency findings.
- Always include doc references for rules that materially influence a finding or recommendation.
- Always include `confidence`, `falsePositiveRisk`, and `estimatedAgentEffort` for findings and recommendations.
- `estimatedAgentEffort` must estimate the effort for an AI agent or coding agent to remediate the issue, not human organizational effort.
- If a selected category is genuinely absent from the project, still produce its category JSON file and mark the coverage status accordingly.
- If evidence is ambiguous, say so explicitly instead of overstating certainty.
- Use repository-relative paths in evidence whenever possible.
- Do not invent scoring rubrics beyond what is explicitly defined in the output schema. If a score is not yet defined, omit it or set it to `null` where the schema allows it.

## Final response format

Return:

1. The path to `analysis-index.json`
2. Paths to the category JSON files written in the current run
3. A concise summary of:
   - the highest-risk consistency problems found
   - categories with missing or uncovered implementations
   - which category JSON files are now ready for the report-generation skill

Do not output only narrative text when this skill runs. The JSON artifacts are mandatory deliverables.
