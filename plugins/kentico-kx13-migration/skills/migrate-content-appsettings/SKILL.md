---
name: "migrate-content-appsettings"
description: "Generates appsettings.json for the Kentico Migration Tool CLI. Use when configuring migration settings, ConvertClassesToContentHub, EntityConfigurations, FieldMigrations, or QuerySourceInstanceApi."
argument-hint: "[migration-plan-path]"
compatibility: "Requires dotnet CLI, filesystem access, and optionally sqlcmd for resolving plan gaps."
---

# Appsettings.json Configuration Generation

Produces a complete, valid appsettings.json configuration file for the Kentico Migration Tool CLI, plus a markdown summary explaining each setting. Takes the migration plan output from the migrate-content-plan skill — or a direct text description — as input.

## Workflow

### Step 1: Discover Instance Configuration (optional)

If the user provides paths to KX13/XbyK projects, or if such projects can be found in the workspace, discover infrastructure values automatically instead of using placeholders. If no projects are available, skip this step entirely and use `<PLACEHOLDER>` values (the default behavior).

Follow the detailed discovery procedures in `references/infrastructure-discovery.md` to locate KX13/XbyK projects, extract connection strings, CMS root paths, SourceInstanceUri, framework type, and NuGet version compatibility. If `QuerySourceInstanceApi` will be needed (determined in Step 4), also prepare ToolApiController deployment per `references/toolapi-deployment-reference.md`.

### Step 2: Read Core Reference Materials

- Read `references/appsettings-reference.md` for the complete per-setting schema documentation, types, constraints, and examples.
- Read `assets/APPSETTINGS_EXAMPLE.json` for a concrete MedioClinic example showing all content-related settings in context.
- If a Kentico documentation lookup tool is available, use it for additional context on configuration options or migration tool behavior.
- If you need context on the migration tool's extension points or configuration, read `../_shared/references/migration-tool.md`.
- If you need documentation links, read `../_shared/references/migration-docs.md`.

**Load on demand (only when needed):**

- Read `references/toolapi-deployment-reference.md` only when QuerySourceInstanceApi is applicable (Step 1c or Step 4).
- Read `references/cli-parameters-reference.md` when determining CLI execution order for the summary (Step 7).
- Read `references/documentation-links.md` when the user needs pointers to official Kentico documentation.

### Step 3: Analyze Input

- If a migration plan file path is provided → read it and extract from these sections:
  - **Project** section → site names (needed for `AssetRootFolders` keys and `Connections` URI)
  - **Convert to Content Hub** → `ConvertClassesToContentHub`
  - **Convert to Reusable Field Schema** → `CreateReusableFieldSchemaForClasses`
  - **Exclusions** → `EntityConfigurations.CMS_Class.ExcludeCodeNames`
  - **Module Classes** with display name patterns → `CustomModuleClassDisplayNamePatterns`
  - **Media Link Fields** / custom form control conversions → `OptInFeatures.CustomMigration.FieldMigrations`
  - **SEO/metadata fields** → `IncludeExtendedMetadata`
  - **Source Instance API Discovery** → `OptInFeatures.QuerySourceInstanceApi`
  - **Execution Plan** → cross-reference for completeness
- If a direct text description is provided → identify the same elements from free text.
- **Cross-reference exclusions:** After extracting `EntityConfigurations.CMS_Class.ExcludeCodeNames` from the plan, verify the list is complete — every class listed in the plan's "Exclusions" section must appear in `ExcludeCodeNames`. If the plan has target-only content types, verify those types are NOT in `ExcludeCodeNames` (they need to exist in XbyK, not be excluded).
- **Cross-reference class inheritance:** Check the plan's class inheritance hierarchy (or the KX13 audit data for `ClassInheritsFromClassID`). If any class in `ExcludeCodeNames` is a **parent class** of a class that IS being migrated, **remove it from `ExcludeCodeNames`** — the migration tool requires the full inheritance chain to resolve child classes during `--page-types`. Excluding a parent class causes all its descendants to fail with "missing dependency ClassInheritsFromClassID". Parent classes with 0 instances are safe to keep — they produce empty content type definitions with no content items.
- **Cross-reference module classes:** If the plan mentions module classes in `ConvertClassesToContentHub`, verify that `CustomModuleClassDisplayNamePatterns` has a corresponding entry for each.
- **Cross-check CLI steps:** If the migration plan's execution plan includes `--custom-modules`, ensure appsettings includes `ConvertClassesToContentHub` entries for any module classes being converted. Flag any CLI steps in the plan that have no corresponding configuration.
- Ask clarifying questions for missing critical info (site names, whether Source Instance API Discovery is needed, source instance URL).

### Step 4: Determine Applicable Settings

