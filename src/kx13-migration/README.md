# KX13 Project codebase migration

AI-assistant prompts for migrating the **codebase** of Kentico Xperience 13 projects to [Xperience by Kentico](https://docs.kentico.com/x/migrate_from_kx13_guides).

## Prerequisites

- Kentico Xperience 13 project (source)
- Xperience by Kentico project (target) with database migrated using the [Kentico Migration Tool](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool)
- AI coding assistant installed (for example: GitHub Copilot, Cursor, Claude Code)

## Workflow

These prompts provide step-by-step AI assistance for migrating an existing Kentico Xperience 13 project codebase to Xperience by Kentico:

1. **Global code migration** - Sets up the new project structure, generates code files, and migrates shared code (localization, styles, services).
2. **Page widgets migration** - Migrates Page Builder widgets and sections to the new project.
3. **Page migration** - Migrates individual pages including controllers, views, and components.
4. **Visual matching** - Ensures migrated pages visually match the original KX13 pages.
5. **Shared component migration** - Migrates reusable components (layouts, headers, breadcrumbs, etc.).

## Usage

### 1. Set up project structure

Place your KX13 and XbyK projects in the same workspace with the following structure:

```
KX13/          # Kentico Xperience 13 project files
XbyK/          # Xperience by Kentico project files
```

Start both projects locally. During the migration, the agent actively visits URLs in both projects to evaluate progress.

### 2. Copy the prompts to the workspace

Copy the appropriate files for your AI assistant. Note that the files also add the Xperience by Kentico [Documentation MCP server](https://docs.kentico.com/x/mcp_server_xp) and [Playwright MCP server](https://github.com/microsoft/playwright-mcp) to your workspace.

> **Important:** For Claude Code, you need to add the servers manually via the command line. Follow the [setup instructions](claude-code/MCP_Setup.md).

### 3. Run the migration prompts

Execute the prompts in sequence. Each prompt builds on the work of the previous step.

#### Step 1: Migrate global code

Prompt name: **migrate-global-code**

Migrates global code, generates code files, and sets up the project foundation. The prompt makes the following changes:

- Creates a new .NET project in the target folder and marks it as [discoverable](https://docs.kentico.com/x/QoXWCQ) by Xperience.
- Uses the code generator utility to [generate classes]((https://docs.kentico.com/x/5IbWCQ)) for migrated database entities (content types, etc.).
- Copies global projects files, such as assets and resources, and global code, such as service registration and project startup logic, to the target.
- Enables [content tree-based routing](https://docs.kentico.com/x/GoXWCQ) and [Page Builder](https://docs.kentico.com/x/6QWiCQ) on the target.

**VS Code GitHub Copilot example:**

```
/migrate-global-code
```

#### Step 2: Migrate page widgets

Prompt name: **migrate-page-widgets**  
Parameters: 
  - *pageName*: The name in the content tree of the source project
  - *legacyPageUrl*: The URL of the page in the source project

Migrates Page Builder [widgets](https://docs.kentico.com/x/7gWiCQ) and [sections](https://docs.kentico.com/x/9AWiCQ) for a specific page to the target.

**VS Code GitHub Copilot example:**

```
/migrate-page-widgets

pageName: home
legacyPageUrl: https://localhost:5001/en-us/home
```

#### Step 3: Migrate page logic

Prompt name: **migrate-page**  
Parameters:
  - *pageName*: The name in the content tree of the source project
  - *legacyPageUrl*: The URL of the page in the source project

Migrates the code of individual pages: controllers, views, layouts, and dependencies.

**VS Code GitHub Copilot example:**

```
/migrate-page

pageName: home
legacyPageUrl: https://localhost:5001/en-us/home
```

#### Step 4: Visual matching (optional)

Prompt name: **migrate-page-visual**  
Parameters:
  - *pageName*: The name in the content tree of the source project
  - *legacyPageUrl*: The URL of the page in the source project
  - *newPageUrl*: The URL of the page in the target project

Ensures the migrated page visually matches the original KX13 page. Use if the migrate-page prompt doesn't successfully replicate the look and feel. The prompt uses Playwright to identify differences in both pages and aligns the migrated page to match the source.

**VS Code GitHub Copilot example:**

```
/migrate-page-visual

pageName: home
legacyPageUrl: https://localhost:5001/en-us/home
newPageUrl: http://localhost:60444/en-us/home
```

#### Step 5: Migrate shared components

Prompt name: **migrate-shared-component**
Parameters:
  - *componentName*: The name of the shared element to migrate. For example: header, footer, navigation menu, sidebar.
  - *legacyPageUrl*: The URL of the page in the source project

Migrates reusable components like headers, footers, and navigation elements. The prompt locates the specified element in the source project and migrates it together with all dependencies (views, layouts, logic, etc.).

**VS Code GitHub Copilot example:**

```
/migrate-shared-component

componentName: breadcrumbs
legacyPageUrl: https://localhost:5001/en-us/home
```

## Included files

### Prompts/Commands

| Prompt | Description | Parameters |
|--------|-------------|------------|
| `migrate-global-code` | Migrates global code and sets up project foundation | None |
| `migrate-page-widgets` | Migrates Page Builder widgets and sections | `pageName`, `legacyPageUrl` |
| `migrate-page` | Migrates a complete page with all dependencies | `pageName`, `legacyPageUrl` |
| `migrate-page-visual` | Ensures visual parity between KX13 and XbyK pages | `pageName`, `legacyPageUrl`, `newPageUrl` |
| `migrate-shared-component` | Migrates shared/reusable components | `componentName`, `legacyPageUrl` |

### Instructions

These files provide context to the AI about your project setup:

- `projects-structure.md` - Describes the workspace folder structure (KX13 and XbyK locations).

## Best practices for usage

- Run prompts in sequence (global code → widgets → pages → visual matching). Each prompt builds on the work done in the previous step.
- Have both KX13 and XbyK applications running -- the agent visits both applications to compare migration progress.
- Review generated code before proceeding to the next step.
- Use the visual matching prompt to fix styling discrepancies.
- Thoroughly test all migrated functionality.

## Prompt customization

These prompt files serve as a baseline for migrating the codebase of KX13 projects to Xperience by Kentico. Modify and enhance the files as required by your implementation, workflow, and requirements. For example, you can update the `instructions/projects-structure.md` file with additional context about the project being migrated.
