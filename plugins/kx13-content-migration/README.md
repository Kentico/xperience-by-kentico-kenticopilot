# Content migration support KX13 → XbyK

AI-assistant skills for migrating the **database content** of Kentico Xperience 13 projects to [Xperience by Kentico](https://docs.kentico.com/x/migrate_from_kx13_guides) via the [Kentico Migration Tool](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool). The plugin plans the migration, configures and executes the Migration Tool, and evaluates the results.

The plugin is intended to be used together with [`kx13-codebase-migration`](../kx13-codebase-migration/README.md) and [`kx13-content-audit`](../kx13-content-audit/README.md). See [KX13 upgrade plugins](../../docs/KX13-Upgrade-Plugins.md) for the full intended workflow.

## Scope

This plugin covers the data-migration side of an upgrade — everything the Migration Tool transfers from a KX13 database to an XbyK database, plus the per-project code extensions needed for non-trivial transformations:

- [Migrate data and binary files](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool/blob/master/Migration.Tool.CLI/README.md#migration-details-for-specific-object-types) — content types, pages, fields, taxonomies, attachments, media libraries, forms.
- [Custom class transformations](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool/blob/master/Migration.Tool.Extensions/README.md#custom-class-mappings-iclassmapping) — merges, splits, renames, Content Hub conversions, reusable field schemas.
- [Custom tables](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool/blob/master/Migration.Tool.CLI/README.md#custom-tables) — migrated as custom module classes by default, or as reusable Content hub items via opt-in. See the `--custom-tables` parameter.
- [Field transformations](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool/blob/master/Migration.Tool.Extensions/README.md#field-migrations-ifieldmigration) — custom form controls, data type changes, HTML sanitization, URL rewrites.
- [Page Builder widget and section transforms](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool/blob/master/Migration.Tool.Extensions/README.md#widget-migrations-iwidgetmigration) — type changes, property restructuring, page-to-widget conversion.
- [Linked-page handling](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool/blob/master/Migration.Tool.Extensions/README.md#customize-linked-page-handling) — materialize, drop, or store as content item references.
- Post-migration evaluation — automated comparison of the migrated database against the plan.

The following areas are not covered by this plugin and the underlying tool, and must be handled separately either via manual migration or other plugins:

- Live-site code (controllers, views, repositories, page-builder rendering) — see [`kx13-codebase-migration`](../kx13-codebase-migration/README.md).
- Custom modules' UI elements, alternative forms, and ACLs.
- The live-site authentication and member registration code path. The migration tool transfers basic member records via `--members`, but external sign-in information (Facebook, Google, etc.) does not migrate, and the live-site auth code must be rewritten against the new `Member` object type and ASP.NET Identity APIs.
- Search, marketing automation, contact groups, personas, A/B testing, integration bus, license keys.

For a full capability comparison, see Kentico's [Plan your strategy for migrating features](https://docs.kentico.com/x/plan_your_strategy_for_migrating_features_guides). For a procedural walkthrough of the data-migration step in the upgrade flow, see Kentico's [Migrate data and binary files](https://docs.kentico.com/x/migrate_data_and_binary_files_guides) guide.

## Prerequisites

- Kentico Xperience 13 project (source) on Refresh 5 (hotfix 13.0.64) or newer, with database access. Follow the [Migration Tool source-instance setup](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool/blob/master/Migration.Tool.CLI/README.md#set-up-the-source-instance) for hotfix and contact-database requirements.
- Xperience by Kentico project (target) on a version compatible with the Migration Tool — see the [Library Version Matrix](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool/blob/master/README.md#library-version-matrix). Follow the [target-instance setup](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool/blob/master/Migration.Tool.CLI/README.md#set-up-the-target-instance) for the Boilerplate template requirement, the "must not be running during migration" rule, and the bulk-deletion list for re-runs.
- A local clone of the [Kentico Migration Tool](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool) repository in your workspace. The code-generation skills write C# extensions directly into its `Migration.Tool.Extensions` project, and `migrate-run` builds and executes its `Migration.Tool.CLI` project. See the [Extensions README](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool/blob/master/Migration.Tool.Extensions/README.md) for the extension project's structure and registration patterns.
- .NET SDK matching the Migration Tool's target framework, plus `sqlcmd` for post-migration validation queries.
- AI coding assistant installed (for example: GitHub Copilot, Claude Code).

> [!NOTE]
> A [Kentico Xperience 13 library on Context7](https://context7.com/websites/kentico_13) is wired into this plugin's `.mcp.json` for KX13 API lookups. Context7 is a third-party service not maintained or supported by Kentico, so your experience may vary.

> [!TIP]
> The companion [`kx13-content-audit`](../kx13-content-audit/README.md) CLI exports a structured snapshot of your KX13 content model as JSON. The plugin output artifact is the expected input for the `migrate-plan` skill below.

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
copilot plugin install kx13-content-migration@xperience-by-kentico-kenticopilot
```

### Claude Code

```bash
/plugin marketplace add Kentico/xperience-by-kentico-kenticopilot
/plugin install kx13-content-migration@xperience-by-kentico-kenticopilot
```

## Usage

### Set up your workspace

Place the KX13 source, XbyK target, Migration Tool, and the content-audit output in a single workspace:

```
<workspace-root>/
├── KX13/                            # KX13 source project 
├── XbyK/                            # XbyK target project 
├── audit-results/                   # Optional: kx13-content-audit JSON + report
├── kentico-migration-tool/
│   ├── Migration.Tool.CLI/          # appsettings.json is generated here
│   └── Migration.Tool.Extensions/   # Generated C# extensions are placed here
└── MigrationProtocol/               # Created by migrate-run; consumed by migrate-eval
```

Ensure the KX13 database is reachable from the machine running the Migration Tool, and that the XbyK database is initialized but otherwise empty (or carrying prior migration data to upsert).

### Configure MCP servers

If you installed the plugin via the marketplace, the bundled `.mcp.json` adds two MCP servers to your workspace:

- [Kentico Docs MCP server](https://docs.kentico.com/x/mcp_server_xp) — used to verify XbyK content model capabilities (content types, reusable field schemas, Content hub, Page Builder, taxonomies).
- [Context7 MCP server](https://context7.com/websites/kentico_13) — used by the planning and code-generation skills to look up KX13 source API references (page types, form controls, widgets, linked pages).

If you copied the plugin files manually, copy `.mcp.json` to your workspace alongside the plugin folder.

## Run the migration skills

The skills group into four phases — Plan, Configure, Generate code extensions, and Execute and evaluate. The flow is iterative: refine the plan, regenerate `appsettings.json` and code extensions, and re-run as issues surface during execution and evaluation.

The "VS Code GitHub Copilot example" blocks below read as one continuous narrative — a single operator working through a DancingGoatMvc-style upgrade — so each prompt makes sense as the next step after the previous one.

### Plan

#### migrate-plan

Prompt name: **migrate-plan**  
Parameters:
  - *source-content-model-path*: Path to the source content model (typically the directory of `kx13-content-audit` JSON output, or a markdown description of the source).
  - *target-content-model-path* (optional): Path to a target XbyK content model description. When provided, the plan compares source vs. target and surfaces structural divergences.

Produces `migration-overview.md` and `migration-detail.md` covering content types, field mappings, widget transformations, page relationships, exclusions, taxonomy planning, manual steps, and the execution plan. Uses the official Kentico Docs MCP server (when configured) to verify XbyK capabilities.

**VS Code GitHub Copilot example:**

```
/migrate-plan

I just finished kx13-content-audit on my DancingGoatMvc database.
Produce the migration plan from the JSON output in ./audit-results/.
```

### Configure

#### migrate-appsettings

Prompt name: **migrate-appsettings**  
Parameters:
  - *migration-plan-path*: Path to the `migration-detail.md` produced by *migrate-plan*.

Generates the Migration Tool's `appsettings.json` (connection strings, `ConvertClassesToContentHub`, `EntityConfigurations`, `OptInFeatures.QuerySourceInstanceApi`, `OptInFeatures.CustomMigration.FieldMigrations`, `AssetRootFolders`, `MigrationProtocolPath`) and a markdown summary that traces every setting back to a plan section. When KX13 and XbyK projects are present in the workspace, infrastructure values (connection strings, source instance URI) are discovered automatically; otherwise, placeholders are emitted.

If the plan calls for [Source instance API discovery](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool/blob/master/Migration.Tool.CLI/README.md#source-instance-api-discovery), the skill also copies the `ToolApiController` into the KX13 project and registers its route.

**VS Code GitHub Copilot example:**

```
/migrate-appsettings

The plan in ./migration-detail.md is ready. Generate the migration
tool's appsettings.json from it.
```

### Generate code extensions

Run all four codegen skills — each skill inspects the plan and automatically skips if its not needed. Afterwards, check that the migration tool project compiles with all the added extensions before running `migrate-run`.

#### migrate-classes

Prompt name: **migrate-classes**  
Parameters:
  - *migration-plan-path*: Path to the `migration-detail.md`.

Generates `IClassMapping` and (optional) `ReusableSchemaBuilder` C# code in the `Migration.Tool.Extensions` project, plus the corresponding DI registration. Handles class merges, splits, field renames, value conversions (`ConvertFrom`), data-type/form-control patches (`WithFieldPatch`), and Content Hub conversions. After generation, the skill builds the project and reports any unresolved TODOs (typically taxonomy tag GUIDs that have to be resolved post-creation).

**VS Code GitHub Copilot example:**

```
/migrate-classes

Generate the IClassMapping and ReusableSchemaBuilder C# extensions
for the page types and reusable field schemas described in
./migration-detail.md.
```

#### migrate-fields

Prompt name: **migrate-fields**  
Parameters:
  - *migration-plan-path*: Path to the `migration-detail.md`.

Generates `IFieldMigration` C# code for cross-class field transforms — custom form controls without an XbyK equivalent, data-type conversions that span multiple classes, HTML sanitization, and URL/path rewrites. Use this when a transform applies globally across classes; for class-scoped definition changes, *migrate-classes* with `WithFieldPatch` is usually sufficient.

**VS Code GitHub Copilot example:**

```
/migrate-fields

Generate the IFieldMigration extensions for the cross-class field
transforms in ./migration-detail.md (HTML sanitization, URL rewrites,
and the legacy form-control conversions the plan flags).
```

#### migrate-widgets

Prompt name: **migrate-widgets**  
Parameters:
  - *migration-plan-path*: Path to `migration-detail.md`.

Generates `IWidgetMigration` and `IWidgetPropertyMigration` C# code for custom widget and section transforms — type renames, property restructuring, consolidation, property-value conversions.

**VS Code GitHub Copilot example:**

```
/migrate-widgets

Generate the IWidgetMigration and IWidgetPropertyMigration extensions
for the custom widgets that ./migration-detail.md flags for transforms.
```

#### migrate-content-items

Prompt name: **migrate-content-items**  
Parameters:
  - *migration-plan-path*: Path to the `migration-detail.md`.

Generates `ContentItemDirectorBase` C# code that controls per-item migration behavior during the `--pages` step: linked-page strategies (`Materialize`, `Drop`, `StoreReferenceInAncestor`), child-as-reference linking (`LinkChildren`), page-to-widget conversion, and conditional template overrides. Filters operate on numeric `NodeClassID`, so the migration plan must include `ClassID` values for the involved page types.

**VS Code GitHub Copilot example:**

```
/migrate-content-items

Generate the ContentItemDirectorBase extensions for the linked-page
strategies, child-as-reference linking, and page-to-widget conversions
in ./migration-detail.md.
```

### Execute and evaluate

Treat `migrate-run` and `migrate-eval` as a loop. Almost every non-trivial migration takes more than one iteration — fix issues raised by the eval, regenerate the relevant extension, re-run.

#### migrate-run

Prompt name: **migrate-run**  
Parameters:
  - *migration-plan-path*: Path to the `migration-detail.md`.

Executes a **single combined `migrate` CLI invocation** with all required flags from the plan's Execution Plan section (`--sites`, `--custom-modules`, `--users`, `--page-types`, `--pages`, `--categories`, `--media-libraries`, `--forms`, etc.) — the migration tool orders the flags internally based on their dependency tree, so this skill never runs flags as separate sequential commands. The skill monitors stdout/stderr, applies pre-flight checks, validates each step with SQL queries, and writes structured logs to `MigrationProtocolPath`. For the full set of CLI parameters and their dependencies, see the official [Migrate Command Parameters](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool/blob/master/Migration.Tool.CLI/README.md#migrate-command-parameters).

> [!IMPORTANT]
> Build the `Migration.Tool.Extensions` project successfully before running this skill. The skill reports build failures rather than running with stale binaries. If `QuerySourceInstanceApi` is enabled, ensure the KX13 instance is running and the `ToolApiController` is reachable.

**VS Code GitHub Copilot example:**

```
/migrate-run

Migration.Tool.Extensions builds clean and the KX13 source app is
running. Execute the migration end-to-end against the configured
target database following ./migration-detail.md.
```

#### migrate-eval

Prompt name: **migrate-eval**  
Parameters:
  - *migration-plan-detail-path*: Path to the `migration-detail.md`.
  - *appsettings-path* (optional): Path to a non-default `appsettings.json` if the migration was run with a different configuration.

Reads the protocol and console logs from `migrate-run`, queries both the KX13 and XbyK databases, and compares the result against the plan across 12 categories (configuration overview, content types, reusable field schemas, taxonomies, content item counts and orphans, field verification, page issues, users, media, forms, custom modules, overall health). Emits a self-contained HTML report with per-category pass/fail/warn status and routing back to the appropriate skill (`migrate-appsettings`, code-gen skills, or manual fix-up) for each finding.

**VS Code GitHub Copilot example:**

```
/migrate-eval

migrate-run finished. Compare the migrated XbyK database against
./migration-detail.md and produce the HTML report so I know what to
fix and which sibling skill to re-run for each finding.
```

## Best practices

- Run an audit first. The [`kx13-content-audit`](../kx13-content-audit/README.md) CLI gives the planning skill the structured input it needs.
- Work iteratively. Treat the configure → codegen → run → eval sequence as one loop. Most issues identified by `migrate-eval` require a re-run of an earlier phase. The skill output directly instructs you about which skills to rerun.
- Several skills emit `TODO` placeholders. Resolve them in the plan before re-running, or post-migration in the generated code. The agents prompt you for that during the workflow.
- Review every generated extension before running the migration.
- Keep `MigrationProtocolPath` stable. `migrate-eval` reads protocol and console logs from the directory that `migrate-appsettings` writes into the config. Don't move the directory between runs unless you also update `appsettings.json`.

## Skill customization

These skill files serve as a baseline for migrating the content of KX13 projects to Xperience by Kentico. Modify and enhance the files as required by your implementation, workflow, and requirements. The reference materials under `skills/_shared/references/` and each skill's `references/` directory are the most useful starting points for adapting the prompts to project-specific conventions or constraints.

## License

Distributed under the MIT License. See [`LICENSE.md`](../../LICENSE.md) for more information.