Evaluate each of the 9 content-related settings from the reference against the input. **Content-only focus** — omit `MigrateOnlyMediaFileInfo`, `MigrateMediaToMediaLibrary`, `MemberIncludeUserSystemFields`, `UseOmActivityNodeRelationAutofix`, `UseOmActivitySiteRelationAutofix`, `CommerceConfiguration` unless explicitly requested.

1. **ConvertClassesToContentHub** — Are any classes marked for Content Hub conversion? For custom tables (`ClassIsCustomTable=1`) that also have a coded `IClassMapping`, do NOT include them here — the `IClassMapping` handles the conversion and including the class would cause a duplicate mapping error from `EnsureSettings()`.
2. **CreateReusableFieldSchemaForClasses** — Are any classes using config-based reusable field schemas (not code-based `ReusableSchemaBuilder`)?
3. **CustomModuleClassDisplayNamePatterns** — Are any module classes being converted? They need display name patterns.
4. **IncludeExtendedMetadata** — Does the plan mention SEO metadata fields (DocumentPageTitle, DocumentPageDescription, DocumentPageKeywords)?
5. **EntityConfigurations** — Are any classes explicitly excluded from migration?
6. **QuerySourceInstanceApi** — Does the site use Page Builder widgets? Is API Discovery recommended? **If the KX13 source instance is not running or cannot be started** (e.g., no access to the server, database-only migration), set `Enabled: false` — the migration tool will fall back to legacy widget migration mode. Include a comment in the summary noting this limitation and that API Discovery can be re-enabled later if the KX13 instance becomes available.
7. **FieldMigrations** — Are there media selection fields that need conversion to content item assets?
8. **AssetRootFolders** — Are media files being migrated as content item assets?
9. **TargetWorkspaceName** — Is a non-default workspace specified?

**Always include (infrastructure):**

 1. **MigrationProtocolPath** — Always include this setting. It specifies the file path where the migration tool writes a structured protocol log (the tool inserts a timestamp into the filename automatically, e.g., `protocol20240115_1430.txt`). The parent directory is auto-created by the tool if it does not exist. This protocol log is **required by the migrate-content-eval skill** to evaluate migration results across multiple categories (page types, widgets, users, modules, totals). Without it, post-migration evaluation cannot be performed. Construct the value as an absolute path under the **migration workspace root** — the common parent directory that contains the KX13 source, XbyK target, and migration tool projects as siblings: `<WorkspaceRoot>/MigrationProtocol/protocol.txt`. This keeps the protocol log independent of any single project and accessible to all skills that consume it. When infrastructure discovery (Step 1) identifies the workspace layout, resolve the workspace root and construct the absolute path automatically. **Note:** this setting is marked `[Obsolete]` in the migration tool source in favor of standard logging, but remains functional and is required for protocol-based evaluation.

Only include settings that have actual values. Omit settings that are not relevant — no empty arrays, empty objects, or placeholder-only values.

Always include infrastructure settings (including `MigrationProtocolPath`) — with discovered values from Step 1 when available, or with placeholder values otherwise.

If `QuerySourceInstanceApi` is determined to be applicable and Step 1c prepared ToolApiController deployment, proceed with the deployment now:

- Copy the controller file to the KX13 project's `Controllers` folder with the generated secret
- Register the `ToolExtendedFeatures` route in the appropriate file (see `references/toolapi-deployment-reference.md`)

### Step 5: Generate appsettings.json

Produce a complete JSON configuration:

