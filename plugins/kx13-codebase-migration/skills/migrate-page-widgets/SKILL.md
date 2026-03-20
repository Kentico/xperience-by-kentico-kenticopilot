---
name: migrate-page-widgets
description: "Migrates Page Builder widgets and sections from a Kentico Xperience 13 page to Xperience by Kentico. Identifies and converts all widget and section components used by the specified page. Use when migrating Page Builder features for a specific page before migrating the page logic itself."
argument-hint: "Name of the page in the content tree and the legacy page URL"
compatibility: "Requires Kentico Docs MCP"
---

You are tasked with the process of migrating the widgets and sections of Page Builder from the legacy project to the new one.

## Input Parameters

- **Page Name** - The name of the page, which widgets need to be migrated (e.g., 'home', 'doctors').
- **Legacy Page URL** - The URL of the page in the KX13 project (e.g., 'https://localhost:5001/en-us/home').

## Structure of the projects

You are currently located in the root folder, which contains two subfolders:

- `KX13/` - This folder contains the Kentico Xperience 13 project files. This is the legacy/source project.
- `XbyK/` - This folder contains the Xperience by Kentico project files. This is the new project.

## Important

When migrating a page, ensure that everything that was fetched dynamically from the database will still be dynamically fetched from the database. Nothing can be statically hardcoded in the new project if it was dynamic in the legacy project.

## Useful Documentation

- Use Kentico Docs MCP to read the following pages:
  - [Adjust your code and adapt](https://docs.kentico.com/guides/architecture/upgrade-from-kx13/adjust-your-code-and-adapt)
  - [Upgrade content retrieval code](https://docs.kentico.com/guides/development/upgrade-deep-dives/upgrade-content-retrieval)
  - [Content Retrieval](https://docs.kentico.com/documentation/developers-and-admins/development/content-retrieval)
  - [Content Retriever API](https://docs.kentico.com/documentation/developers-and-admins/api/content-item-api/content-retriever-api)
  - [Page Builder](https://docs.kentico.com/documentation/developers-and-admins/development/builders/page-builder)
  - [Widgets for Page Builder](https://docs.kentico.com/documentation/developers-and-admins/development/builders/page-builder/widgets-for-page-builder)
  - [Widget Properties](https://docs.kentico.com/documentation/developers-and-admins/development/builders/page-builder/widgets-for-page-builder/widget-properties)
  - [Sections for Page Builder](https://docs.kentico.com/documentation/developers-and-admins/development/builders/page-builder/sections-for-page-builder)
  - [Section Properties](https://docs.kentico.com/documentation/developers-and-admins/development/builders/page-builder/sections-for-page-builder/section-properties)
  - [Upgrade Widgets Introduction](https://docs.kentico.com/guides/development/upgrade-deep-dives/upgrade-widgets-introduction)
- Use web fetch to read the following pages:
  - [Migration Tool README - Pages](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool/blob/master/Migration.Tool.CLI/README.md)

## Migration Steps

1. Read all documentation links mentioned above.
2. Review how the legacy page looks using the provided URL and identify all parts it consists of.
3. Go through pages in the legacy project and identify the provided page.
4. When you know the page, research which sections and widgets this page uses.
5. If present, check how other widgets and sections are implemented in the new project.
6. Migrate all the page builder widgets and sections identified in previous steps together with all their dependencies.
7. When done with implementation, ensure that the new project builds successfully without errors and warnings. If there are issues, fix them until none are present.

Whenever unsure about anything, use Kentico Docs MCP to search for relevant information.

## Output format

When done, provide the user with this exact output (without any additional text):

```
# Migration Complete
Page builder widget migration from the legacy project to the new one has been successfully completed.

**Next steps:**
- Review the changes to ensure everything looks as expected.
- Continue with the migrate-page skill to migrate individual pages.
```
