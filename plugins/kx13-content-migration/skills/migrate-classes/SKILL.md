---
name: migrate-classes
description: Generates C# IClassMapping and ReusableSchemaBuilder code for custom class transformations in the Kentico Migration Tool (KX13 to XbyK). Use for page type remodeling, merges, splits, field renames, value conversions, or Content Hub conversions.
compatibility: Requires dotnet CLI and optionally sqlcmd for resolving plan gaps.
argument-hint: "[migration-plan-path]"
---

# Class Transformation Code Generation

Produces ready-to-use C# code files for the Migration.Tool.Extensions project. Takes the migration plan output from the migrate-plan skill — or a direct text description — as input.

## Workflow

### Step 1: Read Reference Materials

- Read [class-mapping-api.md](references/class-mapping-api.md) for the complete API patterns, annotated code samples, and converter snippets.
- If you need pattern examples for implementation, read [CLASS_MAPPING_EXAMPLE.cs](assets/CLASS_MAPPING_EXAMPLE.cs) for a complete annotated reference implementation showing all patterns.
- If you need context on the migration tool's extension points or configuration, read [migration-tool.md](../_shared/references/migration-tool.md).
- If you need documentation links, read [migration-docs.md](../_shared/references/migration-docs.md).
- If a Kentico documentation lookup tool is available, use it for additional context on XbyK content types, reusable field schemas, or the Migration Tool API.

### Step 2: Analyze Input

- If a migration plan file path is provided → read it and extract from the **Content Model Mapping**, **Field Mappings**, **Custom Value Transforms**, and **Code Extensions to Implement** sections.
- If a direct text description is provided → identify source classes, target classes, field mappings, and transformation needs.
- Ask clarifying questions if source/target class names, fields, or conversion logic are ambiguous.

### Step 3: Identify Code Units to Generate

Determine the set of code files needed:

- One `IClassMapping` static class per target content type (including merges — merge all source-to-target mappings into one `MultiClassMapping`).
- One `IReusableSchemaBuilder` per reusable field schema (if defined via code, not via `CreateReusableFieldSchemaForClasses` appsettings).
- One `ServiceCollectionExtensions` class for DI registration.
- **Skip** code generation for: excluded classes, 1:1 mappings with no field changes, config-only `ConvertClassesToContentHub` entries without field transforms.

### Step 4: Extract Lookup Values from the Migration Plan

Before generating code, extract all lookup values that converters will need directly from the migration plan document. The plan is the trusted source of truth for field names, GUIDs, class names, and data types — do not query databases when the plan already provides the information. If the plan is contradictory, incomplete, or silent on a value, use `sqlcmd` to query the KX13 or XbyK database to resolve the gap.

Follow the detailed extraction procedures in [lookup-extraction-guide.md](references/lookup-extraction-guide.md) to extract source field names, converter lookup keys, XbyK-dependent values (using plan GUIDs or TODO placeholders), and determine when to ask the user or query the database for clarification.

### Step 5: Handle docrelationships Fields

When the migration plan identifies `docrelationships` fields (relationship-based fields that store data in `CMS_Relationship` rather than the coupled data table), follow the detailed patterns in [docrelationships-guide.md](references/docrelationships-guide.md). Key points: use factory DI registration with `ModelFacade` and `CmsRelationshipService`, the `WithoutSource` + `ConvertFrom` + `WithFieldPatch` pattern, and avoid using the `docrelationships` field name as `ConvertFrom` source when the target is not a page reference. See also Sample 11 in `class-mapping-api.md`.

### Step 6: Generate Class Mapping Code

For each target content type, generate a static class with a builder method returning `IClassMapping`:

