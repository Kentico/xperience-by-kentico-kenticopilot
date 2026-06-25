# Migration Approach Categories

These categories cover structural decisions that affect migration complexity. They apply in two scenarios:

- **No target model provided**: Scan the KX13 source model and ask the user how each applicable area should be migrated.
- **Target model provided**: Compare source and target models to **auto-resolve** decisions wherever the target model makes the intended approach clear. Only ask the user about categories that are genuinely ambiguous after comparing models.

Scan for these categories in order. Skip any category that does not apply to the source model.

### Auto-resolution rules (target model provided)

When a target XbyK content model is provided, most categories can be resolved by inspecting the target model without asking the user:

- If the target model contains an entity or structure that clearly maps to a non-default option, **select that option automatically**.
- If the target model is silent on a category (no evidence either way), **use the default option**.
- **Ask the user** when:
  - The target model is ambiguous — e.g., a source entity could map to multiple target structures, or the target model partially addresses a scenario leaving the rest unclear.
  - The resolved option conflicts with how the migration tool works — e.g., the target model implies a transformation that the tool cannot support, or would require an unreasonable amount of custom code for little benefit. In such cases, flag the concern and suggest a practical alternative.

**Always present a summary before proceeding.** Even when all categories auto-resolve cleanly, present the full summary of resolved decisions to the user and wait for confirmation before writing the plan. The user must have the opportunity to review and adjust any decision.

## A. Content Hub conversion

**When to ask — no target model**: A KX13 page type represents content that could benefit from cross-channel reuse (e.g., shared between website and email) or has no meaningful page URL of its own (e.g., `Doctor`, `CompanyService`).

**Auto-resolve — target model provided**: If a KX13 page type maps to a reusable content type in the target model (not a website page type), select **Convert to Content Hub**. If the target model also has a separate webpage type that wraps/references the reusable type (e.g., `Doctor` → `DoctorProfile` (reusable) + `DoctorDetailPage` (wrapper)), note the wrapper page as a post-migration manual step. If the KX13 page type maps to a website page type in the target, select **Keep as webpage**. Only ask the user if a KX13 page type has no clear target equivalent.

| Option | Description |
| --- | --- |
| **Convert to Content Hub** | Convert to Content Hub reusable content item (+ create wrapper pages post-migration if the content also needs a page URL). Enables content reuse across channels but requires IClassMapping, ConvertClassesToContentHub, and manual wrapper page creation after migration. When using `CustomModuleClassDisplayNamePatterns`, you can control how content item names are generated for converted items. |
| **Keep as webpage** (default) | Keep as a webpage content type with 1:1 field mapping. Simpler migration (no Content Hub conversion, no wrapper pages) but limits cross-channel reuse. Content stays in the page tree. |

**Child page hierarchy preservation**: When a page type is converted to Content Hub, its child pages in KX13 lose their parent-child relationship by default (children migrate independently).

**Auto-resolve — target model provided**: If the target reusable content type has a content item reference field whose name or allowed types correspond to the KX13 child page type, select **Link children as references**. Also check for child-as-reference patterns where a **webpage** parent has Content Hub children whose references should be populated during migration (e.g., a ContactPage webpage with Company children converted to Content Hub). If the target webpage type has a content item reference field pointing to a child's Content Hub type, select **Link children as references**. Otherwise use **Flat migration**.

| Option | Description |
| --- | --- |
| **Link children as references** | Use `ContentItemDirectorBase.Direct()` with `options.LinkChildren(fieldName, filteredChildNodes)` on the parent page to populate content item reference fields with references to migrated child pages. The reference field is auto-created on the target content type. Requires child types to be in `ConvertClassesToContentHub` (or set to `ClassContentTypeType.REUSABLE` in `IClassMapping`). Parent can be either a reusable type or a webpage type. When intermediate non-content nodes (e.g., `CMS.Folder`) sit between the parent and child pages, call `LinkChildren` on the intermediate node, or handle via more targeted director logic. Preserves hierarchy as structured references — these references are populated automatically during `--pages`, NOT as a post-migration manual step. |
| **Flat migration** (default) | Children migrate independently; parent-child relationship is lost. Simpler migration; relationships can be re-established manually post-migration. |

## B. Lookup page type as taxonomy

**When to ask — no target model**: A KX13 page type stores a small, fixed set of lookup values (e.g., `DayOfWeek` with 7 items, `Specialty` with a dozen entries) and is referenced by other page types via document relationships.

**Auto-resolve — target model provided**: If a KX13 page type has no corresponding content type in the target model but a taxonomy with matching name/values exists, select **Convert to taxonomy**. If the target model has the KX13 page type as a reusable content type, select **Keep as reusable content**. Only ask if the target model is ambiguous (e.g., both a taxonomy and a content type could match).

