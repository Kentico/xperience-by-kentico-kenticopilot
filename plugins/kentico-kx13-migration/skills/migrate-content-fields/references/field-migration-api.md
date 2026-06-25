# Field Migration API Reference

This document covers the complete API for creating custom field migrations (`IFieldMigration`) in the Kentico Migration Tool. Field migrations transform individual field values and definitions globally across all classes that share a form control, data type, or field pattern.

**Source:** [SampleTextMigration.cs](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool/blob/master/Migration.Tool.Extensions/CommunityMigrations/SampleTextMigration.cs), [AssetMigration.cs](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool/blob/master/Migration.Tool.Extensions/CommunityMigrations/AssetMigration.cs), [Migration Tool CLI README](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool/blob/master/Migration.Tool.CLI/README.md)

---

## Required Namespaces

```csharp
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using Migration.Tool.Common.Enumerations;           // SourceObjectContext
using Migration.Tool.KXP.Api.Services.CmsClass;     // IFieldMigration, FieldMigrationContext
```

---

## 1. IFieldMigration Interface

```csharp
public interface IFieldMigration
{
    int Rank { get; }

    bool ShallMigrate(FieldMigrationContext context);

    void MigrateFieldDefinition(
        FormDefinitionPatcher formDefinitionPatcher,
        XElement field,
        XAttribute? columnTypeAttr,
        string fieldDescriptor);

    void MigrateValue(
        object? sourceValue,
        FieldMigrationContext context,
        out object? targetValue);
}
```

### Members

| Member | Purpose |
|---|---|
| `Rank` | Priority — lower value wins. Custom migrations should use rank **< 100,000**; built-in defaults use 100,000+. |
| `ShallMigrate` | Returns `true` if this migration handles the given field. Called for every field — must be lightweight. |
| `MigrateFieldDefinition` | Transforms the XML field definition (form control, data type, column type, settings). |
| `MigrateValue` | Transforms the field value. Uses `out` parameter for the result. |

---

## 2. FieldMigrationContext

Properties available on the context object passed to `ShallMigrate` and `MigrateValue`:

| Property | Type | Description |
|---|---|---|
| `SourceDataType` | `string` | KX13 field data type (e.g., `"text"`, `"longtext"`, `"integer"`, `"datetime"`) |
| `SourceFormControl` | `string` | KX13 form control name (e.g., `"TextBoxControl"`, `"HtmlAreaControl"`, `"DropDownListControl"`) |
| `FieldName` | `string` | Field code name |
| `ClassName` | `string` | Full class name (e.g., `"MedioClinic.Doctor"`) |
| `SourceObjectContext` | `SourceObjectContext` | Context in which the field is being migrated |

---

## 3. SourceObjectContext Enum

| Value | When it applies |
|---|---|
| `TreeNode` | Page type fields — migrated during `--pages` |
| `CustomTable` | Custom table fields — migrated during `--custom-tables` |
| `Form` | Form fields — migrated during `--forms` |

Use this to apply different transformation logic depending on the source object type.

---

## 4. Rank System

The rank determines which `IFieldMigration` handles a given field when multiple migrations match via `ShallMigrate`. **Lower rank wins.**

| Range | Usage |
|---|---|
| < 100,000 | Custom migrations (your code) — runs first |
| >= 100,000 | Built-in defaults — runs as fallback |

Convention: Use ranks like `1000`, `2000`, `3000` for custom migrations. Leave gaps for future insertions.

---

## 5. ShallMigrate Patterns

`ShallMigrate` determines whether this migration handles a given field. Keep it lightweight — it's called for every field in every class.

### Match by form control name

```csharp
public bool ShallMigrate(FieldMigrationContext context)
    => context.SourceFormControl.Equals("CommunityTextEditor", StringComparison.OrdinalIgnoreCase);
```

### Match by data type

```csharp
public bool ShallMigrate(FieldMigrationContext context)
    => context.SourceDataType.Equals("longtext", StringComparison.OrdinalIgnoreCase);
```

