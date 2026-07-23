# Migration Tool Knowledge Base

## Migration Effort Tiers

Features are classified by migration effort:

- **LOW**: Migration tool handles it with minor code tweaks
- **LOW-MEDIUM**: Tool transfers data but some developer adjustments needed
- **MEDIUM**: Tool transfers data but significant developer work needed
- **HIGH**: Must be rebuilt from scratch in XbyK

## Supported Data Types

The migration tool transfers only database-stored content and related binary files. All code and customizations must be migrated manually.

### What Migrates

The tool **creates content types and their fields automatically** in the target XbyK instance during the `--page-types` phase. Content types do not need to be pre-created manually — the tool creates them based on source page type definitions plus any `IClassMapping` extensions. Similarly, `--categories` migrates KX13 categories as XbyK taxonomies (including taxonomy tags). Custom tables and module classes are also created automatically by `--custom-modules` or `--custom-tables`.

| Source (KX13)        | Target (XbyK)                      | Notes                                                |
| -------------------- | ---------------------------------- | ---------------------------------------------------- |
| Sites                | Website channels                   |                                                      |
| Cultures             | Languages                          |                                                      |
| Page types           | Page content types                 | **Created automatically by the tool**; macros, field categories, inheritance NOT supported |
| Pages                | Web pages                          | Published/Draft/Archived; linked pages become copies by default |
| Page attachments     | Content item assets                | Converted to `Legacy.Attachment` content type; migrated with `--pages` |
| Page templates       | Preset page templates              | KX13 only                                            |
| Categories           | Taxonomies                         | **Created automatically by the tool** via reusable field schema |
| Media libraries      | Content item assets                | Into `Legacy.Mediafile` content type; or media libraries via `MigrateMediaToMediaLibrary` (deprecated) |
| Forms                | Forms                              | Custom controls must be reimplemented                |
| Users (Editor+)      | Users                              | Must have unique email                               |
| Roles (with Editor+) | Roles                              | Permissions NOT migrated                             |
| Members (None priv)  | Members                            | Passwords are `NULL` — must set up password reset     |
| Contacts             | Contacts                           | Target tables must be empty                          |
| Activities           | Activities                         | Custom activities transferred                        |
| Consents             | Consents                           |                                                      |
| Custom modules       | Custom modules                     | **Created automatically by the tool**; UI elements, alternative forms NOT included; `CMS.` prefix removed from code names |
| Custom tables        | Module classes or reusable content | **Created automatically by the tool** via `--custom-tables`; no UI or related data (queries, alternative forms) migrated |
| Setting values       | Settings                           | Only settings that exist in XbyK                     |
| Customers            | Customers                          | KX13 only; `CommerceConfiguration` required          |
| Orders               | Orders                             | KX13 only; order statuses must be created manually and mapped via `OrderStatuses` config |

### What Does NOT Migrate

**Content-related:**

- Macro expressions in page type field default values or other settings
- Page type inheritance (page types that inherit fields from other types cannot be migrated)
- Field categories for page types
- Page permissions (ACLs)
- UI elements, alternative forms, and UI pages for custom modules
- Categories stored as a field of pages, and personal categories

**Non-content (for awareness):**

- Contact groups (static not supported in XbyK; dynamic have incompatible condition format)
- Marketing automation, content personalization, email marketing (A/B testing not available)
- Personas and lead scoring, social marketing
- Search, A/B testing, web analytics, reporting — require integrations or custom modules
- Integration bus — not available in XbyK
- External sign-in information for users; user password hashes (member passwords are `NULL`)
- Custom settings under modules (need custom modules in XbyK)
- License keys

## Field Type and Form Control Mapping

The migration tool automatically maps source data types and form controls to XbyK equivalents during `--page-types`. This mapping is not possible for custom data types or form controls — check content type fields after migration and adjust if necessary.

