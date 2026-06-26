---
name: migrate-content-widgets
description: Generates C# IWidgetMigration and IWidgetPropertyMigration extension code for custom widget and section type transformations (rename, restructure, consolidate, property conversion) during KX13→XbyK migration. Use when the user needs to change widget types, restructure widget data, or convert section types.
compatibility: Requires dotnet CLI and the Migration.Tool.Extensions project structure.
argument-hint: "[migration-plan-path]"
---

# Widget Transformation Code Generation

Produces ready-to-use C# code files for the Migration.Tool.Extensions project. Takes the migration plan output from the migrate-content-plan skill — or a direct text description — as input.

## Workflow

### Step 1: Read Reference Materials

- Read [widget-migration-api.md](references/widget-migration-api.md) for the complete API patterns, annotated code samples, and decision guides.
- If you need pattern examples for implementation, read [WIDGET_MIGRATION_EXAMPLE.cs](assets/WIDGET_MIGRATION_EXAMPLE.cs) for a complete annotated reference implementation showing all patterns.
- If you need context on the migration tool's extension points or configuration, read [migration-tool.md](../_shared/references/migration-tool.md).
- If you need documentation links for further research, read [migration-docs.md](../_shared/references/migration-docs.md).
- If a Kentico documentation lookup tool is available, use it for additional context on widget migration APIs, Page Builder JSON structure, or advanced patterns like content item creation during migration.

### Step 2: Analyze Input

- If a migration plan file path is provided → read it and extract from the **Widget Transformations** section: **Section Type Mappings**, **Custom Widget Type Mappings**, **Widget Restructuring**, and **Widget Property Transforms** tables. Also check the **Code Extensions to Implement** table for rows where Type = `IWidgetMigration` or `IWidgetPropertyMigration`.
- If a direct text description is provided → identify source widget/section identifiers, target identifiers, property mappings, and transformation needs.
- Ask clarifying questions if the target widget types, property mappings, or value transformation logic are ambiguous.

### Step 3: Identify Code Units to Generate

Determine the set of classes needed:

- One `IWidgetMigration` class per source widget or section type (or per consolidation group when multiple source types map to one target).
- One `IWidgetPropertyMigration` class per distinct property transformation pattern that is reusable across widgets or needs to be delegated via the `propertyMigrations` dictionary.
- One `ServiceCollectionExtensions` class (or addition to existing) for DI registration.
- **Skip** code generation for:
  - Built-in widgets with direct XbyK equivalents (`Kentico.Widget.RichText`, `Kentico.FormWidget`) — these migrate automatically.
  - Widgets that are unchanged (same identifier and properties in XbyK) **AND** have no properties requiring `IWidgetPropertyMigration` transforms.
  - Widgets explicitly excluded in the migration plan.
- **Always generate an `IWidgetMigration`** for any widget that needs property transforms via `IWidgetPropertyMigration` — even if the widget identifier is unchanged. The migration tool only invokes standalone `IWidgetPropertyMigration` classes when API Discovery provides `EditingFormControlModel` metadata for the property. If a KX13 widget property lacks an `[EditingComponent(...)]` attribute, `EditingFormControlModel` is null and the property migration is silently skipped. An `IWidgetMigration` that delegates properties via the `propertyMigrations` dictionary uses a separate code path (`explicitMigrations`) that does not depend on API Discovery.
- **Recommend inline conversion** in `MigrateWidget` when the property transformation is simple and specific to one widget (e.g., int→string ratio). Only create a separate `IWidgetPropertyMigration` when the transformation is reusable or needs to be delegated.
- **Consider built-in property migrations first:** `WidgetFileMigration` (media files, Rank 100,000), `WidgetPathSelectorMigration` (path selectors, Rank 100,001), and `WidgetPageSelectorMigration` (page selectors, Rank 100,002) handle common conversions automatically. Only create a custom `IWidgetPropertyMigration` to **override** a built-in when the default conversion target is wrong (e.g., page selector → `ContentItemReference` instead of `WebPageRelatedItem`).

### Step 4: Generate Widget Migration Code

For each migration, generate a class implementing `IWidgetMigration`:

