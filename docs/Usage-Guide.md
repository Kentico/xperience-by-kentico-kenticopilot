# Usage guide

Use this guide to select and install a KentiCopilot plugin. For task-specific inputs, examples, outputs, and limitations, continue to the selected plugin's README.

## Choose a plugin

Plugins are installed independently. Select and install those suitable for your use cases.

> [!NOTE]
> **What is a plugin**
>
> A plugin is a self-contained directory that bundles skills, agents, hooks, and MCP server definitions into one unit your assistant installs and runs as a single step. The alternative is configuring each of those pieces by hand. For more details, see the [open plugin](https://open-plugins.com/) documentation.

| Plugin | What it does |
|---|---|
| [`kentico-digital-experience`](../plugins/kentico-digital-experience/README.md) | Extend Xperience digital experience workflows with custom components, such as Automation actions that add custom step types to the Automation Builder |
| [`kentico-web-development`](../plugins/kentico-web-development/README.md) | Build Xperience websites with AI assistance. Model content from designs, build Page Builder widgets and templates, write content retrieval code, and validate the result against the original design |
| [`kentico-kx13-migration`](../plugins/kentico-kx13-migration/README.md) | Migrate Kentico Xperience 13 projects to Xperience by Kentico by auditing the source content model, driving the Migration Tool, and porting live-site code |
| [`kentico-project-lifecycle`](../plugins/kentico-project-lifecycle/README.md) | Manage Xperience by Kentico project lifecycle. Update Xperience projects to a target version and generate deployment-scoped CI/CD repository.config filters from selected pull requests or commits |

## Check the usage requirements

You need:

- An AI coding assistant that supports agent plugins, tested here with [GitHub Copilot](https://github.com/features/copilot) using VS Code or Copilot CLI, and with [Claude Code](https://www.claude.com/product/claude-code)
- Access to the project the agent will work on
- Git when a skill needs repository history or when you use the manual installation

Skills follow the [Agent Skills specification](https://agentskills.io/specification). Other compatible assistants can use them, but their installation and invocation syntax may differ.

Some plugins also require MCP servers, command-line tools, SDKs, or a running application. Check the **Requirements** section in the selected plugin README before invoking a skill.

Plugin installation does not configure MCP servers in the current packages. Each plugin that uses MCP links to an `MCP-setup.md` page with the required or recommended workspace configuration.

## Install the selected plugin

This repository is an agent plugin marketplace. Add the marketplace to your coding assistant, then install the plugins using names from [Choose a plugin](#choose-a-plugin).

> [!NOTE]
> **What is a plugin marketplace**
>
> A marketplace is a catalog of plugins for coding assistants, usually hosted in a git repository. Each marketplace publishes an index of the plugins it offers and where the files for each one sit. You add a marketplace once, then install the plugins you want from it by name. See the [marketplace specification](https://open-plugins.com/plugin-builders/marketplace) for the format.

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

Replace `kentico-web-development` with another plugin name from [Choose a plugin](#choose-a-plugin).

### Claude Code

```text
/plugin marketplace add Kentico/xperience-by-kentico-kenticopilot
/plugin install kentico-web-development@xperience-by-kentico-kenticopilot
```

Replace `kentico-web-development` with another plugin name from [Choose a plugin](#choose-a-plugin).

### Install individual skills with the skills CLI

You can also use the [skills CLI](https://github.com/vercel-labs/skills) to install only individual skills.

```bash
npx skills add Kentico/xperience-by-kentico-kenticopilot --skill automation-action --agent claude-code github-copilot cursor
```

> [!NOTE]
> **Skills CLI limitations**
>
> The CLI copies skill files and leaves a skills-lock.json tracking its installed version. No other plugin files outside the targeted skill folders are included.

### Manual installation

Use this alternative only when your assistant cannot install from a marketplace.

1. Clone the repository:

   ```bash
   git clone https://github.com/Kentico/xperience-by-kentico-kenticopilot.git
   ```

2. Follow your assistant's plugin or skill-loading conventions for the selected folder under `plugins/`.

Do not copy every plugin into a project by default. Keeping only the relevant plugins reduces noise and prevents unrelated skills from burdening agent context.

## Invoke a skill

A skill is a set of instructions your assistant loads when a request matches it. Nothing needs configuring per task, and you don't need to know a skill exists to benefit from one.

Open the relevant project or workspace in your assistant and describe the outcome you need. Include concrete context such as project paths, requirements files, design files, URLs, versions, PR numbers, or migration-plan paths.

Skills can be activated in two ways:

- **Explicitly**: invoke the skill by name when your assistant exposes it as a command, such as `/update-xperience 31.2.0`.
- **By task description**: ask for the work naturally. The assistant selects a matching skill from its description, for example `Create a Page Builder widget from requirements.md`.

The plugin README provides copyable examples for each of its skills. The skill itself contains the execution instructions; you do not need to open or paste `SKILL.md` into the conversation.

Review generated code, configuration, and reports before using them in a production workflow.

## Write specific prompts

A skill covers the procedure for a task, but it knows nothing about your project. Which existing component to follow, where the design file lives, and which parts of the result matter to you are things only you can tell the assistant. The prompts below show a few examples using the skills from this repository.

| Instead of | Write |
|---|---|
| `Create a widget` | `Create a widget matching ./designs/hero.png, following the conventions of the existing widgets in ./Components/Widgets` |
| `Model the content` | `Model the article listing from ./designs/news.fig, reusing the existing Article content type instead of creating a second one` |
| `Migrate the site` | `Migrate the KX13 instance at ./legacy using the plan in ./migration-plan.md, content types first` |

Point the assistant at your sources, design documents, implementations, and other supplementary materials. Specific examples beat a well written prompt.

Here are several habits, sourced from accepted and reviewed research, that reliably lead to poor outcomes when working with coding assistants:

- **Asking for several unrelated things in one prompt.** The assistant works on them together, and the result becomes harder to review. Ask for one outcome, review it, and continue from there.
- **Describing what you don't want.** *Don't use inline styles* leaves the assistant to guess the alternative. Describe the desired output instead and include examples if possible, as in *use the SCSS variables in ./Assets/styles*.
- **Assuming shared context.** The assistant doesn't know which command builds your project, where your site runs, or which of two similar components is the current one. Say so in the prompt, or record it in your project's agent instructions.
- **Approving a plan you only skimmed.** Corrections are cheapest before the assistant writes any code. Read the design the assistant proposes and change it there.

Each plugin README includes prompt examples for its own skills.

> [!TIP]
> How you set up the task strongly influences its result. See [Work effectively with KentiCopilot](https://docs.kentico.com/x/work_effectively_kenticopilot_guides) for the habits that get the most out of the plugins.
