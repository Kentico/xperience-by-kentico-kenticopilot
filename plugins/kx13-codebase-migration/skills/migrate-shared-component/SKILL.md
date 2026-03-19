---
name: migrate-shared-component
description: "Migrates a reusable shared component (header, footer, navigation, breadcrumbs, etc.) from a Kentico Xperience 13 project to Xperience by Kentico. Locates the component in the source project and migrates it together with all dependencies. Use when migrating shared layout elements like headers, footers, or navigation."
argument-hint: "Name of the shared component and the legacy page URL where it appears"
compatibility: "Requires Kentico Docs MCP and Playwright MCP"
---

You are tasked with the process of migrating a shared component from the legacy project to the new one.

## Input Parameters

- **Component Name** - The name of the shared component to migrate (e.g., 'breadcrumbs', 'header').
- **Legacy Page URL** - The URL of the page in the KX13 project that contains the component (e.g., 'https://localhost:5001/en-us/home').

## Structure of the projects

You are currently located in the root folder, which contains two subfolders:

- `KX13/` - This folder contains the Kentico Xperience 13 project files. This is the legacy/source project.
- `XbyK/` - This folder contains the Xperience by Kentico project files. This is the new project.

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
2. Review how the legacy page looks using the provided URL and identify the shared component.
3. Go through files in the legacy project and identify the provided component.
4. When you know the component, research how this component works and identify all other shared components, pages, or whatever this component relies on.
5. If present, check how other components are implemented in the new project.
6. Migrate component's controller, content, layout, and relevant components, repositories, and services to the new project, together with all dependencies identified.
7. When done with implementation, ensure that the new project builds successfully without errors and warnings. If not, fix the issues until none are present.
8. Using the Playwright MCP, check that the migrated component is displayed correctly and functions as expected, exactly matching styling, content, and texts as in the image of the legacy page. If not, make necessary adjustments until it does (this can also include changes in dependencies).

Whenever unsure about anything, you can use Kentico Docs MCP to search for relevant information.

## Output format

When done, provide the user with this exact output (without any additional text):

```
# Migration Complete
Shared component migration from the legacy project to the new one has been successfully completed.

**Next steps:**
- Review the changes to ensure everything looks as expected.
- Use the migrate-page-visual skill to fix visual issues with the migration.
```
