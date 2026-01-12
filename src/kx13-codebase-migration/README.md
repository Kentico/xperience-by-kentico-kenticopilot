# KX13 Project codebase migration

AI-assistant prompts for migrating the **codebase** of Kentico Xperience 13 projects to [Xperience by Kentico](https://docs.kentico.com/x/migrate_from_kx13_guides).

## Scope

These prompts are designed to help migrate the live site and page presentation logic as described in the following guides:

- [Adjust global code](https://docs.kentico.com/guides/architecture/upgrade-from-kx13/upgrade-walkthrough/adjust-global-code) – Generating code files for content types, copying localization resources, shared views, styles/scripts, and enabling content tree-based routing and Page Builder.
- [Display an upgraded page](https://docs.kentico.com/guides/architecture/upgrade-from-kx13/upgrade-walkthrough/display-an-upgraded-page) – Content retrieval services, repositories, view models, views, controllers, and Page Builder sections/widgets.

The following areas are not covered and must be handled manually:

- Custom modules
- Custom tables
- Authentication and user management
- Search functionality
- E-commerce
- Marketing features

See the [Adjust and adapt your code](https://docs.kentico.com/x/migrate_your_code_guides) migration guide for details.

## Prerequisites

- Kentico Xperience 13 project (source).
- Xperience by Kentico project (target) connected to a database migrated using the [Kentico Migration Tool](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool). The prompts were tested on a fresh Xperience by Kentico project created using the `kentico-xperience-mvc` [project template](https://docs.kentico.com/x/DQKQC).
- AI coding assistant installed (for example: GitHub Copilot, Cursor, Claude Code).

## Usage

### 1. Set up project structure

Place your KX13 and XbyK projects in the same workspace with the following structure:

```
KX13/          # Kentico Xperience 13 project files
XbyK/          # Xperience by Kentico project files
```

Start the KX13 project locally. **Do not start the XbyK project** – the agent builds and starts it on-demand during the migration to evaluate progress.

> **Tip:** You can also provide a URL to a live KX13 site.

### 2. Copy the prompts to the workspace

Copy the appropriate files for your AI assistant. Note that the files also add the Xperience by Kentico [Documentation MCP server](https://docs.kentico.com/x/mcp_server_xp) and [Playwright MCP server](https://github.com/microsoft/playwright-mcp) to your workspace.

> **Important:** For Claude Code, you need to add the MCP servers manually via the command line. Follow the [setup instructions](claude-code/MCP_Setup.md).

### 3. Run the migration prompts

The prompts are divided into three main groups:

#### Global
- [**migrate-global-code**](#migrate-global-code) – Sets up the target XbyK project structure, generates code files, and migrates shared code (localization, styles, business logic, etc.).

#### Component
- [**migrate-shared-component**](#migrate-shared-component) – Migrates reusable components (layouts, headers, breadcrumbs, etc.).

#### Page
- [**migrate-page-widgets**](#migrate-page-widgets) – Migrates Page Builder widgets and sections used by the specified page.
- [**migrate-page**](#migrate-page-logic) – Migrates the specified page and related business and presentation logic, including controllers, views, and dependencies.
- [**migrate-page-visual**](#ensure-page-visual-match) – Ensures migrated pages visually match the original KX13 pages. Used in case the page migration prompts result in visual discrepancies.

In a general workflow, you migrate in waves:

1. Global code to seed the target project with initial logic (using the [*migrate-global-code*](#migrate-global-code) prompt).
2. A single page to establish working URLs in the target project.
    1. First, migrate the page's Page Builder dependencies using [*migrate-page-widgets*](#migrate-page-widgets). Skip this step for pages that don't use Page Builder. 
    2. Then, migrate the business and presentation logic using [*migrate-page*](#migrate-page-logic).
3. Shared components to ensure consistent visuals across pages (using the [*migrate-shared-component*](#migrate-shared-component) prompt). Use the URL of the page migrated in step 2 to verify visual accuracy.
4. Remaining pages, repeating step 2 for each.
    - If any migration results in visual issues, use [*migrate-page-visual*](#ensure-page-visual-match).

## Best practices

- Run prompts in sequence. Each prompt builds on the work done in the previous step. For example, the full sequence to migrate a page is: *migrate-page-widgets* → *migrate-page* → *migrate-page-visual*, repeating as necessary until all pages are converted. You can also omit prompts based on the requirements of the page being converted. If a page doesn't use Page Builder features, you can skip the *migrate-page-widgets* prompt.
- Only run the KX13 application before starting. The agent manages the XbyK application lifecycle (building, starting, stopping) automatically.
- Monitor the XbyK project state between prompts. Some prompts (e.g., *migrate-page-widgets*) may leave the XbyK application running. If a subsequent prompt expects the project to be stopped, you may need to stop it manually before proceeding.
- After running a prompt, review all generated code before proceeding to the next step.
- Use the visual matching prompt to fix styling discrepancies.
- Thoroughly test all migrated functionality.

## Prompt reference

### Migrate global code

Prompt name: **migrate-global-code**

Migrates global code, generates code files, and sets up the project foundation. The prompt makes the following changes:

- Creates a new .NET project in the target folder and marks it as [discoverable](https://docs.kentico.com/x/QoXWCQ) by Xperience.
- Uses the code generator utility to [generate classes](https://docs.kentico.com/x/5IbWCQ) for migrated database entities (content types, etc.).
- Copies global project files, such as assets and resources, and global code, such as service registration and project startup logic, to the target.
- Enables [content tree-based routing](https://docs.kentico.com/x/GoXWCQ) and [Page Builder](https://docs.kentico.com/x/6QWiCQ) on the target.

**VS Code GitHub Copilot example:**

```
/migrate-global-code
```

### Migrate shared component

Prompt name: **migrate-shared-component**
Parameters:
  - *componentName*: The name of the shared element to migrate. For example: header, footer, navigation menu, sidebar.
  - *legacyPageUrl*: The URL of the page in the source project

Migrates reusable components like headers, footers, and navigation elements. The prompt locates the specified element in the source project and migrates it together with all dependencies (views, layouts, logic, etc.).

> **Note:** Ensure the KX13 application is running and accessible at the provided URL before running this prompt. Stop the XbyK project if running.

**VS Code GitHub Copilot example:**

```
/migrate-shared-component

componentName: breadcrumbs
legacyPageUrl: https://localhost:5001/en-us/home
```

### Migrate page widgets

Prompt name: **migrate-page-widgets**  
Parameters: 
  - *pageName*: The name in the content tree of the source project
  - *legacyPageUrl*: The URL of the page in the source project

Migrates Page Builder [widgets](https://docs.kentico.com/x/7gWiCQ) and [sections](https://docs.kentico.com/x/9AWiCQ) used by the specified page.

This prompt can be omitted if the specific page doesn't use Page Builder features.

> **Note:** Ensure the KX13 application is running and accessible at the provided URL before running this prompt. Stop the XbyK project if running.

**VS Code GitHub Copilot example:**

```
/migrate-page-widgets

pageName: home
legacyPageUrl: https://localhost:5001/en-us/home
```

### Migrate page logic

Prompt name: **migrate-page**  
Parameters:
  - *pageName*: The name in the content tree of the source project
  - *legacyPageUrl*: The URL of the page in the source project

Migrates the code of individual pages: controllers, views, layouts, and dependencies.

> **Note:** Ensure the KX13 application is running and accessible at the provided URL before running this prompt. Stop the XbyK project if running.

**VS Code GitHub Copilot example:**

```
/migrate-page

pageName: home
legacyPageUrl: https://localhost:5001/en-us/home
```

### Ensure page visual match

Prompt name: **migrate-page-visual**  
Parameters:
  - *pageName*: The name in the content tree of the source project
  - *legacyPageUrl*: The URL of the page in the source project
  - *newPageUrl*: The URL of the page in the target project

Ensures the migrated page visually matches the original KX13 page. Use if the migrate-page prompt doesn't successfully replicate the look and feel. The prompt uses Playwright to identify differences in both pages and aligns the migrated page to match the source.

> **Note:** Ensure the KX13 application is running and accessible at the provided URL before running this prompt. Stop the XbyK project if running.

**VS Code GitHub Copilot example:**

```
/migrate-page-visual

pageName: home
legacyPageUrl: https://localhost:5001/en-us/home
newPageUrl: http://localhost:60444/en-us/home
```

## Prompt customization

These prompt files serve as a baseline for migrating the codebase of KX13 projects to Xperience by Kentico. Modify and enhance the files as required by your implementation, workflow, and requirements. For example, you can update the `instructions/projects-structure.md` file with additional information about the project being migrated.