- Structure: `{ "Logging": {...}, "Settings": { infrastructure + applicable content settings } }`
- **Valid JSON only** — no trailing commas, no comments in the JSON output
- Wrap in a fenced code block
- Provide a separate markdown summary alongside the JSON explaining:
  - Each setting included and why (trace back to migration plan section)
  - Settings evaluated but omitted and why
  - Values requiring user input (only for settings not discovered in Step 1)
  - Companion code extensions needed (reference only — point to the migrate-content-classes skill, do not generate C# code)
  - CLI parameter execution order relevant to this configuration

### Step 6: Validate Generated Configuration

Before presenting to the user, run every check in `references/validation-checklist.md` against the generated JSON. If any check fails, fix the issue and re-validate before proceeding.

### Step 7: Present and Refine

- Save to user-specified path (default: `Migration.Tool.CLI/appsettings.json` — this file belongs in the Migration Tool CLI project directory)
- Present a summary table:

  | Setting                      | Value / Purpose | Source                                         |
  | ---------------------------- | --------------- | ---------------------------------------------- |
  | `ConvertClassesToContentHub` | 6 classes       | Migration plan: Convert to Content Hub section |
  | ...                          | ...             | ...                                            |

- When infrastructure values were discovered from workspace projects, add a **Discovered Values** table showing each value and its source file:

  | Setting              | Discovered Value          | Source File                         |
  | -------------------- | ------------------------- | ----------------------------------- |
  | `KxConnectionString` | `Data Source=...`         | `KX13/MedioClinic/appsettings.json` |
  | `KxCmsDirPath`       | `C:\...\KX13\CMS`         | Sibling of MVC project              |
  | `SourceInstanceUri`  | `http://localhost:25300/` | `Properties/launchSettings.json`    |
  | ...                  | ...                       | ...                                 |

- When ToolApiController was deployed, add a **ToolApiController Deployment** section noting:
  - Files created/modified (controller file path, Startup.cs/RouteConfig.cs)
  - Reminder to build and start the KX13 instance before running `--pages`
  - How to test: `POST http://localhost:<PORT>/ToolApi/Test` with `{"secret": "..."}` → `{"pong": true}`

- Note companion code extensions the user will need (point to migrate-content-classes skill)
- List CLI parameters needed to use this configuration (e.g., `--sites`, `--page-types`, `--pages`, `--custom-modules`) with their dependency order
- Iterate on feedback

## Rules

- **Content-only focus** — omit `MigrateOnlyMediaFileInfo`, `MigrateMediaToMediaLibrary`, `MemberIncludeUserSystemFields`, `UseOmActivityNodeRelationAutofix`, `UseOmActivitySiteRelationAutofix`, `CommerceConfiguration` unless explicitly requested.
- **Valid JSON only** — no trailing commas, no comments in the JSON output.
- Only include settings that have actual values. Omit settings that are not relevant — no empty arrays, empty objects, or placeholder-only values.
- Always include infrastructure settings — with discovered values from Step 1 when available, or with placeholder values otherwise.
- Handle both structured (migration plan) and free-text input.
- Do not generate C# code — only generate JSON configuration. Point to the migrate-content-classes skill for companion code extensions.
- If a Kentico documentation lookup tool is available, verify uncertain configuration options before generating output.
- This skill generates appsettings.json configuration only — `IClassMapping`, `ContentItemDirectorBase`, `IFieldMigration`, `IWidgetMigration`, and `IWidgetPropertyMigration` code are separate extension points covered by other skills.

## Gotchas

- **Semicolon-separated strings, not arrays** — `ConvertClassesToContentHub` and `CreateReusableFieldSchemaForClasses` must be semicolon-separated strings. The migration tool's `ToolConfiguration` class binds these as a single string and splits on semicolons. **JSON arrays cause a runtime binding error.** Example: `"MedioClinic.Doctor;MedioClinic.CompanyService"`.
- **`ConvertClassesToContentHub` + coded `IClassMapping` conflict (custom tables only)** — `ClassMappingProvider.EnsureSettings()` auto-generates a `MultiClassMapping` only for classes in `ConvertClassesToContentHub` that are **custom tables** (`ClassIsCustomTable=1`). Page types in `ConvertClassesToContentHub` are skipped by `EnsureSettings()` and do not trigger auto-mapping — so page types with coded `IClassMapping` registrations will not conflict. The conflict occurs only when a **custom table** class appears in both `ConvertClassesToContentHub` and has a coded `IClassMapping`: `AppendConfiguredMapping()` throws `InvalidOperationException("Duplicate class mapping '...' (check configuration 'ConvertClassesToContentHub')")`. **Resolution:** Do NOT include a custom table class in `ConvertClassesToContentHub` if it also has a coded `IClassMapping`. The `IClassMapping` alone is sufficient — it already controls the target class type (reusable vs. webpage) and field mapping. Omitting the class from `ConvertClassesToContentHub` prevents `EnsureSettings()` from generating the conflicting auto-mapping.
- **`ConvertClassesToContentHub` is Content Hub only** — Only include classes becoming reusable content items, not webpage content types.
- **`CreateReusableFieldSchemaForClasses` vs `ReusableSchemaBuilder` conflict** — Cannot overlap with classes using code-based `ReusableSchemaBuilder` in extensions. Check the migration plan's Code Extensions section.
- **SourceInstanceUri must be verified** — When `QuerySourceInstanceApi.Enabled` is `true`, always ask the user to confirm the discovered URI before generating the final appsettings.json. A wrong URL causes a connection refused error during `--pages`.
- **Class inheritance breaks exclusions** — KX13 page types can inherit from parent classes via `ClassInheritsFromClassID`. If a parent class is excluded, all child classes fail during `--page-types` with "missing dependency ClassInheritsFromClassID". Always check the migration plan's class inheritance hierarchy before finalizing `ExcludeCodeNames`. Parent classes with 0 page instances are harmless to include — they create empty content type definitions.
- **Never change `KxCmsDirPath` without asking** — The standard `KxCmsDirPath` is the KX13 CMS root directory. If that directory lacks `media/`/`files/` subfolders, **always present the user with the choice** between keeping the CMS root (they may plan to copy media files into it later) or switching to an alternative path where media files exist. Never silently override `KxCmsDirPath` to a non-standard directory — this is the user's decision.
