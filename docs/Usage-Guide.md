# Usage guide

Use this guide to select and install a KentiCopilot plugin. For task-specific inputs, examples, outputs, and limitations, continue to the selected plugin's README.

## Install an AI coding assistant

The plugins are tested with:

- [GitHub Copilot](https://github.com/features/copilot), using VS Code or Copilot CLI
- [Claude Code](https://www.claude.com/product/claude-code)

Skills follow the [Agent Skills specification](https://agentskills.io/specification). Other compatible assistants can use them, but their installation and invocation syntax may differ.

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

## Install the selected plugin

This repository is an agent plugin marketplace. Add the marketplace once, then install one or more plugin names from the table above.

> [!NOTE]
> **What a plugin marketplace is**
>
> A marketplace is a catalog of plugins that lives in a git repository. It names each plugin it offers and where the files for it sit, which is why adding the marketplace by itself installs nothing. You add the catalog once, install the plugins you want from it by name, and your assistant copies each one into its own plugin directory outside your project. Later releases reach you through the same catalog, so you update an installed plugin instead of tracking the repository it came from.

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

A skill is a set of instructions your assistant loads when a request matches it. Nothing needs configuring per task, and you don't need to know a skill exists to benefit from one.

Open the relevant project or workspace in your assistant and describe the outcome you need. Include concrete context such as project paths, requirements files, design files, URLs, versions, PR numbers, or migration-plan paths.

Skills can be activated in two ways:

- **Explicitly**: invoke the skill by name when your assistant exposes it as a command, such as `/update-xperience 31.2.0`.
- **By task description**: ask for the work naturally. The assistant selects a matching skill from its description, for example `Create a Page Builder widget from requirements.md`.

The plugin README identifies the recommended activation method and provides copyable examples. The skill itself contains the execution instructions; you do not need to open or paste `SKILL.md` into the conversation.

Review generated code, configuration, and reports before using them in a production workflow.

## Write specific prompts

A skill covers the procedure for a task, but it knows nothing about your project. Which existing component to follow, where the design file lives, and which parts of the result matter to you are things only you can tell the assistant. [Work effectively with KentiCopilot](https://docs.kentico.com/x/work_effectively_kenticopilot_guides) explains how much difference this makes. The prompts below apply it to the skills in this repository.

| Instead of | Write |
|---|---|
| `Create a widget` | `Create a widget matching ./designs/hero.png, following the conventions of the existing widgets in ./Components/Widgets` |
| `Model the content` | `Model the article listing from ./designs/news.fig, reusing the existing Article content type instead of creating a second one` |
| `Migrate the site` | `Migrate the KX13 instance at ./legacy using the plan in ./migration-plan.md, content types first` |

Point the assistant at your sources instead of describing them. When you name a requirements document, a design export, or an existing implementation, the assistant reads the real thing rather than your summary of it.

Here are several habits, sourced from accepted and reviewed research, that reliably lead to poor outcomes when working with coding assistants:

- **Asking for several unrelated things in one prompt.** The assistant works on them together, and the result becomes harder to review. Ask for one outcome, review it, and continue from there.
- **Describing what you don't want.** *Don't use inline styles* leaves the assistant to guess the alternative. Name the target instead, as in *use the SCSS variables in ./Assets/styles*.
- **Assuming shared context.** The assistant doesn't know which command builds your project, where your site runs, or which of two similar components is the current one. Say so in the prompt, or record it in your project's agent instructions.
- **Approving a plan you only skimmed.** Corrections are cheapest before the assistant writes any code. Read the design the assistant proposes and change it there.

Each plugin README includes prompt examples for its own skills.

> [!TIP]
> How you set up the work matters more than how you word any single prompt. See [Work effectively with KentiCopilot](https://docs.kentico.com/x/work_effectively_kenticopilot_guides) for the habits that get the most out of the plugins.

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