### Match by field name + form control

```csharp
public bool ShallMigrate(FieldMigrationContext context)
    => context.FieldName.Equals("EventDateText", StringComparison.OrdinalIgnoreCase)
    && context.SourceFormControl.Equals("TextBoxControl", StringComparison.OrdinalIgnoreCase);
```

### Match by class name

```csharp
public bool ShallMigrate(FieldMigrationContext context)
    => context.ClassName.Equals("MedioClinic.Doctor", StringComparison.OrdinalIgnoreCase)
    && context.SourceFormControl.Equals("CustomDropDown", StringComparison.OrdinalIgnoreCase);
```

**Important:** Be specific in matching. Broad matches (e.g., matching all `"text"` fields) can interfere with built-in migrations.

---

## 6. MigrateFieldDefinition Patterns

`MigrateFieldDefinition` transforms the XML field definition using `System.Xml.Linq`. The `field` parameter is an `XElement` representing the field's XML definition.

### Change form control (controlname)

```csharp
public void MigrateFieldDefinition(
    FormDefinitionPatcher formDefinitionPatcher,
    XElement field,
    XAttribute? columnTypeAttr,
    string fieldDescriptor)
{
    // Find or create the settings element
    var settings = field.Element("settings") ?? new XElement("settings");
    if (settings.Parent == null) field.Add(settings);

    // Change form control
    var controlName = settings.Element("controlname");
    if (controlName != null)
        controlName.Value = "Kentico.Administration.RichTextEditor";
    else
        settings.Add(new XElement("controlname", "Kentico.Administration.RichTextEditor"));
}
```

### Change column type / data type

```csharp
// Change the column type attribute
if (columnTypeAttr != null)
    columnTypeAttr.Value = "datetime";

// Change the data type in the field element
var dataType = field.Attribute("datatype");
if (dataType != null)
    dataType.Value = "datetime";
```

### Clear settings hashtable entries

```csharp
var settings = field.Element("settings");
if (settings != null)
{
    // Remove specific settings
    settings.Element("MaxLength")?.Remove();
    settings.Element("Size")?.Remove();

    // Or replace all settings
    settings.RemoveAll();
    settings.Add(new XElement("controlname", "Kentico.Administration.RichTextEditor"));
}
```

### Pass-through (no definition change)

When only the value needs transformation and the field definition stays the same:

```csharp
public void MigrateFieldDefinition(
    FormDefinitionPatcher formDefinitionPatcher,
    XElement field,
    XAttribute? columnTypeAttr,
    string fieldDescriptor)
{
    // No definition changes needed — value-only migration
}
```

---

## 7. MigrateValue Patterns

`MigrateValue` transforms the actual field value. The result is returned via the `out` parameter.

### Basic null-safe transformation

```csharp
public void MigrateValue(
    object? sourceValue,
    FieldMigrationContext context,
    out object? targetValue)
{
    if (sourceValue is null or DBNull)
    {
        targetValue = null;
        return;
    }

    if (sourceValue is string s)
    {
        targetValue = TransformString(s);
        return;
    }

    // Pass through unexpected types
    targetValue = sourceValue;
}
```

### Context-specific behavior

```csharp
public void MigrateValue(
    object? sourceValue,
    FieldMigrationContext context,
    out object? targetValue)
{
    if (sourceValue is null or DBNull)
    {
        targetValue = null;
        return;
    }

    targetValue = context.SourceObjectContext switch
    {
        SourceObjectContext.TreeNode => TransformForPages(sourceValue),
        SourceObjectContext.CustomTable => TransformForCustomTables(sourceValue),
        SourceObjectContext.Form => TransformForForms(sourceValue),
        _ => sourceValue
    };
}
```

### Data type conversion

