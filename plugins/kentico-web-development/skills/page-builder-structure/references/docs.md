# Page Builder structure — documentation map

Fetch pages via the Kentico Docs MCP.

## Read first

- **Page Builder (overview)**: <https://docs.kentico.com/documentation/developers-and-admins/development/builders/page-builder>
  - Orientation for any section or template work: enabling the Page Builder feature and the editable area → section → widget zone → widget hierarchy.

## Sections — read all of these when building a section

- **Sections for Page Builder**: <https://docs.kentico.com/documentation/developers-and-admins/development/builders/page-builder/sections-for-page-builder>
- **Section properties (+ full example)**: <https://docs.kentico.com/documentation/developers-and-admins/development/builders/page-builder/sections-for-page-builder/section-properties>
- **Implement flexible sections (guide)**: <https://docs.kentico.com/guides/development/page-builder/implement-flexible-sections>

## Page templates — read all of these when building a template

- **Page templates for Page Builder**: <https://docs.kentico.com/documentation/developers-and-admins/development/builders/page-builder/page-templates-for-page-builder>
- **Page template properties**: <https://docs.kentico.com/documentation/developers-and-admins/development/builders/page-builder/page-templates-for-page-builder/page-template-properties>
- **Create versatile page templates — part 1 (guide)**: <https://docs.kentico.com/guides/development/page-builder/create-versatile-templates-part-1>
- **Create versatile page templates — part 2 (guide)**: <https://docs.kentico.com/guides/development/page-builder/create-versatile-templates-part-2>
- **Create pages with editable areas**: <https://docs.kentico.com/documentation/developers-and-admins/development/builders/page-builder/create-pages-with-editable-areas>
  - Templates render editable areas and must load Page Builder scripts/styles — this page covers both, plus restricting which widgets/sections an area allows.

## References — consult when needed

- **Reference — default Page Builder widgets**: <https://docs.kentico.com/documentation/developers-and-admins/development/builders/page-builder/reference-default-page-builder-widgets>
  - Read when a template or section needs the identifiers of the default widgets (e.g. for an `allowedWidgets` allowlist).
- **Page template filtering**: <https://docs.kentico.com/documentation/developers-and-admins/development/builders/page-builder/page-templates-for-page-builder/page-template-filtering>
  - Read only to restrict which templates appear per page beyond the `ContentTypeNames` registration parameter (advanced `IPageTemplateFilter`; not respected by the management API/MCP).
- **Admin UI localization**: <https://docs.kentico.com/documentation/developers-and-admins/customization/admin-ui-localization>
  - Read when localizing a section's or template's name, description, or property labels — `{$resource.key$}` localization expressions in registration and form-component attributes, and `LocalizationTarget.Builder` resources.
- **Integrate custom code — resource files**: <https://docs.kentico.com/documentation/developers-and-admins/customization/integrate-custom-code#store-application-resources-in-resource-files>
  - Read when creating and registering the `.resx` resource files (`RegisterLocalizationResource` attribute) that back the localization expressions.
- **Reference — tag helpers**: <https://docs.kentico.com/documentation/developers-and-admins/development/reference-tag-helpers>
  - Read when the view needs a tag helper (`<page-builder-styles />`, `<page-builder-scripts />`, editable areas, widget zones).
- **Content-tree-based routing**: <https://docs.kentico.com/documentation/developers-and-admins/development/routing/content-tree-based-routing/set-up-content-tree-based-routing>
  - Read when setting up the routing that serves Page Builder pages.
- **Bundle static assets of builder components**: <https://docs.kentico.com/documentation/developers-and-admins/development/builders/bundle-static-assets-of-builder-components>
  - Read when a section or template ships its own JS/CSS.
- **Secure custom endpoints**: <https://docs.kentico.com/documentation/developers-and-admins/customization/secure-custom-endpoints>
  - Read when a component adds a server endpoint (POST handling) that must be secured.
