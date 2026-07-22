---
name: "migrate-content-items"
description: "Generates C# ContentItemDirectorBase extension code for controlling per-item migration behavior in the Kentico Migration Tool. Use when the user wants to handle linked pages (materialize/drop/store reference), convert pages to widgets, link child pages as content item references, override page templates, or apply conditional migration logic based on node path, class name, or hierarchy."
argument-hint: "[migration-plan-path]"
compatibility: "Requires dotnet CLI and optionally sqlcmd for resolving plan gaps (e.g., missing linked page audit or class IDs)."
---

# Content Item Director Code Generation

Produces ready-to-use C# code files for the Migration.Tool.Extensions project. Takes the migration plan output from the migrate-content-plan skill — or a direct text description — as input.

## Workflow

### Step 1: Read Reference Materials

- Read `references/content-item-director-api.md` for the complete API patterns, method signatures, and annotated code samples.
- If you need pattern examples for implementation, read `assets/CONTENT_ITEM_DIRECTOR_EXAMPLE.cs` for a complete annotated reference implementation showing all patterns.
- If you need context on the migration tool's extension points or configuration, read `../_shared/references/migration-tool.md`.
- If you need documentation links, read `../_shared/references/migration-docs.md`.
- If a Kentico documentation lookup tool is available, use it for additional context on content item directors, linked page handling, or page-to-widget conversion.

### Step 2: Analyze Input

- If a migration plan file path is provided → read it and extract from the **Page Relationship Handling** section (linked pages, child-as-ancestor references, pages-to-widgets, template overrides) and the **Code Extensions to Implement** section (rows where Type = `ContentItemDirectorBase`).
- If a direct text description is provided → identify linked page strategies, page-to-widget conversions, child page linking, and template override needs.
- **Audit linked pages** — check the plan's **Linked Page Handling** or **Linked Page Audit** section first:
  - If the plan explicitly states no linked pages exist (e.g., "No linked pages exist" or "NodeLinkedNodeID is null for all pages"), **trust the plan** and skip the database query. No `DirectLinkedNode` override is needed.
  - If the plan includes a linked page audit with specific rows, use those rows directly — do not re-query the database for confirmation.
  - **Only query the KX13 database** when the plan does not mention linked pages at all (section missing or silent on the topic):

    ```sql
    SELECT t.NodeAliasPath, t.ClassName, t.NodeLinkedNodeID,
           t2.NodeAliasPath AS LinkedToPath, t2.ClassName AS LinkedToClass
    FROM CMS_Tree t
    JOIN CMS_Tree t2 ON t.NodeLinkedNodeID = t2.NodeID
    WHERE t.NodeLinkedNodeID IS NOT NULL
    ```

  - When linked pages do exist, every one MUST have an explicit handling directive. The default behavior (Materialize) creates duplicate content items silently.
- Ask clarifying questions if widget placement details (editable area name, section type), ancestor levels, or conditional logic scope are ambiguous.

### Step 3: Identify Directors to Generate

Determine the set of director classes needed:

- Group by **logical concern**, not by class name. One director can handle multiple source classes via conditional logic (switch/if on `SourceClassName` or `NodeAliasPath`).
- Only override `Direct()` for content item behavior (LinkChildren, AsWidget, OverridePageTemplate, Drop).
- Only override `DirectLinkedNode()` for linked page behavior (Materialize, Drop, StoreReferenceInAncestor).
- A single director can override both methods when the concerns are related.
- One `ServiceCollectionExtensions` class (or per-director extension methods) for DI registration.
- **Always generate a linked page director** when linked pages exist in the source, even if the migration plan does not explicitly mention them. Unhandled linked pages are the most common source of duplicate content items.
- **Always generate Drop directives for utility pages** — pages under paths like `/Reused-content/`, `/Error-pages/`, `/System/`, or similar utility paths should be explicitly dropped unless the plan provides a different mapping. These pages, if not dropped, will be migrated using their source class's default mapping, which often produces incorrect results (e.g., an error page mapped as a ContactPage).
- **Skip** director generation for classes where the default migration behavior is sufficient.

### Step 4: Generate Director Code

For each director, generate a class inheriting from `ContentItemDirectorBase`:

