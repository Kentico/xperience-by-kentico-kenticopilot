---
name: migrate-run
description: Runs the Kentico Migration Tool CLI to migrate content from KX13 to XbyK. Use when the user wants to run migration steps, execute the migration tool, or process a migration plan through the CLI.
compatibility: Requires dotnet CLI, sqlcmd, and network access to KX13 and XbyK SQL Server databases.
argument-hint: "[migration-plan-path]"
---

# Migration Tool Execution

Executes the Kentico Migration Tool CLI commands according to a migration plan — monitoring output, diagnosing errors, and applying fixes. Requires a completed migration plan, configured appsettings.json, and compiled code extensions as prerequisites.

## Prerequisites

Before running this skill, the following must already be complete:

1. **Migration plan** — A migration plan file (from `migrate-plan` skill or manually authored)
2. **appsettings.json** — Configured with real connection strings, ConvertClassesToContentHub, EntityConfigurations, etc. (from `migrate-appsettings` skill)
3. **Code extensions** — All `IClassMapping`, `ContentItemDirectorBase`, `IFieldMigration`, `IWidgetMigration`, and `IWidgetPropertyMigration` implementations compiled (from the respective skills)
4. **Successful build** — The `Migration.Tool.Extensions` project (and the full solution) must build without errors

## Workflow

### Step 1: Read Reference Materials and Validate Setup

- Read [migration-tool-run-reference.md](references/migration-tool-run-reference.md) for CLI usage, parameter details, error patterns, and troubleshooting guidance.
- If you need context on the migration tool's extension points or CLI parameter dependencies, read [migration-tool.md](../_shared/references/migration-tool.md).
- If you need documentation links, read [migration-docs.md](../_shared/references/migration-docs.md).
- Read the migration plan file provided by the user to understand:
  - The **Execution Plan** section — which CLI parameters to run and in what order
  - Any **Pre-Migration** manual steps that must be completed first
  - Which code extensions and configuration settings are expected

### Step 2: Pre-Flight Checks

Before running any migration command, verify the environment is ready. Complete all checks in [pre-flight-checks.md](references/pre-flight-checks.md): locate the CLI project, validate appsettings.json (connection strings, semicolon-separated strings, QuerySourceInstanceApi reachability, MigrationProtocolPath), validate the build, check NuGet version compatibility, scan for TODO placeholders in code, and confirm pre-migration manual steps with the user. Do not proceed if any critical check fails.

### Step 3: Build the Migration Command

From the migration plan's **Execution Plan** section, build a **single CLI command** that includes all required parameter flags. The migration tool accepts multiple flags in one invocation and executes them in the correct dependency order internally — there is no need to run steps separately or manage ordering yourself.

The standard content migration flags are:

| Flag                | Purpose                                                                         |
| ------------------- | ------------------------------------------------------------------------------- |
| `--sites`           | Create website channels (always included)                                       |
| `--custom-modules`  | Migrate custom module classes (if applicable)                                   |
| `--users`           | Migrate users (if applicable)                                                   |
| `--page-types`      | Create content type definitions (runs IClassMapping)                            |
| `--pages`           | Migrate page content, Page Builder data, attachments (runs all code extensions) |
| `--categories`      | Migrate categories as taxonomies (if applicable)                                |
| `--media-libraries` | Migrate media library files (if applicable)                                     |
| `--forms`           | Migrate online forms (if applicable)                                            |

**Default to including all standard flags** unless the plan explicitly excludes them or the source definitively does not have that data type. Running a flag on empty data is harmless (the tool reports "No items to migrate"), but omitting a flag that has data causes silent data loss. If the migration plan does not mention a step, verify with the user whether it should be included rather than silently omitting it.

Example combined command:

```
dotnet run -- migrate --nowait --sites --custom-modules --users --page-types --pages --categories --media-libraries --forms 2>&1 | tee <protocol-dir>/migration-run.log
```

### Step 4: Execute the Migration

#### 4a. Announce the Run

Tell the user which flags are included in the command, what the migration will do, and what to expect (including approximate duration).

#### 4b. Run the Command

Execute the single combined migration command using `dotnet run` from the CLI project directory, capturing console output to a log file:

```
dotnet run -- migrate --nowait --sites --custom-modules --users --page-types --pages --categories --media-libraries --forms 2>&1 | tee <protocol-dir>/migration-run.log
```

- **Always save console logs** — pipe all output (stdout and stderr) to a log file in the same directory as `MigrationProtocolPath` from appsettings.json. Create the folder if it doesn't exist (`mkdir -p <dir>`). Name the log file `migration-run.log`. For re-runs, append a run number: `migration-run2.log`, `migration-run3.log`.
- Set a generous timeout (up to 10 minutes — `--pages` and `--media-libraries` process large volumes of data)
- The `tee` command ensures output is both displayed in real-time and saved to the log file

#### 4c. Monitor Output

Watch the output for success, failure, and warning indicators. Refer to [migration-tool-run-reference.md](references/migration-tool-run-reference.md) for the full output pattern reference. The tool processes each flag sequentially within the single run and reports progress for each.

#### 4d. Diagnose Errors

