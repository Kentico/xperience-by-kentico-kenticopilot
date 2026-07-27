# Migration Detail — {Project Name}

## Source Content Model

### Page Types

| Class Name               | Display Name   | Used At                  | Notes |
| ------------------------ | -------------- | ------------------------ | ----- |
| {Namespace.PageTypeName} | {Display Name} | {URL paths or locations} |       |

#### {Namespace.PageTypeName} — Fields

| Field Name      | Data Type | Form Control | Notes   |
| --------------- | --------- | ------------ | ------- |
| {FieldCodeName} | {text / longtext / integer / decimal / boolean / datetime / guid / binary / file / docrelationships} | {FormControlName} | {Field purpose, relationships, special behavior} |

<!-- Repeat the field table for each page type -->

#### Class Inheritance Hierarchy

KX13 page types can inherit from parent classes (`ClassInheritsFromClassID`). Parent classes **must not be excluded** via `ExcludeCodeNames` — the migration tool requires the full inheritance chain to process child classes during `--page-types`.

| Child Class            | Parent Class            | Inherited Fields                |
| ---------------------- | ----------------------- | ------------------------------- |
| {Namespace.ChildClass} | {Namespace.ParentClass} | {Inherited field names or none} |

<!-- Include this section when ANY page type in the source has a non-null ClassInheritsFromClassID.
  - List every inheritance relationship, including transitive ones (grandchild → child → parent).
  - "Inherited Fields" shows which fields on the child come from the parent's form definition
    (fields with isinherited="true" in ClassFormDefinition).
  - Parent classes with 0 page instances are safe to migrate — they create empty content type
    definitions. But they MUST NOT be excluded, or all descendants fail.
  - If the inheritance info is not in the audit data, query the KX13 database:
    SELECT ClassName, ClassInheritsFromClassID FROM CMS_Class WHERE ClassIsDocumentType = 1
-->

#### Page Relationships (CMS_Relationship)

KX13 `docrelationships` fields store their data in the `CMS_Relationship` table, not in the page type's coupled data table. Each relationship row connects a source page (`LeftNodeID`) to a target page (`RightNodeID`) via a named relationship from `CMS_RelationshipName`. Ad-hoc relationships (created by `docrelationships` fields) encode the field GUID in the relationship name (`ClassName_FieldGUID`), allowing each row to be mapped back to a specific field.

**Relationship Names:**

| Relationship Name | Display Name | Source Page Type | Source Field | Notes |
| ----------------- | ------------ | ---------------- | ------------ | ----- |
| {ClassName_FieldGUID} | {Display Name} | {Namespace.PageType} | {FieldName} | {RelatedPagesLimit, StartingPath, allowed objects} |

**Relationship Data:**

| Left Node Path | Left Class | Right Node Path | Right Class | Relationship Display Name | Order |
| -------------- | ---------- | --------------- | ----------- | ------------------------- | ----- |
| {/path/to/source} | {Namespace.SourceType} | {/path/to/target} | {Namespace.TargetType} | {Display Name} | {n} |

<!-- Include this section when the source audit contains a "Page Relationships (CMS_Relationship)" section.
  - This data is critical for planning ConvertFrom transforms on docrelationships fields.
  - Each relationship row shows which specific pages are connected, enabling accurate
    lookup dictionary planning (e.g., DayOfWeek NodeGUID → taxonomy tag GUID mappings).
  - The relationship name's FieldGUID suffix maps to the field's guid attribute in
    the page type's ClassFormDefinition XML.
  - If the source audit does not include this data, it can be queried from the KX13 database:
    SELECT DISTINCT r.LeftNodeID, r.RightNodeID, r.RelationshipOrder,
      rn.RelationshipName, rn.RelationshipDisplayName,
      lt.NodeAliasPath AS LeftNodePath, lt.ClassName AS LeftClassName,
      rt.NodeAliasPath AS RightNodePath, rt.ClassName AS RightClassName
    FROM CMS_Relationship r
    JOIN CMS_RelationshipName rn ON r.RelationshipNameID = rn.RelationshipNameID
    JOIN CMS_Tree lt ON r.LeftNodeID = lt.NodeID
    JOIN CMS_Tree rt ON r.RightNodeID = rt.NodeID
    ORDER BY rn.RelationshipName, r.RelationshipOrder
-->

### Custom Tables

