---
name: "migrate-content-fields"
description: "Generates C# IFieldMigration extension code for custom field value and definition transformations (form controls, data types, HTML cleanup, path updates) for page types, module classes, system objects, and forms during KX13→XbyK migration. Use when the user needs to handle custom form controls with no XbyK equivalent, cross-class field transforms, HTML sanitization, URL path rewriting, or data type conversions that apply globally across multiple classes."
argument-hint: "[migration-plan-path]"
compatibility: "Requires dotnet CLI and optionally sqlcmd for resolving plan gaps. Optionally uses a Kentico documentation lookup tool for API verification."
---

# Field Transformation Code Generation

Produces ready-to-use C# code files for the Migration.Tool.Extensions project. Takes the migration plan output from the migrate-content-plan skill — or a direct text description — as input.

## Workflow

### Step 1: Read Reference Materials

- Read `references/field-migration-api.md` for the complete API patterns and decision guides.
- If you need pattern examples for implementation, read `assets/FIELD_MIGRATION_EXAMPLE.cs` for a complete annotated reference implementation.
- If you need context on the migration tool's extension points or configuration, read `../_shared/references/migration-tool.md`.
- If you need documentation links for the Kentico Migration Tool, read `../_shared/references/migration-docs.md`.
- If a Kentico documentation lookup tool is available, use it for additional context on XbyK form components, data types, or the Migration Tool API.

### Step 2: Analyze Input

- If a migration plan file path is provided → read it and extract from the **Field Mappings** section: **Field Changes** tables (scan for data type or form control changes), **Custom Form Control Fields** table (lists fields with non-built-in form controls and their handling mechanisms), and **Custom Value Transforms** table.
- If the migration plan has no **Custom Form Control Fields** section, scan the **Field Changes** tables for any field with a data type change or form control change column that is not handled by a built-in mapping or `WithFieldPatch` in an `IClassMapping`. These are candidates for `IFieldMigration` code or `appsettings.json` `FieldMigrations` config.
- If a direct text description is provided → identify source form controls, data types, field names, and transformation needs.
- Ask clarifying questions if the transformation logic, target form components, or scope (which classes) are ambiguous.

### Step 3: Identify Migrations to Generate

Determine the set of `IFieldMigration` classes needed:

- One `IFieldMigration` class per logical transformation concern (e.g., one for custom rich text editors, one for HTML cleanup, one for date format conversion).
- Group related form controls or data types into a single migration when they share the same transformation logic.
- One `ServiceCollectionExtensions` class for DI registration.
- **Cross-reference the built-in mapping table** in `../_shared/references/migration-tool.md` — the migration tool has default `FieldMigration` entries for standard data type + form control combinations (e.g., `text + TextBoxControl → TextInput`, `longtext + HtmlAreaControl → RichTextEditor`). It also has catch-all entries per data type (e.g., `text + _other_ → TextInput`, `longtext + _other_ → TextArea`). If a source form control matches a built-in entry or catch-all AND the target data type doesn't change, no `IFieldMigration` is needed.
- **Check for data type changes**: Built-in catch-all mappings preserve the source data type — they do not change it. If a field requires a data type change (e.g., `text → longtext`) AND a form control change, the catch-all won't produce the correct result. Use `IFieldMigration` code, `appsettings.json` `FieldMigrations` config, or `IClassMapping.WithFieldPatch` instead.
- **Check the migration plan's Custom Form Control Fields section** (if present) — fields marked with handling mechanism "IFieldMigration code" need code generation. Fields marked "Built-in catch-all", "FieldMigrations config", or "WithFieldPatch" do not.
- **Skip** code generation for:
  - Simple form control swaps with no value change → use `appsettings.json` `FieldMigrations` config instead.
  - Field renames or per-class value transforms → use `IClassMapping` `SetFrom`/`ConvertFrom` instead.
  - Field definition changes scoped to a single class → consider `IClassMapping.WithFieldPatch` instead.
  - Built-in conversions already handled by the migration tool defaults (including catch-all entries).
- **Recommend `IClassMapping.ConvertFrom`** when the transform is specific to one class and one field (per-class scope, not cross-class).
- **Recommend `IClassMapping.WithFieldPatch`** when the field definition change (data type, form component) is scoped to a single class and paired with an `IClassMapping` that already exists for that class.

### Step 4: Generate Field Migration Code

For each migration, generate a class implementing `IFieldMigration`:

