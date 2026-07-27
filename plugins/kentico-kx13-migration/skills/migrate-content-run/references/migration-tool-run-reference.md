# Migration Tool CLI Execution Reference

## CLI Usage

The Kentico Migration Tool is a .NET console application run via `dotnet run` or as a published executable.

### Running from Source (Development)

From the `Migration.Tool.CLI` project directory:

```bash
dotnet run -- migrate --sites
dotnet run -- migrate --pages
dotnet run -- migrate --pages --bypass-dependency-check
```

The `--` separator is required between `dotnet run` arguments and migration tool arguments. The `migrate` verb is required before any parameter flags.

### Running as Published Executable

```bash
./Kentico.Migration.Tool migrate --sites
Kentico.Migration.Tool.exe migrate --sites
```

### Common CLI Parameters

| Parameter                   | Purpose                                                       | Typical Duration                          |
| --------------------------- | ------------------------------------------------------------- | ----------------------------------------- |
| `--sites`                   | Creates website channels from KX13 sites                      | Seconds                                   |
| `--custom-modules`          | Migrates custom module classes and their data                 | Seconds to minutes                        |
| `--custom-tables`           | Migrates custom tables                                        | Seconds to minutes                        |
| `--users`                   | Migrates Editor+ users and roles                              | Seconds                                   |
| `--members`                 | Migrates external users as Members                            | Seconds to minutes                        |
| `--settings-keys`           | Migrates supported setting values                             | Seconds                                   |
| `--page-types`              | Creates content type definitions in XbyK (runs IClassMapping) | Seconds to minutes                        |
| `--pages`                   | Migrates page content, Page Builder data, attachments         | Minutes (depends on volume)               |
| `--type-restrictions`       | Migrates content type restrictions                            | Seconds                                   |
| `--categories`              | Migrates categories as taxonomies                             | Seconds to minutes                        |
| `--media-libraries`         | Migrates media library files as content item assets           | Minutes (depends on file count/size)      |
| `--forms`                   | Migrates online forms                                         | Seconds                                   |
| `--contact-management`      | Migrates contacts, activities                                 | Minutes (can be slow with large datasets) |
| `--data-protection`         | Migrates consent agreements                                   | Seconds                                   |
| `--bypass-dependency-check` | Skips dependency validation (use on re-runs)                  | —                                         |

### Parameter Dependencies

```text
--sites (none)
  └── --custom-modules (--sites)
  │     └── --users (--sites, --custom-modules)
  │     └── --media-libraries (--sites, --custom-modules, --users)
  │     └── --forms (--sites, --custom-modules, --users)
  └── --page-types (--sites)
  │     └── --pages (--sites, --users, --page-types)
  │           └── --type-restrictions (--sites, --page-types, --pages)
  │           └── --categories (--sites, --users, --page-types, --pages)
  └── --settings-keys (--sites)
```

---

## Output Patterns

### Success Indicators

- `Handled {N} {ObjectType}` — objects successfully migrated
- `Finished {step}` — step completed
- `completed successfully` — overall success
- `No items to migrate` — step completed but nothing to process (OK)

### Warning Indicators

- `WARN` — non-fatal issue
- `skipped` — item was skipped (may be expected per EntityConfigurations)
- `not found` — referenced item doesn't exist (may indicate missing dependency step)
- `already exists` — item was previously migrated (idempotent re-run)

### Failure Indicators

- `FAIL` / `fail:` — specific item migration failed
- `ERROR` / `error:` — step-level error
- `Exception` — unhandled exception (usually configuration or code bug)
- `CRITICAL` — critical error, migration cannot continue

---

## Common Error Patterns and Troubleshooting

### 1. ConvertClassesToContentHub Binding Error

**Error:** `System.InvalidOperationException` during startup, related to configuration binding of `ConvertClassesToContentHub` or `CreateReusableFieldSchemaForClasses`.

**Cause:** These settings are defined as JSON arrays `[...]` in appsettings.json, but the `ToolConfiguration` class expects a semicolon-separated string.