- Override `Direct(ContentItemSource source, IContentItemActionProvider options)` for content item directives.
- Override `DirectLinkedNode(LinkedPageSource source, ILinkedPageActionProvider options)` for linked page directives.
- Use `switch`/`if` on `source.SourceClassName`, `source.SourceNode.NodeAliasPath`, or `source.LinkedNode.NodeClassID` / `source.LinkedNode.NodeAliasPath` for conditional logic.
- Call `options.LinkChildren(fieldName, filteredChildren)` for child page linking — always include a `Where` filter.
- Call `options.AsWidget(widgetIdentifier, null, null, opts => { ... })` for page-to-widget conversion — always specify the full location chain: `OnAncestorPage().InEditableArea().InSection().InFirstZone()`.
- Call `options.OverridePageTemplate(templateIdentifier)` for template overrides.
- Call `options.StoreReferenceInAncestor(level, fieldName)` for linked page reference storage — verify ancestor level from the page tree structure. **Important:** the ancestor content type must support content item reference fields; see the `StoreReferenceInAncestor` rule below.
- Call `options.Materialize()` to create independent copies of linked pages.
- Call `options.Drop()` to skip migration of specific pages.
- For `DirectLinkedNode()`: if the plan specifies `store_reference` or `drop` for specific linked page paths, use path-based or class-based matching. For all other linked pages not explicitly handled, default to `options.Drop()` rather than allowing implicit `Materialize()` — duplicated content is harder to clean up than missing content that can be re-migrated with a different strategy.
- For `Direct()`: include explicit `options.Drop()` for utility/error page paths that should not be migrated. Use path prefix matching: `source.SourceNode.NodeAliasPath.StartsWith("/Reused-content/", StringComparison.OrdinalIgnoreCase)`.
- **`Direct()` is abstract** — do NOT call `base.Direct(source, options)` (causes `CS0205: Cannot call an abstract base member`). For the default case in `Direct()`, either leave the case empty (no action = default migration behavior) or call a specific action.
- **`DirectLinkedNode()` is virtual** — calling `base.DirectLinkedNode(source, options)` in the default case is valid and preserves standard linked page behavior (Materialize).

### Step 5: Generate Service Registration

Generate service registration using the per-director extension method pattern:

- `services.AddTransient<ContentItemDirectorBase, MyDirector>()` for each director — NOT `AddSingleton`.
- One extension method per director file.
- Include prerequisite comments (e.g., which `appsettings.json` settings or `IClassMapping` registrations are required).

### Step 6: Build Verification

1. Build the `Migration.Tool.Extensions` project to verify the generated code compiles without errors.
2. If the build fails, analyze the error messages, fix all issues in the generated code, and rebuild.
3. Repeat up to 3 attempts. If the build still fails after 3 attempts, present the full build output and error details to the user for manual resolution.

#### 6b. Verify Linked Page Audit Completeness

Skip this sub-step if the migration plan confirmed no linked pages exist. Otherwise, after a successful build:

1. Re-read the linked page data from the migration plan (or from the KX13 query results if a query was needed in Step 2).
2. Walk through the generated `DirectLinkedNode()` override and confirm every linked page has an explicit handling directive (`Drop`, `Materialize`, or `StoreReferenceInAncestor`). Match by `SourceClassName` and/or `NodeAliasPath`.
3. If any linked pages are not covered by the conditional logic (and would fall through to `base.DirectLinkedNode()` which defaults to Materialize), warn that these will be silently duplicated. Recommend adding explicit `options.Drop()` for them.
4. Verify all director registrations use `AddTransient<ContentItemDirectorBase, T>()` — **NOT `AddSingleton`**.

### Step 7: Present and Refine

- Save files to the user-specified path (default: `Migration.Tool.Extensions/ContentItemDirectors/` — generated code belongs in the `Migration.Tool.Extensions` project, matching the `Migration.Tool.Extensions.ContentItemDirectors` namespace).
- Provide a summary table: file name → purpose → what it handles.
- List prerequisites (required appsettings, class mappings, XbyK page templates).
- Ask if any directors need adjustment and iterate on feedback.

## Rules

