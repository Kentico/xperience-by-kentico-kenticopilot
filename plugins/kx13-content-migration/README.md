# Content Migration — KX13 to XbyK

AI-assisted migration of content from Kentico Xperience 13 (KX13) to Xperience by Kentico (XbyK) using Claude Code skills that drive the [Kentico Migration Tool](https://docs.kentico.com/x/migration-tool).

## Skills

Eight skills cover the full migration lifecycle. Run them in order — each skill's output feeds into the next.

### 1. migrate-plan

Creates two documents from a source content model description: a **Migration Overview** (human-readable summary) and a **Migration Detail** (comprehensive, AI-consumable specification). Covers content types, field mappings, widget transformations, page relationships, exclusions, and execution plan.

### 2. migrate-appsettings

Generates the `appsettings.json` for the Migration Tool CLI. Discovers KX13/XbyK infrastructure, configures connection strings, `ConvertClassesToContentHub`, `EntityConfigurations`, and all opt-in features.

### 3. migrate-classes

Generates `IClassMapping` and `ReusableSchemaBuilder` C# code for the `Migration.Tool.Extensions` project. Handles class merges, field renames, data type changes, and Content Hub conversions.

### 4. migrate-content-items

Generates `ContentItemDirectorBase` C# code for linked page handling (materialize/drop/store-reference), child-as-reference mappings, and page-to-widget conversions.

### 5. migrate-fields

Generates `IFieldMigration` C# code for custom form control conversions, data type transforms, HTML sanitization, and path rewrites.

### 6. migrate-widgets

Generates `IWidgetMigration` and `IWidgetPropertyMigration` C# code for custom widget type mappings, section mappings, and property transforms.

### 7. migrate-run

Executes the Migration Tool CLI commands (`migrate --sites`, `--page-types`, `--pages`, etc.) in order, monitoring output, diagnosing errors, applying fixes, and running post-step validation queries. Saves console logs to the `Settings.MigrationProtocolPath` directory from appsettings.json.

### 8. migrate-eval

Evaluates the XbyK database state after migration by comparing it against the migration plan detail. Queries both KX13 and XbyK databases across 12 categories (content types, taxonomies, field verification, orphan detection, etc.) and produces a self-contained HTML report in the same directory.

## Workflow

```
migrate-plan          Plan what migrates and how
        |
migrate-appsettings      Configure the Migration Tool
        |
   +---------+---------+
   |         |         |
migrate  migrate  migrate      Generate C# code extensions
-classes -fields  -widgets
   |         |         |
   +----+----+---------+
        |
migrate-content-items          Handle linked pages and relationships
        |
migrate-run             Execute the migration
        |
migrate-eval                 Verify the results
```

Skills 3-6 (code generation) can be run in any order, but all must complete before step 7. Steps 3 and 5 are almost always needed; steps 4 and 6 depend on whether the migration involves custom field transforms or widget migrations.

## Shared References

The `skills/_shared/references/` directory contains knowledge shared across skills:

- **migration-tool.md** — Migration tool capabilities, supported data types, field mappings, extension point APIs, and effort tiers.
- **migration-docs.md** — Links to official Kentico migration documentation.