| Option | Description |
| --- | --- |
| **Convert to taxonomy** | Exclude the page type, create taxonomy manually (pre-migration), convert referencing fields via ConvertFrom to resolve page relationships to taxonomy tag GUIDs. Cleaner target model but requires manual taxonomy creation and complex field transforms. |
| **Keep as reusable content** (default) | Convert the page type to Content Hub as reusable content items. Referencing fields become content item references. Simpler migration (ConvertClassesToContentHub handles it) but keeps lookup data as content items rather than taxonomy tags. Can be converted to taxonomy post-migration. |

## C. Media and asset field handling

This category covers fields that reference media files or attachments. Two distinct field patterns exist in KX13 — evaluate each that applies:

### C1. File/attachment fields

**When to ask — no target model**: The source model has page types with `file` / `DirectUploadControl` fields (attachments stored on the page type). Multiple types may share the same pattern (e.g., Doctor.BackdropPicture, CompanyService.Icon).

**Auto-resolve — target model provided**: Select **Default attachment migration** unless the user has explicitly requested a centralized asset type. The migration tool always migrates file/attachment binary data as `Legacy.Attachment` content items during `--pages` regardless of which option is chosen — the difference is only whether the target content types get auto-created asset reference fields (default) or whether those fields are excluded and re-created manually pointing to a custom asset type (centralized). Even if the target model defines a centralized asset type (e.g., `ImageAsset`), prefer default attachment migration and note the asset curation as optional post-migration work — do not auto-resolve to centralized asset type based on the target model alone.