```csharp
public void MigrateValue(
    object? sourceValue,
    FieldMigrationContext context,
    out object? targetValue)
{
    if (sourceValue is null or DBNull)
    {
        targetValue = null;
        return;
    }

    if (sourceValue is string s && DateTime.TryParse(s, out var dt))
    {
        targetValue = dt;
        return;
    }

    targetValue = sourceValue;
}
```

---

## 8. TcaDirective / TfcDirective / SfcDirective Constants

The migration tool uses directive constants for matching and transformation configuration:

| Constant Type | Purpose | Example |
|---|---|---|
| `TcaDirective` | Target control action — what to do with the field | `TcaDirective.ConvertToRichText`, `TcaDirective.Clear` |
| `TfcDirective` | Target form component — which XbyK form component to use | `TfcDirective.RichTextEditor`, `TfcDirective.TextInput` |
| `SfcDirective` | Source form control — matching KX13 form controls | `SfcDirective.HtmlArea`, `SfcDirective.TextBox` |

These are used internally by the built-in field migrations. Custom `IFieldMigration` implementations typically match directly on `FieldMigrationContext` properties rather than using these constants.

---

## 9. Service Registration

Every `IFieldMigration` must be registered as a singleton in the DI container.

### Per-migration extension method pattern

```csharp
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using Migration.Tool.Common.Enumerations;
using Migration.Tool.KXP.Api.Services.CmsClass;

namespace Migration.Tool.Extensions.FieldMigrations;

public class CommunityTextEditorFieldMigration : IFieldMigration
{
    // ... implementation ...

    public static IServiceCollection AddCommunityTextEditorMigration(
        this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IFieldMigration>(new CommunityTextEditorFieldMigration());
        return serviceCollection;
    }
}
```

### Central registration in ServiceCollectionExtensions

```csharp
using Migration.Tool.Extensions.FieldMigrations;

namespace Migration.Tool.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection UseCustomizations(this IServiceCollection services)
    {
        // IFieldMigration registrations
        services.AddCommunityTextEditorMigration();
        services.AddDateTextMigration();
        services.AddHtmlCleanupMigration();
        // ... all other field migrations ...

        // Note: fields handled by these IFieldMigration implementations
        // do NOT need entries in appsettings.json FieldMigrations config

        return services;
    }
}
```

---

## 10. Execution Order

`IFieldMigration` runs during:

- `--pages` — for page type fields (SourceObjectContext.TreeNode)
- `--forms` — for form fields (SourceObjectContext.Form)
- `--custom-tables` — for custom table fields (SourceObjectContext.CustomTable)

**Execution sequence within a migration run:**

1. `IClassMapping` creates target content type structure
2. **`IFieldMigration`** transforms field definitions and values
3. `ContentItemDirectorBase` handles page relationships and content items
4. `IWidgetMigration` / `IWidgetPropertyMigration` transform widgets

For each field, the tool iterates all registered `IFieldMigration` implementations sorted by `Rank` (ascending). The first one where `ShallMigrate` returns `true` handles the field.

---

## 11. Relationship with appsettings FieldMigrations

The `OptInFeatures.CustomMigration.FieldMigrations` config in `appsettings.json` provides **configuration-based** field migrations — simple form control swaps without custom logic:

```json
{
    "OptInFeatures": {
        "CustomMigration": {
            "FieldMigrations": [
                {
                    "SourceDataType": "text",
                    "SourceFormControl": "TextBoxControl",
                    "TargetFormComponent": "Kentico.Administration.TextInput"
                }
            ]
        }
    }
}
```

**When to use config vs code:**

| Scenario | Use |
|---|---|
| Simple form control swap, no value change | `appsettings.json` FieldMigrations |
| Custom value transformation | Code: `IFieldMigration` |
| HTML cleanup / sanitization | Code: `IFieldMigration` |
| Conditional logic based on context | Code: `IFieldMigration` |
| Data type change with value conversion | Code: `IFieldMigration` |

Fields handled by `IFieldMigration` code do **not** need entries in the appsettings FieldMigrations config.

---

