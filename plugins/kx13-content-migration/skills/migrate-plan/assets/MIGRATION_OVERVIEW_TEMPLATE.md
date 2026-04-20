# Migration Overview — [Project Name]

## Manual Steps

### Pre-Migration

Steps that must be completed **before** running the migration tool. These are items outside the tool's scope — the migration tool cannot automate them.

| Step                                   | Reason                                   |
| -------------------------------------- | ---------------------------------------- |
| [Description of what needs to be done] | [Why this is necessary before migration] |

<!-- Examples:
  - Create taxonomy tags (e.g. DayOfWeek: Monday–Sunday) needed by field transforms during migration — include a note to fill in the tag GUIDs in the detail document's lookup table, e.g.: "Create **DayOfWeek** taxonomy with 7 tags (Monday–Sunday). After creation, fill in the tag GUIDs in the lookup table in [migration-detail](./migration-detail)."
  - Create target pages with editable areas for page-to-widget migration scenarios
  NOTE: ToolApiController deployment is NOT a manual step — it is handled automatically by the appsettings skill during configuration generation.
-->

### Post-Migration

Steps that should be completed **after** the migration tool finishes. These address gaps the tool cannot bridge automatically.

| Step                                   | Reason                                  |
| -------------------------------------- | --------------------------------------- |
| [Description of what needs to be done] | [Why this is necessary after migration] |

<!-- Examples:
  - Create thin wrapper pages (e.g., DoctorDetailPage for each migrated Doctor content item)
  - Create content items that had no KX13 equivalent (e.g., CallToAction items)
  - Populate content item references that could not be resolved during migration
  - Optionally curate Legacy.Attachment items into custom asset types (e.g., ImageAsset) if structured asset metadata is needed — images are already migrated and usable as Legacy.Attachment references
  - Augment partial records created by merge mappings (e.g., add missing coordinates to address-only records)
  - Verify and fix content item references set by ContentItemDirectorBase
  - Build custom XbyK widgets for features that have no built-in equivalent (e.g., FileDownload)
-->

### Target-Only Entities

Content types and taxonomies that exist only in the target model. The migration tool only creates types from source data — these must be created manually in XbyK.

#### Content Types

| Content Type                | Type               | When to Create                 | Reason                                   |
| --------------------------- | ------------------ | ------------------------------ | ---------------------------------------- |
| [Namespace.ContentTypeName] | [website/reusable] | [Pre-migration/Post-migration] | [Why this type has no source equivalent] |

<!-- Examples:
  - MedioClinic.ImageAsset (reusable) — Post-migration — Centralizes all images; no single KX13 type maps to it
  - MedioClinic.CallToAction (reusable) — Post-migration — KX13 stored raw button text, not structured CTA items
  - MedioClinic.DoctorDetailPage (website) — Post-migration — Thin page wrapper for Content Hub doctors; no KX13 equivalent
-->

#### Taxonomies

| Taxonomy Name  | When to Create                 | Reason                                 |
| -------------- | ------------------------------ | -------------------------------------- |
| [TaxonomyName] | [Pre-migration/Post-migration] | [Why this taxonomy has no KX13 source] |

<!-- When to Create:
  - Pre-migration: if taxonomy tags are needed by field transforms (ConvertFrom) during migration
  - Post-migration: if taxonomy is only referenced by target-only content types (created after migration) or used solely for editorial classification
-->

### Unsupported Features

KX13 features not supported by the migration tool that need manual handling or alternative solutions.

| Feature        | KX13 Usage                | XbyK Alternative                                   |
| -------------- | ------------------------- | -------------------------------------------------- |
| [Feature Name] | [How it was used in KX13] | [Recommended approach in XbyK, or 'not available'] |

<!-- Examples:
  - Newsletter subscription widget → Replace with Form widget + third-party email service
  - Resource key-based labels → Replace with explicit text values (resolve keys before/during migration)
  - Contact groups, marketing automation, email marketing → Set up from scratch in XbyK
  - Custom workflows → Only Published/Draft/Archived steps are migrated; recreate others manually
