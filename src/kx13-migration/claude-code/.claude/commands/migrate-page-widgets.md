---
description: "Migrate page widgets from KX13 to XbyK project"
argument-hint: "pageName legacyPageUrl"
allowed-tools: Bash, Glob, Grep, Read, Edit, Write, NotebookEdit, WebFetch, TodoWrite, WebSearch, BashOutput, AskUserQuestion, Skill, SlashCommand, "kentico.docs.mcp/*", "playwright-mcp/*"
---

You are tasked with process of migrating the widgets and sections of page builder from the legacy project to the new one.

## Input Parameters

- **Page Name:** `${input:pageName}` - The name of the page, which widgets need to be migrated (e.g., 'home', 'doctors').
- **Legacy Page URL:** `${input:legacyPageUrl}` - The URL of the page in the KX13 project (e.g., 'https://localhost:5001/en-us/home').

## Structure of the projects

Look at the file `../instructions/projects-structure.md` to understand the structure of both the legacy and new project.

## Important

When migrating page, ensure that everything that was fetched dynamically from database will still be dynamically fetched from database. Nothing can be statically hardcoded in the new project if it was dynamic in the legacy project.

## Useful Documentation

- Use Kentico docs MCP to read following pages:
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
- Use web fetch to read following pages:
  - [Migration Tool README - Pages](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool/blob/master/Migration.Tool.CLI/README.md)

## Migration Steps

1. Read all documentation links mentioned above.
2. Check out how the legacy page looks like using the provided URL `${input:legacyPageUrl}` and identify all parts it consists of.
3. Go through pages in the legacy project and identify the provided page `${input:pageName}`.
4. When you know the page, research from which sections and widgets this page consists of or which it uses.
5. If present, check how other widgets and sections are implemented in the new project.
6. Migrate all the page builder widgets and sections identified in previous steps together with all their dependencies.
7. When done with implementation, ensure that the new project builds successfully without errors and warnings. If not, fix the issues until no are present.

Whenever unsure about anything, you can use Kentico Docs MCP to search for relevant information.

## Output format

When done, provide user with this exact output (without any additional text):

```
# Migration Complete
Page builder widget migration from the legacy project to the new one has been successfully completed.

**Next steps:**
- Review the changes to ensure everything is looking as expected.
- Continue with the next prompt for migration of the page itself.
```