## 12. Relationship with IClassMapping

Both `IFieldMigration` and `IClassMapping` can affect field migration, but they serve different purposes and cover different aspects:

### Value transforms: IFieldMigration.MigrateValue vs IClassMapping.ConvertFrom

| Aspect       | `IFieldMigration`                                          | `IClassMapping.ConvertFrom`                                   |
| ------------ | ---------------------------------------------------------- | ------------------------------------------------------------- |
| **Scope**    | Global — applies across all classes matching the criteria  | Per-class — applies to one specific source→target mapping     |
| **Matching** | By form control, data type, field name, class name         | By explicit source class + field name                         |
| **Best for** | Custom form controls, cross-class transforms, HTML cleanup | Class-specific value transforms (name splitting, GUID lookup) |
| **Runs on**  | Pages, forms, custom tables                                | Only the mapped class                                         |

### Definition changes: IFieldMigration.MigrateFieldDefinition vs IClassMapping.WithFieldPatch

| Aspect                      | `IFieldMigration.MigrateFieldDefinition`                  | `IClassMapping.WithFieldPatch`                                       |
| --------------------------- | --------------------------------------------------------- | -------------------------------------------------------------------- |
| **Scope**                   | Global — applies to all fields matching `ShallMigrate`    | Per-class, per-field — scoped to the class mapping                   |
| **Execution order**         | Runs first during `--page-types` form definition patching | Runs after `IFieldMigration` during `--page-types`                   |
| **API**                     | `XElement` XML manipulation                               | `FormFieldInfo` properties (`DataType`, `Settings`, `Caption`, etc.) |
| **Best for**                | Cross-class form control/data type changes                | Class-specific definition patches (e.g., one field on one class)     |
| **Also transforms values?** | Yes, via `MigrateValue`                                   | No — pair with `ConvertFrom` for value transforms                    |

### Decision guide

- If the transform applies to many classes sharing a form control → `IFieldMigration`
- If the transform is specific to one class and one field → `IClassMapping.ConvertFrom` (value) / `.WithFieldPatch` (definition)
- If the class isn't being remapped via `IClassMapping` at all → `IFieldMigration`
- If you need access to `IConvertorContext` (NodeGuid, SiteId) → `IClassMapping.ConvertFrom`
- If the field definition change is scoped to one class that already has an `IClassMapping` → `WithFieldPatch` (simpler, keeps logic co-located)

---

## 13. Annotated Code Samples

### Sample 1: Simple Form Control Replacement

**When to use:** A custom KX13 form control has no XbyK equivalent and needs replacement with a standard component.

```csharp
public class CommunityTextEditorFieldMigration : IFieldMigration
{
    public int Rank => 1000;

    public bool ShallMigrate(FieldMigrationContext context)
        => context.SourceFormControl.Equals("CommunityTextEditor", StringComparison.OrdinalIgnoreCase);

    public void MigrateFieldDefinition(
        FormDefinitionPatcher formDefinitionPatcher,
        XElement field,
        XAttribute? columnTypeAttr,
        string fieldDescriptor)
    {
        var settings = field.Element("settings") ?? new XElement("settings");
        if (settings.Parent == null) field.Add(settings);

        // Replace custom control with RichTextEditor
        var controlName = settings.Element("controlname");
        if (controlName != null)
            controlName.Value = "Kentico.Administration.RichTextEditor";
        else
            settings.Add(new XElement("controlname", "Kentico.Administration.RichTextEditor"));

        // Update column type for rich text
        if (columnTypeAttr != null)
            columnTypeAttr.Value = "longtext";
    }

    public void MigrateValue(
        object? sourceValue,
        FieldMigrationContext context,
        out object? targetValue)
    {
        if (sourceValue is null or DBNull)
        {
            targetValue = null;
            return;
        }

        if (sourceValue is string s && context.SourceObjectContext == SourceObjectContext.TreeNode)
        {
            // Wrap plain text in paragraph tags for rich text editor
            targetValue = s.StartsWith("<") ? s : $"<p>{s}</p>";
            return;
        }

        targetValue = sourceValue;
    }
}
```