| Class Name                  | Display Name   | Notes                    |
| --------------------------- | -------------- | ------------------------ |
| {Namespace.CustomTableName} | {Display Name} | {What this table stores} |

#### {Namespace.CustomTableName} — Fields

| Field Name      | Data Type   | Notes           |
| --------------- | ----------- | --------------- |
| {FieldCodeName} | {data type} | {Field purpose} |

### Module Classes

| Class Name                  | Display Name   | Notes                               |
| --------------------------- | -------------- | ----------------------------------- |
| {Namespace.ModuleClassName} | {Display Name} | {What this module class represents} |

### Page Builder Components

**Page templates:**

- {TemplateName} — properties: {list or "none"}

**Sections:**

| Type Identifier          | Properties                |
| ------------------------ | ------------------------- |
| {Namespace.Section.Name} | {Property list or "none"} |

**Custom widgets:**

| Type Identifier         | Properties                  |
| ----------------------- | --------------------------- |
| {Namespace.Widget.Name} | {Property names with types} |

**Built-in KX13 widgets:**

- {List built-in widgets in use, e.g., Kentico.Widget.RichText, Kentico.FormWidget}

---

## Target Content Model

> The migration tool only creates content types from source data (via `IClassMapping` during `--page-types`). Content types marked **Manual** below have no source mapping and must be created manually in XbyK before or after migration.

### Webpage Content Types

| Class Name                  | Display Name   | Reusable Field Schemas | Created By                | Notes                                |
| --------------------------- | -------------- | ---------------------- | ------------------------- | ------------------------------------ |
| {Namespace.ContentTypeName} | {Display Name} | {Schema names or "—"}  | {Migration tool / Manual} | {Purpose and relationship to source} |

<!-- Created By:
  - Migration tool: has a source class mapped via IClassMapping — the tool creates this type during --page-types
  - Manual: no source equivalent — must be created manually in XbyK
-->

### Content Hub (Reusable) Content Types

| Class Name                  | Display Name   | Created By                | Notes                |
| --------------------------- | -------------- | ------------------------- | -------------------- |
| {Namespace.ContentTypeName} | {Display Name} | {Migration tool / Manual} | {Origin and purpose} |

### Reusable Field Schemas

| Schema Name  | Applied To          | Created By                                                         | Description              |
| ------------ | ------------------- | ------------------------------------------------------------------ | ------------------------ |
| {SchemaName} | {Content type list} | {Migration tool (ReusableSchemaBuilder in IClassMapping) / Manual} | {Fields and their types} |

### Taxonomies

> The migration tool only creates taxonomies from KX13 categories (`--categories`). Taxonomies replacing other KX13 constructs (free-text fields, lookup page types) or entirely new taxonomies must be created manually.

| Taxonomy Name  | Tags         | Created By                                                                           | Notes                                       |
| -------------- | ------------ | ------------------------------------------------------------------------------------ | ------------------------------------------- |
| {TaxonomyName} | {Tag values} | {Migration tool (`--categories`) / Manual (pre-migration) / Manual (post-migration)} | {What KX13 construct it replaces, or "New"} |

<!-- Created By for taxonomies:
  - Migration tool (--categories): migrated from KX13 categories
  - Manual (pre-migration): must exist before --pages because field transforms reference tag GUIDs
  - Manual (post-migration): editorial classification, not needed during migration
-->

---

## Content Model Mapping

### Convert to Content Hub

**Configures:** `appsettings.json` → `ConvertClassesToContentHub` (plus `IClassMapping` per class)

| Source Class            | Target Class            | Notes                                 |
| ----------------------- | ----------------------- | ------------------------------------- |
| {Namespace.SourceClass} | {Namespace.TargetClass} | {Field-level changes, rename details} |

**appsettings.json fragment:**

```json
"ConvertClassesToContentHub": "Namespace.SourceClass1;Namespace.SourceClass2"
```

### Webpage Class Mappings (IClassMapping)

All source page types that map to XbyK webpage content types via `IClassMapping`.

| Source Class               | Target Class                  | Type    | Notes                         |
| -------------------------- | ----------------------------- | ------- | ----------------------------- |
| {Namespace.SourcePageType} | {Namespace.TargetContentType} | website | {Field restructuring details} |

### Merge Mappings

#### {Namespace.MergedContentType} — {Display Name}

