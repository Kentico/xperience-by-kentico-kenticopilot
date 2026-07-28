---
name: "migrate-content-audit"
description: "Audits a Kentico Xperience 13 (KX13) project's content model based on the project's database and generates structured Markdown and JSON reports. Use when the user asks to audit, analyze, export, or inspect a KX13 database, content model, page types, content tree, forms, custom tables, or page builder usage."
argument-hint: "[export-scope?] [filters?]"
compatibility: "Requires .NET 8 SDK and access to a Kentico Xperience 13 SQL Server database."
---

# KX13 Content Auditor — Agent Skill

The CLI tool handles the full workflow — querying the database, exporting JSON data, and generating a Markdown report. Interpret the user's request, construct the right CLI command, run it, and present the results.

For full technical details (setup, flags, project structure), see the [`kentico-kx13-migration` plugin README](../../README.md).

---

## User Intent Parsing

Parse the user's natural-language input to determine which export areas and filters to use.

### Export Scope

If the user asks for everything, or gives no specific scope, run a **full export** (no area flags). Otherwise, combine the relevant flags.

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
| "DancingGoat._page types" / "class DancingGoat._"      | `--class-name "DancingGoat.*"` |
| "under /Articles" / "the articles section"              | `--page-path /Articles`        |
| "output to ./my-folder"                                 | `--output ./my-folder`         |

---

## Gotchas

- **`--` separator is required** when invoking via `dotnet run`. Everything after `--` is passed to the CLI; without it, `dotnet run` consumes the flags itself and the auditor sees no arguments.
- **Filter semantics differ per flag.** `--site-name` is **exact match** (no wildcards). `--class-name` accepts `*` wildcards and comma-separated patterns. `--page-path` is a **prefix** match against the node alias path.
- **Default output directory is `audit-results/` under the auditor project root**, not the current working directory. When the auditor runs from the installed plugin, that root is outside the workspace, so always pass `--output <path>`.
- **`--report` is implied by a full export.** When running with no area flags (full export), the Markdown report is generated automatically — do not add `--report` on top. Only pass `--report` when the user explicitly asks for the report alongside selective area flags.
- **TLS errors against KX13 dev databases.** Modern SQL clients require `Encrypt=False;` (or a trusted server cert) in the connection string for typical KX13 dev setups. If the CLI fails with a certificate/SSL error, this is usually the cause.
- **`appsettings.development.json` is intentionally lowercase.** It is loaded as an explicit overlay in `Program.cs` and is git-ignored. Do not rename it to `appsettings.Development.json`. It applies only when the auditor runs from a source copy the user owns.
- **Configuration is read from the build output, not the project directory.** `Program.cs` sets the base path to `AppContext.BaseDirectory`, and the csproj copies `appsettings*.json` on build. Editing a JSON file therefore takes effect only after a rebuild. The environment variable needs no rebuild.
- **Empty results are valid.** A successful run with no rows for a given area means the database genuinely has no data of that type — not a failure.

---

## Workflow

### 1. Locate the auditor

The auditor ships with this plugin. Resolve `<CLI>` to the first path that exists:

1. `<plugin-root>/src/KX13.ContentAuditor.CLI`, where `<plugin-root>` is the directory this plugin is installed in. Read and explore the conventions of your harness.
2. Otherwise search the workspace for `**/KX13.ContentAuditor.slnx` and use the `KX13.ContentAuditor.CLI` directory beside it. This covers a manual installation, a repository clone, and any assistant that does not expose its plugin directory.

If neither resolves, stop and tell the user the auditor source was not found, naming both locations you checked.

Use the resolved absolute path in every command below. Do **not** assume a workspace-relative path: the installed plugin lives outside the workspace.

### 2. Pre-flight Checks

1. Verify the connection string, in this order:
   - the `ConnectionStrings__ConnectionString` environment variable;
   - `ConnectionStrings.ConnectionString` in `<CLI>/appsettings.development.json` or `<CLI>/appsettings.json`.

   If neither is set, ask the user to export the environment variable. Prefer it over the JSON files whenever `<CLI>` is inside the installed plugin, because that directory is replaced on every plugin update. Never write a connection string into the installed plugin, and never pass one on the command line, where it lands in shell history.

2. Build: `dotnet build <CLI> -c Release -q` If the build fails, report errors and stop.

### 3. Run the CLI

```sh
dotnet run --project <CLI> -- [area-flags] [filter-flags] --output <path>
```

The `--` separator after the project path is required.

Always pass `--output` with a path inside the user's workspace, defaulting to `./audit-results/`. The CLI's own default writes beside the project, which for an installed plugin means the results land outside the workspace and disappear on the next plugin update.

### 4. Present Results

After the CLI completes:

1. List the files in the output directory.
2. Tell the user the file paths and summarize what was exported.
3. If the report was generated (`content-model-report.md`), highlight it.

### Error Handling

- **"Connection string is missing or empty"** → go back to pre-flight.
- **SQL connection errors** → report to user (server unreachable or bad credentials).
- **Empty results** → valid; the database may have no data of that type.