-->

---

## Code Extensions Summary

| Extension Type             | Count | Summary                                                                              |
| -------------------------- | ----- | ------------------------------------------------------------------------------------ |
| `IClassMapping`            | [N]   | [Brief summary of what these mappings do; include complexity notes inline if needed] |
| `ContentItemDirectorBase`  | [N]   | [Brief summary of relationship handling; include complexity notes inline if needed]  |
| `IWidgetMigration`         | [N]   | [Brief summary of widget transformations; include complexity notes inline if needed] |
| `IWidgetPropertyMigration` | [N]   | [Brief summary of property transforms; include complexity notes inline if needed]    |
| `IFieldMigration`          | [N]   | [Brief summary of field migrations; include complexity notes inline if needed]       |

---

## Content Model Mapping Overview

### Content Hub Conversions

Source page types and module classes that become reusable content items in the Content Hub.

| Source Class            | Target Class            | Summary                             |
| ----------------------- | ----------------------- | ----------------------------------- |
| [Namespace.SourceClass] | [Namespace.TargetClass] | [Brief description of what changes] |

### Webpage Mappings

Source page types that map to XbyK webpage content types.

| Source Class               | Target Class                  | Summary                             |
| -------------------------- | ----------------------------- | ----------------------------------- |
| [Namespace.SourcePageType] | [Namespace.TargetContentType] | [Brief description of what changes] |

### Merge Mappings

Multiple source types merged into a single target type.

| Target Class            | Source Classes                           | Summary                                  |
| ----------------------- | ---------------------------------------- | ---------------------------------------- |
| [Namespace.TargetClass] | [Namespace.Source1], [Namespace.Source2] | [Why they are merged and what to expect] |

### Exclusions

Source types excluded from migration.

| Source Class              | Reason                                                 |
| ------------------------- | ------------------------------------------------------ |
| [Namespace.ExcludedClass] | [Why this is excluded and how it is handled otherwise] |

<!-- If any source page types use class inheritance (ClassInheritsFromClassID),
  note which classes are NOT excluded despite having 0 instances because they
  serve as parent classes. Example:
  > **Not excluded:** `Namespace.BasicPage` (0 instances) — parent class of
  > `Namespace.HomePage` and `Namespace.SiteSection`. Excluding parent classes
  > breaks child class migration.
-->

### Taxonomy Planning

#### Migrated Taxonomies

Taxonomies created automatically from KX13 categories by the migration tool (`--categories`).

| KX13 Category        | Target Taxonomy      | Summary                            |
| -------------------- | -------------------- | ---------------------------------- |
| [KX13 category name] | [XbyK Taxonomy Name] | [Brief description of tag mapping] |

#### New Taxonomies

Taxonomies with no KX13 category source. Must be created manually or programmatically — see Target-Only Entities above for creation timing.

| Taxonomy Name  | Purpose                         | Referenced By                     | Created By                                         |
| -------------- | ------------------------------- | --------------------------------- | -------------------------------------------------- |
| [TaxonomyName] | [What this taxonomy classifies] | [Content types that reference it] | [Manual (pre-migration) / Manual (post-migration)] |

### Reusable Field Schemas

| Schema Name  | Applied To                   | Fields             |
| ------------ | ---------------------------- | ------------------ |
| [SchemaName] | [Which content types use it] | [Brief field list] |

---

## Field Mapping Overview

### Key Transformations

Summary of notable field transformations. Detailed mappings and code examples are in the migration detail document.

| Source Class            | Transformation                                                                                                    |
| ----------------------- | ----------------------------------------------------------------------------------------------------------------- |
| [Namespace.SourceClass] | [Brief description: e.g., "Split DocumentName → FirstName + LastName"; include complexity notes inline if needed] |

### Attachment Fields

