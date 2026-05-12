# Component analysis

Agent skills for auditing Xperience by Kentico platform components for consistency and maintainability.

## Workflow

These prompts provide a two-stage workflow:

1. Analysis stage - Audits one or more component categories and generates structured JSON artifacts.
2. Report stage - Aggregates the JSON artifacts into a human-readable HTML report and JSON document used by agents to make suggested updates.

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
    analysis-index.json
    categories/
      admin-ui.json
      page-builder.json
    reports/
```

For partial runs, only the selected category files are written in `categories/`.
Other category JSON files are created only when those categories are included in a run.

### 2. Run the report stage

Run the analyze-components-report prompt and provide either the project root path or the component-analysis output folder.

VS Code GitHub Copilot example:

```text
/analyze-components-report

Project folder path: /Users/example/dev/MyXperienceProject
Included categories: admin-ui, page-builder
```

This stage writes:

- .kenticopilot/component-analysis/reports/component-analysis-report.html
- .kenticopilot/component-analysis/reports/component-analysis-summary.json

The HTML report is generated from a reusable template designed for high-level scanning and deep drill-down across large component inventories.
The summary JSON is a stable machine-consumable handoff artifact intended for downstream agents and alternative visualizations (custom dashboards, charts, or external reporting pipelines).

## Best practices

- Run analysis first, report generation second.
- Analyze only changed categories when iterating, then regenerate the report.
- Review and version the generated JSON artifacts so trend changes are visible across runs.
- Treat consistency drift as a priority signal for maintainability and AI-assisted implementation quality.
- Treat `component-analysis-summary.json` as the primary automation input for follow-up remediation agents.

## Prompt reference

### Analyze components

Prompt name: analyze-components

Purpose:

- Audits selected categories for consistency.
- Produces per-category JSON files and analysis-index.json under .kenticopilot/component-analysis.

Notes:

- Requires Kentico Docs MCP.
- Uses fixed docs manifest links plus a final MCP discovery pass.

### Generate component analysis report

Prompt name: analyze-components-report

Purpose:

- Reads existing analysis JSON artifacts.
- Produces HTML and JSON report outputs under .kenticopilot/component-analysis/reports.
- Uses a reference HTML template and deterministic ordering rules for predictable output across runs.

Notes:

- Does not require MCP.
- Does not perform fresh code discovery unless prerequisite artifacts are missing.

## Included skills

- analyze-components - Audits component categories and writes per-category analysis artifacts.
- analyze-components-report - Aggregates analysis artifacts into report outputs.

## Skill customization

These skill files provide a baseline for component consistency analysis in Xperience by Kentico projects. Modify and extend category reference files, docs manifests, and schema expectations to match your project conventions and governance requirements.