- `Rank` — use values < 100,000 (built-in defaults use 100,000+). Leave gaps between values (1000, 2000, 3000) for future insertions.
- `ShallMigrate` — precise matching on `SourceFormControl`, `SourceDataType`, `FieldName`, or `ClassName`. Avoid overly broad matches that interfere with built-in migrations.
- `MigrateFieldDefinition` — use `System.Xml.Linq` XElement API to patch XML field definitions (controlname, column type, data type, settings).
- `MigrateValue` — null-safe (`null or DBNull` check), type-safe (`is string s` pattern), context-aware (`SourceObjectContext` switch when behavior differs).
- Include a per-migration `IServiceCollection` extension method for DI registration.

### Step 5: Generate Service Registration

Generate or update the `ServiceCollectionExtensions` static class:

- Call each migration's extension method.
- `AddSingleton<IFieldMigration>(new T())` for each migration.
- Include comment noting that fields handled by `IFieldMigration` code do not need entries in `appsettings.json` `FieldMigrations` config.
- Include comment noting any prerequisite appsettings or `IClassMapping` registrations.

### Step 6: Build Verification

1. Build the `Migration.Tool.Extensions` project to verify the generated code compiles without errors.
2. If the build fails, analyze the error messages, fix all issues in the generated code, and rebuild.
3. Repeat up to 3 attempts. If the build still fails after 3 attempts, present the full build output and error details to the user for manual resolution.

### Step 7: Present and Refine

- Save files to the user-specified path (default: `Migration.Tool.Extensions/FieldMigrations/` — generated code belongs in the `Migration.Tool.Extensions` project, matching the `Migration.Tool.Extensions.FieldMigrations` namespace).
- Provide a summary table of generated migrations:

  | File                                   | Pattern                  | Rank | Handles                              |
  | -------------------------------------- | ------------------------ | ---- | ------------------------------------ |
  | `CommunityTextEditorFieldMigration.cs` | Form control replacement | 1000 | CommunityTextEditor → RichTextEditor |
  | `DateTextFieldMigration.cs`            | Data type conversion     | 2000 | EventDateText text → datetime        |

- Ask if any migrations need adjustment and iterate on feedback.

## Rules

- Namespace: `Migration.Tool.Extensions.FieldMigrations` (user can override).
- File naming: `{DescriptiveName}FieldMigration.cs`.
- Use string constants for source/target form control names and data types, following the `Source_`/`Target_` prefix convention from the example.
- Add `TODO` comments for values unknown at generation time (e.g., target asset paths, lookup dictionaries).
- Follow exact API patterns from `field-migration-api.md` — do not invent methods that don't exist.
- Every `IFieldMigration` must have a corresponding `AddSingleton<IFieldMigration>(new T())` registration.
- Handle both structured (migration plan) and free-text input.
- `MigrateFieldDefinition` uses `System.Xml.Linq` XElement API — find or create elements, set values, remove obsolete settings.
- `MigrateValue` must handle null (`null or DBNull`) and unexpected types defensively.
- Prefer `IClassMapping.ConvertFrom` for class-specific, per-field transforms; use `IFieldMigration` for cross-class or form-control-driven transforms.
- If a Kentico documentation lookup tool is available, verify uncertain API details before generating code.
- After generating code, always build the project and fix compilation errors before considering the task complete.

## Gotchas

- `ShallMigrate` is called for every field in every class — must be lightweight and specific. Broad matching (e.g., all `"text"` fields) interferes with built-in migrations.
- `MigrateFieldDefinition` and `MigrateValue` must be aligned — if the definition changes the data type, the value must be converted to match.
- Custom rank values must be < 100,000 (built-in defaults use 100,000+; lower rank = higher priority).
- Fields handled by `IFieldMigration` do NOT need `appsettings.json` `FieldMigrations` entries. Skip code generation when config suffices (simple form control swap, no value transform).
- When a field definition change is scoped to a single class that already has an `IClassMapping`, prefer `WithFieldPatch` over `IFieldMigration` — it keeps the definition change co-located with the class mapping. Only use `IFieldMigration` when the same form control or data type change applies across multiple classes.
- Check `SourceObjectContext` when transformation behavior differs across pages, custom tables, and forms.
- This skill generates `IFieldMigration` code only — `IClassMapping`, `ContentItemDirectorBase`, `IWidgetMigration`, and `IWidgetPropertyMigration` are separate extension points.
