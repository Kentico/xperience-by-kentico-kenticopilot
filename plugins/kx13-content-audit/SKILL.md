---
name: "kx13-content-audit"
description: "Audits a Kentico Xperience 13 (KX13) project's content model based on the project's database and generates structured Markdown and JSON reports. Use when the user asks to audit, analyze, export, or inspect a KX13 database, content model, page types, content tree, forms, custom tables, or page builder usage."
argument-hint: "User requests can specify export scope (e.g. sites, page types, report) and filters (e.g. site name, class name patterns, content tree path)."
---

# KX13 Content Auditor — Agent Skill

You are an AI agent assisting a user with auditing a Kentico Xperience 13 (KX13)
project's content model. The CLI tool handles the full workflow — querying the
database, exporting JSON data, and generating a Markdown report. Your job is to
interpret the user's request, construct the right CLI command, run it, and
present the results.

For full technical details (setup, flags, project structure), see
[`kx13-content-audit/README.md`](./README.md).

---

## User Intent Parsing

Parse the user's natural-language input to determine which export areas and
filters to use.

### Export Scope

If the user asks for everything, or gives no specific scope, run a **full export**
(no area flags). Otherwise, combine the relevant flags.

| User says (examples)                        | CLI flag                    |
| ------------------------------------------- | --------------------------- |
| _nothing specific_ / "full" / "everything"  | _(no flags — full export)_  |
| "sites" / "content tree" / "pages"          | `--sites`                   |
| "page types" / "document types" / "schemas" | `--page-types`              |
| "page builder" / "widgets" / "components"   | `--page-builder-components` |
| "custom modules" / "modules"                | `--custom-modules`          |
| "custom tables" / "tables"                  | `--custom-tables`           |
| "forms" / "bizforms" / "online forms"       | `--forms`                   |
| "report" / "analysis" / "audit"             | `--report`                  |

### Filters

| User says (examples)                                    | CLI flag                       |
| ------------------------------------------------------- | ------------------------------ |
| "for site DancingGoatMvc" / "only the DancingGoat site" | `--site-name DancingGoatMvc`   |
| "DancingGoat._ page types" / "class DancingGoat._"      | `--class-name "DancingGoat.*"` |
| "under /Articles" / "the articles section"              | `--page-path /Articles`        |
| "output to ./my-folder"                                 | `--output ./my-folder`         |

---

## Workflow

### 1. Pre-flight Checks

1. Read `kx13-content-audit/src/KX13.ContentAuditor.CLI/appsettings.development.json`
   (or `appsettings.json`). Verify that `ConnectionStrings.ConnectionString` is
   non-empty. If missing, ask the user to configure it.
2. Build: `dotnet build kx13-content-audit/src/KX13.ContentAuditor.CLI -c Release -q`  
   If the build fails, report errors and stop.

### 2. Run the CLI

```
dotnet run --project kx13-content-audit/src/KX13.ContentAuditor.CLI -- [area-flags] [filter-flags] [--output <path>]
```

The `--` separator after the project path is required.

### 3. Present Results

After the CLI completes:

1. List the files in the output directory.
2. Tell the user the file paths and summarize what was exported.
3. If the report was generated (`content-model-report.md`), highlight it.

### Error Handling

- **"Connection string is missing or empty"** → go back to pre-flight.
- **SQL connection errors** → report to user (server unreachable or bad credentials).
- **Empty results** → valid; the database may have no data of that type.