**Fix:** Change from:

```json
"ConvertClassesToContentHub": [
  "Namespace.Class1",
  "Namespace.Class2"
]
```

To:

```json
"ConvertClassesToContentHub": "Namespace.Class1;Namespace.Class2"
```

Same fix applies to `CreateReusableFieldSchemaForClasses`.

### 2. GUID Parse Error (TODO Placeholders)

**Error:** `System.FormatException: Guid should contain 32 digits with 4 dashes` or `Unrecognized Guid format` during `--pages` or `--page-types`.

**Cause:** Code extensions contain `TODO` placeholder strings in positions where GUIDs are expected (e.g., `TaxonomyGroup` settings, taxonomy tag lookups).

**Fix:** Replace all TODO-placeholder strings in GUID positions with `Guid.Empty`:

```csharp
// Before (BREAKS):
f.Settings["TaxonomyGroup"] = "TODO: query XbyK for taxonomy GUID";

// After (SAFE default):
f.Settings["TaxonomyGroup"] = Guid.Empty.ToString(); // TODO: Replace with actual taxonomy GUID from XbyK
```

Rebuild after fixing.

### 3. Duplicate Mapping Conflict

**Error:** `InvalidOperationException: Duplicate class mapping for 'Namespace.ClassName'` during `--page-types`.

**Cause:** The migration tool's `ClassMappingProvider.EnsureSettings()` auto-generates mappings for classes listed in `ConvertClassesToContentHub`. If the same class also has a coded `IClassMapping` registration, there's a duplicate.

**Fix options:**