- `Rank` — use values < 100,000 (built-in defaults use 100,000+). Use small integers (1, 2, 3...) with gaps for future insertions.
- `ShallMigrate` — match on `identifier.TypeIdentifier` using `string.Equals` with `StringComparison.InvariantCultureIgnoreCase`. For multi-site scenarios, also filter on `context.SiteId`.
- `MigrateWidget` — change `value["type"]` to the target identifier, restructure `value["variants"][0]["properties"]` as a new `JObject`, populate the `propertyMigrations` dictionary for properties needing further conversion.
- Include a per-migration `IServiceCollection` extension method for DI registration **in a separate `static` class** (not inside the migration class itself — migration classes use primary constructors for DI injection, which makes them non-static, and C# requires extension methods to be in `static` classes).

For `IWidgetPropertyMigration` classes:

- `ShallMigrate` — match on `propertyName` or `context.EditingFormControlModel?.FormComponentIdentifier`. **Do NOT use `context.ComponentIdentifier`** — this property does not exist on `WidgetPropertyMigrationContext`. The context record only has `SiteId` and `EditingFormControlModel`.
- `MigrateWidgetProperty` — transform the `JToken` value and return a `WidgetPropertyMigrationResult`.
- Include a per-migration `IServiceCollection` extension method for DI registration **in a separate `static` class** (same rule as `IWidgetMigration` — see above).

### Step 5: Generate Service Registration

Generate or update the `ServiceCollectionExtensions` static class:

- Call each migration's extension method.
- `AddTransient<IWidgetMigration, T>()` for each widget migration (**NOT AddSingleton**).
- `AddTransient<IWidgetPropertyMigration, T>()` for each property migration (**NOT AddSingleton**).
- Include comment noting that built-in widgets (`Kentico.Widget.RichText`, `Kentico.FormWidget`) do not need custom code.
- Include comment noting the `QuerySourceInstanceApi` appsettings prerequisite.

### Step 6: Build Verification

1. Build the `Migration.Tool.Extensions` project to verify the generated code compiles without errors.
2. If the build fails, analyze the error messages, fix all issues in the generated code, and rebuild.
3. Repeat up to 3 attempts. If the build still fails after 3 attempts, present the full build output and error details to the user for manual resolution.

#### 6b. Verify Registration Completeness

After a successful build, verify all generated migrations are properly registered:

1. Read the `ServiceCollectionExtensions` class and confirm every `IWidgetMigration` has a corresponding `AddTransient<IWidgetMigration, T>()` call (**NOT `AddSingleton`** — widget migrations are not safe as singletons).
2. Read the `ServiceCollectionExtensions` class and confirm every `IWidgetPropertyMigration` has a corresponding `AddTransient<IWidgetPropertyMigration, T>()` call.
3. Cross-reference against the migration plan's "Widget Transformations" section and "Code Extensions to Implement" table — every widget type listed with a custom mapping must have a generated file and a registration.
4. If any migration is missing registration or uses `AddSingleton` instead of `AddTransient`, fix and rebuild.

### Step 7: Present and Refine

- Save files to the user-specified path (default: `Migration.Tool.Extensions/WidgetMigrations/` — generated code belongs in the `Migration.Tool.Extensions` project, matching the `Migration.Tool.Extensions.WidgetMigrations` namespace).
- Provide a summary table using this format:

  | File                            | Pattern          | Rank | Handles                                                                    |
  | ------------------------------- | ---------------- | ---- | -------------------------------------------------------------------------- |
  | `HeroContentWidgetMigration.cs` | IWidgetMigration | 1    | Maps `Acme.HeroWidget` → `Acme.HeroBanner`, creates content item for image |

- Ask if any migrations need adjustment and iterate on feedback.

## Rules

- Follow exact API patterns from `widget-migration-api.md` — do not invent methods or types that don't exist.
- Handle both structured (migration plan) and free-text input.
- Widget JSON access pattern: `value["type"]` for the type identifier, `((JArray)value["variants"]!)[0]["properties"]` for the property bag.
- Reference built-in `WidgetFileMigration` (media files) and `WidgetPageSelectorMigration` (page selectors) in the `propertyMigrations` dictionary rather than reimplementing conversion logic.
- Namespace: `Migration.Tool.Extensions.WidgetMigrations` (user can override).
- File naming convention: `{TargetWidgetName}WidgetMigration.cs` for widget migrations, `{Concern}PropertyMigration.cs` for property migrations.
- Use string constants for source/target identifiers, following the `Source_`/`Target_` prefix convention from the example.
- Add `TODO` comments for values unknown at generation time (e.g., media GUID lookups, resource key resolution dictionaries, content item GUIDs).
- If a Kentico documentation lookup tool is available, verify uncertain API details before generating code.
- When overriding a built-in property migration, use `Rank < 100,000` to take priority over the built-in (100,000+). The `ShallMigrate` of the custom migration should match the same `FormComponentIdentifier` (via `Kx13FormComponents` constants) as the built-in it replaces.
- This skill generates `IWidgetMigration` and `IWidgetPropertyMigration` code only — `IClassMapping`, `IFieldMigration`, `ContentItemDirectorBase`, and appsettings configuration are separate extension points covered by other skills.

## Gotchas

- Use `AddTransient` for DI registration — **never `AddSingleton`**. Widget migrations are not safe to reuse as singletons.
- `ShallMigrate` must use `StringComparison.InvariantCultureIgnoreCase` — widget type identifiers are case-insensitive.
- Never write custom code for built-in widgets (`Kentico.Widget.RichText`, `Kentico.FormWidget`) — they migrate automatically.
- Check built-in property migrations (`WidgetFileMigration`, `WidgetPathSelectorMigration`, `WidgetPageSelectorMigration`) before writing custom `IWidgetPropertyMigration` classes — only override when the default conversion target is wrong.
- Section types use the same `IWidgetMigration` interface as widgets — there is no separate section migration interface.
- **`WidgetPropertyMigrationContext` only has `SiteId` and `EditingFormControlModel`** — there is no `ComponentIdentifier` property. Do not try to filter by widget type in `IWidgetPropertyMigration.ShallMigrate`. If you need widget-type-scoped property transforms, do them inline in the `IWidgetMigration.MigrateWidget` method or delegate via the `propertyMigrations` dictionary (which is already widget-scoped).
- **Standalone `IWidgetPropertyMigration` requires API Discovery** — the migration tool's `VisualBuilderPatcher` only calls `GetWidgetPropertyMigration` (rank-based lookup) when the property has a non-null `EditingFormControlModel` from API Discovery. KX13 widget properties without `[EditingComponent(...)]` attributes produce null `EditingFormControlModel`, silently skipping all property migrations. Always pair standalone `IWidgetPropertyMigration` classes with an `IWidgetMigration` that explicitly delegates via `propertyMigrations` to guarantee execution regardless of API Discovery metadata.
- **Extension methods must be in separate `static` classes** — migration classes that use primary constructors (for DI injection of `ILogger`, `ModelFacade`, etc.) are non-static. C# requires extension methods to be defined in non-generic `static` classes. Place the `Add{MigrationName}` extension method in a companion `static class {MigrationName}Extensions` in the same file.
