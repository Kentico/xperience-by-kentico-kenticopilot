---
name: "page-builder-structure"
description: "Knowledge and conventions for Page Builder structure in Xperience by Kentico — sections (widget-zone layouts) and page templates (full-page layouts), including registration, properties, widget zones, default sections, and editable areas. Use when creating or modifying Page Builder sections or page templates, or setting up Page Builder layout structure."
compatibility: "Requires Kentico Docs MCP"
---

# Page Builder structure: sections and page templates

This skill points you to what you need to build the structural layers of Page Builder in an Xperience by Kentico project: **sections** (arrange widget zones inside an editable area) and **page templates** (define a page's full layout). Study the project's existing sections and templates first, verify APIs against the docs below, then implement and test.

## Pieces of Page Builder structure

- **Sections** – layout of widget zones within an editable area; _basic_ (partial view) or _view-component-based_. Registered with `RegisterSection`, optional `ISectionProperties`.
- **Widget zones** – the slots inside a section that hold widgets; identifiers matter when editors switch section type.
- **Default section** – the section applied to new/unstyled areas.
- **Page templates** – full-page MVC layouts editors assign to a page. Registered with `RegisterPageTemplate`, scoped by content type.
- **Editable areas** – the Page Builder regions of a template; identifiers matter when editors switch template.

## How to use

- Read `references/docs.md` — the documentation map. Fetch the relevant pages via the Kentico Docs MCP.
