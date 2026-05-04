# KX13 Content Auditor

Reads a Kentico Xperience 13 (KX13) database and exports the content model as structured JSON files plus a Markdown report. Useful for auditing site content, planning migrations, and analyzing page types, content trees, forms, custom tables, and page builder usage.

The output is the canonical input for the [`kx13-content-migration`](../kx13-content-migration/README.md) plugin — its `migrate-plan` skill consumes the audit JSON to produce a Migration Overview and Migration Detail document. For an end-to-end view of how the auditor fits into a full KX13 → XbyK upgrade, see [KX13 upgrade plugins](../../docs/KX13-Upgrade-Plugins.md).

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

After this one-time setup, invoke the `content-audit` skill from your AI assistant and it runs the CLI for you. The CLI sections below document the underlying command surface for direct use or troubleshooting.

## Usage

Run from the plugin folder:

```bash
dotnet run --project src/KX13.ContentAuditor.CLI
```

### Full export (no arguments)

Exports the entire content model as individual JSON files:

```bash
dotnet run --project src/KX13.ContentAuditor.CLI
```

This produces:

- `sites.json` — Sites with cultures, content tree, page builder configs, and custom field values
- `page-types.json` — All page types with field definitions
- `custom-tables.json` — Custom tables with fields and alternative forms
- `custom-modules.json` — Custom modules with classes and references
- `forms.json` — BizForms with fields, validation rules, and alternative forms
- `page-builder-components.json` — Discovered widgets, sections, and page templates in use
- `content-reference-graph.json` — Cross-content reference map (page selectors, media selectors, etc.)
- `relationships.json` — Page-to-page relationships, including ad-hoc Pages fields and named relationships

### Selective export

Export only specific parts of the content model by passing flags:

```bash
# Export just page types
dotnet run --project src/KX13.ContentAuditor.CLI -- --page-types

# Export sites and forms
dotnet run --project src/KX13.ContentAuditor.CLI -- --sites --forms

# Export only page relationships
dotnet run --project src/KX13.ContentAuditor.CLI -- --relationships

# Export page builder components to a custom directory
dotnet run --project src/KX13.ContentAuditor.CLI -- --page-builder-components --output ./my-output
```

### Available flags

#### Export options

| Flag | Description |
|---|---|
| `--sites` | Sites with cultures, content tree, and assigned types |
| `--page-types` | All page types with field definitions |
| `--page-builder-components` | Discovered page builder components (widgets, sections, templates) |
| `--custom-modules` | Custom modules with their classes |
| `--custom-tables` | Custom tables with fields |
| `--forms` | BizForms with fields and alternative forms |
| `--relationships` | Page-to-page relationships and Pages-field reuse links |
| `--report` | Generate a Markdown content model report (`content-model-report.md`). Implied by a full export — only pass this flag explicitly to add the report alongside selective area flags. |

Running without export options exports the full content model (all JSON files + report).

#### Filter options

| Flag | Description |
|---|---|
| `--site-name <name>` | Filter by site code name (exact match, e.g. `DancingGoatMvc`) |
| `--class-name <pattern>` | Filter by class name pattern (`*` wildcard, comma-separated, e.g. `"DancingGoat.*,CMS.MenuItem"`) |
| `--page-path <prefix>` | Filter content tree by node alias path prefix (e.g. `/Articles`) |

#### Other options

| Flag | Description |
|---|---|
| `--output <path>` | Output directory (default: `audit-results/` under the project root) |
| `--help`, `-h` | Show help |

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

### Output shapes

Each JSON file is an array of records (or, for `relationships.json`, a single object). The model classes under `src/KX13.ContentAuditor.DataAccess/Models/` are the authoritative reference for every field name and type — the descriptions below highlight the planning-relevant fields the migration plan skill consumes.

#### `page-types.json`

`PageType[]` — every page type (`CMS_Class.ClassIsDocumentType=1`), including system content types that have been extended. Each record carries class metadata (name, display name, table name, page-builder/URL/metadata flags, URL pattern, name source field, inheritance) and the full `Fields[]` array of `FieldDefinition` records.

Planning-relevant:

- `InheritsFromClassName` — the parent page type when KX13 inheritance is configured. The Kentico Migration Tool cannot migrate page types that inherit fields from a parent. Any non-null value identifies a page type that needs manual handling before `--page-types` runs: either flatten the inheritance in the source database, or exclude the affected page type via `EntityConfigurations.CMS_Class.ExcludeCodeNames` and rebuild it in the target manually.
- `IsSystemPageField` (per field) — distinguishes fields the page type inherits from system parent classes (`DocumentName`, `NodeAliasPath`, etc.) from fields that were added on the page type itself.

#### `custom-tables.json`

