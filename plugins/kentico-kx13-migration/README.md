# KX13 → Xperience by Kentico migration

Skills, references, and helper tooling for upgrading Kentico Xperience 13 (KX13) projects to [Xperience by Kentico](https://docs.kentico.com/x/migrate_from_kx13_guides) (XbyK).

## Start here

Do not begin with an individual skill unless you already have a migration plan and know which stage you are in.

1. Read the [KX13 upgrade workflow](../../docs/KX13-Upgrade.md) for the sequence and boundaries.
2. Prepare the source, target, and Migration Tool versions using Kentico's [official upgrade walkthrough](https://docs.kentico.com/x/upgrade_walkthrough_guides).
3. Install this plugin and configure the [MCP servers required for the stages you will use](./MCP-setup.md).
4. Start with `migrate-content-audit`, then follow the workflow through content and code migration.

## Capabilities

| Area | Skills | Outcome |
|---|---|---|
| [Content-model audit](#content-model-audit) | `migrate-content-audit` | Structured JSON and a Markdown report describing the KX13 content model |
| [Content migration](#content-migration) | `migrate-content-plan`, `migrate-content-appsettings`, `migrate-content-classes`, `migrate-content-fields`, `migrate-content-widgets`, `migrate-content-items`, `migrate-content-run`, `migrate-content-eval` | A planned, configured, executed, and evaluated migration using the Kentico Migration Tool |
| [Codebase migration](#codebase-migration) | `migrate-code-global`, `migrate-code-component`, `migrate-code-page-widgets`, `migrate-code-page`, `migrate-code-page-visual` | Migrated live-site foundation, pages, Page Builder components, and shared components |

## Requirements

- KX13 Refresh 5 (hotfix 13.0.64) or newer with access to the source database
- An XbyK target compatible with the selected [Kentico Migration Tool release](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool/blob/master/README.md#library-version-matrix)
- A local clone of the [Kentico Migration Tool](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool)
- .NET 8 SDK or newer
- `sqlcmd` for validation queries used by the migration skills
- An AI coding assistant with this plugin installed
- The MCP servers required for the selected migration stages, as listed in [MCP setup](./MCP-setup.md)

Follow the Migration Tool's [source-instance](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool/blob/master/Migration.Tool.CLI/README.md#set-up-the-source-instance) and [target-instance](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool/blob/master/Migration.Tool.CLI/README.md#set-up-the-target-instance) requirements before running a migration.

## Install

Follow the marketplace instructions in the [usage guide](../../docs/Usage-Guide.md#install-the-selected-plugin), using the plugin name `kentico-kx13-migration`.

---

## Content-model audit

`migrate-content-audit` runs the bundled .NET auditor against the KX13 database and exports the source model. Use its output as the input to `migrate-content-plan`.

The marketplace package exposes the skill, but the .NET source under `src/` must also be available in the workspace. See the [content auditor guide](./docs/content-auditor.md) for setup, CLI flags, output files, scope, and test coverage.

### migrate-content-audit

```text
/migrate-content-audit

Audit the DancingGoatMvc site as the starting point for migrating it to
Xperience by Kentico. Export the full content model into ./audit-results/
```

---

## Content migration

These skills drive the [Kentico Migration Tool](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool): they turn the audit into a plan, generate configuration and extensions, run the migration, and evaluate the result.

Place the source, target, Migration Tool, and audit output in one workspace:

```
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
> Other layouts can work, but this structure reduces discovery ambiguity.

The skills run in four phases. The configure, generate, run, and evaluate phases form an iterative loop.

### Skill sequence

| Phase | Skill | Outcome |
|---|---|---|
| Plan | `migrate-content-plan` | `migration-overview.md` and the authoritative `migration-detail.md` |
| Configure | `migrate-content-appsettings` | Migration Tool `appsettings.json` traced to the plan |
| Generate | `migrate-content-classes` | `IClassMapping` and optional `ReusableSchemaBuilder` extensions |
| Generate | `migrate-content-fields` | Cross-class `IFieldMigration` extensions |
| Generate | `migrate-content-widgets` | `IWidgetMigration` and `IWidgetPropertyMigration` extensions |
| Generate | `migrate-content-items` | `ContentItemDirectorBase` logic for linked pages, references, and page-to-widget conversions |
| Execute | `migrate-content-run` | One dependency-ordered migration run plus protocol and console logs |
| Evaluate | `migrate-content-eval` | An HTML report comparing the databases and plan, with remediation routing |

Run all four generate-phase skills. Each reads `migration-detail.md`, skips when its extension type is unnecessary, and builds the extensions project after writing code.

The exact parameters and execution guardrails live in each skill's `SKILL.md`; invoke the skill with the plan path rather than copying those instructions into the prompt.

### Example: plan and configure

```text
/migrate-content-plan

Create the migration plan from ./audit-results/.
```

```text
/migrate-content-appsettings

Generate the Migration Tool configuration from ./migration-detail.md.
```

### Example: execute and evaluate

```text
/migrate-content-run

Run the migration described by ./migration-detail.md.
```

```text
/migrate-content-eval

Evaluate the result against ./migration-detail.md and identify which
skill or manual step should address each finding.
```

### Content-migration rules

- Audit before planning and treat `migration-detail.md` as the source of truth.
- Review all generated configuration and C# extensions before running the Migration Tool.
- Build `Migration.Tool.Extensions` successfully before `migrate-content-run`.
- Run the Migration Tool once with the combined flags selected from the plan; the tool orders them by dependency.
- Keep `MigrationProtocolPath` stable between run and evaluation.
- Treat configure → generate → run → evaluate as a loop until the report is acceptable.

---

## Codebase migration

These skills migrate the live-site foundation and presentation code after the target database contains the migrated content types.

Place the source and target projects in the same workspace:

```
KX13/          # Kentico Xperience 13 project files
XbyK/          # Xperience by Kentico project files
```

Start the KX13 application or provide an accessible URL. Leave the XbyK application stopped unless a skill starts it for validation.

### Skill sequence

| Order | Skill | Outcome |
|---|---|---|
| Once | `migrate-code-global` | XbyK project foundation, generated entity classes, global assets, routing, and Page Builder setup |
| Per shared element | `migrate-code-component` | Migrated header, footer, navigation, or other shared component |
| Per Page Builder page | `migrate-code-page-widgets` | Migrated widgets and sections used by the page |
| Per page | `migrate-code-page` | Migrated controller, retrieval code, view model, views, and dependencies |
| When needed | `migrate-code-page-visual` | Visual alignment between the source and target page |

Skip `migrate-code-page-widgets` for pages that do not use Page Builder. Use `migrate-code-page-visual` only after the page is functional.

### Example: initialize the target

```text
/migrate-code-global
```

### Example: migrate a shared component

```text
/migrate-code-component

componentName: breadcrumbs
legacyPageUrl: https://localhost:5001/en-us/home
```

### Example: migrate a Page Builder page

```text
/migrate-code-page-widgets

pageName: home
legacyPageUrl: https://localhost:5001/en-us/home
```

```text
/migrate-code-page

pageName: home
legacyPageUrl: https://localhost:5001/en-us/home
```

```text
/migrate-code-page-visual

pageName: home
legacyPageUrl: https://localhost:5001/en-us/home
newPageUrl: http://localhost:60444/en-us/home
```

### Codebase-migration rules

- Complete content migration before generating target entity classes.
- Run page skills in order: widgets when applicable, page implementation, then visual alignment when needed.
- Keep the KX13 site accessible at the URL supplied to the skills.
- Let each skill manage the XbyK process it starts; verify the process state before invoking the next skill.
- Review and test generated code before moving to the next page.

## Scope and limitations

The plugin assists with the content and live-site portions described in the [upgrade workflow](../../docs/KX13-Upgrade.md). It does not fully automate custom-module UI, authentication and user management, search, commerce storefronts, marketing features, integration bus, or project-specific operational concerns.

Use Kentico's [feature migration strategy](https://docs.kentico.com/x/plan_your_strategy_for_migrating_features_guides) and [code adaptation guide](https://docs.kentico.com/x/migrate_your_code_guides) to plan those areas.

---

## Skill customization

These skill files serve as a baseline for migrating KX13 projects to Xperience by Kentico. Modify and enhance the files as required by your implementation, workflow, and requirements. The reference materials under `skills/_shared/references/` and each skill's `references/` directory are the most useful starting points for adapting the prompts to project-specific conventions or constraints.

## License

Distributed under the MIT License. See [`LICENSE.md`](../../LICENSE.md) for more information.