If the run fails at any point, analyze the error output and apply the appropriate fix from [migration-tool-run-reference.md](references/migration-tool-run-reference.md).

After applying a fix:

- Rebuild the project if code was changed (`dotnet build`)
- Re-run the **same combined command** with `--bypass-dependency-check` appended (steps that already completed will be skipped or updated idempotently):

```
dotnet run -- migrate --nowait --sites --custom-modules --users --page-types --pages --categories --media-libraries --forms --bypass-dependency-check 2>&1 | tee <protocol-dir>/migration-run2.log
```

If the fix doesn't resolve the issue after one re-run, check both the saved console log in the protocol log directory and the migration tool's own log file at `logs/migration.tool.log` for more detail. After two failed attempts, present findings to the user and ask for guidance rather than continuing to retry.

#### 4e. Record Result

After the run completes, record:

- The full command executed
- Overall result (success / failed at which step → fixed → re-run → success)
- Per-step output summary (items migrated, warnings, skipped items for each flag)

#### 4f. Post-Run Validation

After the migration run succeeds, run **all** validation queries from [post-step-validation.md](references/post-step-validation.md) against the XbyK database using `sqlcmd`. Cross-reference results against the migration plan and flag any discrepancies (missing types, orphan pages, missing taxonomies).

### Step 5: Post-Execution Summary

After all steps complete, present a summary table:

| Flag               | Result  | Notes                          |
| ------------------ | ------- | ------------------------------ |
| `--sites`          | Success | Site MedioClinic created       |
| `--custom-modules` | Success | Airports module migrated       |
| `--users`          | Success | 12 users migrated              |
| `--page-types`     | Success | 8 content types created        |
| `--pages`          | Success | 245 pages migrated, 3 warnings |
| ...                | ...     | ...                            |

Then list:

- **Command executed** — the full combined CLI command that was run
- **Fixes applied during execution** — any appsettings.json changes, code fixes, or workarounds applied
- **Validation results** — present the post-run validation findings: expected vs. actual content types (highlight missing and unexpected), exclusion verification, orphan page detection, taxonomy verification, module class verification
- **Warnings** — any non-fatal issues observed (skipped items, missing source files, etc.)
- **Post-migration steps** — reference the migration plan's Post-Migration section with actionable next steps
- **Re-run guidance** — if the user needs to re-run, remind them to use the same combined command with `--bypass-dependency-check` appended
- **Console logs** — remind the user that full console output is saved in the protocol log directory for future reference

## Rules

- **Single combined invocation** — always call the migration tool once with all required flags in a single command. The tool handles dependency ordering internally. Do not run flags as separate sequential invocations.
- **Fix forward, don't skip** — if the run fails, diagnose and fix the issue rather than removing a flag. The migration plan expects all listed steps to complete.
- **Do not modify the migration plan** — this skill executes the plan, not changes it. If the plan appears incorrect, flag it to the user.
- **Generous timeouts** — the combined migration run processes database records and binary files across all steps. Use generous timeouts (up to 10 minutes). `--pages` and `--media-libraries` are typically the slowest.
- **Always save console logs** — every execution must pipe console output to the parent directory of `Settings.MigrationProtocolPath`. This is non-negotiable — logs are essential for post-mortem analysis and `migrate-eval`.
- **Log file review** — if the saved console log in the protocol log directory is insufficient for diagnosis, check the migration tool's detailed log file (typically `logs/migration.tool.log` in the CLI project directory).
- **Validate after the run completes** — run all post-run validation queries (Step 4f) after the migration run succeeds to catch problems.
- **Default to including flags** — it is safer to include a flag that finds nothing to migrate than to omit a flag that has data. Only omit a flag when the user explicitly confirms the source does not have that data.
- **Flag orphan content items** — webpage-type content items without web page entries, and content items with no data rows, are strong indicators of migration failures. These should be investigated, not silently accepted.

## Gotchas

- **`ConvertClassesToContentHub` must be a semicolon-separated string**, not a JSON array. This is the most common configuration error — the tool throws `InvalidOperationException` at startup. Same applies to `CreateReusableFieldSchemaForClasses`.
- **The `migrate` verb is required before any parameter flags** — `dotnet run -- --sites` won't work, it must be `dotnet run -- migrate --sites`.
- **NuGet package versions must match the XbyK database version exactly** — a mismatch causes startup failures. Query `CMSDBVersion` from `CMS_SettingsKey` to verify.
- **Re-runs require `--bypass-dependency-check`** — on re-runs, append `--bypass-dependency-check` to the same combined command. Without it, the tool refuses to run steps whose dependencies it thinks haven't completed (even though they have from the prior run).
- **TODO placeholder strings in GUID positions cause `FormatException`** — replace with `Guid.Empty` (`00000000-0000-0000-0000-000000000000`) as safe defaults before running.
- **`QuerySourceInstanceApi` silently blocks `--pages`** if the KX13 instance is unavailable — disable it in appsettings to fall back to legacy widget migration.
- **"Press any key" prompt blocks the agent** — the migration tool waits for a keypress after each step. Always include the `--nowait` flag (e.g., `dotnet run -- migrate --nowait ...`) to suppress this prompt so the process exits without blocking.
