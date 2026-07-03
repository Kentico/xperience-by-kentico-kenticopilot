---
name: "widget-creation"
description: "Knowledge and conventions for building Page Builder widgets in Xperience by Kentico — view component, properties with admin UI form components, view model, Razor view, registration, localization, caching, and content retrieval. Use whenever creating, building, or modifying a Page Builder widget."
compatibility: "Requires Kentico Docs MCP (for verifying API shapes)"
---

# Page Builder widget creation

This skill provides the knowledge needed to build a custom Page Builder widget in an Xperience by Kentico project from a set of requirements (and an optional design). Read the requirements, study the project's existing widgets, then implement directly.

## Workflow

1. **Understand the requirements.** Read the user's requirements file and any design file (e.g. exported HTML/Figma), and capture every dimension listed under [What to capture](#what-to-capture) below.
2. **Study the project first.** Existing widgets are the source of truth for conventions in this repository. Locate them (the bundled examples use `Components/Widgets/` and `Features/**/Widgets/`, but follow whatever layout your project uses) and mirror their namespace layout, file structure, registration style, localization approach, and content-retrieval patterns. When the project's conventions differ from the examples in `references/`, the project wins.
3. **Verify APIs via the Kentico Docs MCP.** Use the Kentico Docs MCP server and the links in `references/docs.md` to confirm form-component attributes, the `IContentRetriever` API, page-URL retrieval, and content-item system fields before relying on them. Do not guess API shapes — plausible-looking signatures are often wrong.
4. **Implement** the widget following the anatomy and rules below.
5. **Build and test.** Build the web project (locate the `.csproj`/`.sln` the widget belongs to — typically the web application project) and fix any errors related to the new widget. If a build target isn't obvious or the project can't be built in this environment, say so rather than guessing. Verify the widget renders in both edit and live mode.

## What to capture

Before implementing, make sure the requirements (and design) give you each of these. Note anything unstated and either infer it from project conventions or ask:

- **Identification** — widget name, identifier (`CompanyName.WidgetName`), description, icon class.
- **Purpose** — what the widget is for and its use cases.
- **Core functionality** — main features, content-selection capabilities, presentation/layout options, configuration options, and any linked content the widget retrieves.
- **Properties** — for each editor-configurable property: name, type, form component, whether required, default value, description.
- **Data requirements** — external content types the widget reads, the retrieval logic (which `IContentRetriever` methods and how), and any service or external dependencies.
- **View model** — the data structure passed to the view (property, type, source).
- **HTML structure** — the expected markup.
- **Styling** — CSS classes, inline styles, and **existing project styles to reuse** rather than reinventing.
- **Responsive behavior** — how the widget should adapt across screen sizes.
- **Inline editing** — whether any property should be editable directly on the surface in edit mode (via an inline editor) rather than only through the properties dialog. See [Inline editors](#inline-editors) below.
- **JavaScript / client-side** — any client-side interactions or behaviors, and where the scripts live.
- **Constraints / additional notes** — any other requirements or limitations the user specifies.

## Widget anatomy

A typical widget consists of four code files plus registration and localized strings. See `references/example-widgets.md` for complete, working examples (a simple card widget and a call-to-action widget with conditional properties).

- **View component** (`<Name>WidgetViewComponent.cs`) — derives from `ViewComponent`, receives the properties via `InvokeAsync`, retrieves any linked content, builds the view model, and returns the Razor view. Carries the `RegisterWidget` assembly attribute and an `IDENTIFIER` constant.
- **Properties** (`<Name>WidgetProperties.cs`) — implements `IWidgetProperties`. Each editor-configurable property is decorated with an admin UI form component attribute (e.g. `TextInputComponent`, `DropDownComponent`, `CheckBoxComponent`, `ContentItemSelectorComponent`). Use `VisibleIfEqualTo` (and similar) for conditional visibility.
- **View model** (`<Name>WidgetViewModel.cs`) — a plain class holding exactly the data the view needs.
- **Razor view** (`_<Name>Widget.cshtml`) — renders the markup; applies the agreed styling and responsive behavior, reusing existing project CSS classes where possible; handles missing data gracefully.
- **Client-side assets** (optional) — any JavaScript/CSS the widget needs. Follow the project's convention for where these live and how they are bundled; only add them when the requirements call for client-side behavior.

### Inline editors

Inline editors let editors change a property's value directly on the widget surface inside the Page Builder editing UI, instead of only through the properties dialog — useful for text and other content edited "in place." They are an optional Page Builder feature, not required for a working widget.

An inline editor is a separate partial (conventionally under `~/Components/InlineEditors/<EditorName>/`) rendered from the widget view, and only in edit mode. The Card widget in `references/example-widgets.md` shows the pattern: inside a `Context.Kentico().PageBuilder().EditMode` check, render the editor partial with a model that carries the target property name (`nameof(...)`) and current value; in live mode, render the plain value instead. Reuse an existing inline editor from the project when one fits rather than authoring a new one. See the docs link in `references/docs.md` for building custom editors and their registration scripts.

## Important rules

- **Location.** Like all Page Builder components, widget code files must live in the application root of the web project, **not** inside an MVC Area — placing them in an Area can cause unexpected behavior. Beyond that, follow the project's folder layout (e.g. `Components/Widgets/<Name>/` or a feature-based path).

- **Follow project conventions over examples.** Reuse existing widgets' patterns for structure, naming, and shared services.
- **Caching.** Always cache content retrieval unless explicitly told otherwise.
- **Null/empty handling.** Validate that properties and retrieved data are not null/empty before accessing them.
- **Edit vs. live mode.** Configuration prompts and "configure this widget" messaging belong in edit mode only (`Context.Kentico().PageBuilder().EditMode`). On the live site the widget must degrade gracefully when data is missing.
- **Content selection.** To display linked content, prefer the Combined Content Selector / `ContentItemSelectorComponent` over the Web Page Selector.
- **Localization.** Use localized resource strings for all user-facing text (widget name, description, property labels). Add entries to the project's `.resx` resource file; create the resource file and its registration class if none exists.
- **No magic strings.** Use constants or `nameof` instead of hardcoded strings; the `RegisterWidget` identifier should be a constant.
- **Registration.** Register the widget with the `RegisterWidget` assembly attribute (identifier, view component type, localized name, properties type, description, icon class). Use a company/project prefix in the identifier (e.g. `CompanyName.WidgetName`).

## References

- `references/docs.md` — documentation links to check via the Kentico Docs MCP (widgets, content retrieval, UI form components, page URLs, content-item system fields).
- `references/example-widgets.md` — full example widget implementations to model new widgets on.

For broader Page Builder context (sections, page templates, widget zones), see the `page-builder` skill.
