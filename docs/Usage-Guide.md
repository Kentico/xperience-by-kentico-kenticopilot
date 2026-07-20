# Usage guide

Use this guide to select and install a KentiCopilot plugin. For task-specific inputs, examples, outputs, and limitations, continue to the selected plugin's README.

## Choose a plugin

Plugins are installed independently. Select and install those suitable for your use cases.

| Plugin | Choose it when you need to... |
|---|---|
| [`kentico-digital-experience`](../plugins/kentico-digital-experience/README.md) | Implement a custom Automation action |
| [`kentico-web-development`](../plugins/kentico-web-development/README.md) | Prepare a project for AI-assisted development, model content, build Page Builder components, retrieve content, or compare a live implementation with a design |
| [`kentico-kx13-migration`](../plugins/kentico-kx13-migration/README.md) | Audit or migrate content and code from Kentico Xperience 13 |
| [`kentico-project-lifecycle`](../plugins/kentico-project-lifecycle/README.md) | Update Xperience or create a scoped CD Repository configuration |

## Check the plugin requirements

You need:

- An agent-plugin-compatible AI coding assistant
- Access to the project the agent will work on
- Git when a skill needs repository history or when you use the manual installation

Some plugins also require MCP servers, command-line tools, SDKs, or a running application. Check the **Requirements** section in the selected plugin README before invoking a skill.

Plugin installation does not configure MCP servers in the current packages. Each plugin that uses MCP links to an `MCP-setup.md` page with the required or recommended workspace configuration.

## Install an AI coding assistant

The plugins are tested with:

- [GitHub Copilot](https://github.com/features/copilot), using VS Code or Copilot CLI
- [Claude Code](https://www.claude.com/product/claude-code)

Skills follow the [Agent Skills specification](https://agentskills.io/specification). Other compatible assistants can use them, but their installation and invocation syntax may differ.

## Install the selected plugin

This repository is an agent plugin marketplace. Add the marketplace once, then install one or more plugin names from the table above.

### VS Code with GitHub Copilot

1. Add the marketplace to VS Code `settings.json`:

   ```json
   "chat.plugins.marketplaces": [
       "Kentico/xperience-by-kentico-kenticopilot"
   ]
   ```

2. Open the Extensions sidebar.
3. Search for `@agentPlugins`.
4. Select **Install** on the plugin you need.

See [Configure plugin marketplaces](https://code.visualstudio.com/docs/copilot/customization/agent-plugins#_configure-plugin-marketplaces) for VS Code details.

### Copilot CLI

```bash
copilot plugin marketplace add Kentico/xperience-by-kentico-kenticopilot
copilot plugin install kentico-web-development@xperience-by-kentico-kenticopilot
```

Replace `kentico-web-development` with another plugin name from the selection table when needed.

### Claude Code

```text
/plugin marketplace add Kentico/xperience-by-kentico-kenticopilot
/plugin install kentico-web-development@xperience-by-kentico-kenticopilot
```

Replace `kentico-web-development` with another plugin name from the selection table when needed.

## Invoke a skill

Open the relevant project or workspace in your assistant and describe the outcome you need. Include concrete context such as project paths, requirements files, design files, URLs, versions, PR numbers, or migration-plan paths.

Skills can be activated in two ways:

- **Explicitly**: invoke the skill by name when your assistant exposes it as a command, such as `/update-xperience 31.2.0`.
- **By task description**: ask for the work naturally. The assistant selects a matching skill from its description, for example `Create a Page Builder widget from requirements.md`.

The plugin README identifies the recommended activation method and provides copyable examples. The skill itself contains the execution instructions; you do not need to open or paste `SKILL.md` into the conversation.

Review generated code, configuration, and reports before using them in a production workflow.

## Manual installation

Use this alternative only when your assistant cannot install from a marketplace or when you need bundled source that is not distributed with the marketplace package.

1. Clone the repository:

   ```bash
   git clone https://github.com/Kentico/xperience-by-kentico-kenticopilot.git
   ```

2. Follow your assistant's plugin-loading convention for the selected folder under `plugins/`.

Do not copy every plugin into a project by default. Keeping only the relevant plugin reduces noise and prevents unrelated skills from activating.

> [!IMPORTANT]
> The KX13 content auditor includes .NET source under the plugin's `src/` directory. Marketplace installation exposes the skill but does not make that source available in your project workspace. Follow the [content auditor setup](../plugins/kentico-kx13-migration/docs/content-auditor.md) when using `migrate-content-audit`.

## Upgrading from Kentico Xperience 13

Start with the [KX13 upgrade workflow](./KX13-Upgrade.md). It places the audit, content-migration, and codebase-migration skills in the correct order alongside the [official upgrade walkthrough](https://docs.kentico.com/x/upgrade_walkthrough_guides).