**Requires:** Code extension `IClassMapping` (`MultiClassMapping`)

- **Target type**: {website / reusable}
- **Notes**: {Why these are merged, data implications}

| Source Class                | Is Template |
| --------------------------- | ----------- |
| {Namespace.SourcePageType1} | Yes         |
| {Namespace.SourcePageType2} | No          |

Field mapping within `MultiClassMapping`:

| Source Class       | Source Field | Target Field      | Notes                    |
| ------------------ | ------------ | ----------------- | ------------------------ |
| {Namespace.Source} | {FieldName}  | {TargetFieldName} | {Transformation details} |

### Exclusions

**Configures:** `appsettings.json` → `EntityConfigurations` (`ExcludeCodeNames`)

| Source Class              | Reason                                        |
| ------------------------- | --------------------------------------------- |
| {Namespace.ExcludedClass} | {Why excluded and how it's handled otherwise} |

<!-- IMPORTANT: Before finalizing exclusions, cross-reference against the Class Inheritance
  Hierarchy section above. If a class listed here is a parent of any migrated class,
  REMOVE it from ExcludeCodeNames. Parent classes with 0 instances must remain — excluding
  them causes all child classes to fail with "missing dependency ClassInheritsFromClassID".
  Add a note after the table explaining which classes are NOT excluded and why. -->

**appsettings.json fragment:**

```json
"EntityConfigurations": {
  "CMS_Class": {
    "ExcludeCodeNames": [
      "Namespace.ExcludedClass1",
      "Namespace.ExcludedClass2"
    ]
  }
}
```

### Taxonomy Planning

#### Migrated Taxonomies (from KX13 Categories)

Taxonomies created automatically by the migration tool from KX13 categories (`--categories`).

| KX13 Category                | Target Taxonomy      | Target Tags           | Notes                                |
| ---------------------------- | -------------------- | --------------------- | ------------------------------------ |
| {KX13 category name or path} | {XbyK Taxonomy Name} | {Migrated tag values} | {Mapping notes, tag name transforms} |

#### New Taxonomies (manual creation required)

Taxonomies with no KX13 category equivalent. Must be created manually — either pre-migration or post-migration.

| Taxonomy Name  | Tags         | Purpose                         | Referenced By                     | Created By                                         |
| -------------- | ------------ | ------------------------------- | --------------------------------- | -------------------------------------------------- |
| {TaxonomyName} | {Tag values} | {What this taxonomy classifies} | {Content types that reference it} | {Manual (pre-migration) / Manual (post-migration)} |

<!-- Created By for new taxonomies:
  - Manual (pre-migration): must exist before --pages because field transforms reference tag GUIDs
  - Manual (post-migration): only referenced by target-only content types (created after migration) or used solely for editorial classification — not needed during migration
-->

---

## Field Mappings

### Field Changes (IClassMapping)

| Source Class            | Source Field      | Target Class            | Target Field      | Rename | Data Type Change | Notes                           |
| ----------------------- | ----------------- | ----------------------- | ----------------- | ------ | ---------------- | ------------------------------- |
| {Namespace.SourceClass} | {SourceFieldName} | {Namespace.TargetClass} | {TargetFieldName} | {Yes / No} | {e.g., longtext → richtexthtml} | {Form control change, direct mapping, etc.} |

### SEO Metadata Fields

**Configures:** `appsettings.json` → `IncludeExtendedMetadata: true`
**Requires:** `IClassMapping` for each webpage content type to rename extended metadata fields to reusable field schema target field names.

| Source Field (KX13 extended) | Target Field (Schema) | Applied To          |
| ---------------------------- | --------------------- | ------------------- |
| {DocumentPageTitle}          | {SEOMetaTitle}        | {Content type list} |

### File / Attachment Fields

By default, `file`/`DirectUploadControl` fields automatically migrate as `Legacy.Attachment` content items with content item asset reference fields on the target content types. No field exclusion is needed — images and files are fully migrated and immediately usable after migration.

| Source Class            | Source Field | Target Field                      | Notes                                            |
| ----------------------- | ------------ | --------------------------------- | ------------------------------------------------ |
| {Namespace.SourceClass} | {FieldName}  | {TargetFieldName} (content item asset) | {Auto-migrated as Legacy.Attachment reference; optional post-migration curation into custom asset type} |

### Custom Form Control Fields

Source form controls not covered by the migration tool's built-in Field Type and Form Control Mapping table. For each, specify the handling mechanism.

| Source Class            | Source Field | Source Data Type + Form Control | Target Data Type + Form Component | Handling Mechanism                                                                                           |
| ----------------------- | ------------ | ------------------------------- | --------------------------------- | ------------------------------------------------------------------------------------------------------------ |
| {Namespace.SourceClass} | {FieldName}  | {e.g., text + countrySelector}  | {e.g., text + Text input}         | {Built-in catch-all / `FieldMigrations` config / `IFieldMigration` code / `WithFieldPatch` in IClassMapping} |

<!-- Handling Mechanism options:
  - Built-in catch-all: the built-in mapping has a catch-all entry for this data type
    (e.g., text + _other_ → TextInput). No configuration or code needed.
  - FieldMigrations config: simple form control swap with no value change. Add an entry
    to appsettings.json OptInFeatures.CustomMigration.FieldMigrations.
  - IFieldMigration code: custom value transform or definition change that requires code.
    Must have a corresponding row in the Code Extensions table.
  - WithFieldPatch in IClassMapping: definition change scoped to a specific class mapping.
    Pair with ConvertFrom if value also needs conversion.

  When the handling mechanism is "Built-in catch-all", verify that the catch-all target
  is acceptable for the field's use case. For example, text + countrySelector maps to
  TextInput via catch-all, which is correct for plain text country names but would not
  be correct if the field stores country codes that need conversion.

  When the data type changes (e.g., text → longtext), a built-in catch-all for the
  SOURCE data type won't produce the TARGET data type. In that case, use IFieldMigration
  or WithFieldPatch — catch-all mappings do not change data types.
-->

### Custom Value Transforms

**Requires:** Code extension `IClassMapping` (`ConvertFrom`) or `IFieldMigration`

| Source Class            | Source Field | Target Field      | Transformation                                                                             |
| ----------------------- | ------------ | ----------------- | ------------------------------------------------------------------------------------------ |
| {Namespace.SourceClass} | {FieldName}  | {TargetFieldName} | {Detailed description of the transformation logic, including complexity notes if relevant} |

<!-- Include implementation hints:
  - ConvertFrom targeting which source field
  - Lookup dictionaries needed (e.g., specialty text → taxonomy tag GUID)
  - NodeID resolution steps
  - Edge cases to handle

  When a transform references taxonomy tag GUIDs that are not yet known (assigned
  when the taxonomy is created pre-migration), use `TODO` as the placeholder in the
  "XbyK Tag GUID" column of the lookup dictionary table so these entries are easy
  to find and fill in later. Example:
    | Day    | KX13 NodeGUID                          | XbyK Tag GUID |
    | Monday | `f5a5892d-c828-4af0-9413-d1912285e3fb` | `TODO`        |
-->

---

## Page Relationship Handling

### Linked Page Handling

**Requires:** Code extension `ContentItemDirectorBase.DirectLinkedNode()`

| Scope                                | Strategy     | Notes |
| ------------------------------------ | ------------ | ----- |
| {Namespace.PageType or path pattern} | {materialize / drop / store_reference} | {Deduplication implications, linked node behavior} |

### Child Pages as Ancestor References

**Requires:** Code extension `ContentItemDirectorBase.Direct()` with `options.LinkChildren(fieldName, filteredChildNodes)`

References are populated automatically during `--pages` — not as a post-migration manual step. The parent's `Direct()` method calls `LinkChildren` to link filtered direct children as content item references. The reference field is auto-created on the target content type if it doesn't exist.

| Parent Class            | Child Class            | Source Path       | Reference Field     | Notes                                             |
| ----------------------- | ---------------------- | ----------------- | ------------------- | ------------------------------------------------- |
| {Namespace.ParentClass} | {Namespace.ChildClass} | {/path/pattern/*} | {FieldNameOnParent} | {Direct child / folder skipping, filtering notes} |

<!--
  - Parent Class: the page type on which LinkChildren is called in Direct()
  - Child Class: the child page type being linked as content item references
  - Reference Field: auto-created content item reference field on the parent target type
  - When intermediate nodes (e.g., CMS.Folder) sit between the parent and child,
    call LinkChildren on the intermediate node or note "via CMS.Folder" in Notes.
  - Both parent and child types must be configured for Content Hub
    (ConvertClassesToContentHub or ClassContentTypeType.REUSABLE in IClassMapping)
    unless the parent is a webpage type — then only the child needs Content Hub config.
-->

### Linked Page Audit

Every linked page in the KX13 page tree must have an explicit migration strategy. Unhandled linked pages create duplicate content items in XbyK. Use the linked page data from the source audit to populate this table.

| Source Path            | Source Class         | Linked To (Original Path) | Strategy                               | Notes                                    |
| ---------------------- | -------------------- | ------------------------- | -------------------------------------- | ---------------------------------------- |
| {/path/to/linked-page} | {Namespace.PageType} | {/path/to/original-page}  | {materialize / drop / store_reference} | {Why this strategy, deduplication notes} |

<!--
  Strategies:
  - materialize: Create a separate content item (accepts intentional content duplication)
  - drop: Skip the linked page entirely (original page is sufficient)
  - store_reference: Store a reference to the original content item instead of duplicating
-->

### Pages as Widgets

**Requires:** Code extension `ContentItemDirectorBase` (`AsWidget`)

| Source Class               | Source Path      | Target Widget Type           | Ancestor Level | Editable Area     | Section Type            | Zone  |
| -------------------------- | ---------------- | ---------------------------- | -------------- | ----------------- | ----------------------- | ----- |
| {Namespace.SourcePageType} | {/path/filter/*} | {Namespace.WidgetIdentifier} | -1             | {area-identifier} | {SectionTypeIdentifier} | first |

**Property mapping**: {How page fields map to widget properties}

---

## Widget Transformations

### Source Instance API Discovery

**Configures:** `appsettings.json` → `OptInFeatures.QuerySourceInstanceApi`

- **Enabled**: {Yes / No}
- **Purpose**: {Why it's enabled/disabled}
- **Setup**: Automated — `ToolApiController.cs` is deployed to the KX13 instance by the appsettings skill during configuration generation. Ensure the KX13 instance is built and running before executing `--pages`.

### Built-in Widget Migration

No `IWidgetMigration` code needed for these:

| Source Identifier         | XbyK Built-in Target  | Notes                                             |
| ------------------------- | --------------------- | ------------------------------------------------- |
| `Kentico.Widget.RichText` | XbyK Rich text widget | `content` → `Content` via API Discovery           |
| `Kentico.FormWidget`      | XbyK Form widget      | `selectedForm` → `SelectedForm` via API Discovery |

### Section Type Mappings

**Requires:** Code extension `IWidgetMigration`

| Source Identifier     | Target Identifier    | Property Changes                                      |
| --------------------- | -------------------- | ----------------------------------------------------- |
| {Source.Section.Name} | {Target.SectionName} | {Detailed property mapping with transformation logic} |

### Custom Widget Type Mappings

**Requires:** Code extension `IWidgetMigration`

| Source Identifier    | Target Identifier   | Notes                                                                                            |
| -------------------- | ------------------- | ------------------------------------------------------------------------------------------------ |
| {Source.Widget.Name} | {Target.WidgetName} | {Detailed property mapping, GUID resolution, special handling, and complexity notes if relevant} |

### Widget Property Transforms

**Requires:** Code extension `IWidgetPropertyMigration`

| Widget Identifier  | Property Name  | Transformation                                | Notes                             |
| ------------------ | -------------- | --------------------------------------------- | --------------------------------- |
| {WidgetIdentifier} | {PropertyName} | {Detailed transformation logic with examples} | {Edge cases, lookup requirements} |

---

## Execution Plan

### appsettings.json Configuration

Full configuration to generate from this plan:

```json
{
  "Settings": {
    "ConvertClassesToContentHub": "",
    "EntityConfigurations": {},
    "IncludeExtendedMetadata": true,
    "OptInFeatures": {
      "QuerySourceInstanceApi": {
        "Enabled": true,
        "Url": "<KX13 source instance URL>"
      }
    }
  }
}
```

### Code Extensions to Implement

| Extension       | Class          | Description             |
| --------------- | -------------- | ----------------------- |
| {ExtensionName} | {IClassMapping / ContentItemDirectorBase / IWidgetMigration / IWidgetPropertyMigration / IFieldMigration} | {Detailed description of what this extension does, fields it handles, transforms it performs, and complexity notes if relevant} |