Source file/attachment fields and their migration path. By default, file fields migrate as `Legacy.Attachment` content items with working asset reference fields on the target content types — no exclusion or special handling needed.

| Source Class            | Source Field | Migration Path                          | Post-Migration Action                  |
| ----------------------- | ------------ | --------------------------------------- | -------------------------------------- |
| [Namespace.SourceClass] | [FieldName]  | Legacy.Attachment → [Final target type] | [Optional curation or "None needed"]   |

---

## Widget Transformation Overview

### Source Instance API Discovery

- **Enabled**: [Yes/No]
- **Notes**: [Brief setup requirements]

### Built-in Widgets

Built-in KX13 widgets that migrate automatically with no custom code.

| Source Widget       | XbyK Equivalent    |
| ------------------- | ------------------ |
| [Source identifier] | [XbyK widget name] |

### Custom Widget Mappings

Custom widgets requiring `IWidgetMigration` code.

| Source Widget       | Target Widget       | Summary                                                                        |
| ------------------- | ------------------- | ------------------------------------------------------------------------------ |
| [Source identifier] | [Target identifier] | [Brief description of what changes; include complexity notes inline if needed] |

### Section Mappings

| Source Section      | Target Section      | Summary                                                                            |
| ------------------- | ------------------- | ---------------------------------------------------------------------------------- |
| [Source identifier] | [Target identifier] | [Brief description of property changes; include complexity notes inline if needed] |

---

## Page Relationship Overview

Summary of how page relationships and linked pages are handled.

| Source Pattern                           | Strategy                           | Summary                             |
| ---------------------------------------- | ---------------------------------- | ----------------------------------- |
| [Description of the source relationship] | [materialize/drop/store_reference] | [Brief explanation of what happens] |

---

## Execution Summary

### Migrate Command

`Migration.Tool.CLI.exe migrate` parameters:

| Parameter              | Status | Notes |
| ---------------------- | ------ | ----- |
| `--sites`              | —      |       |
| `--custom-modules`     | —      |       |
| `--custom-tables`      | —      |       |
| `--categories`         | —      |       |
| `--users`              | —      |       |
| `--members`            | —      |       |
| `--forms`              | —      |       |
| `--media-libraries`    | —      |       |
| `--page-types`         | —      |       |
| `--pages`              | —      |       |
| `--type-restrictions`  | —      |       |
| `--settings-keys`      | —      |       |
| `--contact-management` | —      |       |
| `--data-protection`    | —      |       |
| `--customers`          | —      |       |
| `--orders`             | —      |       |

---

## Operational Notes

### Iterative Migration

The migration tool supports repeated (iterative) runs with upsert behavior — existing items are updated without creating duplicates. This allows incremental refinement of migration configuration and extensions across multiple runs.

- Use `--bypass-dependency-check` on subsequent runs when dependencies were already migrated successfully (e.g., `migrate --pages --bypass-dependency-check` if sites, users, and page types were already migrated).
- Most data types support upsert. Exceptions are listed below under "Bulk Deletion Requirements".

### Bulk Deletion Requirements

Certain data types use bulk SQL copy and **do not support upsert**. Before re-running migration for these types, manually delete all existing target data first:

| CLI Parameter          | Data to Delete Before Re-run      |
| ---------------------- | --------------------------------- |
| `--contact-management` | All Contacts and their Activities |
| `--data-protection`    | All Consent agreements            |
| `--forms`              | All Form submissions              |
| `--custom-modules`     | All Custom module class data      |

Failure to delete these before a repeated run will result in duplicate records.

### Logging and Troubleshooting

Review migration results using:

- **Console output** — real-time progress, warnings, and errors during execution
- **Log files** — detailed execution logs written to `logs/log-<date>.txt` in the CLI output directory

Address all errors and warnings before considering the migration complete. Log files contain detailed diagnostic information useful for troubleshooting failed or partially completed migrations.
