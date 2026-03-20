# Xperience by Kentico: KentiCopilot

[![Kentico Labs](https://img.shields.io/badge/Kentico_Labs-grey?labelColor=orange&logo=data:image/svg+xml;base64,PHN2ZyBjbGFzcz0ic3ZnLWljb24iIHN0eWxlPSJ3aWR0aDogMWVtOyBoZWlnaHQ6IDFlbTt2ZXJ0aWNhbC1hbGlnbjogbWlkZGxlO2ZpbGw6IGN1cnJlbnRDb2xvcjtvdmVyZmxvdzogaGlkZGVuOyIgdmlld0JveD0iMCAwIDEwMjQgMTAyNCIgdmVyc2lvbj0iMS4xIiB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciPjxwYXRoIGQ9Ik05NTYuMjg4IDgwNC40OEw2NDAgMjc3LjQ0VjY0aDMyYzE3LjYgMCAzMi0xNC40IDMyLTMycy0xNC40LTMyLTMyLTMyaC0zMjBjLTE3LjYgMC0zMiAxNC40LTMyIDMyczE0LjQgMzIgMzIgMzJIMzg0djIxMy40NEw2Ny43MTIgODA0LjQ4Qy00LjczNiA5MjUuMTg0IDUxLjIgMTAyNCAxOTIgMTAyNGg2NDBjMTQwLjggMCAxOTYuNzM2LTk4Ljc1MiAxMjQuMjg4LTIxOS41MnpNMjQxLjAyNCA2NDBMNDQ4IDI5NS4wNFY2NGgxMjh2MjMxLjA0TDc4Mi45NzYgNjQwSDI0MS4wMjR6IiAgLz48L3N2Zz4=)](https://github.com/Kentico/.github/blob/main/SUPPORT.md#labs-limited-support)

## Description

AI agent prompts and instructions for Xperience by Kentico development. This repository provides pre-configured prompts for common development tasks, helping developers accelerate their workflow with AI coding assistants.

This repository contains plugins (skills, instructions, MCP server configuration) tested for the following AI coding assistants:

- GitHub Copilot
- Claude Code

Skills are transferable to other solutions. Follow the conventions of your specific assistant.


## Available plugins

This repository provides plugins, each containing a set of skills for AI coding assistants. See the plugin README files for full details.

### Widget creation

> **Location:** [plugins/widget-creation/](./plugins/widget-creation/)

Two-stage workflow for building [Page Builder](https://docs.kentico.com/x/6QWiCQ) widgets. The AI first researches your requirements against your project structure and the Xperience documentation, then generates the full widget implementation (view component, properties, Razor view, view model, localization). Full instructions are available in the [README](./plugins/widget-creation/README.md).

| Skill | Description |
|---|---|
| `widget-create-research` | Analyzes requirements and design files, generates implementation instructions |
| `widget-create-implementation` | Creates widget code following the generated instructions and project conventions |

### KX13 codebase migration

> **Location:** [plugins/kx13-codebase-migration/](./plugins/kx13-codebase-migration/)

AI-assisted migration of Kentico Xperience 13 live-site code (pages, widgets, shared components) to Xperience by Kentico. Full instructions are available in the [README](./plugins/kx13-codebase-migration/README.md).

| Skill | Description |
|---|---|
| `migrate-global-code` | Sets up the Xperience by Kentico project foundation (code generation, localization, routing, Page Builder) |
| `migrate-page` | Migrates a page's controller, views, repositories, and dependencies |
| `migrate-page-widgets` | Migrates Page Builder widgets and sections for a specified page |
| `migrate-shared-component` | Migrates reusable components (header, footer, etc.) with dependencies |
| `migrate-page-visual` | Compares old and new pages visually with Playwright, fixes discrepancies |

## Requirements

- [Xperience by Kentico](https://docs.kentico.com) 30.6.0 or newer
- An AI coding assistant, for example:
  - [GitHub Copilot](https://github.com/features/copilot)
  - [Claude Code](https://www.claude.com/product/claude-code)

## Install as a plugin

This repository is an [agent plugin marketplace](https://code.visualstudio.com/docs/copilot/customization/agent-plugins). Install plugins directly from the marketplace — no need to clone the repository or copy files manually.

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
copilot plugin install widget-creation@xperience-by-kentico-kenticopilot
copilot plugin install kx13-codebase-migration@xperience-by-kentico-kenticopilot
```

### Claude Code

```bash
/plugin marketplace add Kentico/xperience-by-kentico-kenticopilot
/plugin install widget-creation@xperience-by-kentico-kenticopilot
/plugin install kx13-codebase-migration@xperience-by-kentico-kenticopilot
```

For more details, see the [Usage Guide](./docs/Usage-Guide.md).

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
