# KX13 → Xperience by Kentico migration

AI-assistant skills, references, and a migrate-content-audit CLI for migrating Kentico Xperience 13 (KX13) projects to [Xperience by Kentico](https://docs.kentico.com/x/migrate_from_kx13_guides) (XbyK).

This plugin consolidates the full KX13 upgrade toolkit into three areas:

| Area | Skills | What it does |
|---|---|---|
| [Content-model audit](#content-model-audit) | `migrate-content-audit` | Reads a KX13 database and exports the content model as structured JSON + a Markdown report — the canonical input for migration planning. |
| [Content migration](#content-migration) | `migrate-content-plan`, `migrate-content-appsettings`, `migrate-content-classes`, `migrate-content-fields`, `migrate-content-widgets`, `migrate-content-items`, `migrate-content-run`, `migrate-content-eval` | Plans, configures, executes, and evaluates the database **content** migration via the [Kentico Migration Tool](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool). |
| [Codebase migration](#codebase-migration) | `migrate-code-global`, `migrate-code-component`, `migrate-code-page-widgets`, `migrate-code-page`, `migrate-code-page-visual` | Migrates the live-site **code** — controllers, views, repositories, shared components, and Page Builder rendering. |

For an end-to-end view of how the three areas fit together in a full upgrade, see `docs/KX13-Upgrade-Plugins.md` and Kentico's [official upgrade walkthrough](https://docs.kentico.com/x/upgrade_walkthrough_guides).

## Prerequisites

- Kentico Xperience 13 project (source) on Refresh 5 (hotfix 13.0.64) or newer, with database access. Follow the [Migration Tool source-instance setup](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool/blob/master/Migration.Tool.CLI/README.md#set-up-the-source-instance) for hotfix and contact-database requirements.
- Xperience by Kentico project (target) on a version compatible with the Migration Tool — see the [Library Version Matrix](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool/blob/master/README.md#library-version-matrix). Follow the [target-instance setup](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool/blob/master/Migration.Tool.CLI/README.md#set-up-the-target-instance) for the Boilerplate template requirement, the "must not be running during migration" rule, and the bulk-deletion list for re-runs.
- A local clone of the [Kentico Migration Tool](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool) repository in your workspace (required by the content-migration skills).
- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) or later (for the migrate-content-audit CLI and the Migration Tool), plus `sqlcmd` for post-migration validation queries.
- AI coding assistant installed (for example: GitHub Copilot, Claude Code).

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
copilot plugin install kentico-kx13-migration@xperience-by-kentico-kenticopilot
```

### Claude Code

```bash
/plugin marketplace add Kentico/xperience-by-kentico-kenticopilot
/plugin install kentico-kx13-migration@xperience-by-kentico-kenticopilot
```

## Configure MCP servers

This plugin requires some MCP servers to be set up in your workspace. See `MCP-setup.md` for the list and copy-paste-ready configuration.

---

## Content-model audit

Reads a KX13 database and exports the content model as structured JSON files plus a Markdown report. Useful for auditing site content, planning migrations, and analyzing page types, content trees, forms, custom tables, and Page Builder usage. The output is the canonical input for the [`migrate-content-plan`](#migrate-content-plan) skill below.

The audit area has two parts:

- An AI skill (`migrate-content-audit`) that interprets a natural-language audit request, runs the CLI with the right flags, and presents the results.
- A .NET 8 CLI (under `src/`) that performs the actual database read and JSON export.

> [!IMPORTANT]
> The marketplace install delivers the skill only. The CLI source needs to be present in your workspace for the skill to run; the skill checks for it on first invocation and stops with instructions to clone the repository if the source is missing.

### Set up the auditor source

The plugin install does not include the CLI source. Set it up once per workspace:

1. Clone this repository so the plugin folder is available locally. (We recommend ensuring the *kentico-kx13-migration* folder sits directly in the root of your workspace.)
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

After this one-time setup, invoke the `migrate-content-audit` skill from your AI assistant and it runs the CLI for you.

### migrate-content-audit

Prompt name: **migrate-content-audit**
Parameters:

- *scope* (optional): Which areas to export. Defaults to a full export when omitted (e.g., "page types and forms", "page builder components", "everything").
- *filters* (optional): Site, class-name, or content-tree scoping (e.g., "site DancingGoatMvc", "DancingGoat.* page types", "under /Articles").
- *output-path* (optional): Destination directory for the JSON and report. Defaults to `audit-results/` under the auditor project.

Interprets the request, picks the matching [CLI flags](#cli-usage), runs the auditor against the configured KX13 database, and presents the resulting JSON files (and the Markdown report on full exports). The output is the canonical input for the [`migrate-content-plan`](#migrate-content-plan) prompt — the typical first step of a KX13 → XbyK upgrade.

> [!NOTE]
> The skill stops with cloning instructions if the [auditor source](#set-up-the-auditor-source) is not present in the workspace.

**VS Code GitHub Copilot example:**

```text
/migrate-content-audit

Audit the DancingGoatMvc site as the starting point for migrating it to
Xperience by Kentico. Export the full content model into ./audit-results/
```

### CLI usage

The `migrate-content-audit` skill is the primary entry point — it picks the right flags from a given prompt. The CLI is documented here for direct invocation (troubleshooting, scripting, CI). Run with `--help` for all commands.

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

JSON files are written to `audit-results/` under the auditor project root by default. This directory is git-ignored. Use `--output <path>` to override the location.

#### Customizable storage in KX13 — where each one lands

KX13 has three primary places where developers store custom data, plus extensions to existing schemas. The auditor maps them to JSON files as follows:

| KX13 customization | Output file |
|---|---|
| Page types (content tree pages, including custom fields added to `CMS.MenuItem` and other system content types) | `page-types.json` |
| Custom tables (lightweight tabular custom storage) | `custom-tables.json` |
| Custom modules and their module classes (richer custom storage with relationships) | `custom-modules.json` |
| Bizforms (form submissions, with the form's class definition and the bizform-level configuration) | `forms.json` |

> [!NOTE]
> The auditor does not capture custom fields added to **system objects** — `cms.user`, `cms.member`, `cms.role`, and similar. Migrating these is the Kentico Migration Tool's responsibility: the [`--custom-modules`](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool/blob/master/Migration.Tool.CLI/README.md#migrate-command-parameters) CLI command migrates custom fields in supported system classes alongside custom modules and module classes. Only a subset of system classes have built-in custom-field migration coverage out of the box, so for the long tail you may need to extend the tool with your own logic. Custom fields on system **content types** like `CMS.MenuItem` are still captured in `page-types.json`.

#### Auditor project structure

```text
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

---

## Content migration

AI-assistant skills for migrating the **database content** of KX13 projects to XbyK via the [Kentico Migration Tool](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool). The skills plan the migration, configure and execute the Migration Tool, and evaluate the results.

### Scope

This area covers the data-migration side of an upgrade — everything the Migration Tool transfers from a KX13 database to an XbyK database, plus the per-project code extensions needed for non-trivial transformations:

- [Migrate data and binary files](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool/blob/master/Migration.Tool.CLI/README.md#migration-details-for-specific-object-types) — content types, pages, fields, taxonomies, attachments, media libraries, forms.
- [Custom class transformations](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool/blob/master/docs/customization/Class-Mappings.md) — merges, splits, renames, Content Hub conversions, reusable field schemas.
- [Custom tables](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool/blob/master/Migration.Tool.CLI/README.md#custom-tables) — migrated as custom module classes by default, or as reusable Content hub items via opt-in. See the `--custom-tables` parameter.
- [Field transformations](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool/blob/master/docs/customization/Field-Migrations.md) — custom form controls, data type changes, HTML sanitization, URL rewrites.
- [Page Builder widget and section transforms](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool/blob/master/docs/customization/Widget-Migrations.md) — type changes, property restructuring, page-to-widget conversion.
- [Linked-page handling](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool/blob/master/Migration.Tool.Extensions/README.md#customize-linked-page-handling) — materialize, drop, or store as content item references.
- Post-migration evaluation — automated comparison of the migrated database against the plan.

The following areas are not covered by the Migration Tool and must be handled separately, either manually or via the [codebase-migration](#codebase-migration) skills:

- Live-site code (controllers, views, repositories, page-builder rendering) — see [Codebase migration](#codebase-migration).
- Custom modules' UI elements, alternative forms, and ACLs.
- The live-site authentication and member registration code path. The migration tool transfers basic member records via `--members`, but external sign-in information (Facebook, Google, etc.) does not migrate, and the live-site auth code must be rewritten against the new `Member` object type and ASP.NET Identity APIs.
- Search, marketing automation, contact groups, personas, A/B testing, integration bus, license keys.

For a full capability comparison, see Kentico's [Plan your strategy for migrating features](https://docs.kentico.com/x/plan_your_strategy_for_migrating_features_guides). For a procedural walkthrough of the data-migration step in the upgrade flow, see Kentico's [Migrate data and binary files](https://docs.kentico.com/x/migrate_data_and_binary_files_guides) guide.

### Set up your workspace

Place the KX13 source, XbyK target, Migration Tool, and the migrate-content-audit output in a single workspace:

```text
<workspace-root>/
├── KX13/                            # KX13 source project
├── XbyK/                            # XbyK target project
├── audit-results/                   # Optional: migrate-content-audit JSON + report
├── kentico-migration-tool/
│   ├── Migration.Tool.CLI/          # appsettings.json is generated here
│   └── Migration.Tool.Extensions/   # Generated C# extensions are placed here
└── MigrationProtocol/               # Created by migrate-content-run; consumed by migrate-content-eval
```

> [!TIP]
> While many agents can adapt to different folder names and structures, following the diagram above will align your workspace with the skills and reduce the risk of issues.

Ensure the KX13 database is reachable from the machine running the Migration Tool, and that the XbyK database is initialized but otherwise empty (or carrying prior migration data to upsert).

### Run the content-migration skills

The skills group into four phases — Plan, Configure, Generate code extensions, and Execute and evaluate. The flow is iterative: refine the plan, regenerate `appsettings.json` and code extensions, and re-run as issues surface during execution and evaluation.

The "VS Code GitHub Copilot example" blocks below read as one continuous narrative — a single operator working through a DancingGoatMvc-style upgrade — so each prompt makes sense as the next step after the previous one.

#### Plan

##### migrate-content-plan

Prompt name: **migrate-content-plan**  
Parameters:

- *source-content-model-path*: Path to the source content model (typically the directory of `migrate-content-audit` JSON output, or a markdown description of the source).
- *target-content-model-path* (optional): Path to a target XbyK content model description. When provided, the plan compares source vs. target and surfaces structural divergences.

Produces `migration-overview.md` and `migration-detail.md` covering content types, field mappings, widget transformations, page relationships, exclusions, taxonomy planning, manual steps, and the execution plan. Uses the official Kentico Docs MCP server (when configured) to verify XbyK capabilities.

**VS Code GitHub Copilot example:**

```text
/migrate-content-plan

I just finished migrate-content-audit on my DancingGoatMvc database.
Produce the migration plan from the JSON output in ./audit-results/.
```

#### Configure

##### migrate-content-appsettings

Prompt name: **migrate-content-appsettings**  
Parameters:

- *migration-plan-path*: Path to the `migration-detail.md` produced by *migrate-content-plan*.

Generates the Migration Tool's `appsettings.json` (connection strings, `ConvertClassesToContentHub`, `EntityConfigurations`, `OptInFeatures.QuerySourceInstanceApi`, `OptInFeatures.CustomMigration.FieldMigrations`, `AssetRootFolders`, `MigrationProtocolPath`) and a markdown summary that traces every setting back to a plan section. When KX13 and XbyK projects are present in the workspace, infrastructure values (connection strings, source instance URI) are discovered automatically; otherwise, placeholders are emitted.

If the plan calls for [Source instance API discovery](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool/blob/master/Migration.Tool.CLI/README.md#source-instance-api-discovery), the skill also copies the `ToolApiController` into the KX13 project and registers its route.

**VS Code GitHub Copilot example:**

```text
/migrate-content-appsettings

The plan in ./migration-detail.md is ready. Generate the migration
tool's appsettings.json from it.
```

#### Generate code extensions

Run all four codegen skills — each skill inspects the plan and automatically skips if it's not needed. Afterwards, check that the migration tool project compiles with all the added extensions before running `migrate-content-run`.

##### migrate-content-classes

Prompt name: **migrate-content-classes**  
Parameters:

- *migration-plan-path*: Path to the `migration-detail.md`.

Generates `IClassMapping` and (optional) `ReusableSchemaBuilder` C# code in the `Migration.Tool.Extensions` project, plus the corresponding DI registration. Handles class merges, splits, field renames, value conversions (`ConvertFrom`), data-type/form-control patches (`WithFieldPatch`), and Content Hub conversions. After generation, the skill builds the project and reports any unresolved TODOs (typically taxonomy tag GUIDs that have to be resolved post-creation).

**VS Code GitHub Copilot example:**

```text
/migrate-content-classes

Generate the IClassMapping and ReusableSchemaBuilder C# extensions
for the page types and reusable field schemas described in
./migration-detail.md.
```

##### migrate-content-fields

Prompt name: **migrate-content-fields**  
Parameters:

- *migration-plan-path*: Path to the `migration-detail.md`.

Generates `IFieldMigration` C# code for cross-class field transforms — custom form controls without an XbyK equivalent, data-type conversions that span multiple classes, HTML sanitization, and URL/path rewrites. Use this when a transform applies globally across classes; for class-scoped definition changes, *migrate-content-classes* with `WithFieldPatch` is usually sufficient.

**VS Code GitHub Copilot example:**

```text
/migrate-content-fields

Generate the IFieldMigration extensions for the cross-class field
transforms in ./migration-detail.md (HTML sanitization, URL rewrites,
and the legacy form-control conversions the plan flags).
```

##### migrate-content-widgets

Prompt name: **migrate-content-widgets**  
Parameters:

- *migration-plan-path*: Path to `migration-detail.md`.

Generates `IWidgetMigration` and `IWidgetPropertyMigration` C# code for custom widget and section transforms — type renames, property restructuring, consolidation, property-value conversions.

**VS Code GitHub Copilot example:**

```text
/migrate-content-widgets

Generate the IWidgetMigration and IWidgetPropertyMigration extensions
for the custom widgets that ./migration-detail.md flags for transforms.
```

##### migrate-content-items

Prompt name: **migrate-content-items**  
Parameters:

- *migration-plan-path*: Path to the `migration-detail.md`.

Generates `ContentItemDirectorBase` C# code that controls per-item migration behavior during the `--pages` step: linked-page strategies (`Materialize`, `Drop`, `StoreReferenceInAncestor`), child-as-reference linking (`LinkChildren`), page-to-widget conversion, and conditional template overrides. Filters operate on numeric `NodeClassID`, so the migration plan must include `ClassID` values for the involved page types.

**VS Code GitHub Copilot example:**

```text
/migrate-content-items

Generate the ContentItemDirectorBase extensions for the linked-page
strategies, child-as-reference linking, and page-to-widget conversions
in ./migration-detail.md.
```

#### Execute and evaluate

Treat `migrate-content-run` and `migrate-content-eval` as a loop. Almost every non-trivial migration takes more than one iteration — fix issues raised by the eval, regenerate the relevant extension, re-run.

##### migrate-content-run

Prompt name: **migrate-content-run**  
Parameters:

- *migration-plan-path*: Path to the `migration-detail.md`.

Executes a **single combined `migrate` CLI invocation** with all required flags from the plan's Execution Plan section (`--sites`, `--custom-modules`, `--users`, `--page-types`, `--pages`, `--categories`, `--media-libraries`, `--forms`, etc.) — the migration tool orders the flags internally based on their dependency tree, so this skill never runs flags as separate sequential commands. The skill monitors stdout/stderr, applies pre-flight checks, validates each step with SQL queries, and writes structured logs to `MigrationProtocolPath`. For the full set of CLI parameters and their dependencies, see the official [Migrate Command Parameters](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool/blob/master/Migration.Tool.CLI/README.md#migrate-command-parameters).

> [!IMPORTANT]
> Build the `Migration.Tool.Extensions` project successfully before running this skill. The skill reports build failures rather than running with stale binaries. If `QuerySourceInstanceApi` is enabled, ensure the KX13 instance is running and the `ToolApiController` is reachable.

**VS Code GitHub Copilot example:**

```text
/migrate-content-run

Migration.Tool.Extensions builds clean and the KX13 source app is
running. Execute the migration end-to-end against the configured
target database following ./migration-detail.md.
```

##### migrate-content-eval

Prompt name: **migrate-content-eval**  
Parameters:

- *migration-plan-detail-path*: Path to the `migration-detail.md`.
- *appsettings-path* (optional): Path to a non-default `appsettings.json` if the migration was run with a different configuration.

Reads the protocol and console logs from `migrate-content-run`, queries both the KX13 and XbyK databases, and compares the result against the plan across 12 categories (configuration overview, content types, reusable field schemas, taxonomies, content item counts and orphans, field verification, page issues, users, media, forms, custom modules, overall health). Emits a self-contained HTML report with per-category pass/fail/warn status and routing back to the appropriate skill (`migrate-content-appsettings`, code-gen skills, or manual fix-up) for each finding.

**VS Code GitHub Copilot example:**

```text
/migrate-content-eval

migrate-content-run finished. Compare the migrated XbyK database against
./migration-detail.md and produce the HTML report so I know what to
fix and which sibling skill to re-run for each finding.
```

### Content-migration best practices

- Run an audit first. The [migrate-content-audit](#content-model-audit) CLI gives the planning skill the structured input it needs.
- Work iteratively. Treat the configure → codegen → run → eval sequence as one loop. Most issues identified by `migrate-content-eval` require a re-run of an earlier phase. The skill output directly instructs you about which skills to rerun.
- Several skills emit `TODO` placeholders. Resolve them in the plan before re-running, or post-migration in the generated code. The agents prompt you for that during the workflow.
- Review every generated extension before running the migration.
- Keep `MigrationProtocolPath` stable. `migrate-content-eval` reads protocol and console logs from the directory that `migrate-content-appsettings` writes into the config. Don't move the directory between runs unless you also update `appsettings.json`.

---

## Codebase migration

AI-assistant skills for migrating the **codebase** of KX13 projects to XbyK — the live site and page presentation logic, as described in these guides:

- [Adjust global code](https://docs.kentico.com/x/adjust_global_code_guides) – Generating code files for content types, copying localization resources, shared views, styles/scripts, and enabling content tree-based routing and Page Builder.
- [Display an upgraded page](https://docs.kentico.com/x/display_an_upgraded_page_guides) – Content retrieval services, repositories, view models, views, controllers, and Page Builder sections/widgets.

The following areas are not covered and must be handled manually: custom modules, custom tables, authentication and user management, search functionality, e-commerce, and marketing features. See the [Adjust your code and adapt your project](https://docs.kentico.com/x/migrate_your_code_guides) migration guide for details.

### Set up your workspace

Place your KX13 and XbyK projects in the same workspace:

```text
KX13/          # Kentico Xperience 13 project files
XbyK/          # Xperience by Kentico project files
```

Start the KX13 project locally (or provide a URL to a live KX13 site). **Do not start the XbyK project** – the agent builds and starts it on-demand during the migration to evaluate progress. The XbyK target should be connected to a database already migrated with the [content-migration](#content-migration) skills.

### Run the codebase-migration skills

The skills are divided into three groups, run in waves:

1. **Global** — [migrate-code-global](#migrate-code-global) seeds the target project with initial logic.
2. **Page** — for each page: [migrate-code-page-widgets](#migrate-code-page-widgets) (skip if the page has no Page Builder), then [migrate-code-page](#migrate-code-page), then [migrate-code-page-visual](#migrate-code-page-visual) if visual discrepancies remain.
3. **Component** — [migrate-code-component](#migrate-code-component) ensures consistent visuals across pages; use the URL of a migrated page to verify accuracy.

#### migrate-code-global

Prompt name: **migrate-code-global**

Migrates global code, generates code files, and sets up the project foundation:

- Creates a new .NET project in the target folder and marks it as [discoverable](https://docs.kentico.com/x/QoXWCQ) by Xperience.
- Uses the code generator utility to [generate classes](https://docs.kentico.com/x/5IbWCQ) for migrated database entities (content types, etc.).
- Copies global project files (assets, resources) and global code (service registration, startup logic) to the target.
- Enables [content tree-based routing](https://docs.kentico.com/x/GoXWCQ) and [Page Builder](https://docs.kentico.com/x/6QWiCQ) on the target.

**VS Code GitHub Copilot example:**

```text
/migrate-code-global
```

#### migrate-code-component

Prompt name: **migrate-code-component**  
Parameters:

- *componentName*: The name of the shared element to migrate. For example: header, footer, navigation menu, sidebar.
- *legacyPageUrl*: The URL of the page in the source project.

Migrates reusable components like headers, footers, and navigation elements. The prompt locates the specified element in the source project and migrates it together with all dependencies (views, layouts, logic, etc.).

> **Note:** Ensure the KX13 application is running and accessible at the provided URL before running this prompt. Stop the XbyK project if running.

**VS Code GitHub Copilot example:**

```text
/migrate-code-component

componentName: breadcrumbs
legacyPageUrl: https://localhost:5001/en-us/home
```

#### migrate-code-page-widgets

Prompt name: **migrate-code-page-widgets**  
Parameters:

- *pageName*: The name in the content tree of the source project.
- *legacyPageUrl*: The URL of the page in the source project.

Migrates Page Builder [widgets](https://docs.kentico.com/x/7gWiCQ) and [sections](https://docs.kentico.com/x/9AWiCQ) used by the specified page. Can be omitted if the page doesn't use Page Builder features.

> **Note:** Ensure the KX13 application is running and accessible at the provided URL before running this prompt. Stop the XbyK project if running.

**VS Code GitHub Copilot example:**

```text
/migrate-code-page-widgets

pageName: home
legacyPageUrl: https://localhost:5001/en-us/home
```

#### migrate-code-page

Prompt name: **migrate-code-page**  
Parameters:

- *pageName*: The name in the content tree of the source project.
- *legacyPageUrl*: The URL of the page in the source project.

Migrates the code of individual pages: controllers, views, layouts, and dependencies.

> **Note:** Ensure the KX13 application is running and accessible at the provided URL before running this prompt. Stop the XbyK project if running.

**VS Code GitHub Copilot example:**

```text
/migrate-code-page

pageName: home
legacyPageUrl: https://localhost:5001/en-us/home
```

#### migrate-code-page-visual

Prompt name: **migrate-code-page-visual**  
Parameters:

- *pageName*: The name in the content tree of the source project.
- *legacyPageUrl*: The URL of the page in the source project.
- *newPageUrl*: The URL of the page in the target project.

Ensures the migrated page visually matches the original KX13 page. Use if *migrate-code-page* doesn't successfully replicate the look and feel. The prompt uses Playwright to identify differences in both pages and aligns the migrated page to match the source.

> **Note:** Ensure the KX13 application is running and accessible at the provided URL before running this prompt. Stop the XbyK project if running.

**VS Code GitHub Copilot example:**

```text
/migrate-code-page-visual

pageName: home
legacyPageUrl: https://localhost:5001/en-us/home
newPageUrl: http://localhost:60444/en-us/home
```

### Codebase-migration best practices

- Run prompts in sequence. Each builds on the previous step. The full sequence to migrate a page is *migrate-code-page-widgets* → *migrate-code-page* → *migrate-code-page-visual*, repeating as necessary. Omit prompts that don't apply (e.g., skip *migrate-code-page-widgets* for pages without Page Builder).
- Only run the KX13 application before starting. The agent manages the XbyK application lifecycle (building, starting, stopping) automatically.
- Monitor the XbyK project state between prompts. Some prompts (e.g., *migrate-code-page-widgets*) may leave the XbyK application running; stop it manually before a prompt that expects it stopped.
- After running a prompt, review all generated code before proceeding.
- Use the visual matching prompt to fix styling discrepancies, and thoroughly test all migrated functionality.

---

## Skill customization

These skill files serve as a baseline for migrating KX13 projects to Xperience by Kentico. Modify and enhance the files as required by your implementation, workflow, and requirements. The reference materials under `skills/_shared/references/` and each skill's `references/` directory are the most useful starting points for adapting the prompts to project-specific conventions or constraints.

## License

Distributed under the MIT License. See `LICENSE.md` for more information.