### Sample 2: Data Type Conversion (text → datetime)

**When to use:** A field stores dates as text in KX13 and should become a proper datetime in XbyK.

```csharp
public class DateTextFieldMigration : IFieldMigration
{
    public int Rank => 2000;

    public bool ShallMigrate(FieldMigrationContext context)
        => context.FieldName.Equals("EventDateText", StringComparison.OrdinalIgnoreCase)
        && context.SourceFormControl.Equals("TextBoxControl", StringComparison.OrdinalIgnoreCase);

    public void MigrateFieldDefinition(
        FormDefinitionPatcher formDefinitionPatcher,
        XElement field,
        XAttribute? columnTypeAttr,
        string fieldDescriptor)
    {
        // Change column type to datetime
        if (columnTypeAttr != null)
            columnTypeAttr.Value = "datetime";

        var dataType = field.Attribute("datatype");
        if (dataType != null)
            dataType.Value = "datetime";

        // Update form control to datetime picker
        var settings = field.Element("settings") ?? new XElement("settings");
        if (settings.Parent == null) field.Add(settings);

        var controlName = settings.Element("controlname");
        if (controlName != null)
            controlName.Value = "Kentico.Administration.DateTimeInput";
        else
            settings.Add(new XElement("controlname", "Kentico.Administration.DateTimeInput"));
    }

    public void MigrateValue(
        object? sourceValue,
        FieldMigrationContext context,
        out object? targetValue)
    {
        if (sourceValue is null or DBNull)
        {
            targetValue = null;
            return;
        }

        if (sourceValue is string s && DateTime.TryParse(s, out var dt))
        {
            targetValue = dt;
            return;
        }

        // Already a DateTime or unexpected type — pass through
        targetValue = sourceValue;
    }
}
```

### Sample 3: HTML Cleanup / Sanitization

**When to use:** Legacy HTML fields contain deprecated tags that need cleaning across all content types.

```csharp
public class HtmlCleanupFieldMigration : IFieldMigration
{
    public int Rank => 3000;

    public bool ShallMigrate(FieldMigrationContext context)
        => context.SourceDataType.Equals("longtext", StringComparison.OrdinalIgnoreCase)
        && context.SourceFormControl.Equals("HtmlAreaControl", StringComparison.OrdinalIgnoreCase);

    public void MigrateFieldDefinition(
        FormDefinitionPatcher formDefinitionPatcher,
        XElement field,
        XAttribute? columnTypeAttr,
        string fieldDescriptor)
    {
        // Definition stays the same — only value cleanup needed
    }

    public void MigrateValue(
        object? sourceValue,
        FieldMigrationContext context,
        out object? targetValue)
    {
        if (sourceValue is null or DBNull)
        {
            targetValue = null;
            return;
        }

        if (sourceValue is string html)
        {
            // Remove <font> tags but keep content
            html = System.Text.RegularExpressions.Regex.Replace(
                html, @"</?font[^>]*>", string.Empty, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Replace <b> with <strong>
            html = System.Text.RegularExpressions.Regex.Replace(
                html, @"<b(\s|>)", "<strong$1", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            html = System.Text.RegularExpressions.Regex.Replace(
                html, @"</b>", "</strong>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Replace <i> with <em>
            html = System.Text.RegularExpressions.Regex.Replace(
                html, @"<i(\s|>)", "<em$1", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            html = System.Text.RegularExpressions.Regex.Replace(
                html, @"</i>", "</em>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            targetValue = html;
            return;
        }

        targetValue = sourceValue;
    }
}
```

### Sample 4: Path/URL Transformation

**When to use:** Fields contain KX13 media paths (e.g., `/getmedia/`) that need rewriting for XbyK.

