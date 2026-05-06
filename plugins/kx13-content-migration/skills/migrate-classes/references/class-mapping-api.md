# Class Mapping API Reference

This document covers the complete API for creating custom class mappings (`IClassMapping`) and reusable field schemas (`IReusableSchemaBuilder`) in the Kentico Migration Tool.

**Source:** [ClassMappingSample.cs](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool/blob/master/Migration.Tool.Extensions/ClassMappings/ClassMappingSample.cs), [Kentico Docs — Remodel page types](https://docs.kentico.com/guides/upgrade-to-xbyk/upgrade-deep-dives/remodel-page-types-as-reusable-field-schemas.html), [Kentico Docs — Speed up remodeling with AI](https://docs.kentico.com/guides/upgrade-to-xbyk/upgrade-deep-dives/speed-up-remodeling-with-ai.html)

---

## Required Namespaces

```csharp
using CMS.DataEngine;
using CMS.FormEngine;
using Microsoft.Extensions.DependencyInjection;
using Migration.Tool.Common.Builders;
using Migration.Tool.KXP.Api.Auxiliary;       // FormComponents constants
using Migration.Tool.Common.Abstractions;     // IClassMapping, IConvertorContext
using Migration.Tool.Common.Helpers;          // GuidHelper (for deterministic GUIDs)

// Additional namespaces needed for docrelationships / CMS_Relationship converters:
using Microsoft.Data.SqlClient;               // SqlParameter for ModelFacade queries
using Migration.Tool.Source;                  // ModelFacade
using Migration.Tool.Source.Model;            // ICmsTree, ICmsClass
using Migration.Tool.Source.Services;         // CmsRelationshipService
```

---

## 1. MultiClassMapping API

### Constructor

```csharp
var m = new MultiClassMapping(targetClassName, target =>
{
    target.ClassName = targetClassName;              // e.g. "Namespace.ClassName"
    target.ClassTableName = "Namespace_ClassName";   // SQL table name
    target.ClassDisplayName = "Display Name";
    target.ClassType = ClassType.CONTENT_TYPE;
    target.ClassContentTypeType = ClassContentTypeType.WEBSITE;  // or .REUSABLE
    target.ClassWebPageHasUrl = false;               // optional, only for REUSABLE types
});
```

**Target configuration properties:**

| Property               | Type                   | Values                      | Notes                                          |
| ---------------------- | ---------------------- | --------------------------- | ---------------------------------------------- |
| `ClassName`            | string                 | `"Namespace.ClassName"`     | Must match target content type code name       |
| `ClassTableName`       | string                 | `"Namespace_ClassName"`     | SQL table name (underscores, not dots)         |
| `ClassDisplayName`     | string                 |                             | Human-readable name                            |
| `ClassType`            | `ClassType`            | `ClassType.CONTENT_TYPE`    | Always CONTENT_TYPE for page types             |
| `ClassContentTypeType` | `ClassContentTypeType` | `.WEBSITE` or `.REUSABLE`   | Website = has URL, Reusable = Content Hub item |
| `ClassWebPageHasUrl`   | bool                   | `true` (default) or `false` | Set `false` for reusable types                 |

### Mapping to pre-existing (prefabricated) content types

When the target content type already exists in the XbyK instance (created manually or by other means), pass an empty configure delegate:

```csharp
var m = new MultiClassMapping("DancingGoatCore.PrefabArticle", _ => { });
```

**Prerequisites for prefabricated mappings:**

- CLI must be invoked with `--bypass-dependency-check` and without `--page-types`
- Target content type, reusable field schemas, and related structures must already exist in XbyK
- Field types in the target must be compatible with source field types

### BuildField Chain API

#### AsPrimaryKey

Define the primary key field for the target content type.

```csharp
m.BuildField("ContentTypeID").AsPrimaryKey();
```

Convention: use `{ShortTargetName}ID` (e.g., `DoctorID`, `OfficeLocationID`).

#### SetFrom

Map a field directly from a source class. Copies the value as-is.

```csharp
m.BuildField("TargetField")
    .SetFrom("SourceNamespace.SourceClass", "SourceField", isTemplate: true);
```

**Parameters:**

- `sourceClassName` — full class name (e.g., `"DancingGoatCore.Coffee"`)
- `sourceFieldName` — source field name
- `isTemplate` (bool, optional) — when `true`, copies the KX13 field definition (caption, data type, default value, form control) to create the XbyK field. When `false` or omitted, only transfers the value. **Use `isTemplate: true` on exactly one source per field** — the one providing the field definition.

**Multiple sources for one field (merge):**

```csharp
var title = m.BuildField("Title");
title.SetFrom(sourceClassName1, "EventTitle", true);   // provides template
title.SetFrom(sourceClassName2, "EventTitle");          // value only
title.SetFrom(sourceClassName3, "TitleCT");             // value only (custom table)
```

#### ConvertFrom

Transform a field value with a custom converter function.

```csharp
m.BuildField("TargetField")
    .ConvertFrom("SourceClass", "SourceField", includeDefinition, converter);
```

**Parameters:**

- `sourceClassName` — full class name
- `sourceFieldName` — source field name
- `includeDefinition` (bool) — when `true`, uses the source field definition as a template (similar to `isTemplate` in `SetFrom`). **Warning:** When `includeDefinition` is `false`, no field definition is created from the source. If you later chain `.WithFieldPatch()`, it will receive a null `FormFieldInfo` and throw a `NullReferenceException`. Use `includeDefinition: true` if you need to patch field metadata.
- `converter` — `Func<object?, IConvertorContext, object?>` function

**Converter signature:**

```csharp
Func<object?, IConvertorContext, object?> converter = (value, context) =>
{
    switch (context)
    {
        case ConvertorTreeNodeContext treeNodeContext:
            // Available: nodeGuid, nodeSiteId, documentId, migratingFromVersionHistory
            break;
        case ConvertorCustomTableContext customTableContext:
            // Available for custom table sources
            break;
        default:
            break;
    }

    return value switch
    {
        string s => TransformString(s),
        null => null,
        _ => throw new InvalidOperationException($"Unexpected value: {value}")
    };
};
```

#### WithFieldPatch

Modify field metadata (caption, settings, etc.) after mapping.

**Important:** `WithFieldPatch` requires a field definition to exist. It works correctly after `SetFrom(isTemplate: true)`, `ConvertFrom(includeDefinition: true)`, `WithoutSource`, or `WithFactory`. When used after `ConvertFrom(includeDefinition: false)` or on fields that don't carry a source definition, the `FormFieldInfo` parameter (`f`) will be `null`, causing a `NullReferenceException`. Always use `includeDefinition: true` when combining `ConvertFrom` with `WithFieldPatch`, or use `WithFactory` to create the definition explicitly.

```csharp
m.BuildField("TargetField")
    .SetFrom("SourceClass", "SourceField", true)
    .WithFieldPatch(f => f.Caption = "New Caption");

// Or using the enum-based approach:
m.BuildField("TargetField")
    .SetFrom("SourceClass", "SourceField", true)
    .WithFieldPatch(f => f.SetPropertyValue(FormFieldPropertyEnum.FieldCaption, "New Caption"));
```

#### WithoutSource

Create a new field that has no source in KX13. Requires specifying the data type.

```csharp
m.BuildField("NewTaxonomyField")
    .WithoutSource("taxonomy")
    .WithFieldPatch(f =>
    {
        f.Caption = "Categories";
        f.AllowEmpty = true;
        f.Visible = true;      // Required — FormDefinitionHelper resets visibility for unrecognized types like "taxonomy"
        f.Enabled = true;      // Required — ensures the field is editable in the XbyK admin form
        f.DataType = "taxonomy";
        f.Settings["controlname"] = "Kentico.Administration.TagSelector";
        f.Settings["TaxonomyGroup"] = "[\"TAXONOMY-GROUP-GUID\"]";
    });
```

**Important:** `WithoutSource` fields must have `AllowEmpty = true`, otherwise unmapped classes will throw errors.

**Important:** For **all** fields using `WithFieldPatch`, always set `f.Visible = true` and `f.Enabled = true`. The migration tool's `FormDefinitionPatcher.PatchField()` can reset or strip the `Visible` attribute depending on the class type and whether the field type is recognized. This affects all `WithoutSource` fields (not just taxonomy) and can also affect fields on non-document-type classes. Without explicitly setting these properties, the field may exist in the schema but be invisible in the XbyK editable form.

Common data types for `WithoutSource`: `"integer"`, `"text"`, `"taxonomy"`, `"longtext"`, `"boolean"`, `"datetime"`.

#### WithFactory

Create a field with full manual control over its `FormFieldInfo`.

```csharp
m.BuildField("FieldName")
    .WithFactory(() => new FormFieldInfo
    {
        Name = "FieldName",
        Caption = "Display Caption",
        Guid = new Guid("XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX"),
        DataType = FieldDataType.Text,
        Size = 200,
        AllowEmpty = true,
        Settings =
        {
            ["controlname"] = FormComponents.AdminTextInputComponent
        }
    });
```

### UseResusableSchema

Assign a reusable field schema to the content type. **Note the typo in the method name — this is the actual API spelling.**

```csharp
m.UseResusableSchema("Namespace.SchemaName");
```

After assigning, map values to the schema fields:

```csharp
m.UseResusableSchema(schemaName);
m.BuildField("SchemaFieldName").SetFrom("SourceClass", "SourceField");
```

Schema fields mapped from a class mapping do **not** use `isTemplate: true` — the field definition comes from the schema builder.

### SetHandler\<T\>

Set a custom handler for custom table sources.

```csharp
m.SetHandler<SampleCustomTableHandler>();

// Also register the handler in DI:
serviceCollection.AddTransient<SampleCustomTableHandler>();
```

Custom handlers extend `DefaultCustomTableClassMappingHandler`:

```csharp
public sealed class SampleCustomTableHandler : DefaultCustomTableClassMappingHandler
{
    public SampleCustomTableHandler(ILogger<DefaultCustomTableClassMappingHandler> logger)
        : base(logger) { }

    public override void EnsureContentItemLanguageMetadata(
        ContentItemLanguageMetadataModel languageMetadataInfo,
        CustomTableMappingHandlerContext context)
    {
        base.EnsureContentItemLanguageMetadata(languageMetadataInfo, context);
        // Custom metadata logic here
    }
}
```

### FilterCategories

Filter which KX13 categories are migrated for this class.

```csharp
int excludedCategoryId = 17;
int[] excludedCategories = [excludedCategoryId];
m.FilterCategories((className, categoryID) => !excludedCategories.Contains(categoryID));
```

---

## 2. ReusableSchemaBuilder API

### Constructor

```csharp
var sb = new ReusableSchemaBuilder(
    "Namespace.SchemaName",      // schema class name
    "Display Name",              // display name
    "Description text"           // description
);
```

### ConvertFrom (bulk field copy with rename)

Copy all fields from a source class and rename them via a transform function. Return `null` to exclude a field.

```csharp
sb.ConvertFrom("SourceNamespace.SourceClass", fieldName => fieldName switch
{
    "SourceField1" => "SchemaField1",      // rename
    "SourceField2" => "SchemaField2",      // rename
    "FieldToExclude" => null,              // exclude
    _ => null                              // exclude all others
});
```

To copy all fields without renaming:

```csharp
sb.ConvertFrom("SourceClass", x => x);
```

### BuildField + CreateFrom

Copy a single field's definition from a source class field.

```csharp
sb.BuildField("SchemaFieldName")
    .CreateFrom("SourceClass", "SourceFieldName");
```

### BuildField + WithFactory

Define a schema field manually with full control.

```csharp
sb.BuildField("SchemaFieldName")
    .WithFactory(() => new FormFieldInfo
    {
        Name = "SchemaFieldName",
        Caption = "Display Caption",
        Guid = new Guid("XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX"),
        DataType = FieldDataType.Text,
        Size = 200,
        Settings =
        {
            ["controlname"] = FormComponents.AdminTextInputComponent
        }
    });
```

---

## 3. FormFieldInfo Properties

Key properties used with `WithFactory` and `WithFieldPatch`:

| Property       | Type       | Notes                                                                               |
| -------------- | ---------- | ----------------------------------------------------------------------------------- |
| `Name`         | string     | Field code name                                                                     |
| `Caption`      | string     | Display label                                                                       |
| `Guid`         | Guid       | Unique identifier; use `GuidHelper.CreateFieldGuid("name")` for deterministic GUIDs |
| `DataType`     | string     | Use `FieldDataType` constants (see below)                                           |
| `Size`         | int        | Max length for text fields                                                          |
| `Precision`    | int        | Decimal precision                                                                   |
| `AllowEmpty`   | bool       | Whether null/empty is allowed                                                       |
| `DefaultValue` | string     | Default value as string                                                             |
| `Settings`     | dictionary | Form control settings (e.g., `["controlname"]`)                                     |

### FieldDataType Constants

| Constant                             | Value                    |
| ------------------------------------ | ------------------------ |
| `FieldDataType.Text`                 | `"text"`                 |
| `FieldDataType.LongText`             | `"longtext"`             |
| `FieldDataType.RichText`             | `"richtexthtml"`         |
| `FieldDataType.Integer`              | `"integer"`              |
| `FieldDataType.LongInteger`          | `"longinteger"`          |
| `FieldDataType.Double`               | `"double"`               |
| `FieldDataType.Decimal`              | `"decimal"`              |
| `FieldDataType.DateTime`             | `"datetime"`             |
| `FieldDataType.Boolean`              | `"boolean"`              |
| `FieldDataType.Guid`                 | `"guid"`                 |
| `FieldDataType.ContentItemAsset`     | `"contentitemasset"`     |
| `FieldDataType.ContentItemReference` | `"contentitemreference"` |

### FormComponents Constants

| Constant                                           | Notes                           |
| -------------------------------------------------- | ------------------------------- |
| `FormComponents.AdminTextInputComponent`           | Single-line text input          |
| `FormComponents.AdminTextAreaComponent`            | Multi-line text area            |
| `FormComponents.AdminRichTextEditorComponent`      | Rich text (HTML) editor         |
| `FormComponents.AdminNumberInputComponent`         | Numeric input                   |
| `FormComponents.AdminCheckBoxComponent`            | Boolean checkbox                |
| `FormComponents.AdminDateTimeInputComponent`       | Date/time picker                |
| `FormComponents.AdminAssetSelectorComponent`       | Content item asset selector     |
| `FormComponents.AdminContentItemSelectorComponent` | Content item reference selector |

---

## 4. Service Registration Pattern

Every `IClassMapping` and `IReusableSchemaBuilder` must be registered as a singleton.

### Per-mapping extension method pattern

```csharp
using CMS.DataEngine;
using CMS.FormEngine;
using Microsoft.Extensions.DependencyInjection;
using Migration.Tool.Common.Builders;
using Migration.Tool.KXP.Api.Auxiliary;

namespace Migration.Tool.Extensions.ClassMappings;

public static class MyTargetClassMapping
{
    // ... private constants and builder methods ...

    public static IServiceCollection AddMyTargetMapping(this IServiceCollection serviceCollection)
    {
        var mapping = BuildMapping();
        serviceCollection.AddSingleton<IClassMapping>(mapping);
        return serviceCollection;
    }
}
```

### ServiceCollectionExtensions (central registration)

```csharp
using Migration.Tool.Extensions.ClassMappings;

namespace Migration.Tool.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection UseCustomizations(this IServiceCollection services)
    {
        services.AddHomePageMapping();
        services.AddDoctorMapping();
        // ... all other mappings ...
        return services;
    }
}
```

### Registration with schema builders

```csharp
public static IServiceCollection AddMappingWithSchema(this IServiceCollection serviceCollection)
{
    var schemaBuilder = BuildSchema();
    var mapping = BuildMapping();

    serviceCollection.AddSingleton<IClassMapping>(mapping);
    serviceCollection.AddSingleton<IReusableSchemaBuilder>(schemaBuilder);

    return serviceCollection;
}
```

### Custom table handler registration

```csharp
// Handler must be registered as Transient
serviceCollection.AddTransient<SampleCustomTableHandler>();
// The mapping itself is still Singleton
serviceCollection.AddSingleton<IClassMapping>(m);
```

### Factory DI registration (for service-dependent converters)

When a converter needs to query the KX13 database at conversion time (e.g., for `docrelationships` fields stored in `CMS_Relationship`), use factory-based registration to inject services into the mapping builder:

```csharp
public static IServiceCollection AddMyMapping(this IServiceCollection serviceCollection)
{
    serviceCollection.AddSingleton<IClassMapping>(sp =>
        BuildMapping(
            sp.GetRequiredService<ModelFacade>(),
            sp.GetRequiredService<CmsRelationshipService>()));
    return serviceCollection;
}

private static MultiClassMapping BuildMapping(
    ModelFacade modelFacade,
    CmsRelationshipService relationshipService)
{
    // modelFacade and relationshipService are now available
    // for use in ConvertFrom converters
    var m = new MultiClassMapping(targetClassName, target => { /* ... */ });
    // ...
    return m;
}
```

**When to use:** Any time a `ConvertFrom` converter needs to query the source database (e.g., `CMS_Relationship`, `CMS_User`, `CMS_Tree`) at migration time. The `BuildMapping` method receives injected services and can use them inside converter closures.

---

## 5. Annotated Code Samples

### Sample 1: Simple Remodel — Rename Fields and Change Captions

**When to use:** Source and target are the same class (or 1:1 rename), but fields need renaming or metadata changes.

```csharp
public static class CoffeeClassMapping
{
    public static IServiceCollection AddCoffeeMapping(this IServiceCollection serviceCollection)
    {
        const string sourceClassName = "DancingGoatCore.Coffee";
        const string targetClassName = "DancingGoatCore.CoffeeRemodeled";

        var m = new MultiClassMapping(targetClassName, target =>
        {
            target.ClassName = targetClassName;
            target.ClassTableName = "DancingGoatCore_CoffeeRemodeled";
            target.ClassDisplayName = "Coffee remodeled";
            target.ClassType = ClassType.CONTENT_TYPE;
            target.ClassContentTypeType = ClassContentTypeType.WEBSITE;
        });

        m.BuildField("CoffeeRemodeledID").AsPrimaryKey();

        // Rename field and change caption
        m.BuildField("FarmRM")
            .SetFrom(sourceClassName, "CoffeeFarm", true)
            .WithFieldPatch(f => f.Caption = "Farm RM");

        // Direct rename keeping source definition
        m.BuildField("CoffeeCountryRM")
            .SetFrom(sourceClassName, "CoffeeCountry", true)
            .WithFieldPatch(f => f.Caption = "Country RM");

        serviceCollection.AddSingleton<IClassMapping>(m);
        return serviceCollection;
    }
}
```

### Sample 2: Reusable Content Type

**When to use:** Converting a KX13 page type to a reusable content item in the Content Hub.

```csharp
var m = new MultiClassMapping(targetClassName, target =>
{
    target.ClassName = targetClassName;
    target.ClassTableName = "Namespace_ClassName";
    target.ClassDisplayName = "Display Name";
    target.ClassType = ClassType.CONTENT_TYPE;
    target.ClassContentTypeType = ClassContentTypeType.REUSABLE;
    target.ClassWebPageHasUrl = false;
});
```

Also requires `ConvertClassesToContentHub` in `appsettings.json`.

### Sample 3: Class Merge — Multiple Sources into One Target

**When to use:** Consolidating two or more KX13 page types into a single XbyK content type.

```csharp
public static IServiceCollection AddEventMapping(this IServiceCollection serviceCollection)
{
    const string targetClassName = "ET.Event";
    const string sourceClassName1 = "_ET.Event1";
    const string sourceClassName2 = "_ET.Event2";

    var m = new MultiClassMapping(targetClassName, target =>
    {
        target.ClassName = targetClassName;
        target.ClassTableName = "ET_Event";
        target.ClassDisplayName = "Event";
        target.ClassType = ClassType.CONTENT_TYPE;
        target.ClassContentTypeType = ClassContentTypeType.WEBSITE;
    });

    m.BuildField("EventID").AsPrimaryKey();

    // Field from multiple sources — first with isTemplate provides definition
    var title = m.BuildField("Title");
    title.SetFrom(sourceClassName1, "EventTitle", true);  // template source
    title.SetFrom(sourceClassName2, "EventTitle");         // value only
    title.WithFieldPatch(f => f.Caption = "Event title");

    // Field from one source only
    var description = m.BuildField("Description");
    description.SetFrom(sourceClassName2, "EventSmallDesc", true);
    description.WithFieldPatch(f => f.Caption = "Event description");

    // Field with value conversion
    var startDate = m.BuildField("StartDate");
    startDate.SetFrom(sourceClassName1, "EventDateStart", true);
    startDate.ConvertFrom(sourceClassName2, "EventStartDateAsText", false, (value, context) =>
    {
        return value switch
        {
            string s when !string.IsNullOrWhiteSpace(s) => DateTime.Parse(s),
            DateTime dt => dt,
            null => null,
            _ => throw new InvalidOperationException($"Unexpected value: {value}")
        };
    });
    startDate.WithFieldPatch(f => f.Caption = "Event start date");

    serviceCollection.AddSingleton<IClassMapping>(m);
    return serviceCollection;
}
```

### Sample 4: New Fields Without Source

**When to use:** Adding new fields in the target that don't exist in KX13 (taxonomy tags, ratings, etc.).

```csharp
// New integer field
m.BuildField("CoffeeRating")
    .WithoutSource("integer")
    .WithFieldPatch(f =>
    {
        f.Caption = "Coffee Rating";
        f.Visible = true;      // Required — FormDefinitionPatcher may reset visibility
        f.Enabled = true;      // Required — ensures the field is editable
        f.AllowEmpty = true;
        f.DataType = FieldDataType.Integer;
        f.DefaultValue = "0";
    });

// New taxonomy field
m.BuildField("CoffeeCategories")
    .WithoutSource("taxonomy")
    .WithFieldPatch(f =>
    {
        f.Caption = "Coffee Categories";
        f.Visible = true;      // Required — FormDefinitionPatcher may reset visibility
        f.Enabled = true;      // Required — ensures the field is editable
        f.AllowEmpty = true;
        f.DataType = "taxonomy";
        f.Settings["controlname"] = "Kentico.Administration.TagSelector";
        // TODO: Replace with actual taxonomy group GUID
        f.Settings["TaxonomyGroup"] = "[\"TAXONOMY-GROUP-GUID\"]";
    });
```

### Sample 5: Reusable Schema with Manual Field Definitions

**When to use:** Creating a reusable field schema with full control over field definitions, shared across multiple content types.

```csharp
public static class OGMetadataMapping
{
    public const string SchemaName = "DancingGoat.OGMetadata";

    private static ReusableSchemaBuilder BuildSchema()
    {
        var sb = new ReusableSchemaBuilder(SchemaName, "Open Graph Metadata",
            "Reusable schema for OG metadata fields");

        sb.BuildField("OGSchemaTitle")
            .WithFactory(() => new FormFieldInfo
            {
                Name = "OGSchemaTitle",
                Caption = "OG title",
                Guid = new Guid("8E46AA5A-2776-4e48-85e4-2288fdfad9c5"),
                DataType = FieldDataType.Text,
                Size = 90,
                Settings =
                {
                    ["controlname"] = FormComponents.AdminTextInputComponent
                }
            });

        sb.BuildField("OGSchemaDescription")
            .WithFactory(() => new FormFieldInfo
            {
                Name = "OGSchemaDescription",
                Caption = "OG Description",
                Guid = new Guid("277af268-3b5e-414b-85cc-61c7f86e43a6"),
                DataType = FieldDataType.Text,
                Size = 200,
                Settings =
                {
                    ["controlname"] = FormComponents.AdminTextInputComponent
                }
            });

        return sb;
    }

    private static MultiClassMapping BuildArticleMapping()
    {
        var mapping = new MultiClassMapping("DancingGoat.Article", target =>
        {
            target.ClassName = "DancingGoat.Article";
            target.ClassTableName = "DancingGoat_Article";
            target.ClassDisplayName = "Article";
            target.ClassType = ClassType.CONTENT_TYPE;
            target.ClassContentTypeType = ClassContentTypeType.WEBSITE;
        });

        mapping.BuildField("ArticleID").AsPrimaryKey();

        // Assign reusable schema
        mapping.UseResusableSchema(SchemaName);

        // Map content-type-specific fields (isTemplate: true)
        mapping.BuildField("ArticleTitle")
            .SetFrom("DancingGoatCore.Article", "ArticleTitle", true);
        mapping.BuildField("ArticleText")
            .SetFrom("DancingGoatCore.Article", "ArticleText", true);

        // Map schema fields (no isTemplate — definition comes from schema builder)
        mapping.BuildField("OGSchemaTitle")
            .SetFrom("DancingGoatCore.Article", "OGTitle");
        mapping.BuildField("OGSchemaDescription")
            .SetFrom("DancingGoatCore.Article", "OGDescription");

        return mapping;
    }

    public static IServiceCollection AddArticleMapping(this IServiceCollection serviceCollection)
    {
        var schemaBuilder = BuildSchema();
        var articleMapping = BuildArticleMapping();

        serviceCollection.AddSingleton<IClassMapping>(articleMapping);
        serviceCollection.AddSingleton<IReusableSchemaBuilder>(schemaBuilder);

        return serviceCollection;
    }
}
```

### Sample 6: Reusable Schema with ConvertFrom (bulk rename)

**When to use:** Creating a schema from an existing class where most/all fields are included with renames.

```csharp
var sb = new ReusableSchemaBuilder("DancingGoat.Grinder", "Common Grinder Fields",
    "Reusable schema that defines common grinder fields");

sb.ConvertFrom("DancingGoatCore.ManualGrinder", fieldName => fieldName switch
{
    "ManualGrinderPromotionTitle" => "GrinderPromotionTitle",
    "ManualGrinderPromotionDescription" => "GrinderPromotionDescription",
    "ManualGrinderBannerText" => "GrinderBannerText",
    _ => null  // exclude all other fields
});
```

### Sample 7: Reusable Schema with CreateFrom (single field copy)

**When to use:** Copying individual field definitions from source when you only need a few.

```csharp
sb.BuildField("ZipCode")
    .CreateFrom("DancingGoatCore.Cafe", "CafeZipCode");

sb.BuildField("Phone")
    .CreateFrom("DancingGoatCore.Cafe", "CafePhone");
```

### Sample 8: Custom Table / Module Class Mapping

**When to use:** Migrating custom tables or module classes to content types. Runs during `--custom-modules` or `--custom-tables` instead of `--page-types`.

```csharp
public static IServiceCollection AddCustomTableMapping(this IServiceCollection serviceCollection)
{
    const string targetClassName = "ET.Event";
    const string sourceCustomTable = "_ET.EventCustomTable";

    var m = new MultiClassMapping(targetClassName, target =>
    {
        target.ClassName = targetClassName;
        target.ClassTableName = "ET_Event";
        target.ClassDisplayName = "Event";
        target.ClassType = ClassType.CONTENT_TYPE;
        target.ClassContentTypeType = ClassContentTypeType.REUSABLE;
    });

    // Set custom handler for custom table processing
    m.SetHandler<SampleCustomTableHandler>();
    serviceCollection.AddTransient<SampleCustomTableHandler>();

    m.BuildField("EventID").AsPrimaryKey();

    m.BuildField("Title")
        .SetFrom(sourceCustomTable, "TitleCT", true)
        .WithFieldPatch(f => f.Caption = "Event title");

    serviceCollection.AddSingleton<IClassMapping>(m);
    return serviceCollection;
}
```

### Sample 9: Category Filtering

**When to use:** Excluding specific KX13 categories from migration for a class.

```csharp
const int EbookCategoryID = 17;
int[] excludedCategories = [EbookCategoryID];
m.FilterCategories((className, categoryID) => !excludedCategories.Contains(categoryID));
```

### Sample 10: Field Cloning with Deterministic GUIDs

**When to use:** Creating a copy of a field within the same target class. Requires unique GUID.

```csharp
var descriptionCopy = m.BuildField("DescriptionCopy");
descriptionCopy.SetFrom(sourceClassName, "EventSmallDesc", true);
descriptionCopy.WithFieldPatch(f =>
{
    f.Guid = GuidHelper.CreateFieldGuid("DescriptionCopy");
    f.Caption = "Event description copy";
});
```

### Sample 11: docrelationships Field → Taxonomy Tags (CMS_Relationship)

**When to use:** Migrating a KX13 `docrelationships` field to a taxonomy field in XbyK. KX13 `docrelationships` fields do **not** store data in the coupled data table — they store related node references in the `CMS_Relationship` table. This requires:

1. **Factory DI registration** — inject `ModelFacade` and `CmsRelationshipService` so the converter can query relationships at migration time.
2. **Avoid ConvertToPages override (only when the target is NOT a page reference)** — the built-in field migration pipeline (`ContentItemMapper.MapProperties`) detects `docrelationships` and `Pages` source fields and triggers a `ConvertToPages` directive that unconditionally overwrites the converter's output with `ContentItemReference` GUIDs. When converting to a taxonomy, object code name, or any non-page target, use a **non-relationship source field** (e.g., the primary key field) in `ConvertFrom` to prevent this override. The converter ignores the incoming value anyway. **Do not apply this workaround** when the target field is a `contentitemreference` (Pages) field — in that case, `ConvertToPages` is the correct behavior and the original `docrelationships` source field should be kept.
3. **WithoutSource + ConvertFrom + WithFieldPatch** — use `WithoutSource("taxonomy")` to create the field definition, then `ConvertFrom` to provide the conversion logic, then `WithFieldPatch` to set taxonomy form control settings with `Visible = true` and `Enabled = true`.
4. **PascalCase `Identifier`** — taxonomy tag reference JSON must use PascalCase property names: `[{"Identifier":"guid"}]`. XbyK's `TagReference` class expects `Identifier`, not `identifier`.

```csharp
// Requires factory DI registration — see "Factory DI registration" section above.
private static MultiClassMapping BuildMapping(
    ModelFacade modelFacade,
    CmsRelationshipService relationshipService)
{
    // Pre-load the source field GUID from the class form definition
    // (needed to query the correct relationship in CMS_Relationship)
    var sourceClass = modelFacade.SelectWhere<ICmsClass>(
        "ClassName = @className",
        new SqlParameter("className", Source_ClassName)).FirstOrDefault();
    Guid relationshipFieldGuid = Guid.Empty;
    if (sourceClass != null && !string.IsNullOrWhiteSpace(sourceClass.ClassFormDefinition))
    {
        var fi = new FormInfo(sourceClass.ClassFormDefinition);
        var field = fi.GetFormField("SourceRelationshipFieldName");
        if (field != null)
            relationshipFieldGuid = field.Guid;
    }

    // NodeGUID → XbyK Tag GUID lookup
    // Keys: KX13 NodeGUIDs from the migration plan's Custom Value Transforms table
    // Values: Actual taxonomy tag GUIDs from XbyK — run:
    //   SELECT TagName, CAST(TagGUID AS CHAR(36)) FROM CMS_Tag
    //     WHERE TagTaxonomyID = (SELECT TaxonomyID FROM CMS_Taxonomy WHERE TaxonomyName = 'MyTaxonomy')
    var nodeGuidToTagGuid = new Dictionary<Guid, Guid>
    {
        [new Guid("KX13-NODE-GUID-1")] = new Guid("XBYK-TAG-GUID-1"),
        [new Guid("KX13-NODE-GUID-2")] = new Guid("XBYK-TAG-GUID-2"),
    };

    var m = new MultiClassMapping(targetClassName, target => { /* ... */ });

    // Use a NON-RELATIONSHIP source field (e.g., primary key) to avoid ConvertToPages override.
    // Do NOT use the docrelationships field name as the ConvertFrom source field.
    var taxonomyField = m.BuildField("TargetTaxonomyField");
    taxonomyField.WithoutSource("taxonomy");
    taxonomyField.ConvertFrom(Source_ClassName, "SourcePrimaryKeyField", false, (value, context) =>
    {
        if (context is not ConvertorTreeNodeContext treeNodeContext)
            return null;
        if (relationshipFieldGuid == Guid.Empty)
            return null;

        // Look up the source node by GUID to get its NodeID
        var sourceNode = modelFacade.SelectWhere<ICmsTree>(
            "NodeGUID = @nodeGuid",
            new SqlParameter("nodeGuid", treeNodeContext.NodeGuid)).FirstOrDefault();
        if (sourceNode == null)
            return null;

        // Query CMS_Relationship for this node's relationships
        var relations = relationshipService.GetNodeRelationships(
            sourceNode.NodeID, Source_ClassName, relationshipFieldGuid);

        var tagReferences = new List<object>();
        foreach (var relation in relations)
        {
            if (relation.RightNode is not null &&
                nodeGuidToTagGuid.TryGetValue(relation.RightNode.NodeGUID, out var tagGuid))
            {
                // PascalCase "Identifier" — matches XbyK TagReference property
                tagReferences.Add(new { Identifier = tagGuid });
            }
        }

        return tagReferences.Count > 0
            ? System.Text.Json.JsonSerializer.Serialize(tagReferences)
            : null;
    });
    taxonomyField.WithFieldPatch(f =>
    {
        f.Caption = "Tag field";
        f.Visible = true;   // Required — FormDefinitionHelper resets visibility for "taxonomy" type
        f.Enabled = true;   // Required — ensures the field is editable
        f.Settings["controlname"] = "Kentico.Administration.TagSelector";
        f.Settings["TaxonomyGroup"] = "[\"TAXONOMY-GROUP-GUID\"]";
    });

    return m;
}
```

**ConvertToPages pipeline warning:** The migration tool's `ContentItemMapper.MapProperties` method scans every mapped field and checks whether the source field's form control is `docrelationships` or `Pages`. If it matches, it unconditionally applies a `ConvertToPages` directive that:

1. Takes the converter's output (your taxonomy JSON string).
2. Discards it and replaces it with `ContentItemReference` GUIDs resolved from `SpoiledGuidContext`.
3. Saves this replacement to the target field, overwriting your converter's result.

This produces wrong GUIDs in the target field (e.g., spoiled node GUIDs instead of taxonomy tag GUIDs). When the target field is a taxonomy, object code name, or any non-page type, the fix is to use a source field that is **not** a `docrelationships` or `Pages` type in `ConvertFrom`, so the pipeline does not detect it as a relationship field. The converter receives the irrelevant source value and ignores it — it queries `CMS_Relationship` directly via `CmsRelationshipService`.

**When NOT to apply this workaround:** If the target field is a `contentitemreference` (Pages) field that should link to migrated pages, keep the original `docrelationships` source field. In that case, `ConvertToPages` is the correct behavior — it resolves KX13 node references to XbyK content item GUIDs.

---

## 6. Common Converter Snippets

### DocumentName / NodeName Extraction

Extract the page name as a field value. Use for pages that have no custom name field.

```csharp
m.BuildField("TargetName")
    .ConvertFrom("SourceClass", "DocumentName", false, (value, context) =>
    {
        return value?.ToString()?.Trim();
    });
```

### Name Splitting (First / Last)

Split a full name into first and last name fields.

```csharp
// First name — everything before the last space
m.BuildField("FirstName")
    .ConvertFrom("SourceClass", "DocumentName", false, (value, context) =>
    {
        if (value is not string name || string.IsNullOrWhiteSpace(name))
            return null;
        var lastSpace = name.LastIndexOf(' ');
        return lastSpace > 0 ? name[..lastSpace].Trim() : name.Trim();
    });

// Last name — everything after the last space
m.BuildField("LastName")
    .ConvertFrom("SourceClass", "DocumentName", false, (value, context) =>
    {
        if (value is not string name || string.IsNullOrWhiteSpace(name))
            return null;
        var lastSpace = name.LastIndexOf(' ');
        return lastSpace > 0 ? name[(lastSpace + 1)..].Trim() : null;
    });
```

### Taxonomy Tag GUID Lookup

Map a free-text value to a taxonomy tag GUID. Query the KX13 database for distinct source values to populate dictionary keys. The dictionary values (GUIDs) must come from the XbyK instance after taxonomies are created.

**Important:** Use PascalCase `Identifier` in the JSON output — XbyK's `TagReference` class expects `Identifier`, not `identifier`.

```csharp
// Dictionary keys: populated from KX13 DB query:
//   SELECT DISTINCT Specialty FROM MedioClinic_Doctor WHERE Specialty IS NOT NULL
// Dictionary values: TODO — replace with actual taxonomy tag GUIDs from XbyK after
//   creating the taxonomy. Query XbyK:
//   SELECT TagName, TagGUID FROM CMS_Tag
//     WHERE TagTaxonomyID = (SELECT TaxonomyID FROM CMS_Taxonomy WHERE TaxonomyName = 'MedicalSpecialty')
var specialtyLookup = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase)
{
    ["Cardiology"] = new Guid("TODO-GUID-AFTER-XBK-TAXONOMY-SETUP"),
    ["Emergency Medicine"] = new Guid("TODO-GUID-AFTER-XBK-TAXONOMY-SETUP"),
    ["General Practice"] = new Guid("TODO-GUID-AFTER-XBK-TAXONOMY-SETUP"),
};

m.BuildField("DoctorSpecialty")
    .ConvertFrom("MedioClinic.Doctor", "Specialty", false, (value, context) =>
    {
        if (value is not string specialty || string.IsNullOrWhiteSpace(specialty))
            return null;
        if (!specialtyLookup.TryGetValue(specialty.Trim(), out var guid))
            return null;
        // PascalCase "Identifier" — matches XbyK TagReference property
        return $"[{{\"Identifier\":\"{guid}\"}}]";
    });
```

### Data Type Conversion (double to decimal)

```csharp
m.BuildField("TargetDecimalField")
    .ConvertFrom("SourceClass", "SourceDoubleField", true, (value, context) =>
    {
        return value switch
        {
            double d => (decimal)d,
            float f => (decimal)f,
            null => null,
            _ => Convert.ToDecimal(value)
        };
    });
```

### Country Code to Name Lookup

Convert a KX13 country selector value to a display name. Query the KX13 database first to discover the actual value format — KX13 `countrySelector` fields may store simple ISO codes (e.g., `US`), full names, or composite values (e.g., `USA;NewHampshire` for country+state).

```csharp
// First, query KX13 to discover actual format:
//   SELECT DISTINCT Country FROM <SourceTable> WHERE Country IS NOT NULL
// Then build the lookup based on the actual format found.

// Example: composite "Country;State" format discovered in KX13 data
var countryLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    ["USA"] = "United States",
    ["GBR"] = "United Kingdom",
    ["CZE"] = "Czech Republic",
    // ... populated from KX13 distinct values
};

m.BuildField("Country")
    .ConvertFrom("SourceClass", "Country", true, (value, context) =>
    {
        if (value is not string raw || string.IsNullOrWhiteSpace(raw))
            return null;
        // Handle composite format: "CountryCode;State" → extract country part
        var code = raw.Contains(';') ? raw.Split(';')[0] : raw;
        return countryLookup.TryGetValue(code.Trim(), out var name) ? name : raw;
    });
```

### Date Type Conversion (date to datetime)

```csharp
m.BuildField("EventDate")
    .ConvertFrom("SourceClass", "EventDate", true, (value, context) =>
    {
        return value switch
        {
            DateTime dt => dt,
            string s when !string.IsNullOrWhiteSpace(s) => DateTime.Parse(s),
            null => null,
            _ => throw new InvalidOperationException($"Unexpected date value: {value}")
        };
    });
```

### Null-Safe Value Extraction

General pattern for null-safe value extraction with type checking.

```csharp
m.BuildField("TargetField")
    .ConvertFrom("SourceClass", "SourceField", includeDefinition, (value, context) =>
    {
        // Always handle null
        if (value is null)
            return null;

        // Type-check and transform
        if (value is string s)
            return string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        // Unexpected type — fail loudly to catch issues during migration
        throw new InvalidOperationException(
            $"Unexpected value type {value.GetType().Name} for SourceField");
    });
```

### UserID to Code Name Resolution

Convert a KX13 UserID to a user code name for XbyK `objectcodenames` fields. Query the KX13 database to build the lookup dictionary instead of hardcoding placeholder values.

```csharp
// Populated from KX13 DB query:
//   SELECT u.UserID, u.UserName FROM CMS_User u
//     WHERE u.UserID IN (SELECT DISTINCT UserAccount FROM MedioClinic_Doctor WHERE UserAccount IS NOT NULL)
var userLookup = new Dictionary<int, string>
{
    [52] = "doctor.smith",
    [53] = "doctor.jones",
    // ... all mappings resolved from KX13 CMS_User table
};

m.BuildField("DoctorUserAccount")
    .ConvertFrom("MedioClinic.Doctor", "UserAccount", false, (value, context) =>
    {
        if (value is not int userId)
            return null;
        return userLookup.TryGetValue(userId, out var codeName) ? codeName : null;
    });
```