| Option | Description |
| --- | --- |
| **Default attachment migration** (default) | Let file fields migrate naturally as `Legacy.Attachment` content items with content item asset reference fields auto-created on the target content types (the migration tool's default behavior). No field exclusion needed. Images and files are fully migrated and immediately usable. Post-migration: optionally curate `Legacy.Attachment` items into custom asset types (e.g., `ImageAsset`) if structured fields like alt text or categories are needed. Simplest migration with working image references from day one. |
| **Centralized asset type** | Exclude file fields from migration mapping. Files still migrate as `Legacy.Attachment` items (this is automatic), but the target content types won't have fields pointing to them. Post-migration: create a centralized asset content type (e.g., `ImageAsset`), create items from `Legacy.Attachment` data, and populate content item reference fields manually. More work upfront but produces a cleaner target structure if structured asset metadata is required before go-live. |

### C2. Media selection text fields

**When to ask — no target model**: The source model has page types with text fields using the `MediaSelectionControl` form control (storing paths like `~/getmedia/GUID/filename.jpg`).

**Auto-resolve — target model provided**: If the target model changes these fields to a `contentitemreference` or asset data type, select **Convert to content item references**. If the target model keeps them as text fields, select **Keep as text**.

| Option | Description |
| --- | --- |
| **Convert to content item references** | Configure `OptInFeatures.CustomMigration.FieldMigrations` with `TargetDataType: "contentitemreference"` and `TargetFormComponent: "Kentico.Administration.ContentItemSelector"`. Fields become content item selectors pointing to migrated media assets. Best target structure — proper content item references. Use `FieldNameRegex` to control which fields are converted. |
| **Keep as text** (default) | Fields migrate as plain text with original `~/getmedia/` URLs preserved. Simplest migration; field conversion can be done post-migration. |

## D. Type splitting

**When to ask — no target model**: A single KX13 page type is used in semantically different contexts (e.g., `NamePerexText` serves as both a contact page at `/Contact-us` and error pages at `/Reused-content/Error-pages`).

**Auto-resolve — target model provided**: If the target model has multiple content types whose fields trace back to a single KX13 page type, select **Split into multiple types**. The primary target type is the one with the most field overlap; secondary types are created manually post-migration.

| Option | Description |
| --- | --- |
| **Split into multiple types** | Map the source type to the primary target type via IClassMapping. Drop or handle secondary instances via ContentItemDirectorBase. Create the secondary target type(s) manually post-migration. Precise content typing but requires careful routing of individual pages. |
| **Single target type** (default) | Map the source type to one target type (rename fields as needed). All instances migrate as the same type. Simpler migration; semantic distinction can be added post-migration by creating the secondary type and re-typing content items. |

## E. Type merging

**When to ask — no target model**: Multiple KX13 page types share the same or very similar field structures and could be consolidated (e.g., `LandingPage` and `EventLandingPage` differ by only 1–2 fields).

**Auto-resolve — target model provided**: If a single target content type has fields that map to multiple KX13 page types, select **Merge into one type**. The source type with the most field overlap becomes the template source in MultiClassMapping.

| Option | Description |
| --- | --- |
| **Merge into one type** | Use MultiClassMapping to merge source types into one target type. Fields from non-template source classes are null for pages that don't have them. Reduces type count but requires careful field mapping across sources. |
| **Keep types separate** (default) | Map each source type 1:1 to its own target type (with field renaming as needed). Simpler mapping; types can be consolidated post-migration if desired. |

## F. Custom widget migration

**When to ask — no target model**: The source model includes custom widgets whose type identifiers or property models could be improved (e.g., renamed widget types, consolidated widgets, clearer property names, updated value formats, references that should become content item references).

**Auto-resolve — target model provided**: If the target model defines widgets with different type identifiers, renamed properties, changed value formats, or consolidated structures compared to source widgets, select **Restructure widgets** for each affected widget and plan the specific IWidgetMigration/IWidgetPropertyMigration implementations based on the target widget definitions. If source widgets have no corresponding target widget definition or the target model doesn't cover widgets, select **Minimal widget migration**.

This category covers two distinct extension points that can be used independently or together:

- **Widget migrations** (`IWidgetMigration`): Change widget types, consolidate multiple widgets into one, or restructure the overall widget data shape.
- **Widget property migrations** (`IWidgetPropertyMigration`): Transform individual property values within a widget — update content references, convert value formats, or transform paths/URLs.

| Option | Description |
| --- | --- |
| **Restructure widgets** | Write IWidgetMigration for type/structure changes and/or IWidgetPropertyMigration for property value transforms. Produces the desired widget structure but each widget with custom changes needs migration code. Use IWidgetMigration for type renames, consolidations, or structural changes. Use IWidgetPropertyMigration for individual property value transforms (reference updates, format conversions, path transforms). |
| **Minimal widget migration** (default) | Keep original property names and value formats where possible. Rename only the widget type identifier if it changed. Simpler migration; property restructuring applied post-migration or in XbyK widget code (adapting the view model to accept both old and new property shapes). |

## G. Custom tables and module classes target structure

**When to ask — no target model**: The source instance contains custom tables or custom module classes with data.

**Auto-resolve — target model provided**: If a source custom table or module class appears in the target model as a reusable content type, select **Reusable content items** for that class. If it does not appear in the target model at all, select **Module classes** (default). Evaluate custom tables and module classes together since they use the same mechanism (`ConvertClassesToContentHub`).

| Option | Description |
| --- | --- |
| **Module classes** (default) | Custom tables and module classes migrate as module classes with no extra configuration. Data is system-managed and not editable in Content hub. Appropriate for settings, lookup tables, system records, configuration data. Simplest migration. |
| **Reusable content items** | Add class code names to `ConvertClassesToContentHub`. Data becomes editable in Content hub and can be linked from other content types. Appropriate for content-like data (shared snippets, reference items used by pages). Use `CustomModuleClassDisplayNamePatterns` to control content item naming for module classes (e.g., `"Acme.CustomClass": "Item-{CustomClassGuid}"`). May require `IClassMapping` if field restructuring is needed. |

You can mix strategies: keep some classes as module classes while converting others to reusable content items. List only the ones being converted in `ConvertClassesToContentHub`.

## H. Linked page handling strategy

**When to ask — no target model**: The source KX13 instance has linked pages (`NodeLinkedNodeID IS NOT NULL`). Common pattern: originals live in a `/Reused-content/` folder and linked pages are placed where content is displayed (e.g., under `/Home/`).

**Auto-resolve — target model provided**: If the original linked page's content type is converted to Content Hub (category A), select **Store as reference** (the original becomes reusable content and the link becomes a reference). If the original's content type stays as a webpage and the linked page's tree location is under an excluded/archived path, select **Drop linked pages**. Otherwise select **Materialize**. Only ask the user when the linked page pattern is ambiguous (e.g., mixed usage across tree sections).

Linked pages in KX13 are references to original pages — they share content and store no data of their own. By default the migration tool materializes them into separate content items, creating duplicates. Decisions should be made per content type or per page-tree section.

| Option | Description |
| --- | --- |
| **Drop linked pages** | Skip migration of linked pages entirely via `ContentItemDirectorBase` with `options.Drop()`. Appropriate when linked pages reference archived/obsolete content or content that will be recreated manually. No content duplication but linked content is lost. |
| **Store as reference** | Create a content item reference field on an ancestor page pointing to the original content via `ContentItemDirectorBase.DirectLinkedNode()` with `options.StoreReferenceInAncestor(parentLevel, fieldName)`. Preserves relationships without duplication. Requires originals to be in `ConvertClassesToContentHub` so they become reusable content. Note: this is for **linked pages** only — for regular parent-child relationships, use `LinkChildren` in `Direct()` (see Category A, child page hierarchy preservation). |
| **Materialize** (default) | Create independent copies of the linked content as separate pages. Default behavior — no custom code needed. Simplest migration but duplicates data. |

## I. Pages as widgets conversion

**When to ask — no target model**: A KX13 parent page serves as a listing and displays content from child pages, where the child page content would be better represented as Page Builder widgets on the parent in XbyK (e.g., child `Coffee` pages under a `Store` listing page).

**Auto-resolve — target model provided**: If a KX13 child page type has no corresponding content type in the target model but a widget with matching properties exists, select **Convert to widgets**. If the child page type maps to a content type in the target model (as a page or Content Hub item), select **Keep as pages**.

The target page must have a Page Builder editable area and any widget components used in the migration must be present in the XbyK project before migration.

| Option | Description |
| --- | --- |
| **Convert to widgets** | Use `ContentItemDirectorBase` with `options.AsWidget()` to convert child pages to widgets on ancestor pages. Widget placement is configured via `options.Location` (editable area, section, zone). Content can also be saved as reusable content items and linked from widget properties. Requires widget component code and target page with editable area to exist pre-migration. |
| **Keep as pages** (default) | Migrate child pages as-is (website pages or Content Hub items via category A). Simpler migration; widget conversion can be done post-migration in XbyK. |

## J. Reusable field schema extraction

**When to ask — no target model**: Multiple KX13 page types share a common set of fields (e.g., SEO fields, metadata fields, address fields) that could be extracted into a reusable field schema, or a KX13 page type uses inheritance from a parent page type.

**Auto-resolve — target model provided**: If the target model defines reusable field schemas, select **Extract reusable field schema**. Choose the mechanism based on context: use `CreateReusableFieldSchemaForClasses` (config) when the schema maps to a single KX13 parent page type with inheritance; use `ReusableSchemaBuilder` in `IClassMapping` (code) when the schema extracts fields from multiple unrelated source types. If the target model has no reusable field schemas, select **Duplicate fields per type**.

Two mechanisms are available (they cannot be combined in the same migration run):

- **Configuration**: `CreateReusableFieldSchemaForClasses` — converts specified parent page types to reusable field schemas. Best for page type inheritance scenarios.
- **Code**: `ReusableSchemaBuilder` in `IClassMapping` — extracts specific fields from multiple page types into a shared schema. Best for cross-type field consolidation.

| Option | Description |
| --- | --- |
| **Extract reusable field schema** | Use `CreateReusableFieldSchemaForClasses` (config) for page type inheritance, or `ReusableSchemaBuilder` in `IClassMapping` (code) for cross-type field extraction. Cleaner target model with field reuse. These two approaches cannot be combined in the same migration run. |
| **Duplicate fields per type** (default) | Each content type keeps its own copy of common fields. Simpler migration; schemas can be extracted post-migration in XbyK admin. |

## How to Present Decisions

### When a target model is provided

1. **Auto-resolve**: Walk through every applicable category and apply the "Auto-resolve" rules. For each category where the target model makes the intended approach clear and it is feasible with the migration tool, select the matching option.
2. **Flag concerns**: If any auto-resolved option seems impractical, conflicts with migration tool behavior, or would require disproportionate effort, do not silently accept it. Instead, flag the concern in the summary with a brief explanation and a suggested alternative.
3. **Present the full summary and wait for confirmation**: Present all resolved decisions (including flagged concerns and any ambiguous categories that need user input) as a single summary. For each resolved category, show the category name, the specific source entities involved, and the selected option. For ambiguous or flagged categories, present the options as questions with trade-off summaries. For category C, present C1 and C2 as separate sub-decisions only when both apply; if only one applies, present it without the sub-numbering.
4. **Wait for user confirmation before writing the plan.** Do not proceed to plan generation until the user has reviewed and approved (or adjusted) the summary.

### When no target model is provided

- List only the categories that actually apply to the source model. Do not present categories that don't apply.
- For each applicable category, name the specific source entities involved (e.g., "MedioClinic.Doctor", "MedioClinic.DayOfWeek").
- Summarize the concrete trade-off: what extra migration work the non-default option requires vs. what the default approach defers.
- If there are 3+ decisions, present all of them to the user and collect all answers at once. For 1–2 decisions, inline questions in the conversation are fine.
- **Wait for user responses before writing the plan.**

### After decisions are confirmed

- Proceed to generate the plan using the chosen approaches. Record which option was selected in the plan documents (e.g., a brief note in the Content Model Mapping section).
