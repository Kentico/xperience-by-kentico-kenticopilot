# KX13 content auditor

The KX13 content auditor reads a Kentico Xperience 13 (KX13) database and exports its content model as structured JSON plus a Markdown report. The `migrate-content-audit` skill runs the auditor for an AI-assisted migration. You can also invoke the .NET CLI directly for troubleshooting, scripting, or CI pipelines.

The output is the canonical input for `migrate-content-plan`.

## What is included

- `migrate-content-audit` – interprets the requested scope and filters, invokes the CLI, and summarizes the result.
- `src/KX13.ContentAuditor.CLI` – parses command-line options and hosts the application.
- `src/KX13.ContentAuditor.Application` – orchestrates exports and reporting.
- `src/KX13.ContentAuditor.DataAccess` – reads and analyzes the KX13 database.
- `src/KX13.ContentAuditor.Tests` – covers CLI option parsing, content-model orchestration, schema parsing, content-reference analysis, and Page Builder component discovery.

## Requirements

- Kentico Xperience 13 Refresh 5 (hotfix 13.0.64) or newer
- Access to the KX13 database
- .NET 8 SDK or newer
- A writable copy of the auditor source in your migration workspace, placed by the setup below

> [!IMPORTANT]
> Installing the plugin copies the auditor source along with the skill, but it lands in your assistant's plugin directory rather than in your workspace. That directory is replaced whenever the plugin updates, so a connection string or a build output written there disappears at the next update. Copy the source into the workspace once, before the first run.

## Set up the auditor

1. Copy the auditor source into your workspace at `kentico-kx13-migration/src/`. The skill builds and runs from that path.

   Your assistant knows its own plugin directory as `${CLAUDE_PLUGIN_ROOT}`, so it can make the copy for you:

   ```text
   Copy the KX13 content auditor source from ${CLAUDE_PLUGIN_ROOT}/src
   to kentico-kx13-migration/src/ in this workspace.
   ```

   With a [manual installation](../../../docs/Usage-Guide.md#manual-installation), copy `plugins/kentico-kx13-migration/src/` out of your clone to the same place.

   The copy is a working tool rather than part of the migrated project, so keep it out of the project's version control.

2. Configure the connection string in `kentico-kx13-migration/src/KX13.ContentAuditor.CLI/appsettings.json`:

   ```json
   {
     "ConnectionStrings": {
       "ConnectionString": "Data Source=YOUR_SERVER;Initial Catalog=YOUR_KX13_DB;Integrated Security=True;Encrypt=False;"
     }
   }
   ```

   To keep local settings out of version control, create `appsettings.development.json` in the same directory instead. The CLI loads it as an override.

3. Build the solution from `plugins/kentico-kx13-migration/`:

   ```bash
   dotnet build src/KX13.ContentAuditor.slnx
   ```

## Run through the skill

Invoke `migrate-content-audit` with an optional scope, filter, and output location:

```text
/migrate-content-audit

Audit the DancingGoatMvc site as the starting point for migration.
Export the full content model to ./audit-results/.
```

Examples of narrower requests:

```text
Audit page types and forms for classes matching DancingGoat.*.
```

```text
Export the content tree below /Articles for the DancingGoatMvc site.
```

With no scope, the skill requests a full export and Markdown report.

## Run the CLI directly

From `plugins/kentico-kx13-migration/`:

```bash
dotnet run --project src/KX13.ContentAuditor.CLI -- [flags]
```

With no flags, the CLI exports the full content model and report. Combine area flags for a selective export and add filter flags to narrow the result.

| Flag | Description |
|---|---|
| `--sites` | Export sites, cultures, content trees, and Page Builder configurations |
| `--page-types` | Export page types and field definitions |
| `--page-builder-components` | Export widgets, sections, and page templates in use |
| `--custom-modules` | Export custom modules and their classes |
| `--custom-tables` | Export custom tables, fields, and alternative forms |
| `--forms` | Export BizForms, fields, validation, and alternative forms |
| `--relationships` | Export page relationships and Pages-field links |
| `--report` | Add the Markdown report to a selective export. Implied for a full export |
| `--site-name <name>` | Filter by site code name |
| `--class-name <pattern>` | Filter by class name. Supports `*` and comma-separated patterns |
| `--page-path <prefix>` | Filter the content tree by node alias path prefix |
| `--output <path>` | Set the output directory. Defaults to `audit-results/` under the auditor project root, or under the current working directory when the project root cannot be resolved |

Run the CLI with `--help` for its current command reference.

## Output

The auditor writes only the files relevant to the selected scope:

| Source area | Output |
|---|---|
| Sites and content trees | `sites.json` |
| Page types and system content-type extensions | `page-types.json` |
| Page Builder usage | `page-builder-components.json` |
| Custom modules and module classes | `custom-modules.json` |
| Custom tables | `custom-tables.json` |
| BizForms | `forms.json` |
| Page relationships | `relationships.json` |
| Content reference analysis in a full export | `content-reference-graph.json` |
| Non-fatal audit errors, when present | `failures.json` |
| Human-readable summary | `content-model-report.md` |

The selected area flags control the exact JSON file set. Results default to `audit-results/` under the auditor project root, falling back to the current working directory when the project root cannot be resolved. The auditor project ignores its default results directory.

## Scope and limitations

The auditor captures the content model and references needed for migration planning. It does not replace the Kentico Migration Tool and does not migrate data.

Custom fields added to system objects such as `cms.user`, `cms.member`, or `cms.role` are outside the auditor's model export. The Migration Tool handles supported system-class fields through its custom-module migration. Additional fields may require custom migration logic.

Categories, commerce data, marketing entities, custom-module UI, and access-control behavior require separate review in the [KX13 upgrade workflow](../README.md#upgrade-workflow).
