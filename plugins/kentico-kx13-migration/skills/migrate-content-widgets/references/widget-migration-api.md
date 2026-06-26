# Widget Migration API Reference

This document covers the complete API for creating custom widget migrations (`IWidgetMigration`) and widget property migrations (`IWidgetPropertyMigration`) in the Kentico Migration Tool. Widget migrations transform widget type identifiers, restructure widget properties, and consolidate widgets. Property migrations transform individual property values (media GUID resolution, format conversion, content item creation).

**Source:** [SampleWidgetMigration.cs](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool/blob/master/Migration.Tool.Extensions/CommunityMigrations/SampleWidgetMigration.cs), [Migration Tool Extensions README — Customize widget migrations](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool/blob/master/Migration.Tool.Extensions/README.md#customize-widget-property-migrations), [Kentico Docs — Migrate widget data to content hub](https://docs.kentico.com/guides/upgrade-to-xbyk/upgrade-deep-dives/migrate-widget-data-to-content-hub), [Kentico Docs — Transform widget properties](https://docs.kentico.com/guides/upgrade-to-xbyk/upgrade-deep-dives/transform-widget-properties), [WidgetPathSelectorMigration.cs](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool/blob/master/Migration.Tool.Extensions/DefaultMigrations/WidgetPathSelectorMigration.cs), [WidgetPageSelectorMigration.cs](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool/blob/master/Migration.Tool.Extensions/DefaultMigrations/WidgetPageSelectorMigration.cs)

---

## Required Namespaces

```csharp
using Newtonsoft.Json.Linq;                              // JToken, JArray, JObject
using Microsoft.Extensions.DependencyInjection;          // IServiceCollection, AddTransient
using Migration.Tool.KXP.Api.Services.CmsClass;          // IWidgetMigration, IWidgetPropertyMigration,
                                                          // WidgetMigrationResult, WidgetPropertyMigrationResult,
                                                          // WidgetIdentifier, WidgetMigrationContext,
                                                          // WidgetPropertyMigrationContext
using Migration.Tool.Extensions.DefaultMigrations;       // WidgetFileMigration, WidgetPageSelectorMigration,
                                                          // WidgetPathSelectorMigration
using Migration.Tool.Common.Enumerations;                // Kx13FormComponents
using Migration.Tool.Common.Services;                    // ISpoiledGuidContext
```

For advanced patterns (content item creation during migration):

```csharp
using CMS.ContentEngine;                                 // ContentItemReference, ContentItemData,
                                                          // CreateContentItemParameters
using CMS.Core;                                          // Service.Resolve
using CMS.DataEngine;                                    // ContentItemInfo
using Microsoft.Extensions.Logging;                      // ILogger<T>
```

---

## 1. IWidgetMigration Interface

```csharp
public interface IWidgetMigration
{
    int Rank { get; }

    bool ShallMigrate(WidgetMigrationContext context, WidgetIdentifier identifier);

    Task<WidgetMigrationResult> MigrateWidget(
        WidgetIdentifier identifier,
        JToken? value,
        WidgetMigrationContext context);
}
```

### Members

| Member | Purpose |
|---|---|
| `Rank` | Priority — lower value wins. Custom migrations should use rank **< 100,000**; built-in defaults use 100,000+. |
| `ShallMigrate` | Returns `true` if this migration handles the given widget/section. Called for every widget — must be lightweight. |
| `MigrateWidget` | Transforms the widget JSON — change type identifier, restructure properties, define property migrations. Returns `Task<WidgetMigrationResult>`. |

---

## 2. WidgetIdentifier

| Property | Type | Description |
|---|---|---|
| `TypeIdentifier` | `string` | Widget or section type code name (e.g., `"MedioClinic.Widget.Text"`, `"MedioClinic.Section.SingleColumn"`) |

---

## 3. WidgetMigrationContext

| Property | Type | Description |
|---|---|---|
| `SiteId` | `int` | Source KX13 site ID — use to scope migrations to a specific site in multi-site environments |

---

## 4. WidgetMigrationResult

Constructor:

```csharp
new WidgetMigrationResult(JToken? value, Dictionary<string, Type>? propertyMigrations)
```

| Parameter | Type | Description |
|---|---|---|
| `value` | `JToken?` | The modified widget JSON (same reference passed into `MigrateWidget`, mutated in place or rebuilt) |
| `propertyMigrations` | `Dictionary<string, Type>?` | Maps property names to `IWidgetPropertyMigration` types for further per-property processing. Use `typeof(WidgetFileMigration)` for media file properties. Pass empty dictionary or `null` when no property migrations are needed. |

---

## 5. Widget JSON Structure

KX13 stores widget configurations as JSON in `CMS_Document.DocumentPageBuilderWidgets`. The migration tool passes the individual widget token to `MigrateWidget`.

```json
{
    "type": "MedioClinic.Widget.Text",
    "variants": [
        {
            "identifier": "c9cd393b-...",
            "name": null,
            "properties": {
                "text": "Hello world",
                "cssClass": "text-large"
            },
            "conditionTypeParameters": null
        }
    ]
}
```

### Access Patterns

```csharp
// Change widget type identifier
value!["type"] = "RichTextWidget";

// Access the first variant's properties
var variants = (JArray)value!["variants"]!;
var singleVariant = variants[0];
var properties = singleVariant["properties"];

// Read a property
var textValue = singleVariant["properties"]!["text"];

// Rebuild properties (new JObject)
singleVariant["properties"] = new JObject
{
    ["RichTextWidgetContent"] = singleVariant["properties"]!["text"],
};
```

---

## 6. IWidgetPropertyMigration Interface

```csharp
public interface IWidgetPropertyMigration
{
    int Rank { get; }

    bool ShallMigrate(WidgetPropertyMigrationContext context, string propertyName);

    Task<WidgetPropertyMigrationResult> MigrateWidgetProperty(
        string key,
        JToken? value,
        WidgetPropertyMigrationContext context);
}
```

### Members

| Member | Purpose |
|---|---|
| `Rank` | Priority — lower value wins. Same ranking system as `IWidgetMigration`. |
| `ShallMigrate` | Returns `true` if this migration handles the given property. `propertyName` is the JSON property key. `context` provides site ID and form component metadata. |
| `MigrateWidgetProperty` | Transforms the property value. `key` is the property name, `value` is the current JToken value. Returns `Task<WidgetPropertyMigrationResult>`. |

### WidgetPropertyMigrationContext

| Property | Type | Description |
|---|---|---|
| `SiteId` | `int` | Source site ID (accessible via deconstruction: `(int siteId, _) = context`) |
| `EditingFormControlModel?.FormComponentIdentifier` | `string?` | The form component identifier for the property (see note below) |

> **`ComponentIdentifier` does NOT exist** on `WidgetPropertyMigrationContext`. The record only has `SiteId` and `EditingFormControlModel`. To scope property transforms by widget type, use the `propertyMigrations` dictionary in `IWidgetMigration.MigrateWidget` (which is already widget-scoped) rather than attempting to filter in `IWidgetPropertyMigration.ShallMigrate`.

> **API Discovery prerequisite:** `EditingFormControlModel` is only populated when `OptInFeatures.QuerySourceInstanceApi` is enabled in appsettings.json and the `ToolApiController.cs` is deployed to the KX13 instance. Without it, the value is `null` and form-component-based matching in `ShallMigrate` will not work. Property-name-based matching still works without API Discovery **only when the property is delegated via the `propertyMigrations` dictionary from an `IWidgetMigration`**. Standalone `IWidgetPropertyMigration` classes (not delegated via `propertyMigrations`) are only invoked when `EditingFormControlModel` is non-null — if the KX13 widget property lacks an `[EditingComponent(...)]` attribute, the property migration is silently skipped.

### WidgetPropertyMigrationResult

Constructor:

```csharp
new WidgetPropertyMigrationResult(JToken? value)
```

| Parameter | Type | Description |
|---|---|---|
| `value` | `JToken?` | The transformed property value |

---

## 7. Built-in Property Migrations

These are provided by the migration tool in `Migration.Tool.Extensions.DefaultMigrations`:

| Type | Rank | Purpose | When to use |
|---|---|---|---|
| `WidgetFileMigration` | 100,000 | Migrates media file property values (media library GUID → content item asset reference) | Reference in `propertyMigrations` dictionary for media file properties |
| `WidgetPathSelectorMigration` | 100,001 | Migrates path selector property values (KX13 `NodeAliasPath` → XbyK `TreePath`). Converts `PathSelectorItem` JSON structure. | Reference in `propertyMigrations` dictionary for path selector properties |
| `WidgetPageSelectorMigration` | 100,002 | Migrates page selector property values (KX13 `PageSelectorItem` → XbyK `WebPageRelatedItem`). Uses `ISpoiledGuidContext` to resolve migrated page GUIDs. | Reference in `propertyMigrations` dictionary for page selector properties |

Reference these types in the `propertyMigrations` dictionary returned by `MigrateWidget` rather than reimplementing the conversion logic:

```csharp
var propertyMigrations = new Dictionary<string, Type>
{
    ["imageProperty"] = typeof(WidgetFileMigration),
    ["pageReference"] = typeof(WidgetPageSelectorMigration),
    ["pathProperty"] = typeof(WidgetPathSelectorMigration)
};
```

> **Overriding built-ins:** To replace a built-in's behavior (e.g., converting page selectors to `ContentItemReference` instead of `WebPageRelatedItem`), create a custom `IWidgetPropertyMigration` with a lower `Rank` that matches the same `FormComponentIdentifier`. See Pattern 9 in the example file.

### Kx13FormComponents Constants

The built-in property migrations match on form component identifiers using constants from `Migration.Tool.Common.Enumerations.Kx13FormComponents`. Use these when writing custom `ShallMigrate` methods that target specific form controls:

| Constant | Value | Used by |
|---|---|---|
| `Kx13FormComponents.Kentico_MediaFilesSelector` | `"Kentico.MediaFilesSelector"` | `WidgetFileMigration` |
| `Kx13FormComponents.Kentico_PathSelector` | `"Kentico.PathSelector"` | `WidgetPathSelectorMigration` |
| `Kx13FormComponents.Kentico_PageSelector` | `"Kentico.PageSelector"` | `WidgetPageSelectorMigration` |

---

## 8. Rank System

The rank determines which migration handles a given widget/property when multiple migrations match via `ShallMigrate`. **Lower rank wins.**

| Range | Usage |
|---|---|
| < 100,000 | Custom migrations (your code) — runs first |
| >= 100,000 | Built-in defaults — runs as fallback |

Convention: Use ranks like `1`, `2`, `3` or `10`, `20`, `30` for custom migrations. Leave gaps for future insertions.

---

## 9. ShallMigrate Patterns

### Match by widget type identifier (most common)

```csharp
public bool ShallMigrate(WidgetMigrationContext context, WidgetIdentifier identifier)
    => string.Equals("MedioClinic.Widget.Text", identifier.TypeIdentifier,
        StringComparison.InvariantCultureIgnoreCase);
```

### Match multiple widget types (consolidation)

```csharp
public bool ShallMigrate(WidgetMigrationContext context, WidgetIdentifier identifier)
    => string.Equals("MedioClinic.Widget.TextA", identifier.TypeIdentifier,
        StringComparison.InvariantCultureIgnoreCase)
    || string.Equals("MedioClinic.Widget.TextB", identifier.TypeIdentifier,
        StringComparison.InvariantCultureIgnoreCase);
```

### Match with site filter (multi-site)

```csharp
public bool ShallMigrate(WidgetMigrationContext context, WidgetIdentifier identifier)
    => string.Equals("MedioClinic.Widget.Text", identifier.TypeIdentifier,
        StringComparison.InvariantCultureIgnoreCase)
    && context.SiteId == SOURCE_SITE_ID;
```

### IWidgetPropertyMigration — Match by property name

```csharp
public bool ShallMigrate(WidgetPropertyMigrationContext context, string propertyName)
    => propertyName.Equals("leftColumnWidth", StringComparison.InvariantCultureIgnoreCase);
```

### IWidgetPropertyMigration — Match by form component identifier

```csharp
public bool ShallMigrate(WidgetPropertyMigrationContext context, string propertyName)
    => "CloudinarySelectorComponent".Equals(
        context.EditingFormControlModel?.FormComponentIdentifier,
        StringComparison.InvariantCultureIgnoreCase);
```

---

## 10. MigrateWidget Patterns

### Simple type rename (no property changes)

```csharp
public Task<WidgetMigrationResult> MigrateWidget(
    WidgetIdentifier identifier, JToken? value, WidgetMigrationContext context)
{
    value!["type"] = Target_TypeIdentifier;

    return Task.FromResult(new WidgetMigrationResult(value, null));
}
```

### Type rename with property rename

```csharp
public Task<WidgetMigrationResult> MigrateWidget(
    WidgetIdentifier identifier, JToken? value, WidgetMigrationContext context)
{
    value!["type"] = "RichTextWidget";

    var variants = (JArray)value!["variants"]!;
    var singleVariant = variants[0];
    singleVariant["properties"] = new JObject
    {
        ["RichTextWidgetContent"] = singleVariant["properties"]!["text"],
    };

    return Task.FromResult(new WidgetMigrationResult(value, null));
}
```

### Type rename with propertyMigrations dictionary

```csharp
public Task<WidgetMigrationResult> MigrateWidget(
    WidgetIdentifier identifier, JToken? value, WidgetMigrationContext context)
{
    value!["type"] = "ImageDisplayWidget";

    var variants = (JArray)value!["variants"]!;
    var singleVariant = variants[0];
    singleVariant["properties"] = new JObject
    {
        ["ImageDisplayWidgetImage"] = singleVariant["properties"]!["imageGuid"],
    };

    // Delegate media file property migration to the built-in WidgetFileMigration
    var propertyMigrations = new Dictionary<string, Type>
    {
        ["ImageDisplayWidgetImage"] = typeof(WidgetFileMigration)
    };

    return Task.FromResult(new WidgetMigrationResult(value, propertyMigrations));
}
```

### Inline property value conversion

```csharp
public Task<WidgetMigrationResult> MigrateWidget(
    WidgetIdentifier identifier, JToken? value, WidgetMigrationContext context)
{
    value!["type"] = "TwoColumnSection";

    var variants = (JArray)value!["variants"]!;
    var singleVariant = variants[0];

    // Inline value conversion: int width → string ratio
    var leftWidth = singleVariant["properties"]!["leftColumnWidth"]?.Value<int>() ?? 50;
    var ratio = $"{leftWidth}/{100 - leftWidth}";

    singleVariant["properties"] = new JObject
    {
        ["ColumnRatio"] = ratio,
    };

    return Task.FromResult(new WidgetMigrationResult(value, null));
}
```

---

## 11. MigrateWidgetProperty Patterns

### Simple value conversion

```csharp
public Task<WidgetPropertyMigrationResult> MigrateWidgetProperty(
    string key, JToken? value, WidgetPropertyMigrationContext context)
{
    // Convert int width to string ratio
    var leftWidth = value?.Value<int>() ?? 50;
    var ratio = $"{leftWidth}/{100 - leftWidth}";

    return Task.FromResult(new WidgetPropertyMigrationResult(JToken.FromObject(ratio)));
}
```

### Content item reference creation

```csharp
public Task<WidgetPropertyMigrationResult> MigrateWidgetProperty(
    string key, JToken? value, WidgetPropertyMigrationContext context)
{
    // TODO: Look up the migrated content item GUID from the source media GUID
    var sourceGuid = value?.Value<string>();
    var contentItemRef = new ContentItemReference
    {
        Identifier = /* resolved GUID */ Guid.Empty
    };

    var result = JToken.FromObject(new[] { contentItemRef });
    return Task.FromResult(new WidgetPropertyMigrationResult(result));
}
```

---

## 12. Service Registration

Every `IWidgetMigration` and `IWidgetPropertyMigration` must be registered as **transient** in the DI container.

### Per-migration extension method pattern

**Important:** Extension methods must be in a separate `static` class, not inside the migration class itself. Migration classes that use primary constructors for DI injection (e.g., `ILogger`, `ModelFacade`) are non-static, and C# requires extension methods to be in non-generic `static` classes.

```csharp
using Microsoft.Extensions.DependencyInjection;
using Migration.Tool.KXP.Api.Services.CmsClass;

namespace Migration.Tool.Extensions.WidgetMigrations;

public class TextWidgetMigration : IWidgetMigration
{
    // ... implementation ...
}

// Extension method MUST be in a separate static class
public static class TextWidgetMigrationExtensions
{
    public static IServiceCollection AddTextWidgetMigration(
        this IServiceCollection services)
    {
        services.AddTransient<IWidgetMigration, TextWidgetMigration>();
        return services;
    }
}
```

### Central registration in ServiceCollectionExtensions

```csharp
using Migration.Tool.Extensions.WidgetMigrations;

namespace Migration.Tool.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection UseCustomizations(this IServiceCollection services)
    {
        // IWidgetMigration registrations
        services.AddSingleColumnSectionMigration();
        services.AddTwoColumnSectionMigration();
        services.AddTextWidgetMigration();
        services.AddImageWidgetMigration();
        // ... all other widget migrations ...

        // IWidgetPropertyMigration registrations
        services.AddColumnRatioPropertyMigration();
        // ... all other property migrations ...

        // Note: built-in widgets (Kentico.Widget.RichText, Kentico.FormWidget)
        // migrate automatically — no custom code needed.

        return services;
    }
}
```

**Important:**

- Use `AddTransient`, **NOT** `AddSingleton` or `AddScoped`.
- Multiple migrations can be registered — the tool evaluates all by `Rank` for each widget/property.
- The first migration where `ShallMigrate` returns `true` (lowest `Rank`) handles the widget.

---

## 13. Execution Order

Widget migrations execute during the `--pages` CLI phase.

**Execution sequence within a migration run:**

1. `IClassMapping` creates target content type structure
2. `IFieldMigration` transforms field definitions and values
3. `ContentItemDirectorBase` handles page relationships and content items
4. **`IWidgetMigration`** transforms widget type identifiers and restructures properties
5. **`IWidgetPropertyMigration`** transforms individual property values (delegated from `propertyMigrations` dictionary)

For each widget in the Page Builder JSON, the tool iterates all registered `IWidgetMigration` implementations sorted by `Rank` (ascending). The first one where `ShallMigrate` returns `true` handles the widget.

### Critical: How IWidgetPropertyMigration is invoked

The `VisualBuilderPatcher.MigrateProperties` method resolves property migrations via two paths:

1. **Explicit delegation** (`explicitMigrations` dictionary): If an `IWidgetMigration.MigrateWidget` returned a `propertyMigrations` dictionary containing the property key, that specific `IWidgetPropertyMigration` type is used. **This path always works regardless of API Discovery.**
2. **Rank-based lookup** (`GetWidgetPropertyMigration`): If the property is NOT in `explicitMigrations`, the tool checks whether API Discovery provided an `EditingFormControlModel` for the property. If `editingFcm` is non-null, it calls `GetWidgetPropertyMigration(context, key)` which iterates all registered `IWidgetPropertyMigration` implementations by rank. **If `editingFcm` is null (no API Discovery metadata), this path is skipped entirely and the property is left untouched.**

**Consequence:** A standalone `IWidgetPropertyMigration` (not delegated via `propertyMigrations`) is only invoked when the KX13 widget property has an `[EditingComponent(...)]` attribute that API Discovery can read. Widget properties without this attribute silently skip all property migrations. **Always pair custom `IWidgetPropertyMigration` with an `IWidgetMigration` that delegates via `propertyMigrations` to guarantee execution.**

---

## 14. Decision Guide: IWidgetMigration vs IWidgetPropertyMigration

| Scenario | Use | Why |
|---|---|---|
| Renaming widget/section type identifier | `IWidgetMigration` | Only `MigrateWidget` can change `value["type"]` |
| Restructuring properties (add/remove/rename) | `IWidgetMigration` | Rebuild the `properties` JObject in `MigrateWidget` |
| Consolidating multiple source widgets → one target | `IWidgetMigration` | `ShallMigrate` can match multiple type identifiers |
| Converting a property value (media GUID → content item ref) | `IWidgetPropertyMigration` | Reusable across widgets; delegated via `propertyMigrations` dict |
| Both type rename AND property value conversion | Both | `IWidgetMigration` renames and restructures, then `propertyMigrations` dictionary delegates specific properties to `IWidgetPropertyMigration` types |
| Simple property format change (int → string) | Either | Can be done inline in `MigrateWidget` or via `IWidgetPropertyMigration` if reusable |

---

## 15. Annotated Code Samples

### Sample 1: Simple Section Type Rename

**When to use:** A KX13 section type identifier changes in XbyK but has no property changes.

```csharp
public class SingleColumnSectionMigration : IWidgetMigration
{
    private const string Source_TypeIdentifier = "MedioClinic.Section.SingleColumn";
    private const string Target_TypeIdentifier = "SingleColumnSection";

    public int Rank => 1;

    public bool ShallMigrate(WidgetMigrationContext context, WidgetIdentifier identifier)
        => string.Equals(Source_TypeIdentifier, identifier.TypeIdentifier,
            StringComparison.InvariantCultureIgnoreCase);

    public Task<WidgetMigrationResult> MigrateWidget(
        WidgetIdentifier identifier, JToken? value, WidgetMigrationContext context)
    {
        value!["type"] = Target_TypeIdentifier;
        return Task.FromResult(new WidgetMigrationResult(value, null));
    }
}
```

### Sample 2: Section with Inline Property Conversion

**When to use:** Section type rename plus a property value format change that can be done inline.

```csharp
public class TwoColumnSectionMigration : IWidgetMigration
{
    private const string Source_TypeIdentifier = "MedioClinic.Section.TwoColumn";
    private const string Target_TypeIdentifier = "TwoColumnSection";

    public int Rank => 2;

    public bool ShallMigrate(WidgetMigrationContext context, WidgetIdentifier identifier)
        => string.Equals(Source_TypeIdentifier, identifier.TypeIdentifier,
            StringComparison.InvariantCultureIgnoreCase);

    public Task<WidgetMigrationResult> MigrateWidget(
        WidgetIdentifier identifier, JToken? value, WidgetMigrationContext context)
    {
        value!["type"] = Target_TypeIdentifier;

        var variants = (JArray)value!["variants"]!;
        var singleVariant = variants[0];

        var leftWidth = singleVariant["properties"]!["leftColumnWidth"]?.Value<int>() ?? 50;
        singleVariant["properties"] = new JObject
        {
            ["ColumnRatio"] = $"{leftWidth}/{100 - leftWidth}",
        };

        return Task.FromResult(new WidgetMigrationResult(value, null));
    }
}
```

### Sample 3: Widget Rename with Property Mapping

**When to use:** Widget type changes and properties are renamed, but values stay the same format.

```csharp
public class TextWidgetMigration : IWidgetMigration
{
    private const string Source_TypeIdentifier = "MedioClinic.Widget.Text";
    private const string Target_TypeIdentifier = "RichTextWidget";

    public int Rank => 3;

    public bool ShallMigrate(WidgetMigrationContext context, WidgetIdentifier identifier)
        => string.Equals(Source_TypeIdentifier, identifier.TypeIdentifier,
            StringComparison.InvariantCultureIgnoreCase);

    public Task<WidgetMigrationResult> MigrateWidget(
        WidgetIdentifier identifier, JToken? value, WidgetMigrationContext context)
    {
        value!["type"] = Target_TypeIdentifier;

        var variants = (JArray)value!["variants"]!;
        var singleVariant = variants[0];
        singleVariant["properties"] = new JObject
        {
            ["RichTextWidgetContent"] = singleVariant["properties"]!["text"],
        };

        return Task.FromResult(new WidgetMigrationResult(value, null));
    }
}
```

### Sample 4: Widget with Media GUID Property (WidgetFileMigration)

**When to use:** Widget has a media file property that needs conversion via the built-in `WidgetFileMigration`.

```csharp
public class ImageWidgetMigration : IWidgetMigration
{
    private const string Source_TypeIdentifier = "MedioClinic.Widget.Image";
    private const string Target_TypeIdentifier = "ImageDisplayWidget";

    public int Rank => 4;

    public bool ShallMigrate(WidgetMigrationContext context, WidgetIdentifier identifier)
        => string.Equals(Source_TypeIdentifier, identifier.TypeIdentifier,
            StringComparison.InvariantCultureIgnoreCase);

    public Task<WidgetMigrationResult> MigrateWidget(
        WidgetIdentifier identifier, JToken? value, WidgetMigrationContext context)
    {
        value!["type"] = Target_TypeIdentifier;

        var variants = (JArray)value!["variants"]!;
        var singleVariant = variants[0];
        singleVariant["properties"] = new JObject
        {
            ["ImageDisplayWidgetImage"] = singleVariant["properties"]!["imageGuid"],
            ["ImageDisplayWidgetAltText"] = singleVariant["properties"]!["altText"],
        };

        // Delegate media file conversion to built-in WidgetFileMigration
        var propertyMigrations = new Dictionary<string, Type>
        {
            ["ImageDisplayWidgetImage"] = typeof(WidgetFileMigration)
        };

        return Task.FromResult(new WidgetMigrationResult(value, propertyMigrations));
    }
}
```

### Sample 5: Complex Widget with Value Mapping

**When to use:** Widget properties need inline value transformations (color mapping, resource key resolution).

```csharp
public class ButtonWidgetMigration : IWidgetMigration
{
    private const string Source_TypeIdentifier = "MedioClinic.Widget.Button";
    private const string Target_TypeIdentifier = "CTAButtonWidget";

    private static readonly Dictionary<string, string> ColorToStyleMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["red"] = "Primary",
        ["blue"] = "Secondary",
        ["green"] = "Accent",
        ["gray"] = "Muted",
    };

    public int Rank => 5;

    public bool ShallMigrate(WidgetMigrationContext context, WidgetIdentifier identifier)
        => string.Equals(Source_TypeIdentifier, identifier.TypeIdentifier,
            StringComparison.InvariantCultureIgnoreCase);

    public Task<WidgetMigrationResult> MigrateWidget(
        WidgetIdentifier identifier, JToken? value, WidgetMigrationContext context)
    {
        value!["type"] = Target_TypeIdentifier;

        var variants = (JArray)value!["variants"]!;
        var singleVariant = variants[0];
        var sourceProps = singleVariant["properties"]!;

        // Map buttonColor string → style override
        var colorValue = sourceProps["buttonColor"]?.Value<string>() ?? "";
        var style = ColorToStyleMap.GetValueOrDefault(colorValue, "Primary");

        // TODO: Resolve linkTextResourceKey to localized text via KX13 resource table lookup
        var resourceKey = sourceProps["linkTextResourceKey"]?.Value<string>();

        singleVariant["properties"] = new JObject
        {
            ["CTAButtonWidgetUrl"] = sourceProps["url"],
            ["CTAButtonWidgetStyleOverride"] = style,
            ["CTAButtonWidgetLabelOverride"] = resourceKey ?? "",
        };

        return Task.FromResult(new WidgetMigrationResult(value, null));
    }
}
```

### Sample 6: Advanced — Content Item Creation During Migration

**When to use:** Widget data should be extracted into a reusable content item during migration, with the widget property updated to reference the new item. Based on the [Kentico Docs — Migrate widget data to content hub](https://docs.kentico.com/guides/upgrade-to-xbyk/upgrade-deep-dives/migrate-widget-data-to-content-hub) pattern.

```csharp
public class HeroContentWidgetMigration(
    ILogger<HeroContentWidgetMigration> logger) : IWidgetMigration
{
    private const string Source_TypeIdentifier = "DancingGoat.LandingPage.HeroImage";
    private const string Target_ContentTypeName = "DancingGoatCore.Hero";
    private const int Source_SiteId = 1;

    public int Rank => 100;

    public bool ShallMigrate(WidgetMigrationContext context, WidgetIdentifier identifier)
        => string.Equals(Source_TypeIdentifier, identifier.TypeIdentifier,
            StringComparison.InvariantCultureIgnoreCase)
        && Source_SiteId == context.SiteId;

    public async Task<WidgetMigrationResult> MigrateWidget(
        WidgetIdentifier identifier, JToken? value, WidgetMigrationContext context)
    {
        var variants = (JArray)value!["variants"]!;
        var singleVariant = variants[0];

        // Extract text properties and create a reusable content item
        var heroItemReference = await CreateHeroContentItem(singleVariant["properties"]!);

        singleVariant["properties"] = new JObject
        {
            ["hero"] = heroItemReference,
            ["image"] = singleVariant["properties"]!["image"],
            ["theme"] = singleVariant["properties"]!["theme"],
            ["openInNewTab"] = JToken.FromObject(false),
        };

        return new WidgetMigrationResult(value, new Dictionary<string, Type>());
    }

    private async Task<JToken?> CreateHeroContentItem(JToken properties)
    {
        const string workspaceName = "KenticoDefault";
        const int adminUserId = 53; // Global Administrator — verify in your database
        const string language = "en-US";

        var heading = properties["text"]?.ToString() ?? "";
        var target = properties["buttonTarget"]?.ToString() ?? "";
        var callToAction = properties["buttonText"]?.ToString() ?? "";

        var ciManager = Service.Resolve<IContentItemManagerFactory>().Create(adminUserId);

        var createParams = new CreateContentItemParameters(
            contentTypeName: Target_ContentTypeName,
            name: $"MigratedHeroItem{Guid.NewGuid():N}",
            displayName: $"Hero item - {heading}",
            languageName: language,
            workspaceName: workspaceName);

        var contentItemData = new ContentItemData();
        contentItemData.SetValue("HeroHeading", heading);
        contentItemData.SetValue("HeroTarget", target);
        contentItemData.SetValue("HeroCallToAction", callToAction);

        int itemId = await ciManager.Create(createParams, contentItemData);
        if (itemId <= 0) throw new Exception("Unable to create Hero content item");

        await ciManager.TryPublish(itemId, language);

        var itemGuid = CMS.ContentEngine.Internal.ContentItemInfo.Provider.Get(itemId).ContentItemGUID;
        return JToken.FromObject(new[] { new ContentItemReference { Identifier = itemGuid } });
    }
}
```

---

## 16. Common Snippets

### JToken property access

```csharp
// Read string
var text = singleVariant["properties"]!["text"]?.Value<string>();

// Read int with default
var width = singleVariant["properties"]!["leftColumnWidth"]?.Value<int>() ?? 50;

// Read array
var guids = singleVariant["properties"]!["imageGuids"]?.ToObject<List<string>>();

// Check for null/empty
if (value == null || string.IsNullOrEmpty(value.ToString())) { /* handle missing */ }
```

### New JObject creation

```csharp
singleVariant["properties"] = new JObject
{
    ["targetProp1"] = singleVariant["properties"]!["sourceProp1"],
    ["targetProp2"] = "static value",
    ["targetProp3"] = JToken.FromObject(new[] { "array", "values" }),
};
```

### Media GUID lookup (TODO pattern)

```csharp
// TODO: Resolve source media library GUID to migrated content item GUID.
// The WidgetFileMigration built-in handles this automatically when referenced
// in the propertyMigrations dictionary. For manual resolution, query the
// target database for the migrated asset's ContentItemGUID.
var sourceMediaGuid = sourceProps["imageGuid"]?.Value<string>();
```

### Color/value mapping dictionary

```csharp
private static readonly Dictionary<string, string> ColorToStyleMap =
    new(StringComparer.OrdinalIgnoreCase)
{
    ["red"] = "Primary",
    ["blue"] = "Secondary",
    ["green"] = "Accent",
};

var style = ColorToStyleMap.GetValueOrDefault(sourceColor, "Primary");
```

### Resource key resolution (TODO pattern)

```csharp
// TODO: Resource key resolution — look up the value from KX13's
// CMS_ResourceString / CMS_ResourceTranslation tables and insert
// the resolved text. XbyK does not use resource keys for content.
var resourceKey = sourceProps["linkTextResourceKey"]?.Value<string>();
var resolvedText = resourceKey; // Placeholder — replace with actual lookup
```
