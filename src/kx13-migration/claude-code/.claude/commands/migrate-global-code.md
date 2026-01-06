---
description: "Migrate global code from KX13 to XbyK project."
allowed-tools: Bash, Glob, Grep, Read, Edit, Write, NotebookEdit, WebFetch, TodoWrite, WebSearch, BashOutput, AskUserQuestion, Skill, SlashCommand, mcp__kentico.docs.mcp__*
---

You are tasked with the process of migrating global code from a Kentico Xperience 13 project to an Xperience by Kentico project.

## Structure of the projects

Look at the file `../instructions/projects-structure.md` to understand the structure of both the legacy and new project.

## Migration Steps

1. Check out the structure of both the legacy and new project.
2. Use Kentico Docs MCP to read the following page: https://docs.kentico.com/guides/architecture/upgrade-from-kx13/upgrade-walkthrough/adjust-global-code (note that this guide is written for a sample project and that there will be some differences between the sample project and the project you are migrating)
3. Create a new project for generated code files (named {ProjectName}.Entities).
   1. Configure given project as described in the documentation.
   2. **CRITICAL:** Ensure the .csproj file contains the following (without this, content item reference fields will fail to populate):
      ```xml
      <ItemGroup>
        <AssemblyAttribute Include="CMS.AssemblyDiscoverableAttribute" />
      </ItemGroup>
      ```
4. Generate code files by running the `--kxp-codegen` command as described in the documentation. Always use `--skip-confirmation` flag to avoid interactive prompts
5. Copy relevant global code from source project to the new project.
   1. Localization
   2. Shared views
   3. Styles and scripts
   4. Identifiers
   5. Services registration
6. Configure the project to display content.
   1. Enable Content tree-based routing and Page Builder.
   2. Add future custom service registrations and localization.
7. In the end, ensure that the new project builds successfully without errors and warnings, if not, some part of documentation was not followed correctly, so fix the issues based on the docs page.

Notes relevant to the migration process:

- Do not change any other files or settings outside of the global code migration process (that are not mentioned in the docs page).
- When commenting out code, always add "TODO:" note to make it easier to find later.

## Output format

When done, provide user with this exact output (without any additional text):

```
# Migration Complete
Global code migration from the legacy project to the new one has been successfully completed.

**Next steps:**
- Update your channel configuration to include the port of the local XbyK instance (the one the project launches with).
    Follow these steps: https://docs.kentico.com/guides/architecture/upgrade-from-kx13/upgrade-walkthrough/adjust-global-code#adjust-system-url
- Review the changes to ensure everything looks as expected.
- Continue with the /migrate-page-widgets prompt to migrate Page Builder widgets used by the specified page.
```
