# Xperience by Kentico: KentiCopilot

[![Kentico support](https://img.shields.io/badge/Kentico-Supported-0078D4)](#support)

## What is KentiCopilot?

KentiCopilot is an agent plugin marketplace for Xperience by Kentico development. Each plugin packages task-specific skills, reference material, and optional helper tooling that an AI coding assistant loads when relevant.

The plugins are tested with GitHub Copilot and Claude Code. The skills follow the open [Agent Skills specification](https://agentskills.io/specification) and can be adapted to other compatible assistants.

## Choose a plugin

This repository contains several plugins, each addressing a specific product area or scenario. See each plugin's README for its requirements, invocation examples, outputs, and limitations.

> [!TIP]
> Upgrading from Kentico Xperience 13? Start with the [kentico-kx13-migration](./plugins/kentico-kx13-migration/README.md) plugin.

| Plugin | Use case |
|---|---|
| [`kentico-web-development`](./plugins/kentico-web-development/README.md) | Preparing a project for agentic development, modeling content, building Page Builder components, retrieving content, and checking an implementation against a design |
| [`kentico-digital-experience`](./plugins/kentico-digital-experience/README.md) | Extending digital experience features for Xperience by Kentico |
| [`kentico-kx13-migration`](./plugins/kentico-kx13-migration/README.md) | Auditing and migrating a Kentico Xperience 13 project, including content and live-site code |
| [`kentico-project-lifecycle`](./plugins/kentico-project-lifecycle/README.md) | Updating an Xperience project and configuring scoped Continuous Deployment Repository content |

## Requirements

- An AI coding assistant with agent plugin support, such as [GitHub Copilot](https://github.com/features/copilot) or [Claude Code](https://www.claude.com/product/claude-code)
- An Xperience project relevant to the selected plugin
- Any plugin-specific tools listed in that plugin's README

## Install as a plugin

This repository is an [agent plugin marketplace](https://code.visualstudio.com/docs/copilot/customization/agent-plugins). Add the marketplace once, then install the plugin you selected.

> [!TIP]
> 
> For installation alternatives, including installing a single skill without the marketplace, see the [Usage guide](./docs/Usage-Guide.md).

### VS Code (GitHub Copilot)

1. Add the marketplace to your VS Code settings (`settings.json`):

   ```json
   "chat.plugins.marketplaces": [
       "Kentico/xperience-by-kentico-kenticopilot"
   ]
   ```

2. Open the Extensions sidebar and search `@agentPlugins` to browse and install available plugins.

### Copilot CLI

```bash
copilot plugin marketplace add Kentico/xperience-by-kentico-kenticopilot
copilot plugin install kentico-web-development@xperience-by-kentico-kenticopilot
```

### Claude Code

```text
/plugin marketplace add Kentico/xperience-by-kentico-kenticopilot
/plugin install kentico-web-development@xperience-by-kentico-kenticopilot
```

The commands install `kentico-web-development` as an example; substitute another plugin name from the catalog when needed.

## Documentation

| If you want to... | Read |
|---|---|
| Install a plugin and invoke its skills | [Usage guide](./docs/Usage-Guide.md) |
| Choose and run a specific capability | The relevant [plugin README](#choose-a-plugin) |

## License

Distributed under the MIT License. See [`LICENSE.md`](./LICENSE.md) for more information.

## Support

[![Kentico support](https://img.shields.io/badge/Kentico-Supported-0078D4)](#support)

Kentico maintains this repository and responds to issues raised here. Fixes and updates are delivered on a best-effort basis.

For any security issues see [`SECURITY.md`](https://github.com/Kentico/.github/blob/main/SECURITY.md).
