---
description: "Migrate page from KX13 to XbyK project"
argument-hint: "pageName legacyPageUrl"
tools:
  [
    "vscode",
    "execute",
    "read",
    "edit",
    "search",
    "web/fetch",
    "kentico.docs.mcp/*",
    "playwright-mcp/*",
    "agent",
    "todo",
  ]
---

You are tasked with the process of migrating a page from the legacy project to the new one.

## Input Parameters

- **Page Name:** `${input:pageName}` - The name of the page to migrate (e.g., 'home', 'doctors').
- **Legacy Page URL:** `${input:legacyPageUrl}` - The URL of the page in the KX13 project (e.g., 'https://localhost:5001/en-us/home').

## Structure of the projects

Look at the file `../instructions/projects-structure.instructions.md` to understand the structure of both the legacy (source) and new (target) projects.

## Important

When migrating a page, ensure that everything that was fetched dynamically from the database will still be dynamically fetched from the database. Nothing can be statically hardcoded in the new project if it was dynamic in the legacy project.

## Useful Documentation

- Use Kentico Docs MCP to read the following pages:
  - [Display an upgraded page](https://docs.kentico.com/guides/architecture/upgrade-from-kx13/upgrade-walkthrough/display-an-upgraded-page)
  - [Adjust your code and adapt](https://docs.kentico.com/guides/architecture/upgrade-from-kx13/adjust-your-code-and-adapt)
  - [Upgrade content retrieval code](https://docs.kentico.com/guides/development/upgrade-deep-dives/upgrade-content-retrieval)
  - [Content Retrieval](https://docs.kentico.com/documentation/developers-and-admins/development/content-retrieval)
  - [Content Retriever API](https://docs.kentico.com/documentation/developers-and-admins/api/content-item-api/content-retriever-api)
  - [Page Builder](https://docs.kentico.com/documentation/developers-and-admins/development/builders/page-builder)
  - [Widgets for Page Builder](https://docs.kentico.com/documentation/developers-and-admins/development/builders/page-builder/widgets-for-page-builder)
  - [Sections for Page Builder](https://docs.kentico.com/documentation/developers-and-admins/development/builders/page-builder/sections-for-page-builder)
- Use web fetch to read the following pages:
  - [Migration Tool README - Pages](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool/blob/master/Migration.Tool.CLI/README.md#pages)

## Migration Steps

1. Read all documentation links mentioned above.
2. Check out how the legacy page looks like using the provided URL `${input:legacyPageUrl}` and identify all parts it consists of.
3. Go through pages in the legacy project and identify the provided page `${input:pageName}`.
4. When you know the page, research how this page works and identify all other shared pages, components, or whatever this page relies on.
5. If present, check how other pages are implemented in the new project.
6. Migrate page's controller, content, layout, and relevant components, repositories, and services to the new project, together with all dependencies identified.
7. When done with implementation, ensure that the new project builds successfully without errors and warnings. If not, fix the issues until none are present.
8. Using the Playwright MCP, check that the migrated page is displayed correctly and functions as expected, exactly matching styling, content, and texts as in the image of the legacy page. If not, make necessary adjustments until it does (this can also include changes in dependencies).

Whenever unsure about anything, you can use Kentico Docs MCP to search for relevant information.

## Output format

When done, provide user with this exact output (without any additional text):

```
# Migration Complete
Page migration from the legacy project to the new one has been successfully completed.

**Next steps:**
- Review the changes to ensure everything looks as expected.
- You can use the /migrate-page-visual prompt to fix visual issues caused by the migration.

The page migration is now complete. Continue migrating other pages by repeatedly running /migrate-page-widgets and /migrate-page. To migrate shared components such as layouts, headers, footers, use /migrate-shared-component.
```