- Use `MultiClassMapping` fluent builder following the patterns in `class-mapping-api.md`.
- `BuildField().SetFrom()` for direct field renames.
- `BuildField().ConvertFrom()` for value transformations — include null handling and explanatory comments.
- **CRITICAL: When `ConvertFrom` needs `WithFieldPatch`, NEVER use fluent chaining like `.ConvertFrom(includeDefinition: false).WithFieldPatch()`.** This causes a `NullReferenceException` at runtime because no `FormFieldInfo` exists. Instead, **always** use the three-step pattern with a local variable:
  ```csharp
  var field = m.BuildField("TargetField");
  field.WithoutSource("text");  // creates the FormFieldInfo
  field.ConvertFrom(source, "SourceField", false, converter);
  field.WithFieldPatch(f => { /* safe — f is not null */ });
  ```
  This applies to ALL `ConvertFrom` fields that need metadata patches — not just taxonomy or docrelationships fields. The only safe alternatives are `ConvertFrom(includeDefinition: true)` (which copies the source definition) or `SetFrom(isTemplate: true)` (which also copies the definition).
- `BuildField().WithFieldPatch()` for metadata changes (caption, data type, form control). **Always include `f.Visible = true` and `f.Enabled = true`** in every `WithFieldPatch` call — the migration tool's `FormDefinitionPatcher` can reset field visibility.
- `BuildField().WithFactory()` for entirely new field definitions.
- `BuildField().WithoutSource()` for fields without a KX13 source.
- For merge scenarios: multiple source classes in one `MultiClassMapping`, with `isTemplate: true` on exactly one source per field.
- **Important:** `isTemplate: true` must only be used on the class's **own** fields — never on fields that belong to a reusable schema (e.g., `DocumentName`, `DocumentPageTitle`). Shared schema fields are inherited from the schema and should use `isTemplate: false` or omit the parameter.
- For split scenarios: multiple `MultiClassMapping` registrations from the same source class, each targeting a different target.
- For custom tables / module classes: same `MultiClassMapping` API with `SetHandler<T>()`.
- Generate `ReusableSchemaBuilder` for reusable field schemas; reference in mappings via `UseResusableSchema()`.
- For every field rename specified in the migration plan's "Field Changes" table, generate the corresponding `SetFrom("SourceClass", "OldFieldName", isTemplate: true)` mapping to the new target field name. Do not rely on default field migration for renamed fields — default migration only works when source and target field names match.
- When `IncludeExtendedMetadata` is `true` in appsettings, the migration tool creates `DocumentPageTitle`, `DocumentPageDescription`, and `DocumentPageKeywords` fields automatically. If the plan renames these (e.g., to `SEOMetaTitle`), the class mapping must explicitly map them using `SetFrom` or `ConvertFrom`.

### Step 7: Generate Service Registration

Generate a single `ServiceCollectionExtensions` static class:

- `IServiceCollection` extension method calling each mapping's extension method.
- `AddSingleton<IClassMapping>(mapping)` for each mapping.
- `AddSingleton<IReusableSchemaBuilder>(schemaBuilder)` for each schema builder.
- Include comment noting which `appsettings.json` settings are prerequisites (e.g., `ConvertClassesToContentHub`, `IncludeExtendedMetadata`).

### Step 8: Verify ConvertClassesToContentHub Compatibility

`ClassMappingProvider.EnsureSettings()` auto-generates a `MultiClassMapping` only for classes in `ConvertClassesToContentHub` that are **custom tables** (`ClassIsCustomTable=1`). Page types in `ConvertClassesToContentHub` are skipped by `EnsureSettings()` and do not trigger auto-mapping — so page types with coded `IClassMapping` registrations will not conflict.

The conflict occurs only when a **custom table** class appears in both `ConvertClassesToContentHub` and has a coded `IClassMapping`: `AppendConfiguredMapping()` throws `InvalidOperationException("Duplicate class mapping '...' (check configuration 'ConvertClassesToContentHub')")`.

**Resolution:** If any generated mapping targets a **custom table** source class that is also listed in `ConvertClassesToContentHub`, **remove that class from `ConvertClassesToContentHub`** in appsettings.json. The coded `IClassMapping` alone is sufficient — it already controls the target class type (reusable vs. webpage) and field mapping. Omitting the class from `ConvertClassesToContentHub` prevents `EnsureSettings()` from generating the conflicting auto-mapping.