1. Patch `ClassMappingProvider.EnsureSettings()` to skip auto-mapping for classes that already have a coded `IClassMapping`
2. Remove the class from `ConvertClassesToContentHub` if the `IClassMapping` handles the conversion fully (ensure the mapping's `ConfigureTarget` sets `ClassContentTypeType = "Reusable"`)

### 4. NuGet Package Version Mismatch

**Error:** `System.InvalidOperationException` at startup mentioning assembly version mismatch, or `Kentico.Xperience.WebApp` version conflict, or hotfix/upgrade errors on database operations.

**Cause:** The Kentico NuGet packages in the migration tool project don't match the target XbyK database version.

**Fix:**

1. Query XbyK DB version: `SELECT KeyValue FROM CMS_SettingsKey WHERE KeyName = 'CMSDBVersion'`
2. Update NuGet packages: `dotnet add package Kentico.Xperience.WebApp --version {DB_VERSION}`
3. Rebuild the project

### 5. QuerySourceInstanceApi Connection Failure

**Error:** `HttpRequestException: Connection refused` or `No connection could be made because the target machine actively refused it` during `--pages`.

**Cause:** `QuerySourceInstanceApi` is enabled in appsettings.json but the KX13 source instance is not running.

**Fix:** Either:

1. Start the KX13 instance and verify the ToolApiController endpoint responds
2. Disable API Discovery in appsettings.json:

```json
"QuerySourceInstanceApi": {
  "Enabled": false
}
```

The migration tool will fall back to legacy widget migration mode. Built-in widgets still migrate correctly; custom widgets use their `IWidgetMigration` implementations.

### 6. Foreign Key Constraint Violation

**Error:** `SqlException: FOREIGN KEY constraint` during `--pages`.

**Cause:** A dependency step was not run. Most commonly, `--users` was skipped before `--pages` (pages reference users as authors).

**Fix:** Run the missing dependency step first, then re-run with `--bypass-dependency-check`.

### 7. Missing Source Media Files

**Error:** `WARN: Source file not found: {path}` during `--media-libraries`.

**Cause:** The media file binary exists in the KX13 database but not on the file system at the path specified by `KxCmsDirPath`.

**Fix:** This is typically non-fatal — the file record is skipped. Verify:

1. `KxCmsDirPath` points to the correct directory
2. The missing files are genuinely absent (deleted from source) vs. relocated

### 8. Widget Migration NullReferenceException

**Error:** `NullReferenceException` in a custom `IWidgetMigration` or `IWidgetPropertyMigration` during `--pages`.

**Cause:** Widget property expected by the migration code is missing or null in the source Page Builder JSON data.

**Fix:** Add null checks in the migration code:

```csharp
// Before:
var text = (string)properties["text"]!;

// After:
var text = properties["text"]?.Value<string>() ?? string.Empty;
```

Rebuild after fixing.

### 9. Timeout on Large Datasets

**Error:** `SqlException: Timeout expired` during `--pages`, `--media-libraries`, or `--contact-management`.

**Cause:** Large number of records or slow database connection.

**Fix:** Increase `Connect Timeout` in both connection strings in appsettings.json:

```text
Connect Timeout=120
```

### 10. EnsureSettings Auto-Mapping Override

**Error:** The migration tool creates content type fields that don't match the `IClassMapping` definition, or creates unexpected content types.

**Cause:** `EnsureSettings()` auto-processes classes that appear in `ConvertClassesToContentHub` even when they have coded `IClassMapping` extensions.

**Fix:** The `IClassMapping` registration must match the class configuration exactly. If the `IClassMapping` targets a different class name than the source, ensure both the source and target are correctly listed/excluded in settings.

### 11. Duplicate Class Mapping for ConvertClassesToContentHub + IClassMapping

**Error:** `InvalidOperationException: Duplicate class mapping for 'Namespace.ClassName'` during `--custom-modules` or `--page-types`.

**Cause:** A class listed in `ConvertClassesToContentHub` also has a coded `IClassMapping` registration. The migration tool's `ClassMappingProvider.EnsureSettings()` auto-generates a mapping for every class in `ConvertClassesToContentHub`, which conflicts with the coded registration.

**Fix:** Patch `ClassMappingProvider.EnsureSettings()` to skip auto-generating mappings for classes that already have a coded `IClassMapping`. Add a guard before the auto-mapping logic:

```csharp
// Skip auto-mapping for classes that have coded IClassMapping registrations
if (classMappings.Any(m => m.SourceClassNames.Contains(className)))
    continue;
```

The class must remain in `ConvertClassesToContentHub` — removing it causes Content Hub processing to fail. The patch prevents only the duplicate auto-mapping.

### 12. NullReferenceException in WithFieldPatch after ConvertFrom

**Error:** `NullReferenceException` in a `WithFieldPatch` lambda during `--page-types`, typically at a line like `f.Caption = "..."` or `f.Settings[...] = ...`.

**Cause:** `ConvertFrom` with `includeDefinition: false` doesn't carry a source field definition, so when `ClassMappingProvider.ExecuteMappings()` calls `GetFormField()` for the field, it returns `null`. This null `FormFieldInfo` is passed directly to the `WithFieldPatch` callback.

**Fix:** Either:

1. Use `ConvertFrom(includeDefinition: true)` instead, which carries the source field definition and gives `WithFieldPatch` something to work with
2. Use `WithFactory` to create the field definition from scratch, combined with `ConvertFrom` for the value transformation
3. Patch `ClassMappingProvider.ExecuteMappings()` to create a basic `FormFieldInfo` when `GetFormField()` returns null, so `WithFieldPatch` always receives a non-null object

---

## Log File Location

The migration tool writes detailed logs to:

- **Default path:** `logs/log.txt` (relative to the CLI project directory)
- **Configured in:** `appsettings.json` → `Logging.pathFormat`

The log file contains more detail than console output is useful for diagnosing intermittent failures or tracing specific item migrations.

---

## Re-Run Considerations

- All commands are **append-mode** — they create new items or update existing ones, they don't delete
- Use `--bypass-dependency-check` when re-running a step after fixing an error (dependencies already completed)
- Re-running `--page-types` after code changes updates content type definitions
- Re-running `--pages` re-processes all pages (existing items may be updated)
- Re-running `--media-libraries` skips already-imported files (checks by GUID)
