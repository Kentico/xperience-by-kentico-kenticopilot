# KX13 Content Auditor

Reads a Kentico Xperience 13 (KX13) database and exports the content model as structured JSON files plus a Markdown report. Useful for auditing site content, planning migrations, and analyzing page types, content trees, forms, custom tables, and page builder usage.

The output is then used as the input for the [`kx13-content-migration`](../kx13-content-migration/README.md) plugin — its `migrate-plan` skill consumes the audit JSON to produce a Migration Overview and Migration Detail document. For an end-to-end view of how the auditor fits into a full KX13 → XbyK upgrade, see [KX13 upgrade plugins](../../docs/KX13-Upgrade-Plugins.md).

The plugin has two parts:

- An AI skill (`content-audit`) that interprets a natural-language audit request, runs the CLI with the right flags, and presents the results.
- A .NET 8 CLI (under `src/`) that performs the actual database read and JSON export.

> [!IMPORTANT]
> The marketplace install delivers the skill only. The CLI source needs to be present in your workspace for the skill to run; the skill checks for it on first invocation and stops with instructions to clone the repository if the source is missing. See [Set up the auditor source](#set-up-the-auditor-source) below.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) or later
- SQL Server with a KX13 database (SQL Server 2019+ or LocalDB)
- AI coding assistant installed (for example: GitHub Copilot, Claude Code)

## Install the plugin

### VS Code (GitHub Copilot)

Add the marketplace to your VS Code settings (`settings.json`), then browse and install from the Extensions sidebar (`@agentPlugins`):

```json
"chat.plugins.marketplaces": [
    "Kentico/xperience-by-kentico-kenticopilot"
]
```

For more information, see: [VS Code plugin marketplace](https://code.visualstudio.com/docs/copilot/customization/agent-plugins#_configure-plugin-marketplaces)

### Copilot CLI

```bash
copilot plugin marketplace add Kentico/xperience-by-kentico-kenticopilot
copilot plugin install kx13-content-audit@xperience-by-kentico-kenticopilot
```

### Claude Code

```bash
/plugin marketplace add Kentico/xperience-by-kentico-kenticopilot
/plugin install kx13-content-audit@xperience-by-kentico-kenticopilot
```

## Set up the auditor source

The plugin install does not include the CLI source. Set it up once per workspace:

1. Clone this repository so the plugin folder is available locally.
2. Configure the connection string in the CLI project's `appsettings.json` (`src/KX13.ContentAuditor.CLI/appsettings.json` inside the plugin folder):

   ```json
   {
     "ConnectionStrings": {
       "ConnectionString": "Data Source=YOUR_SERVER;Initial Catalog=YOUR_KX13_DB;Integrated Security=True;Encrypt=False;"
     }
   }
   ```

   Alternatively, create an `appsettings.development.json` file in the same directory (this file is git-ignored). The CLI loads it automatically as a local override when present.

3. Build the solution from the plugin folder:

   ```bash
   dotnet build src/KX13.ContentAuditor.slnx
   ```

After this one-time setup, invoke the `kx13-content-audit` skill from your AI assistant and it runs the CLI for you. The CLI sections below document the underlying command surface for direct use or troubleshooting.

## Prompt reference

### kx13-content-audit

Prompt name: **kx13-content-audit**
Parameters:
  - *scope* (optional): Which areas to export. Defaults to a full export when omitted (e.g., "page types and forms", "page builder components", "everything").
  - *filters* (optional): Site, class-name, or content-tree scoping (e.g., "site DancingGoatMvc", "DancingGoat.* page types", "under /Articles").
  - *output-path* (optional): Destination directory for the JSON and report. Defaults to `audit-results/` under the auditor project.

Interprets the request, picks the matching [CLI flags](#available-flags), runs the auditor against the configured KX13 database, and presents the resulting JSON files (and the Markdown report on full exports). The output is the canonical input for the [`migrate-plan`](../kx13-content-migration/README.md#migrate-plan) prompt — the typical first step of a KX13 → Xperience by Kentico upgrade.

> [!NOTE]
> The skill stops with cloning instructions if the [auditor source](#set-up-the-auditor-source) is not present in the workspace.

**VS Code GitHub Copilot example:**

```
/kx13-content-audit

Audit the DancingGoatMvc site as the starting point for migrating it to
Xperience by Kentico. Export the full content model into ./audit-results/
```

## CLI usage

The `kx13-content-audit` skill is the primary entry point — it picks the right flags from a given prompt. The CLI is documented here for direct invocation (troubleshooting, scripting, CI). Run with `--help` for all commands. The flags below are what the skill maps the prompt onto.

```bash
dotnet run --project src/KX13.ContentAuditor.CLI -- [flags]
```

With no flags, the tool exports the full content model and the Markdown report. Combine area flags for a selective export, and add filter flags to scope the result.

| Flag | Description |
|---|---|
| `--sites` | Sites with cultures, content tree, page builder configs |
| `--page-types` | Page types with field definitions |
| `--page-builder-components` | Widgets, sections, and page templates in use |
| `--custom-modules` | Custom modules and their classes |
| `--custom-tables` | Custom tables with fields and alternative forms |
| `--forms` | BizForms with fields, validation, alternative forms |
| `--relationships` | Page-to-page relationships and Pages-field links |
| `--report` | Add the Markdown report alongside selective area flags (implied by a full export) |
| `--site-name <name>` | Filter by site code name (e.g. `DancingGoatMvc`) |
| `--class-name <pattern>` | Filter by class name (`*` wildcard, comma-separated) |
| `--page-path <prefix>` | Filter content tree by node alias path prefix |
| `--output <path>` | Output directory (default: `audit-results/`) |

## Output

JSON files are written to `audit-results/` under the auditor project root by default. This directory is git-ignored. Use `--output <path>` to override the location.

### Customizable storage in KX13 — where each one lands

KX13 has three primary places where developers store custom data, plus extensions to existing schemas. The auditor maps them to JSON files as follows:

| KX13 customization | Output file |
|---|---|
| Page types (content tree pages, including custom fields added to `CMS.MenuItem` and other system content types) | `page-types.json` |
| Custom tables (lightweight tabular custom storage) | `custom-tables.json` |
| Custom modules and their module classes (richer custom storage with relationships) | `custom-modules.json` |
| Bizforms (form submissions, with the form's class definition and the bizform-level configuration) | `forms.json` |

> [!NOTE]
> The auditor does not capture custom fields added to **system objects** — `cms.user`, `cms.member`, `cms.role`, and similar. Migrating these is the Kentico Migration Tool's responsibility: the [`--custom-modules`](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool/blob/master/Migration.Tool.CLI/README.md#migrate-command-parameters) CLI command migrates custom fields in supported system classes alongside custom modules and module classes. Only a subset of system classes have built-in custom-field migration coverage out of the box, so for the long tail you may need to extend the tool with your own logic. Custom fields on system **content types** like `CMS.MenuItem` are still captured in `page-types.json`.

## Project Structure

```
src/
├── KX13.ContentAuditor.CLI/            # Console entry point, argument parsing, JSON export
├── KX13.ContentAuditor.Application/    # Orchestration service (ContentModelService)
└── KX13.ContentAuditor.DataAccess/     # Database access layer
    ├── Models/                         # POCO model classes
    ├── Repositories/                   # SQL queries + result mapping
    │   └── Interfaces/                 # Repository contracts
    ├── Parsers/                        # XML/JSON parsers (ClassFormDefinition, PageBuilder)
    ├── Analysis/                       # Component discovery + content reference analysis
    └── DbAccess/                       # Raw ADO.NET query executor
```

## License

Distributed under the MIT License. See [`LICENSE.md`](../../LICENSE.md) for more information.