**When to skip:** If no generated mapping's source class is a custom table listed in `ConvertClassesToContentHub`, no action is needed. Page type classes are never affected.

### Step 9: Build Verification

1. Build the `Migration.Tool.Extensions` project to verify the generated code compiles without errors.
2. If the build fails, analyze the error messages, fix all issues in the generated code, and rebuild.
3. Repeat up to 3 times. If the build still fails after 3 attempts, present the full build output and error details to the user for manual resolution.

After a successful build, verify registration completeness and WithFieldPatch safety per [build-verification.md](references/build-verification.md). If any fixes are applied, rebuild and re-verify.

### Step 10: Present and Refine

- Save files to the user-specified path (default: `Migration.Tool.Extensions/ClassMappings/` — generated code belongs in the `Migration.Tool.Extensions` project, matching the `Migration.Tool.Extensions.ClassMappings` namespace).
- Provide a summary table: file name → purpose → effort estimate.

#### Resolved vs. Remaining Manual Steps

Present two clearly separated sections:

**Resolved from Migration Plan:**

- List each lookup value that was populated directly from the migration plan (e.g., "DayOfWeek NodeGUIDs: 7 keys extracted from Custom Value Transforms table", "Source field names verified against plan's field tables").
- If the ClassMappingProvider patch (Step 7) was applied, note which source classes triggered it.

**Remaining Manual Steps (require XbyK instance):**
For each remaining TODO in the generated code, provide:

1. **What** needs to be filled in (e.g., "Taxonomy tag GUIDs for DayOfWeek taxonomy").
2. **Why** it could not be resolved from the plan (e.g., "XbyK tag GUIDs are assigned at taxonomy creation time and are not in the migration plan").
3. **How** to resolve it — give the specific SQL query the user should run against the XbyK database:
   - _Taxonomy tag GUIDs:_ "Create the taxonomy and tags in XbyK Administration → Taxonomies. Then run: `SELECT TagName, CAST(TagGUID AS CHAR(36)) AS TagGUID FROM CMS_Tag WHERE TagTaxonomyID = (SELECT TaxonomyID FROM CMS_Taxonomy WHERE TaxonomyName = '<name>')`. Copy the GUIDs into the lookup dictionary."
   - _Taxonomy group GUIDs:_ "Run: `SELECT CAST(TaxonomyGUID AS CHAR(36)) FROM CMS_Taxonomy WHERE TaxonomyName = '<name>'`. Use this GUID in `f.Settings[\"TaxonomyGroup\"]`."
   - _Content item reference GUIDs:_ "Run the migration for the referenced content type first, then run: `SELECT ContentItemGUID FROM CMS_ContentItem WHERE ContentItemName = '<name>'`."

- Ask if any mappings need adjustment and iterate on feedback.

## Rules

- Follow exact API patterns from `class-mapping-api.md` — do not invent methods that don't exist.
- Every `IClassMapping` and `IReusableSchemaBuilder` must have a corresponding `AddSingleton` registration.
- Handle both structured (migration plan) and free-text input.
- In merge scenarios, exactly one source class has `isTemplate: true` per field. Only the class's own fields use `isTemplate: true` — fields belonging to a reusable schema (e.g., `DocumentName`) must not.
- Primary key convention: `{ShortTargetName}ID`; table name: `Namespace_ClassName` (underscores, not dots).
- Prefer `SetFrom` for direct field copies; use `ConvertFrom` only when value transformation is needed.
- `ConvertFrom` converters must handle null and unexpected types defensively with explanatory comments.
- `WithoutSource` fields must have `AllowEmpty = true`.
- File naming: `{TargetClassName}ClassMapping.cs`; namespace: `Migration.Tool.Extensions.ClassMappings` (user can override). One extension method per file.
- For all other coding conventions (string constants, `using` directives, XML doc comments), follow the patterns in [CLASS_MAPPING_EXAMPLE.cs](assets/CLASS_MAPPING_EXAMPLE.cs).
- Extract all lookup values from the migration plan (Step 4). The migration plan is the trusted source of truth for all lookup values including taxonomy tag GUIDs and taxonomy group GUIDs. If the plan contains actual GUIDs, use them directly in the generated code. If the plan contains `TODO` placeholders, generate `TODO` placeholders in the code with suggested SQL queries from [xbyk-query-patterns.md](references/xbyk-query-patterns.md). If the plan is contradictory, incomplete, or silent on a value, use `sqlcmd` to query the KX13 or XbyK database to resolve it. Only ask the user when the ambiguity cannot be resolved from the plan or database.
- Do not regenerate `appsettings.json` fragments — only generate C# code.
- If a Kentico documentation lookup tool is available, verify uncertain API details before generating code.
- This skill generates class mapping and reusable schema code only — `ContentItemDirectorBase`, `IFieldMigration`, `IWidgetMigration`, and `IWidgetPropertyMigration` are separate extension points not covered here.

