# Kentico Migration Documentation Links

## Migration Strategy & Planning

- **Plan your strategy for migrating features**: https://docs.kentico.com/guides/upgrade-to-xbyk/upgrade-from-kx13/plan-your-strategy-for-migrating-features
- **Upgrade walkthrough**: https://docs.kentico.com/x/upgrade_walkthrough_guides
- **Upgrade FAQ**: https://docs.kentico.com/guides/upgrade-to-xbyk/upgrade-from-kx13/upgrade-faq

## Migration Tool

- **Supported Data**: https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool/blob/master/docs/Supported-Data.md
- **Migration Tool CLI README**: https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool/blob/master/Migration.Tool.CLI/README.md
- **Migration Tool Extensions**: https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool/tree/master/Migration.Tool.Extensions
- **Migration details for specific object types**: https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool/blob/master/Migration.Tool.CLI/README.md#migration-details-for-specific-object-types

## Widget Migration

- **Upgrade widgets introduction**: https://docs.kentico.com/guides/upgrade-to-xbyk/upgrade-deep-dives/upgrade-widgets-introduction
- **Transform widget properties**: https://docs.kentico.com/guides/upgrade-to-xbyk/upgrade-deep-dives/transform-widget-properties
- **Migrate widget data to Content hub**: https://docs.kentico.com/guides/upgrade-to-xbyk/upgrade-deep-dives/migrate-widget-data-to-content-hub
- **Convert child pages to widgets**: https://docs.kentico.com/guides/upgrade-to-xbyk/upgrade-deep-dives/convert-child-pages-to-widgets

## Content Model Deep Dives

- **Transfer page hierarchy to Content hub**: https://docs.kentico.com/guides/upgrade-to-xbyk/upgrade-deep-dives/transfer-page-hierarchy-to-content-hub
- **Remodel page types as reusable field schemas**: https://docs.kentico.com/guides/upgrade-to-xbyk/upgrade-deep-dives/remodel-page-types-as-reusable-field-schemas
- **Speed up remodeling with AI**: https://docs.kentico.com/guides/upgrade-to-xbyk/upgrade-deep-dives/speed-up-remodeling-with-ai
- **Optimize images during upgrade**: https://docs.kentico.com/guides/upgrade-to-xbyk/upgrade-deep-dives/optimize-images-during-upgrade


## XbyK Content Model

For these topics, use the Kentico Docs MCP server (`kentico.docs.mcp`) to search or fetch documentation.

- **Content types**: search "content types overview"
- **Reusable field schemas**: search "reusable field schemas"
- **Content hub**: search "content hub"
- **Page Builder**: search "page builder overview"
- **Taxonomies**: search "taxonomies"
- **Custom modules**: search "create basic module" or "add channels to module" (for website-specific settings)

## KX13 Source Documentation

For KX13-specific topics (source instance page types, widgets, form controls, Page Builder, etc.), use the **Context7 MCP server** (`context7`).

1. The KX13 library ID is `/websites/kentico_13` — use it directly with the `query-docs` tool, no need to call `resolve-library-id` first.
2. Example queries:
   - "Rich text widget for Page Builder — how does Kentico.Widget.RichText work?"
   - "How do page relationships and linked pages work in KX13?"
   - "What form controls are available for page type fields?"
   - "How does the media library selector form control work?"