`CustomTable[]` — every `CMS_Class` with `ClassIsCustomTable=1`. Each record carries class metadata, the `Fields[]` array, and an `AlternativeForms[]` array.

`AlternativeForms[].FormDefinitionDelta` is the XML delta from the base class form. Alt forms are how KX13 surfaces different field UIs in different contexts (insert, update, on-site editing), so the delta is the right shape for understanding what an alt form actually changes.

#### `custom-modules.json`

`CustomModule[]` — `CMS_Resource` rows whose name does not start with `CMS`. Each module carries a `Classes[]` array of its module classes (`ModuleClass`), and each module class carries its `Fields[]`, a hoisted `References[]` view, and a `ParentClassName`.

- `References[]` — foreign-key fields whose form-control settings declare a target object type. Useful for understanding how module classes relate to each other without reading every `FormControlSettings` dictionary.
- `ParentClassName` — reflects `ClassInheritsFromClassID`. As with page types, the Migration Tool does not handle module-class inheritance.

#### `forms.json`

`Form[]` — every bizform (`CMS_Form`) with its bizform-level settings (recipients, redirect, confirmation email, builder layout, activity logging, etc.), the underlying class field definitions, and any alternative forms.

Form fields use `FormFieldDefinition`, which extends `FieldDefinition` with form-specific properties: `ValidationRule`, `ValidationErrorMessage`, `VisibilityCondition`, `ExplanationText`, `Tooltip`, and `LiveSiteFormComponentIdentifier` (the live-site form component, separate from the admin component). `VisibilityCondition` is typically a KX13 macro expression, which has no direct XbyK equivalent.

#### `FieldDefinition` — the shared per-field shape

Every page type, custom table, module class, and form field uses the same base shape: a GUID, name, caption, data type, size/precision, required/visible flags, default value, the legacy form-control name, the modern form-component identifier, a `FormControlSettings` dictionary, hoisted reference metadata, and the KX13 field category.

Planning-relevant:

- `FormControlName` is the legacy KX13 control. `FormComponentIdentifier` is the modern component (set when the page type was already saved with an XbyK-style component). The Migration Tool maps standard combinations automatically; uncommon controls show up here and need an `IFieldMigration` or `WithFieldPatch` extension.
- `FormControlSettings` preserves any control-specific configuration not explicitly modeled — selector queries, allowed paths, list values, file extensions, and so on. This is the dictionary you scan when deciding what custom transforms a field needs.
- `ReferenceToObjectType` and `ReferenceType` are hoisted out of `FormControlSettings` for foreign-key fields. Use these to plan reference resolution (page selectors, module-class FKs).
- `Category` reflects KX13 field categories. The Migration Tool does not preserve field categories — record this if it matters for editor UX in the target.

#### `sites.json`

`Site[]` — sites with cultures, full content tree, page builder configurations stored per page, and custom field values for each tree node. The largest file in a typical export.

#### `page-builder-components.json`

`PageBuilderComponentDefinition[]` — every widget, section, and page template the auditor discovered in use across the content tree, with the property definitions reconstructed from observed usage.

#### `content-reference-graph.json`

A flat array of `PageContentReferenceEntry` — one entry per page that contains references, listing each referenced node, media file, or object discovered by parsing field values.

#### `relationships.json`

`ExportRelationships` — page-to-page relationships from `CMS_Relationship`, including ad-hoc Pages-field relationships keyed by class name + field GUID, and named relationships defined in `CMS_RelationshipName`.

#### `content-model-report.md`

Generated alongside JSON in full exports (or when `--report` is passed explicitly). A human-readable Markdown summary of the same data — useful for skimming the model before handing the JSON to the migration plan skill.

### Known limitations

- **`HasCustomFields` on page types is heuristic.** It reports whether the page type has its own backing table (`ClassTableName is not null`), not whether non-system fields actually exist. Inspect `Fields[]` filtered by `IsSystemPageField=false` for the precise answer.

## Project Structure

```
src/
├── KX13.ContentAuditor.CLI/            # Console entry point, argument parsing, JSON export
├── KX13.ContentAuditor.Application/    # Orchestration service (ContentModelService)
└── KX13.ContentAuditor.DataAccess/     # Database access layer
    ├── Models/                         # 25 POCO model classes
    ├── Repositories/                   # SQL queries + result mapping
    │   └── Interfaces/                 # Repository contracts
    ├── Parsers/                        # XML/JSON parsers (ClassFormDefinition, PageBuilder)
    ├── Analysis/                       # Component discovery + content reference analysis
    └── DbAccess/                       # Raw ADO.NET query executor
```

## License

Distributed under the MIT License. See [`LICENSE.md`](../../LICENSE.md) for more information.