## Gotchas

- **API typo:** Use the exact spelling `UseResusableSchema` — this is the actual method name in the API.
- **`ConvertFrom` + `WithFieldPatch` crash:** Never combine `ConvertFrom(includeDefinition: false)` with `WithFieldPatch` — it receives a null `FormFieldInfo` and throws a `NullReferenceException` at runtime. This applies to **all field types** (text, longtext, taxonomy, etc.), not just docrelationships. The anti-pattern to detect and avoid:
  ```csharp
  // WRONG — NullReferenceException at runtime:
  m.BuildField("Target").ConvertFrom(src, "Field", false, converter).WithFieldPatch(f => { ... });
  // ALSO WRONG — fluent chain still has no definition:
  m.BuildField("Target").ConvertFrom(src, "Field", false, converter)
      .ConvertFrom(src2, "Field", false, converter).WithFieldPatch(f => { ... });
  ```
  Instead, use `WithoutSource(dataType)` to create the field definition first, then chain `ConvertFrom` for the value converter, then chain `WithFieldPatch` for metadata patches. **Always use a local variable** to make the three-step sequence explicit:
  ```csharp
  // CORRECT — WithoutSource creates FormFieldInfo before WithFieldPatch:
  var field = m.BuildField("Target");
  field.WithoutSource("text");
  field.ConvertFrom(src, "Field", false, converter);
  field.WithFieldPatch(f => { ... });
  ```
  Note: `WithFactory` is only available on `IReusableFieldBuilder` (schema fields), not on `FieldBuilder` (class mapping fields).