```csharp
public class UrlPathFieldMigration : IFieldMigration
{
    public int Rank => 4000;

    public bool ShallMigrate(FieldMigrationContext context)
        => context.SourceFormControl.Equals("UrlSelector", StringComparison.OrdinalIgnoreCase);

    public void MigrateFieldDefinition(
        FormDefinitionPatcher formDefinitionPatcher,
        XElement field,
        XAttribute? columnTypeAttr,
        string fieldDescriptor)
    {
        // No definition change — URL fields keep the same structure
    }

    public void MigrateValue(
        object? sourceValue,
        FieldMigrationContext context,
        out object? targetValue)
    {
        if (sourceValue is null or DBNull)
        {
            targetValue = null;
            return;
        }

        if (sourceValue is string url)
        {
            // Convert KX13 /getmedia/ paths to XbyK format
            if (url.Contains("/getmedia/", StringComparison.OrdinalIgnoreCase))
            {
                // Extract GUID from /getmedia/{guid}/filename pattern
                var match = System.Text.RegularExpressions.Regex.Match(
                    url, @"/getmedia/([0-9a-fA-F-]+)/");
                if (match.Success)
                {
                    targetValue = $"/assets/{match.Groups[1].Value}";
                    return;
                }
            }

            targetValue = url;
            return;
        }

        targetValue = sourceValue;
    }
}
```

### Sample 5: Context-Specific Migration

**When to use:** The same form control needs different transformation depending on whether it's on a page, custom table, or form.

```csharp
public class CustomDropdownFieldMigration : IFieldMigration
{
    public int Rank => 5000;

    public bool ShallMigrate(FieldMigrationContext context)
        => context.SourceFormControl.Equals("CustomDropDown", StringComparison.OrdinalIgnoreCase);

    public void MigrateFieldDefinition(
        FormDefinitionPatcher formDefinitionPatcher,
        XElement field,
        XAttribute? columnTypeAttr,
        string fieldDescriptor)
    {
        var settings = field.Element("settings") ?? new XElement("settings");
        if (settings.Parent == null) field.Add(settings);

        // Replace with XbyK drop-down component
        var controlName = settings.Element("controlname");
        if (controlName != null)
            controlName.Value = "Kentico.Administration.DropDownSelector";
        else
            settings.Add(new XElement("controlname", "Kentico.Administration.DropDownSelector"));
    }

    public void MigrateValue(
        object? sourceValue,
        FieldMigrationContext context,
        out object? targetValue)
    {
        if (sourceValue is null or DBNull)
        {
            targetValue = null;
            return;
        }

        if (sourceValue is not string s)
        {
            targetValue = sourceValue;
            return;
        }

        // Different behavior per source context
        targetValue = context.SourceObjectContext switch
        {
            // Pages: convert semicolon-separated values to JSON array
            SourceObjectContext.TreeNode => ConvertToJsonArray(s),

            // Custom tables: keep as-is (already in correct format)
            SourceObjectContext.CustomTable => s,

            // Forms: convert pipe-separated to semicolon-separated
            SourceObjectContext.Form => s.Replace("|", ";"),

            _ => s
        };
    }

    private static string ConvertToJsonArray(string semicolonSeparated)
    {
        var values = semicolonSeparated
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(v => $"\"{v.Trim()}\"");
        return $"[{string.Join(",", values)}]";
    }
}
```

### Sample 6: Service Registration

```csharp
using Migration.Tool.Extensions.FieldMigrations;

namespace Migration.Tool.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection UseCustomizations(this IServiceCollection services)
    {
        // IFieldMigration registrations
        services.AddCommunityTextEditorMigration();
        services.AddDateTextMigration();
        services.AddHtmlCleanupMigration();
        services.AddUrlPathMigration();
        services.AddCustomDropdownMigration();

        // Note: fields handled by the above IFieldMigration code
        // do NOT need entries in appsettings.json FieldMigrations config.
        // Only add config entries for simple form control swaps
        // that don't require custom value transformation logic.

        return services;
    }
}
```