- Follow exact API from `migrate-content-items-api.md` — do not invent methods that don't exist.
- One director per logical concern — use conditional logic inside for multiple classes.
- Every director subclass must have a corresponding `AddTransient` registration (NOT `AddSingleton` or `AddScoped`).
- **Never call `base.Direct()`** — it is abstract and will not compile. For unhandled cases in `Direct()`, leave the default branch empty (no action = default migration behavior).
- Call `base.DirectLinkedNode()` for unhandled cases in `DirectLinkedNode()` only — it is virtual and defaults to Materialize.
- Handle both structured (migration plan) and free-text input.
- This skill generates `ContentItemDirectorBase` code only — `IClassMapping`, `IFieldMigration`, `IWidgetMigration`, and `IWidgetPropertyMigration` are separate extension points not covered here.
- Do **not** ask the user about taxonomy GUIDs, tag GUIDs, or lookup dictionary values. The migration plan is the single source of truth for these values — they are resolved at plan creation time and consumed by the `migrate-content-classes` skill at code generation time. If you encounter taxonomy references while reading the migration plan, treat them as informational context for understanding the data model, not something this skill needs to resolve or query for.
- For `StoreReferenceInAncestor`: verify ancestor level from the page tree (`-1` = parent, `-2` = grandparent). Account for non-migrated intermediate types like `CMS.Folder`.
- For `AsWidget`: always specify the full location chain — `OnAncestorPage().InEditableArea().InSection().InFirstZone()`.
- For `LinkChildren`: always include a `Where` filter on `source.ChildNodes` to select appropriate child types.
- Namespace: `Migration.Tool.Extensions.ContentItemDirectors` (user can override).
- File naming convention: `{ConcernName}Director.cs`.
- Use string constants for class names and field names, following the `Source_`/`Target_` prefix convention from the example.
- Add `TODO` comments for values unknown at generation time (e.g., widget type identifiers, editable area names, section identifiers). Class IDs should be available from the migration plan's page type table; only add `TODO` if genuinely missing.
- Do not regenerate `appsettings.json` or class mapping code — only `ContentItemDirectorBase` code.
- If a Kentico documentation lookup tool is available, verify uncertain API details before generating code.
- After generating code, always build the `Migration.Tool.Extensions` project and fix any compilation errors before considering the task complete.

## Gotchas

- **Default linked pages to Drop** — unless the migration plan explicitly requests Materialize or StoreReference for a linked page, generate `options.Drop()` for it in `DirectLinkedNode()`. This prevents silent content duplication. Add a comment explaining the decision so the user can switch to Materialize if needed.
- **Handle utility paths explicitly** — pages under system/utility paths (`/Reused-content/`, `/Error-pages/`, `/System/`, etc.) must have explicit `Direct()` handling (typically `options.Drop()`). Never let them fall through to default migration, which maps them based on their source class name and produces unexpected results.
- **Query KX13 for linked pages only when needed** — if the migration plan explicitly states no linked pages exist or includes a complete linked page audit, trust the plan and skip the database query. Only query the KX13 database when the plan is silent on linked pages (no Linked Page Handling/Audit section). Do not re-query for confirmation when the plan already covers it.
- **`Direct()` is abstract, not virtual** — `ContentItemDirectorBase.Direct()` is declared `abstract`. Calling `base.Direct(source, options)` produces compiler error `CS0205: Cannot call an abstract base member`. In the default/fallback branch of your `Direct()` override, simply do nothing (no action = standard migration behavior). By contrast, `DirectLinkedNode()` is `virtual`, so `base.DirectLinkedNode(source, options)` works fine.
- **`ICmsTree` does not have `SourceClassName`** — child nodes from `source.ChildNodes` and linked nodes from `source.LinkedNode` are `ICmsTree` objects. `ICmsTree` exposes `NodeClassID` (int), `NodeAliasPath`, `NodeName`, `NodeGUID`, etc. — but NOT `SourceClassName`. The `SourceClassName` property exists only on the top-level `ContentItemSource` and `LinkedPageSource` records. When filtering child nodes in `LinkChildren`, use `NodeClassID` (e.g., `.Where(c => c.NodeClassID == cafeClassId)`). Read class IDs from the migration plan's page type table (ClassID column). Only query the `CMS_Class` table if the plan does not include class IDs.
- **`ConvertClassesToContentHub` must list every child type used in `LinkChildren`** — if a child content type referenced in a `LinkChildren` call is not included in the `ConvertClassesToContentHub` appsettings list, the linking silently fails (no error, no reference created). When generating prerequisite comments on the director or its registration method, explicitly list every child type that must appear in `ConvertClassesToContentHub`. When presenting the summary, cross-check the plan's `ConvertClassesToContentHub` value against all child types used in `LinkChildren` calls and warn if any are missing.
- **`StoreReferenceInAncestor` requires a compatible ancestor** — the ancestor content type must support content item reference fields. If the ancestor is a webpage content type that does not already have a content item reference field defined (via `IClassMapping` `WithoutSource("contentitemreference")` or by the tool auto-creating it), `StoreReferenceInAncestor` will fail with "Content item is not reusable, but specifies linked children". Before generating `StoreReferenceInAncestor`, verify that the ancestor content type will have the reference field — check the corresponding `IClassMapping` for a `WithoutSource("contentitemreference")` field matching the `fieldName` parameter, or confirm the tool will auto-create it.
