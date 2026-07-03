---
name: "page-builder"
description: "Knowledge and conventions for Page Builder structure in Xperience by Kentico — sections (widget-zone layouts) and page templates (full-page layouts), including registration, properties, widget zones, default sections, and editable areas. Use when creating or modifying Page Builder sections or page templates, or setting up Page Builder layout structure."
compatibility: "Requires Kentico Docs MCP"
---

# Page Builder structure: sections and page templates

This skill provides the knowledge needed to build the structural layers of Page Builder in an Xperience by Kentico project: **sections** (which arrange widget zones inside an editable area) and **page templates** (which define a page's full layout). For building individual widgets, use the `widget-creation` skill.

## Workflow

1. **Understand the requirement.** Decide whether you need a section (a reusable widget-zone layout content editors choose inside an editable area), a page template (a full-page layout editors assign to a page), or both.
2. **Study the project first.** Existing sections and page templates are the source of truth for conventions in this repository (the bundled examples use `Components/Sections/<Name>/` and `PageTemplates/`, but follow whatever layout your project uses). Mirror their structure, registration style, naming, and localization. When project conventions differ from the docs examples, the project wins.
3. **Verify APIs via the Kentico Docs MCP** and the links in `references/docs.md` before relying on them. Do not guess API shapes — plausible-looking signatures are often wrong.
4. **Implement** following the patterns below and the examples in `references/`.
5. **Build and test.** Build the project, fix errors, and verify the layout renders in both edit and live mode.

## Sections

Sections define the layout of widget zones within an editable area. The system ships a built-in **Default** section with a single widget zone; create custom sections for more advanced layouts.

Key facts:

- **Every section must contain at least one widget zone** — rendered with `@await Html.Kentico().WidgetZoneAsync()` (or the `<widget-zone />` Tag Helper). Sections without a widget zone are not supported.
- **Two flavors:** _basic_ (a partial view only) and _view-component-based_ (recommended when the section needs business logic, page context, or POST handling). A view-component section's `Invoke`/`InvokeAsync` method must declare a `ComponentViewModel` parameter (or `ComponentViewModel<TProperties>` for sections with properties) — both the synchronous and asynchronous signatures are supported, so pick whichever the logic needs.
- **Properties** are optional; define an `ISectionProperties`-style property class and pass its `System.Type` as `PropertiesType` during registration. Access values via `Model.Properties` in the view.
- **Register** with the `[assembly: RegisterSection(...)]` attribute (`Kentico.PageBuilder.Web.Mvc`). Use a company/project prefix in the identifier. Optional `Description` and `IconClass` (icons use the `icon-` prefix).
- **Location matters:** section code files must live in the application root (`~/Components/Sections/<SectionName>/`), **not** inside an MVC Area.
- **Named widget zones** — give zones matching identifiers across your sections (`WidgetZoneAsync("main")`) so widgets transfer correctly when an editor switches section type.
- **Default section** — override the system default by setting `DefaultSectionIdentifier` (and optionally `RegisterDefaultSection = false`) on `PageBuilderOptions`, or per editable area via `EditableAreaOptions`.
- **Limit widgets per zone** with the `allowedWidgets` parameter of `WidgetZoneAsync` (a guideline for editors, not a security measure).

See `references/example-section.md` for a complete view-component-based section with a configurable property.

## Page templates

Page templates let content editors assign a full-page MVC layout to a page without developer involvement — ideal for repeating structures like articles and landing pages.

Key facts:

- **The rendered output must be a full HTML page** — including `<html>`, `<head>`, `<body>`, all stylesheet/script links, and the Page Builder resources (`<page-builder-styles />` in the head, `<page-builder-scripts />` inside the body before `</body>`). This markup can live directly in the template view _or_ in a shared layout the template view references (`Layout = "..."`) — use whichever the project's other templates use, and ensure the Page Builder resource tags appear exactly once. Pages using templates do **not** have their own per-page view.
- **Default vs. preset:** developers create _default_ templates in code; editors can save _preset_ templates that snapshot Page Builder content on top of a default template. Preset templates do **not** store structured (field) content.
- **Content type setup:** the content type must have _Include in routing_ enabled and Page Builder enabled. The route's action method returns a `TemplateResult` (optionally with a custom model object).
- **Model:** the view uses `TemplateViewModel` or `TemplateViewModel<TProperties>`; access properties via `Model.Properties`, page context via `Model.Page`, and a custom model via `Model.GetTemplateModel<T>()`.
- **Register** with the `[assembly: RegisterPageTemplate(...)]` attribute (`Kentico.PageBuilder.Web.Mvc.PageTemplates`). Scope availability with the `ContentTypeNames` parameter (recommended) rather than `IPageTemplateFilter` where possible. Optional `Description` and `IconClass` (page-template icons use the `xp-` prefix from `Kentico.Xperience.Admin.Base.Icons`).
- **Location matters:** template code files must live in the application root (`~/PageTemplates/`), **not** inside an MVC Area. Store views in `~/PageTemplates/` named `_<Identifier>.cshtml`.
- **Editable-area transfer:** give editable areas matching identifiers across templates of the same content type so Page Builder content transfers when an editor switches templates; non-matching areas discard their content.

See `references/example-page-template.md` for a complete page template with a property.

## Important rules

- **Follow project conventions over docs examples.** Reuse existing sections/templates for structure, naming, and shared services.
- **Null/empty handling.** Validate page data and property values before accessing them.
- **Localization.** Use localized resource strings for editor-facing names and descriptions; add entries to the project's `.resx` file.
- **No magic strings.** Use constants or `nameof`; identifiers should be constants with a company/project prefix.
- **Edit vs. live mode.** Ensure the layout renders correctly in the Page Builder editing interface and on the live site.
- **Verify with the docs MCP** whenever you are unsure about an attribute, signature, or option.

## References

- `references/docs.md` — documentation links to check via the Kentico Docs MCP.
- `references/example-section.md` — a view-component-based section with a configurable property.
- `references/example-page-template.md` — a page template with a property.