- **`ConvertClassesToContentHub` + coded `IClassMapping` conflict (custom tables only):** `ClassMappingProvider.EnsureSettings()` auto-generates mappings only for **custom tables** (`ClassIsCustomTable=1`) in `ConvertClassesToContentHub`. Page types are not affected. If a custom table class has both a coded `IClassMapping` and a `ConvertClassesToContentHub` entry, `AppendConfiguredMapping()` throws a "Duplicate class mapping" error. **Resolution:** Remove the custom table class from `ConvertClassesToContentHub` — the `IClassMapping` alone handles the conversion. Step 8 verifies this and recommends the appsettings change if needed.
- **Renamed fields are silent failures:** Default migration only copies fields when source and target names match. Every field rename in the migration plan needs an explicit `SetFrom` or `ConvertFrom` — otherwise the target field is silently empty with no error.
- **`IncludeExtendedMetadata` fields:** When enabled, the tool auto-creates `DocumentPageTitle`, `DocumentPageDescription`, and `DocumentPageKeywords`. If the plan renames these, the class mapping must explicitly map them using `SetFrom` or `ConvertFrom`.
- **`docrelationships` fields store data in `CMS_Relationship`, not in the coupled data table.** The `ConvertFrom` converter receives `null` for the source value because the data is not in the page type's SQL table. Instead, inject `ModelFacade` and `CmsRelationshipService` via factory DI registration and query `CMS_Relationship` directly at conversion time. See Sample 11 in `class-mapping-api.md` for the complete pattern.
- **`ConvertToPages` override for relationship fields:** The migration tool's `ContentItemMapper.MapProperties` method detects `docrelationships` and `Pages` source fields and unconditionally replaces the converter's output with `ContentItemReference` GUIDs. When the target field is a taxonomy, object code name, or any non-page type, avoid this by using a **non-relationship source field** (e.g., the primary key field) in `ConvertFrom` so the pipeline doesn't detect it as a relationship. The converter ignores the incoming value and queries `CMS_Relationship` directly. **Do not apply this workaround** when the target field is a `contentitemreference` (Pages) field that should link to migrated pages — in that case, `ConvertToPages` is the correct behavior and the original `docrelationships` source field should be kept.
- **All `WithFieldPatch` calls must set `Visible = true` and `Enabled = true`:** The migration tool's `FormDefinitionPatcher.PatchField()` can reset or strip the `Visible` attribute on fields depending on the class type and whether the field type is recognized. This affects all fields created via `WithoutSource` (not just taxonomy), and can also affect fields from `ConvertFrom(includeDefinition: true)` on non-document-type classes. Always set `f.Visible = true` and `f.Enabled = true` in every `WithFieldPatch` call to ensure the field appears in the XbyK editable form. Without these, fields may exist in the schema but be invisible to editors.
- **Taxonomy tag reference JSON uses PascalCase `Identifier`:** XbyK's `TagReference` class expects `[{"Identifier":"guid"}]`, not `[{"identifier":"guid"}]`. Using lowercase causes deserialization to produce null values.
- **`WithFactory` is only available on `IReusableFieldBuilder`** (inside `ReusableSchemaBuilder`), not on `FieldBuilder` (inside `MultiClassMapping`). For class mapping fields that need a new definition with value conversion, use `WithoutSource` + `ConvertFrom` + `WithFieldPatch` instead.
- **Reusable schemas must be created before class mappings reference them:** If a class mapping calls `UseResusableSchema("Accelerator.Base")` but no schema with that name exists yet, the migration tool throws a `NullReferenceException` in `ReusableSchemaService.AddReusableSchemaToDataClass`. A schema can be created via **either** of two mutually exclusive paths (they cannot be combined):
  1. **`CreateReusableFieldSchemaForClasses` in appsettings.json** — converts an existing KX13 page type class into a reusable schema during the `--page-types` step. Every schema name referenced in `UseResusableSchema()` must appear in this list.
  2. **`IReusableSchemaBuilder` in code** — defines schema fields explicitly via the extension API. These are executed by `ExecReusableSchemaBuilders()` before class mappings run.

  Both paths run before `UseResusableSchema` is evaluated, so either one satisfies the dependency. When generating class mapping code, verify that every schema referenced in `UseResusableSchema()` is covered by **one** of these two paths and warn the user if any are missing.

- **Field names must exactly match KX13 source column names:** The migration tool throws "Field X not found in source class Y" when a `SetFrom` or `ConvertFrom` references a field name that doesn't exist. Always cross-reference field names against the **migration plan** — never guess or infer field names from similar-sounding patterns. For example, `LeaderCTALinkText1` vs. the actual `LeaderCTAText1` is a subtle difference that causes a hard failure. If the audit data is available (e.g., `content-model-report.md`, `class-analysis.json`, or KX13 auditor output), you can use it to verify field names, but the migration plan must be trusted as authoritative. If the plan references a field that doesn't appear in its own documentation tables, flag it as ambiguous and ask the user for clarification.
- **Base schema fields must NOT use `isTemplate: true` in `SetFrom`:** The `isTemplate: true` parameter marks a field as belonging to the class's **own** definition (copied from the template source). Shared/reusable schema fields like `DocumentName`, `DocumentPageTitle`, `DocumentPageDescription`, etc. come from the reusable schema — not from the class itself. Setting `isTemplate: true` on these fields causes them to be incorrectly added to the class's own field list instead of being inherited from the schema. Only the class's own fields (those not part of a reusable schema) should use `isTemplate: true`.