| Source Data Type         | Target Data Type         | Source Form Control     | Target Form Component |
| ------------------------ | ------------------------ | ----------------------- | --------------------- |
| Text                     | Text                     | Text box                | Text input            |
| Text                     | Text                     | Drop-down list          | Dropdown selector     |
| Text                     | Text                     | Radio buttons           | Radio button group    |
| Text                     | Text                     | Text area               | Text area             |
| Text                     | Text                     | _other_                 | Text input            |
| Long text                | Long text                | Rich text editor        | Rich text editor      |
| Long text                | Long text                | Text box                | Text input            |
| Long text                | Long text                | Drop-down list          | Dropdown selector     |
| Long text                | Long text                | Text area               | Text area             |
| Long text                | Long text                | _other_                 | Rich text editor      |
| Integer number           | Integer number           | _any_                   | Number input          |
| Long integer number      | Long integer number      | _any_                   | Number input          |
| Floating-point number    | Floating-point number    | _any_                   | Number input          |
| Decimal number           | Decimal number           | _any_                   | Decimal number input  |
| Date and time            | Date and time            | _any_                   | Datetime input        |
| Date                     | Date                     | _any_                   | Date input            |
| Time interval            | Time interval            | _any_                   | None (not supported)  |
| Boolean (Yes/No)         | Boolean (Yes/No)         | _any_                   | Check box             |
| Attachments              | Content item assets      | _any_ (Attachments)     | Media file selector   |
| File                     | Content item assets      | _any_ (Direct uploader) | Media file selector   |
| Unique identifier (Guid) | Unique identifier (Guid) | _any_                   | None (not supported)  |
| Pages                    | Pages                    | _any_ (Pages)           | Page selector         |

Attachment and File fields are converted to content item assets (`Legacy.Attachment` content type) during migration.

