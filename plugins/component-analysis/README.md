# Component analysis

Agent skills for auditing Xperience by Kentico platform components for consistency and maintainability.

## Workflow

These prompts provide a two-stage workflow:

1. Analysis stage - Audits one or more component categories and generates structured JSON artifacts.
2. Report stage - Validates analysis artifacts and copies a static SPA report shell that renders the JSON data at runtime.

This split keeps the analysis reusable. Teams can analyze only changed categories and regenerate reports without rerunning code discovery.

## Supported categories

- admin-ui
- page-builder
- email-builder
- form-builder
- global-extensibility

## Prerequisites

- Xperience by Kentico solution in the workspace.
- AI coding assistant installed (for example, GitHub Copilot or Claude Code).
- Kentico Docs MCP server configured (required by the analyze-components prompt).

## Install the plugin

### VS Code (GitHub Copilot)

Add the marketplace to your VS Code settings (settings.json), then browse and install from the Extensions sidebar (@agentPlugins):

```json
"chat.plugins.marketplaces": [
    "Kentico/xperience-by-kentico-kenticopilot"
]
```

### Copilot CLI

```bash
copilot plugin marketplace add Kentico/xperience-by-kentico-kenticopilot
copilot plugin install component-analysis@xperience-by-kentico-kenticopilot
```

### Claude Code

```bash
/plugin marketplace add Kentico/xperience-by-kentico-kenticopilot
/plugin install component-analysis@xperience-by-kentico-kenticopilot
```

## Usage

### 1. Run the analysis stage

Run the analyze-components prompt and provide the project root path. You can optionally limit the run to selected categories.

VS Code GitHub Copilot example:

```text
/analyze-components

Project folder path: /Users/example/dev/MyXperienceProject
Categories: admin-ui, page-builder
```

The prompt writes JSON artifacts under the analyzed project's output folder:

```text
.kenticopilot/
  component-analysis/
    analysis/
      analysis-index.json
      component-analysis-summary.json
      admin-ui.json
      page-builder.json
    report/
```

For partial runs, only the selected category files are written in `analysis/`.
Other category JSON files are created only when those categories are included in a run.

### 2. Run the report stage

Run the analyze-components-report prompt and provide either the project root path or the component-analysis output folder.

VS Code GitHub Copilot example:

```text
/analyze-components-report

Project folder path: /Users/example/dev/MyXperienceProject
Included categories: admin-ui, page-builder
```

This stage validates artifact presence/schema compatibility, then writes SPA assets to:

- .kenticopilot/component-analysis/report/index.html
- .kenticopilot/component-analysis/report/app.js
- .kenticopilot/component-analysis/report/styles.css
- .kenticopilot/component-analysis/report/tokens.css

The SPA reads analysis JSON files from relative paths and dynamically renders the report UI.
The summary JSON (`analysis/component-analysis-summary.json`) is produced by the analysis stage and consumed by the SPA.

## Best practices

- Run analysis first, report deployment second.
- Analyze only changed categories when iterating, then regenerate the report.
- Review and version the generated JSON artifacts so trend changes are visible across runs.
- Treat consistency drift as a priority signal for maintainability and AI-assisted implementation quality.
- Treat `component-analysis-summary.json` as the primary automation input for follow-up remediation agents and report rendering.

## Prompt reference

### Analyze components

Prompt name: analyze-components

Purpose:

- Audits selected categories for consistency.
- Produces per-category JSON files, analysis-index.json, and summary artifacts under .kenticopilot/component-analysis/analysis.

Notes:

- Requires Kentico Docs MCP.
- Uses fixed docs manifest links plus a final MCP discovery pass.

### Generate component analysis report

Prompt name: analyze-components-report

Purpose:

- Reads existing analysis JSON artifacts.
- Validates analysis JSON artifacts against schema files.
- Copies a zero-build SPA shell to .kenticopilot/component-analysis/report.

Notes:

- Does not require MCP.
- Does not perform fresh code discovery or analysis.

## Included skills

- analyze-components - Audits component categories and writes per-category analysis artifacts.
- analyze-components-report - Validates analysis artifacts and deploys the SPA report shell.

## Skill customization

These skill files provide a baseline for component consistency analysis in Xperience by Kentico projects. Modify and extend category reference files, docs manifests, and schema expectations to match your project conventions and governance requirements.
