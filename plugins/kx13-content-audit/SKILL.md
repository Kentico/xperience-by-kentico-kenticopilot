---
name: "kx13-content-audit"
description: "Audits a Kentico Xperience 13 (KX13) project's content model based on the project's database and generates structured Markdown and JSON reports. Use when the user asks to audit, analyze, export, or inspect a KX13 database, content model, page types, content tree, forms, custom tables, or page builder usage."
argument-hint: "Optional export scope (sites, page types, report, etc.) and filters (site name, class name pattern, content tree path)"
compatibility: "Requires .NET 8 SDK and access to a Kentico Xperience 13 SQL Server database."
---

# KX13 Content Auditor — Agent Skill

The CLI tool handles the full workflow — querying the database, exporting JSON
data, and generating a Markdown report. Interpret the user's request, construct
the right CLI command, run it, and present the results.

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

## Gotchas

- **`--` separator is required** when invoking via `dotnet run`. Everything after `--` is passed to the CLI; without it, `dotnet run` consumes the flags itself and the auditor sees no arguments.
- **Filter semantics differ per flag.** `--site-name` is **exact match** (no wildcards). `--class-name` accepts `*` wildcards and comma-separated patterns. `--page-path` is a **prefix** match against the node alias path.
- **Default output directory is `audit-results/` under the auditor project root**, not the current working directory. Pass `--output <path>` if the user expects results elsewhere.
- **`--report` is implied by a full export.** When running with no area flags (full export), the Markdown report is generated automatically — do not add `--report` on top. Only pass `--report` when the user explicitly asks for the report alongside selective area flags.
- **TLS errors against KX13 dev databases.** Modern SQL clients require `Encrypt=False;` (or a trusted server cert) in the connection string for typical KX13 dev setups. If the CLI fails with a certificate/SSL error, this is usually the cause.
- **`appsettings.development.json` is intentionally lowercase.** It is loaded as an explicit overlay in `Program.cs` and is git-ignored. Do not rename it to `appsettings.Development.json`.
- **Empty results are valid.** A successful run with no rows for a given area means the database genuinely has no data of that type — not a failure.

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