Text fields using the **Media selection** form control can be optionally converted to content item assets via `OptInFeatures.CustomMigration.FieldMigrations` configuration — see [Media Text Field Conversion](#media-text-field-conversion).

## Linked Pages in KX13

In Kentico Xperience 13, a **linked page** is a content tree node that references another page (the "original") rather than storing its own content. Linked pages exist as separate `TreeNode` items in the content tree but are empty shells — all field data lives on the original page. Any edit to the original is reflected in all linked pages, and vice versa.

### Key characteristics

Linked pages have a non-null `NodeLinkedNodeID` which stores the `NodeID` of the original page. The linked page has its own `NodeID` but shares content via the link. Linked page data is provided in the source audit input.

- **Shared content**: The linked page stores no field data. All content comes from the original. Editing either side updates the same data.
- **Separate tree node**: Each linked page has its own `NodeID` and `NodeAliasPath`, so it appears as a distinct item in the content tree.
- **Own URL**: The original and linked pages can have different URLs (based on their content tree position). Kentico recommends canonical link elements to avoid SEO duplicate content issues.
- **Own permissions**: Each linked page can have independent page-level ACLs.
- **Multilingual**: A linked page represents all culture versions of the original. Deleting a linked page removes the link across all cultures.

### Common KX13 patterns

1. **Page taxonomy / categorization**: Parent pages act as categories. Linked pages let a single content item appear under multiple category parents (e.g., a product in two departments).
2. **Content reuse with "Reused-content" folders**: Original content items are stored in a shared folder (e.g., `/Reused-content/Company-services/`), and linked pages are placed where the content is displayed (e.g., `/Home/Cardio-therapy` → links to `/Reused-content/Company-services/Cardio-therapy`). The originals serve as content snippets with no own live-site URL.

### Migration implications

By default, the migration tool **materializes** linked pages — it creates a separate content item for each linked page, duplicating data that was shared in KX13. This is often undesirable because:

- It creates duplicate content items in XbyK where KX13 had only one source of truth.
- Editors would need to maintain multiple copies instead of one.

The `ContentItemDirectorBase.DirectLinkedNode()` extension point controls linked page behavior during `--pages`:

| Strategy | Behavior | When to use |
|---|---|---|
| `Materialize()` | Creates a separate, independent content item (default) | When intentional content duplication is acceptable or the linked page needs to diverge post-migration |
| `Drop()` | Skips the linked page entirely | When the original page is sufficient and the linked page's tree position is not needed in XbyK |
| `StoreReferenceInAncestor(level, field)` | Stores a content item reference to the original on an ancestor page | When the ancestor page needs a reference field pointing to the content (e.g., a homepage referencing service items) |

**Every linked page must have an explicit strategy in the migration plan.** Unhandled linked pages silently create duplicates.

## Extension Points

The migration tool provides five extension points for customization:

### 1. Class Mappings (IClassMapping via MultiClassMapping)

Controls content type structure transformation. Use for:

- **Merging** multiple source page types into one target content type
- **Splitting** one page type into multiple target types
- **Remodeling** page types (rename fields, change data types/form controls)
- **Converting** page types to reusable content or Content hub items
- **Creating reusable field schemas** shared across types

Key API:

- `MultiClassMapping(targetClassName, configureTarget)` — define target class
- `BuildField("FieldName")` — define a target field
- `SetFrom("SourceClass", "SourceField", isTemplate)` — map source field
- `ConvertFrom("SourceClass", "SourceField", includeDefinition, converter)` — transform values
- `WithFieldPatch(patch)` — modify field definition (data type, form component, caption, settings) via `FormFieldInfo` properties. Runs during `--page-types` after built-in `IFieldMigration` processing. Use for class-scoped definition changes (e.g., changing `DataType`, `Settings["controlname"]`). Does NOT transform field values — pair with `ConvertFrom` if value conversion is also needed.
- `ReusableSchemaBuilder` — create shared field schemas
- `FilterCategories(filter)` — filter categories per class

Runs during: `--page-types`, `--custom-modules`

### 2. Content Item Directors (ContentItemDirectorBase)

Controls per-page migration behavior. Use for:

- **Linked page handling**: Materialize (copy), Drop (skip), or StoreReferenceInAncestor
- **Pages to widgets**: Convert pages into Page Builder widget instances
- **Child page references**: Link child pages as content item references on parents
- **Page template override**: Change page template during migration

Two override methods target different page types:

**`Direct(source, options)` — regular pages / content items** (`IContentItemActionProvider`):

- `options.LinkChildren(fieldName, childNodes)` — link filtered direct child pages as content item references in the specified field on the parent content type. The reference field is auto-created on the target type if it doesn't exist. Multiple calls can create multiple reference fields on the same parent. Always filter `source.ChildNodes` with a `Where` clause. Requires both parent and child types to be configured for Content Hub (via `ConvertClassesToContentHub` or `ClassContentTypeType.REUSABLE` in `IClassMapping`).
- `options.AsWidget(widgetType, ...)` — convert page to widget on an ancestor page
- `options.OverridePageTemplate(templateName)` — change template
- `options.Drop()` — skip migration of this content item

**`DirectLinkedNode(source, options)` — linked pages only** (`ILinkedPageActionProvider`):

- `options.Materialize()` — create independent copy (default)
- `options.Drop()` — skip migration
- `options.StoreReferenceInAncestor(level, fieldName)` — store a content item reference to the original linked content on an ancestor page. `level` is a negative integer: `-1` = direct parent, `-2` = grandparent. The reference field is auto-created if it doesn't exist.

**`LinkChildren` vs `StoreReferenceInAncestor`**: These serve different scenarios. `LinkChildren` is called on the **parent** page in `Direct()` to link its direct children as references — use for parent-child hierarchy preservation (e.g., Store → ProductSections). `StoreReferenceInAncestor` is called on the **linked page** in `DirectLinkedNode()` to store a reference on an ancestor — use for linked page deduplication (e.g., linked CompanyService under Home → store reference on Home page).

Runs during: `--pages`

### 3. Field Migrations (IFieldMigration)

Transforms individual field values and definitions. Use for:

- Custom form control conversions
- Data type transformations
- Value format changes

Key API:

- `Rank` — priority (lower wins)
- `ShallMigrate(context)` — selection based on data type, form control, field name
- `MigrateFieldDefinition(...)` — transform XML field definition
- `MigrateValue(value, context)` — transform field value

Runs during: `--pages`, `--forms`, `--custom-tables`

### 4. Widget Migrations (IWidgetMigration)

Transforms entire widget instances. Use for:

- Renaming widget types
- Consolidating/restructuring widgets
- Remapping widget data

Key API:

- `ShallMigrate(context, identifier)` — selection based on widget type, site
- `MigrateWidget(identifier, value, context)` — transform widget data

Runs during: `--pages`

### 5. Widget Property Migrations (IWidgetPropertyMigration)

Transforms individual widget property values. Use for:

- Converting page paths to content item GUIDs
- Updating image references to content item assets
- Format conversions for property values

Key API:

- `ShallMigrate(context, propertyName)` — selection based on property name, form control, site
- `MigrateWidgetProperty(key, value, context)` — transform property value

Runs during: `--pages`

### Working with Source and Target APIs

When implementing custom extensions (`IClassMapping`, `ContentItemDirectorBase`, `IFieldMigration`, `IWidgetMigration`, `IWidgetPropertyMigration`, `IPipelineBehavior`), use these APIs to interact with source and target instances:

**Querying source data — `ModelFacade`**:

- Primary API for reading source instance data in custom extensions
- Provides version-agnostic access to source database objects (pages, page types, custom tables, relationships)
- Inject via constructor: `ModelFacade modelFacade`
- Example: look up page data by `NodeID`, query custom table records, resolve page type field definitions

**Writing to target — `IImporter` with UMT models**:

- Primary API for writing data to the target XbyK instance
- Uses Universal Migration Tool (UMT) model classes:
  - `ContentItemModel` — content item creation/update
  - `DataClassModel` — content type definitions
  - `ContentItemLanguageMetadataModel` — language-specific content metadata
  - `WebPageItemModel` — web page items in website channels
- Inject via constructor: `IImporter importer`
- Native Xperience table APIs are also available for advanced scenarios not covered by UMT models

## XbyK Default Page Builder Widgets

XbyK ships with built-in Page Builder widgets that are direct equivalents of KX13 built-in widgets:

| XbyK Widget | Properties | KX13 Equivalent |
|---|---|---|
| **Rich text** | `RichTextWidgetProperties` — `Content` (string, HTML) | `Kentico.Widget.RichText` (`content`) |
| **Form** | `FormWidgetProperties` — `SelectedForm`, `AfterSubmitMode`, etc. | `Kentico.FormWidget` (`selectedForm`) |

Identifiers are available via `SystemComponentIdentifiers` (e.g., `SystemComponentIdentifiers.RICH_TEXT_WIDGET_IDENTIFIER`).

**Built-in KX13 system widgets migrate with minimal effort in any migration mode** (legacy, API Discovery, or custom). API Discovery gives the cleanest result by mapping them to native XbyK UI controls. Never write custom `IWidgetMigration` code for built-in widgets that have a direct XbyK equivalent. Never consolidate a built-in widget with a custom widget into a new custom widget — this replaces a minimal-effort migration with unnecessary code.

## Widget Migration Approaches

Three approaches for widget migration:

1. **Lift-and-shift (Legacy Mode)**: Data transfers as-is. Fast but uses deprecated compatibility mode.
2. **Source Instance API Discovery**: Automatically maps built-in KX13 widget properties to native XbyK form components. Recommended baseline for the cleanest result.
3. **Custom migration**: Use the upgrade to modernize widgets, restructure content model, and leverage Content hub. Only needed for custom widgets that have no built-in XbyK counterpart.

Built-in system widgets (Rich text, Form) migrate with minimal effort in any mode. Start with them as test cases to validate the migration process before tackling custom widgets.

## Key Configuration Options (appsettings.json)

| Setting                                         | Purpose                                                               |
| ----------------------------------------------- | --------------------------------------------------------------------- |
| `ConvertClassesToContentHub`                    | List of page types/custom tables/module classes to migrate as reusable content items |
| `CreateReusableFieldSchemaForClasses`           | List of page types to convert to reusable field schemas. Cannot be combined with `ReusableSchemaBuilder` in custom class mappings. |
| `CustomModuleClassDisplayNamePatterns`          | Display name patterns for content items migrated from custom module classes (e.g. `"Item-{CustomClassGuid}"`) |
| `EntityConfigurations`                          | Per-object-type config (exclude code names via `ExcludeCodeNames`, explicit PK mapping). Keys are **database table names** with underscores (e.g., `CMS_Class`, `CMS_SettingsKey`, `CMS_Site`), NOT code names with dots |
| `OptInFeatures.QuerySourceInstanceApi`          | Enable Source Instance API Discovery for widget migration             |
| `OptInFeatures.CustomMigration.FieldMigrations` | Convert text fields using Media selection form control to content item assets |
| `MigrateOnlyMediaFileInfo`                      | Skip binary media file transfer (useful for shared/cloud storage)     |
| `MigrateMediaToMediaLibrary`                    | Migrate media as media libraries instead of content item assets (deprecated — media libraries will be removed in future XbyK) |
| `MemberIncludeUserSystemFields`                 | Which user system fields from `CMS_User`/`CMS_UserSettings` to include for Members |
| `IncludeExtendedMetadata`                       | Migrate `DocumentPageTitle`, `DocumentPageDescription`, `DocumentPageKeywords` |
| `UseOmActivityNodeRelationAutofix`              | Handle activity references to non-existing pages: `DiscardData`, `AttemptFix`, or `Error` |
| `UseOmActivitySiteRelationAutofix`              | Handle activity site references: `DiscardData`, `AttemptFix`, or `Error` |
| `TargetWorkspaceName`                           | Code name of the XbyK workspace for migrated content items            |
| `AssetRootFolders`                              | Dictionary defining root content folder per site for asset content items |
| `CommerceConfiguration`                         | Commerce data migration settings (KX13 only): `CommerceSiteNames`, `OrderStatuses`, `IncludeCustomerSystemFields`, `IncludeAddressSystemFields`, `IncludeOrderSystemFields`, `IncludeOrderItemsSystemFields`, `IncludeOrderAddressSystemFields`, `KX13OrderFilter`, `SystemFieldPrefix` |

## Media Text Field Conversion

Text fields using the **Media selection** form control store media file paths as plain text. By default, these migrate as-is (text values). Use `OptInFeatures.CustomMigration.FieldMigrations` to convert them to content item assets during migration.

```json
"OptInFeatures": {
  "CustomMigration": {
    "FieldMigrations": [
      {
        "ClassName": "Namespace.PageType",
        "FieldName": "HeaderImage",
        "TargetFormComponent": "Kentico.Administration.ContentItemAssetSelector"
      }
    ]
  }
}
```

Each entry specifies the source class name, field name, and the target form component. The tool converts the text media path to a content item asset reference during `--pages`.

## Custom Tables Migration Decision

Custom tables (`--custom-tables`) can be migrated to two target structures:

| Target | Configuration | When to Use |
| --- | --- | --- |
| **Custom module classes** (default) | No extra config needed | Structured data that editors don't manage in Content hub (settings, lookup tables, system records) |
| **Reusable content items** | Add class name to `ConvertClassesToContentHub` | Content-like data that editors should manage in Content hub (shared snippets, reference data used by pages) |

Decision criteria: Will editors need to create, edit, or link this data in Content hub? If yes, use reusable content items. If the data is system-managed or read-only, module classes are simpler.

## Workspace and Asset Folder Configuration

- **`TargetWorkspaceName`**: Specifies which XbyK workspace receives migrated content items. Set this to the target workspace code name if your XbyK instance uses multiple workspaces. Omit or leave as default if using a single workspace.
- **`AssetRootFolders`**: A dictionary mapping site code names to root content folder paths for asset content items. Controls the folder structure under which migrated media and attachment assets are organized in Content hub.

```json
"Settings": {
  "TargetWorkspaceName": "default",
  "AssetRootFolders": {
    "MySiteCodeName": "assets/media"
  }
}
```

## CLI Parameter Dependencies

| Parameter              | Dependencies                                                         |
| ---------------------- | -------------------------------------------------------------------- |
| `--sites`              | (none)                                                               |
| `--custom-modules`     | `--sites`                                                            |
| `--custom-tables`      | (none)                                                               |
| `--users`              | `--sites`, `--custom-modules`                                        |
| `--members`            | `--sites`, `--custom-modules`                                        |
| `--settings-keys`      | `--sites`                                                            |
| `--page-types`         | `--sites`                                                            |
| `--pages`              | `--sites`, `--users`, `--page-types`                                 |
| `--type-restrictions`  | `--sites`, `--page-types`, `--pages`                                 |
| `--categories`         | `--sites`, `--users`, `--page-types`, `--pages`                      |
| `--media-libraries`    | `--sites`, `--custom-modules`, `--users`                             |
| `--forms`              | `--sites`, `--custom-modules`, `--users`                             |
| `--contact-management` | `--users`, `--custom-modules`                                        |
| `--data-protection`    | `--sites`, `--users`, `--contact-management`                         |
| `--customers`          | `--sites`, `--custom-modules`, `--users`, `--members`                |
| `--orders`             | `--sites`, `--custom-modules`, `--users`, `--members`, `--customers` |

Use `--bypass-dependency-check` to skip dependency validation on repeated runs when dependencies were already migrated.
