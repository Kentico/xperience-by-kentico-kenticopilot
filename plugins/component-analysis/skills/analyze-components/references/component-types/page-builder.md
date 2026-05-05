# Page Builder category guidance

## In scope

- widgets
- sections
- templates
- personalization condition types
- editable area support where relevant to component usage

## Discovery anchors

- `RegisterWidget`
- `RegisterSection`
- template registration attributes
- personalization condition registrations
- `ComponentViewModel`
- widget or section property classes
- Razor views under component-oriented folders

## Consistency priorities

- stable unique identifiers with a clear prefixing strategy
- consistent registration locations and metadata completeness
- file, folder, and class naming follows one repeatable convention across similar component types
- widgets remain in global scope where Kentico guidance requires it
- properties models and editing components follow a repeatable pattern
- non-trivial logic lives in view components or services rather than in Razor views
- sections define widget zones consistently
- templates include required builder support and asset handling consistently
- scripts and styles follow one asset organization strategy

## Evidence to capture

- registration attribute locations
- identifier patterns across widgets, sections, and templates
- file paths and class names for equivalent widgets/sections to verify naming convention alignment
- properties model usage and editing component annotations
- view-component versus partial-view usage patterns
- examples of script initialization and asset placement

## Common high-value findings

- multiple folder and naming conventions for otherwise similar widgets
- direct business logic in views for some widgets but not others
- inconsistent use of typed properties and view models
- widgets placed in areas or non-standard locations that conflict with platform guidance
- inconsistent registration metadata such as names, descriptions, or icons

## Recommendation style

Prefer patterns that create one obvious way to build the next widget, section, or template in the project.

---

## Platform-specific checks (sourced from Kentico docs)

### Widgets — registration and file placement

- **Identifier format**: `CompanyName.ComponentName` — check all widget identifiers for a consistent company/project prefix. Inconsistent identifiers break deployability to other projects.
- **`RegisterWidget` parameters**: for basic widgets the required parameters are `identifier`, `name`, and `customViewName` (if not using default convention); for view-component widgets, `identifier`, `viewComponentType`, and `name`. `propertiesType` is optional but required when properties exist. Missing `propertiesType` when properties exist causes the dialog to open empty.
- **NOT in Areas**: widgets must be placed at the application root scope, not inside ASP.NET Core Areas. Files in Areas lead to unexpected behavior (incorrect view resolution).
- **File organization**: recommended layout is `~/Components/Widgets/<WidgetName>/` containing the view component, partial view, property model, and view model. Verify that comparable widgets follow the same folder structure.
- **`AllowCache` flag consistency**: `RegisterWidget` supports an `AllowCache` property. Check whether widgets that retrieve data without personalization or user-specific content have caching enabled; widgets with user-specific output should have it disabled. Inconsistency in this flag is a performance and correctness risk.
- **`Description` and `IconClass` metadata**: check for completeness and consistency across all widget registrations. Missing descriptions degrade the editor experience. `IconClass` values must start with the `icon-` prefix.

### Widgets — view model and property separation

- **`ComponentViewModel<TPropertiesType>` as the model**: the partial view or view component must declare `ComponentViewModel<TPropertiesType>` as its model (or parameter) — not the properties class directly. Using the properties class as the model is explicitly unsupported.
- **Do NOT pass the property model directly to views**: create a separate view model. The property model should be consumed in the view component's `Invoke`/`InvokeAsync` method and its data mapped to a view model before the view is rendered.
- **`Page` property of `ComponentViewModel`**: used to access the page where the widget is rendered (ID, language, content type, channel). If widgets need page context, this is the correct path — not HTTP context hacks.
- **Property model**: must implement `IWidgetProperties`. `Newtonsoft.Json.JsonIgnore` should be applied to any dynamically computed properties that must not persist in the database.

### Widgets — scripts and styles

- **Asset placement**: live-site scripts/styles go to `~/wwwroot/PageBuilder/Public/Widgets/<WidgetName>/`; admin/editor-only assets go to `~/wwwroot/PageBuilder/Admin/Widgets/<WidgetName>/`. Using a Shared subfolder is acceptable for cross-widget assets. Check that all widgets follow the same placement strategy.
- **Script initialization pattern**: inline `<script>` in widget views must guard against both `DOMContentLoaded` (initial load) and dynamic insertion (Page Builder editing). The standard pattern is `if (document.readyState === "loading") { ... addEventListener ... } else { ... directly call ... }`. Flag widget scripts that only bind to `DOMContentLoaded` without the dynamic-insertion fallback.
- **No duplicate scripts**: scripts and styles must not be linked or executed directly in widget views — doing so causes duplication when multiple widget instances appear on the same page.

### Sections — registration and structure

- **`RegisterSection` parameters**: `identifier`, `name`, and optionally `PropertiesType` and `CustomViewName`. For view-component sections: `identifier`, `viewComponentType`, and `name`. Check consistency across all section registrations.
- **Identifier prefix**: same `CompanyName.SectionName` convention as widgets. Flag sections without a project-unique prefix.
- **NOT in Areas**: same constraint as widgets. Files must be at application root scope.
- **Every section must contain at least one widget zone**: using `WidgetZoneAsync()` or the `<widget-zone />` Tag Helper. Sections without any zone are not supported and will error at runtime.
- **File organization**: recommended layout is `~/Components/Sections/<SectionName>/`. View, view component, properties, and view model should be co-located.
- **Named widget zones for consistent transfer**: when a user changes section type in the editor, widgets are moved by zone order or by zone identifier. Use named zones (`WidgetZoneAsync("main")`) and reuse the same identifiers across comparable sections so widget transfer is predictable.
- **Do NOT pass the property model directly to views**: same as widgets — create a separate view model and pass that.
- **Properties model**: must implement `ISectionProperties`. `Newtonsoft.Json.JsonIgnore` applies for computed properties.
- **Asset placement**: `~/wwwroot/PageBuilder/Public/Sections/<SectionName>/` and `~/wwwroot/PageBuilder/Admin/Sections/<SectionName>/`. Same DOMContentLoaded/dynamic-insertion script guard required.

### Sections — default section configuration

- **`DefaultSectionIdentifier` in `PageBuilderOptions`**: if the project uses a custom default section, `RegisterDefaultSection = false` must be set to disable the system built-in default. Check that existing pages have been migrated before the built-in section is disabled.
- **Per-area override via `EditableAreaOptions`**: individual editable areas can override the global default. Check that per-area overrides use the correct identifier and that sections exist.

### Properties configuration dialogs

- **Editing component attributes from `Kentico.Xperience.Admin.Base.FormAnnotations`**: when defining configuration dialogs for widget/section properties, editing components must come from the admin namespace. Check that projects are not accidentally referencing the Form Builder namespace.
- **Visibility conditions and validation rules on properties**: widget and section properties support the same visibility conditions and validation rules as admin UI form components. Check whether conditional property display is used where it would improve editor UX, and whether it is applied consistently.
- **Custom form components with content item links**: if a widget/section property uses a custom form component or inline editor that lets editors link to content items or pages (other than the built-in selectors or rich text editor), a **custom reference extractor** is required to enable usage tracking. Flag missing reference extractors.
