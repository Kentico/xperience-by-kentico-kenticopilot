# Xperience by Kentico: KentiCopilot

[![Kentico Labs](https://img.shields.io/badge/Kentico_Labs-grey?labelColor=orange&logo=data:image/svg+xml;base64,PHN2ZyBjbGFzcz0ic3ZnLWljb24iIHN0eWxlPSJ3aWR0aDogMWVtOyBoZWlnaHQ6IDFlbTt2ZXJ0aWNhbC1hbGlnbjogbWlkZGxlO2ZpbGw6IGN1cnJlbnRDb2xvcjtvdmVyZmxvdzogaGlkZGVuOyIgdmlld0JveD0iMCAwIDEwMjQgMTAyNCIgdmVyc2lvbj0iMS4xIiB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciPjxwYXRoIGQ9Ik05NTYuMjg4IDgwNC40OEw2NDAgMjc3LjQ0VjY0aDMyYzE3LjYgMCAzMi0xNC40IDMyLTMycy0xNC40LTMyLTMyLTMyaC0zMjBjLTE3LjYgMC0zMiAxNC40LTMyIDMyczE0LjQgMzIgMzIgMzJIMzg0djIxMy40NEw2Ny43MTIgODA0LjQ4Qy00LjczNiA5MjUuMTg0IDUxLjIgMTAyNCAxOTIgMTAyNGg2NDBjMTQwLjggMCAxOTYuNzM2LTk4Ljc1MiAxMjQuMjg4LTIxOS41MnpNMjQxLjAyNCA2NDBMNDQ4IDI5NS4wNFY2NGgxMjh2MjMxLjA0TDc4Mi45NzYgNjQwSDI0MS4wMjR6IiAgLz48L3N2Zz4=)](https://github.com/Kentico/.github/blob/main/SUPPORT.md#labs-limited-support)

## What is KentiCopilot?

KentiCopilot is an agent plugin marketplace for Xperience by Kentico development. Each plugin packages task-specific skills, reference material, and optional helper tooling that an AI coding assistant loads when relevant.

The plugins are tested with GitHub Copilot and Claude Code. The skills follow the open [Agent Skills specification](https://agentskills.io/specification) and can be adapted to other compatible assistants.

## Choose a plugin

Install only the plugin that matches your task. The plugin README is the source of truth for its requirements, invocation examples, outputs, and limits.

| Plugin | Use it for | Included skills |
|---|---|---|
| [`kentico-digital-experience`](./plugins/kentico-digital-experience/README.md) | Extending Xperience digital-experience features, currently custom Automation actions | `automation-action` |
| [`kentico-web-development`](./plugins/kentico-web-development/README.md) | Preparing a project for agentic development, modeling content, building Page Builder components, retrieving content, and checking an implementation against a design | `agentify`, `design-to-content`, `page-builder-widgets`, `page-builder-structure`, `content-retrieval`, `design-validation` |
| [`kentico-kx13-migration`](./plugins/kentico-kx13-migration/README.md) | Auditing and migrating a Kentico Xperience 13 project, including content and live-site code | `migrate-content-*`, `migrate-code-*` |
| [`kentico-project-lifecycle`](./plugins/kentico-project-lifecycle/README.md) | Updating an Xperience project and configuring scoped Continuous Deployment Repository content | `update-xperience`, `cd-repository-configure` |

Upgrading from Kentico Xperience 13? Start with the [KX13 upgrade workflow](./docs/KX13-Upgrade.md).

## Requirements

- An AI coding assistant with agent plugin support, such as [GitHub Copilot](https://github.com/features/copilot) or [Claude Code](https://www.claude.com/product/claude-code)
- An Xperience project relevant to the selected plugin
- Any plugin-specific tools listed in that plugin's README

## Install as a plugin

This repository is an [agent plugin marketplace](https://code.visualstudio.com/docs/copilot/customization/agent-plugins). Add the marketplace once, then install the plugin you selected.

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

```bash
/plugin marketplace add Kentico/xperience-by-kentico-kenticopilot
/plugin install kentico-web-development@xperience-by-kentico-kenticopilot
```

The commands install `kentico-web-development` as an example; substitute another plugin name from the catalog when needed. For installation alternatives and how skills are activated, see the [Usage guide](./docs/Usage-Guide.md).

## Documentation

| If you want to... | Read |
|---|---|
| Install a plugin and invoke its skills | [Usage guide](./docs/Usage-Guide.md) |
| Choose and run a specific capability | The relevant [plugin README](#choose-a-plugin) |
| Plan a full KX13 upgrade | [KX13 upgrade workflow](./docs/KX13-Upgrade.md) |
| Add or change a plugin or skill | [Contributing setup](./docs/Contributing-Setup.md) |

## Contributing

To see the guidelines for Contributing to Kentico open source software, please see [Kentico's `CONTRIBUTING.md`](https://github.com/Kentico/.github/blob/main/CONTRIBUTING.md) for more information and follow the [Kentico's `CODE_OF_CONDUCT`](https://github.com/Kentico/.github/blob/main/CODE_OF_CONDUCT.md).

Instructions and technical details for contributing to **this** project can be found in [Contributing Setup](./docs/Contributing-Setup.md).

## License

Distributed under the MIT License. See [`LICENSE.md`](./LICENSE.md) for more information.

## Support

[![Kentico Labs](https://img.shields.io/badge/Kentico_Labs-grey?labelColor=orange&logo=data:image/svg+xml;base64,PHN2ZyBjbGFzcz0ic3ZnLWljb24iIHN0eWxlPSJ3aWR0aDogMWVtOyBoZWlnaHQ6IDFlbTt2ZXJ0aWNhbC1hbGlnbjogbWlkZGxlO2ZpbGw6IGN1cnJlbnRDb2xvcjtvdmVyZmxvdzogaGlkZGVuOyIgdmlld0JveD0iMCAwIDEwMjQgMTAyNCIgdmVyc2lvbj0iMS4xIiB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciPjxwYXRoIGQ9Ik05NTYuMjg4IDgwNC40OEw2NDAgMjc3LjQ0VjY0aDMyYzE3LjYgMCAzMi0xNC40IDMyLTMycy0xNC40LTMyLTMyLTMyaC0zMjBjLTE3LjYgMC0zMiAxNC40LTMyIDMyczE0LjQgMzIgMzIgMzJIMzg0djIxMy40NEw2Ny43MTIgODA0LjQ4Qy00LjczNiA5MjUuMTg0IDUxLjIgMTAyNCAxOTIgMTAyNGg2NDBjMTQwLjggMCAxOTYuNzM2LTk4Ljc1MiAxMjQuMjg4LTIxOS41MnpNMjQxLjAyNCA2NDBMNDQ4IDI5NS4wNFY2NGgxMjh2MjMxLjA0TDc4Mi45NzYgNjQwSDI0MS4wMjR6IiAgLz48L3N2Zz4=)](https://github.com/Kentico/.github/blob/main/SUPPORT.md#labs-limited-support)

This project has **Kentico Labs limited support**.

See [`SUPPORT.md`](https://github.com/Kentico/.github/blob/main/SUPPORT.md#full-support) for more information.

For any security issues see [`SECURITY.md`](https://github.com/Kentico/.github/blob/main/SECURITY.md).
