---
name: "page-builder-widgets"
description: "Knowledge and conventions for building and modifying Page Builder widgets in Xperience by Kentico — view component, properties with admin UI form components, view model, Razor view, registration, localization, and content retrieval. Proactively read this before creating, building, or modifying a Page Builder widget."
compatibility: "Requires Kentico Docs MCP"
---

# Page Builder widgets

This skill points you to what you need to build or modify a Page Builder widget in an Xperience by Kentico project. Study the project's existing widgets first, verify APIs against the docs below, then implement and test.

## Pieces of a widget

- **View component** (`<Name>WidgetViewComponent.cs`) – entry point; receives properties, retrieves content, builds the view model, returns the view. Carries `RegisterWidget` and an `IDENTIFIER` constant. Widgets with no content retrieval or logic (static widgets) can skip the view component entirely and register a plain partial view.
- **Properties** (`<Name>WidgetProperties.cs`) – implements `IWidgetProperties`; editor-configurable data, each field decorated with an admin UI form component (and optional visibility conditions).
- **View model** (`<Name>WidgetViewModel.cs`) – exactly what the view needs to render.
- **Razor view** (`_<Name>Widget.cshtml`) – the markup; reuses project CSS, degrades gracefully when data is missing.
- **Inline editors** (optional) – edit a property on the widget surface in edit mode.
- **Client-side assets** (optional) – JS/CSS following the project's bundling convention.
- **Server communication** (optional) – controller for POST actions, persisting page context via a PageData extension method.

## How to use

- Read `references/docs.md` — the documentation map. Fetch the relevant pages via the Kentico Docs MCP.
