# Content Item Director API Reference

This document covers the complete API for creating custom content item directors (`ContentItemDirectorBase`) in the Kentico Migration Tool. Directors control per-item migration behavior during the `--pages` phase — linked page handling, page-to-widget conversion, child page linking, and template overrides.

**Source:** [Migration.Tool.Extensions README — Customize linked page handling](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool/blob/master/Migration.Tool.Extensions/README.md#customize-linked-page-handling), [Migrate pages to widgets](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool/blob/master/Migration.Tool.Extensions/README.md#migrate-code-pages-to-widgets), [Custom child links](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool/blob/master/Migration.Tool.Extensions/README.md#custom-child-links), [Kentico Docs — Transfer page hierarchy to Content Hub](https://docs.kentico.com/guides/upgrade-to-xbyk/upgrade-deep-dives/transfer-page-hierarchy-to-content-hub), [Kentico Docs — Convert child pages to widgets](https://docs.kentico.com/guides/upgrade-to-xbyk/upgrade-deep-dives/convert-child-pages-to-widgets)

---

## Required Namespaces

```csharp
using Migration.Tool.Source.Mappers.ContentItemMapperDirectives;
using Newtonsoft.Json.Linq;              // for JObject in AsWidget property filling
using Microsoft.Extensions.DependencyInjection;  // for service registration
```

---

## 1. ContentItemDirectorBase Class

Abstract base class with two virtual methods. Subclass it and override one or both methods to control migration behavior.

```csharp
public class MyDirector : ContentItemDirectorBase
{
    // Override to control regular page/content item migration
    public override void Direct(ContentItemSource source, IContentItemActionProvider options)
    {
        // ... conditional logic ...
        // NOTE: Direct() is abstract — do NOT call base.Direct(). Leave default case empty.
    }

    // Override to control linked page migration
    public override void DirectLinkedNode(LinkedPageSource source, ILinkedPageActionProvider options)
    {
        // ... conditional logic ...
        base.DirectLinkedNode(source, options);  // fallback for unhandled cases (virtual — OK to call)
    }
}
```

**Important:**

- `Direct()` is **abstract** — do NOT call `base.Direct(source, options)` (causes `CS0205`). For unhandled cases, leave the default branch empty (no action = standard migration behavior).
- `DirectLinkedNode()` is **virtual** — calling `base.DirectLinkedNode(source, options)` is valid and preserves default behavior (Materialize).

---

## 2. Direct() Method API

```csharp
public override void Direct(ContentItemSource source, IContentItemActionProvider options)
```

Called for every content item being migrated during `--pages`. Use conditional logic on `source` properties to decide what action to take.

### ContentItemSource Properties

| Property | Type | Description |
|---|---|---|
| `SourceClassName` | `string` | KX13 page type code name (e.g., `"MedioClinic.Company"`) |
| `SourceNode` | `ICmsTree` | The source page tree node with navigation and metadata properties |
| `ChildNodes` | `IEnumerable<ICmsTree>` | Direct child nodes (use with `LinkChildren`) |

### SourceNode Properties (ICmsTree)

| Property | Type | Description |
|---|---|---|
| `NodeAliasPath` | `string` | Full tree path (e.g., `"/Contact-us/Medio-Clinic"`) |
| `NodeClassID` | `int` | Class ID of the page type |
| `NodeName` | `string` | Node name / alias |
| `NodeGUID` | `Guid` | Unique identifier |
| `NodeParentID` | `int` | Parent node ID |
| `NodeLevel` | `int` | Depth in the tree (0 = root) |
| `NodeOrder` | `int` | Sort order among siblings |
| `NodeSiteID` | `int` | Site ID |

### IContentItemActionProvider Methods

| Method | Description |
|---|---|
| `options.LinkChildren(fieldName, childNodes)` | Link filtered child pages as content item references in the specified field |
| `options.OverridePageTemplate(templateIdentifier)` | Change the page template used in XbyK |
| `options.AsWidget(widgetIdentifier, null, null, configAction)` | Convert this page to a widget on an ancestor page |
| `options.Drop()` | Skip migration of this content item entirely |

If none of these methods is called, the page migrates with default behavior. Do not call `base.Direct()` — it is abstract and will not compile.

---

## 3. AsWidget Deep Dive

Converts a page into a Page Builder widget instance on an ancestor page.

```csharp
options.AsWidget("Namespace.Widgets.WidgetIdentifier", null, null, opts =>
{
    // 1. Configure widget location
    opts.Location
        .OnAncestorPage(-1)                            // -1 = parent, -2 = grandparent
        .InEditableArea("editableAreaName")             // matches @await Html.Kentico().EditableAreaAsync("editableAreaName")
        .InSection("Namespace.SectionIdentifier")       // section type identifier
        .InFirstZone();                                 // places widget in the first zone of the section

    // 2. Configure widget properties
    opts.Properties.Fill(true, (itemProps, reusableItemGuid, childGuids) =>
    {
        // Option A: Copy all source properties as-is
        var widgetProps = JObject.FromObject(itemProps);

        // Option B: Build properties manually
        var widgetProps = new JObject();

        // Link to the converted Content Hub reusable item (single reference)
        widgetProps["propertyName"] = LinkedItemPropertyValue(reusableItemGuid!.Value);

        // Link to child content items converted to Content Hub items (multiple references)
        widgetProps["childrenPropertyName"] = LinkedItemsPropertyValue(childGuids);

        // Set a media file property
        // widgetProps["imageProperty"] = MediaFilePropertyValue("MEDIA_FILE_GUID");

        // Set static/custom values
        widgetProps["alignment"] = "ImageLeft";

        return widgetProps;
    });
});
```

### AsWidget Parameters

| Parameter | Type | Description |
|---|---|---|
| `widgetIdentifier` | `string` | Widget type identifier matching the XbyK target project (e.g., `"DancingGoat.Widgets.AboutUsSection"`) |
| Parameter 2 | `null` | Reserved — pass `null` |
| Parameter 3 | `null` | Reserved — pass `null` |
| `configAction` | `Action<WidgetOptions>` | Lambda to configure location and properties |

### Location Chain

The location chain is **required** and must include all four steps:

| Method | Parameter | Description |
|---|---|---|
| `OnAncestorPage(level)` | `int` | Ancestor level: `-1` = direct parent, `-2` = grandparent, etc. |
| `InEditableArea(name)` | `string` | Editable area identifier from the XbyK page template view |
| `InSection(identifier)` | `string` | Section type identifier from the XbyK project |
| `InFirstZone()` | — | Places widget in the first zone of the section |

### Properties.Fill Callback

```csharp
opts.Properties.Fill(bool convertToReusable, Func<JObject, Guid?, IEnumerable<Guid>, JObject> callback)
```

| Parameter | Type | Description |
|---|---|---|
| `convertToReusable` | `bool` | `true` to also create a Content Hub reusable item from the source page |
| `itemProps` | `JObject` | Source page properties as JSON |
| `reusableItemGuid` | `Guid?` | GUID of the converted Content Hub item (available when `convertToReusable` is `true`) |
| `childGuids` | `IEnumerable<Guid>` | GUIDs of child items converted to Content Hub items |

### Helper Methods (inherited from ContentItemDirectorBase)

| Method | Returns | Description |
|---|---|---|
| `LinkedItemPropertyValue(Guid itemGuid)` | `JToken` | Creates a single content item reference value for a widget property |
| `LinkedItemsPropertyValue(IEnumerable<Guid> itemGuids)` | `JToken` | Creates a multiple content item reference value for a widget property |
| `MediaFilePropertyValue(string mediaFileGuid)` | `JToken` | Creates a media file reference value for a widget property |

---

## 4. LinkChildren Deep Dive

Links child pages as content item references on the parent content type. The migration tool auto-creates the reference field on the target content type.

```csharp
options.LinkChildren("FieldName", source.ChildNodes!.Where(x => x.NodeClassID == someClassId));
```

| Parameter | Type | Description |
|---|---|---|
| `fieldName` | `string` | Name of the content item reference field to create/populate on the target type |
| `childNodes` | `IEnumerable<ICmsTree>` | Filtered child nodes — always apply a `Where` filter |

**Key points:**

- Multiple `LinkChildren` calls can create multiple reference fields on the same parent.
- Children can be filtered by `NodeClassID`, `NodeAliasPath`, `NodeName`, or any `ICmsTree` property. Note: `ICmsTree` does NOT have a `SourceClassName` property — use `NodeClassID` instead.
- The same child can appear in multiple reference fields.
- The reference field is auto-created on the target content type if it doesn't exist.

### Example: Multiple child groups

```csharp
public override void Direct(ContentItemSource source, IContentItemActionProvider options)
{
    if (source.SourceClassName == "DancingGoatCore.StoreSection")
    {
        int productSectionClassId = 5508;

        options.LinkChildren("ProductSections",
            source.ChildNodes!.Where(x => x.NodeClassID == productSectionClassId));
        options.LinkChildren("Children",
            source.ChildNodes!.Where(x => x.NodeClassID != productSectionClassId));
    }
}
```

---

## 5. DirectLinkedNode() Method API

```csharp
public override void DirectLinkedNode(LinkedPageSource source, ILinkedPageActionProvider options)
```

Called for every **linked page** (KX13 linked documents) encountered during `--pages` migration. Decides how to handle the link.

### LinkedPageSource Properties

| Property | Type | Description |
|---|---|---|
| `SourceNode` | `ICmsTree` | The node that **contains** the link (the page with the linked document reference) |
| `LinkedNode` | `ICmsTree` | The **target** node being linked to (the original page) |
| `SourceSite` | `ICmsSite` | The site containing the source node |

### ILinkedPageActionProvider Methods

| Method | Description |
|---|---|
| `options.Materialize()` | Create an independent copy of the linked content (default behavior) |
| `options.Drop()` | Skip migration of this linked page entirely |
| `options.StoreReferenceInAncestor(level, fieldName)` | Store a content item reference to the linked page in an ancestor's field |

### StoreReferenceInAncestor Parameters

| Parameter | Type | Description |
|---|---|---|
| `level` | `int` | Relative ancestor level: `-1` = direct parent, `-2` = grandparent, etc. |
| `fieldName` | `string` | Content item reference field name on the ancestor (auto-created if needed) |

**Important:** When the direct parent is a non-migrated type (e.g., `CMS.Folder`), use `-2` or deeper to reach the actual target ancestor.

### Example: Site-based and path-based decisions

```csharp
public override void DirectLinkedNode(LinkedPageSource source, ILinkedPageActionProvider options)
{
    var linkedNode = source.LinkedNode;

    // ICmsTree does not have SourceClassName — use NodeClassID for routing
    switch (linkedNode.NodeClassID)
    {
        case 1234: // MedioClinic.CompanyService class ID
            // Create independent Content Hub copies for linked services
            options.Materialize();
            break;

        case 5678: // MedioClinic.Company class ID
            // Store reference on parent ContactPage
            options.StoreReferenceInAncestor(-1, "ContactPageOfficeLocations");
            break;

        default:
            // Fallback: default materialization (DirectLinkedNode is virtual — base call OK)
            base.DirectLinkedNode(source, options);
            break;
    }
}
```

---

## 6. Source Properties Reference

### ContentItemSource — Conditional Logic Properties

Use these properties in `if`/`switch` statements inside `Direct()`:

| Property Path | Use Case |
|---|---|
| `source.SourceClassName` | Route by KX13 page type (most common) |
| `source.SourceNode.NodeAliasPath` | Route by tree location (path-based logic) |
| `source.SourceNode.NodeClassID` | Route by class ID (useful for child filtering) |
| `source.SourceNode.NodeName` | Route by specific page name |
| `source.SourceNode.NodeLevel` | Route by tree depth |
| `source.SourceNode.NodeSiteID` | Route by site (multi-site scenarios) |
| `source.ChildNodes` | Filter and link child pages |

### LinkedPageSource — Conditional Logic Properties

Use these properties in `if`/`switch` statements inside `DirectLinkedNode()`:

| Property Path | Use Case |
|---|---|
| `source.LinkedNode.NodeClassID` | Route by linked page's class ID (most common — `ICmsTree` does NOT have `SourceClassName`) |
| `source.LinkedNode.NodeAliasPath` | Route by linked page's location |
| `source.LinkedNode.NodeClassID` | Route by linked page's class ID |
| `source.SourceNode.NodeAliasPath` | Route by containing page's location |
| `source.SourceSite.SiteName` | Route by site name |

---

## 7. Service Registration

Directors are registered as **transient** services (not singleton like `IClassMapping`).

```csharp
using Migration.Tool.Extensions.ContentItemDirectors;
using Migration.Tool.Source.Mappers.ContentItemMapperDirectives;

namespace Migration.Tool.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection UseCustomizations(this IServiceCollection services)
    {
        // ... other registrations ...

        services.AddTransient<ContentItemDirectorBase, MyDirector>();

        // ... other registrations ...
    }
}
```

**Per-director extension method pattern** (recommended for generated code):

```csharp
// In the director file:
public static class ContactPageItemDirectorExtensions
{
    public static IServiceCollection AddContactPageItemDirector(this IServiceCollection services)
    {
        // Prerequisite: Ensure --pages is included in the CLI execution
        // Prerequisite: IClassMapping for target types must be registered
        services.AddTransient<ContentItemDirectorBase, ContactPageItemDirector>();
        return services;
    }
}
```

**Important:**

- Use `AddTransient`, NOT `AddSingleton` or `AddScoped`.
- Multiple directors can be registered — the tool evaluates all of them for each page.
- Each director runs independently; order is not guaranteed between directors.

---

## 8. Execution Order and Important Notes

### When Directors Run

- Directors execute during the `--pages` CLI phase.
- `Direct()` is called for every page being migrated.
- `DirectLinkedNode()` is called for every linked page encountered.
- Directors run **before** widget migrations (`IWidgetMigration`) and field migrations (`IFieldMigration`).

### Auto-Created Reference Fields

- `StoreReferenceInAncestor` and `LinkChildren` auto-create content item reference fields on the target content type if they don't already exist.
- Field names should follow the target type's naming convention (e.g., `ContactPageOfficeLocations`).

### Linked Page Processing

- Linked pages are processed in topological order to resolve dependencies.
- Cross-site linked pages are **not supported** — only same-site links are processed.

### Compatibility with Other Extensions

- Directors are compatible with custom `IClassMapping` code — the class mapping restructures the content type, while the director controls per-item behavior.
- A page converted to a widget via `AsWidget` should have its class listed in `ConvertClassesToContentHub` in `appsettings.json` if `Properties.Fill` uses `convertToReusable: true`.
- `OverridePageTemplate` requires the template to exist in the target XbyK instance.
