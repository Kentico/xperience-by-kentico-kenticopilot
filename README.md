# Xperience by Kentico: KentiCopilot

[![Kentico Labs](https://img.shields.io/badge/Kentico_Labs-grey?labelColor=orange&logo=data:image/svg+xml;base64,PHN2ZyBjbGFzcz0ic3ZnLWljb24iIHN0eWxlPSJ3aWR0aDogMWVtOyBoZWlnaHQ6IDFlbTt2ZXJ0aWNhbC1hbGlnbjogbWlkZGxlO2ZpbGw6IGN1cnJlbnRDb2xvcjtvdmVyZmxvdzogaGlkZGVuOyIgdmlld0JveD0iMCAwIDEwMjQgMTAyNCIgdmVyc2lvbj0iMS4xIiB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciPjxwYXRoIGQ9Ik05NTYuMjg4IDgwNC40OEw2NDAgMjc3LjQ0VjY0aDMyYzE3LjYgMCAzMi0xNC40IDMyLTMycy0xNC40LTMyLTMyLTMyaC0zMjBjLTE3LjYgMC0zMiAxNC40LTMyIDMyczE0LjQgMzIgMzIgMzJIMzg0djIxMy40NEw2Ny43MTIgODA0LjQ4Qy00LjczNiA5MjUuMTg0IDUxLjIgMTAyNCAxOTIgMTAyNGg2NDBjMTQwLjggMCAxOTYuNzM2LTk4Ljc1MiAxMjQuMjg4LTIxOS41MnpNMjQxLjAyNCA2NDBMNDQ4IDI5NS4wNFY2NGgxMjh2MjMxLjA0TDc4Mi45NzYgNjQwSDI0MS4wMjR6IiAgLz48L3N2Zz4=)](https://github.com/Kentico/.github/blob/main/SUPPORT.md#labs-limited-support)

## Description

AI agent prompts and instructions for Xperience by Kentico development. This repository provides pre-configured prompts for common development tasks, helping developers accelerate their workflow with AI coding assistants.

This repository contains prompts for the following solutions:

- GitHub Copilot
- Cursor
- Claude Code

Prompts are transferrable to other solutions. Follow the conventions of your specific assistant.


## Available prompts

Example prompts for the following scenarios are available for your AI coding assistants. See their respective README files for full details on how to use the prompts in different solutions.

### Widget creation

> **Location:** [src/widget-creation/](./src/widget-creation/)

Two-stage workflow for building [Page Builder](https://docs.kentico.com/x/6QWiCQ) widgets. The AI first researches your requirements against Xperience docs, then generates the full widget implementation (view component, properties, Razor view, view model, localization). Full instructions are available in the [README](./src/widget-creation/README.md).

| Prompt | Description |
|---|---|
| `widget-create-research` | Analyzes requirements and design files, generates implementation instructions |
| `widget-create-implementation` | Creates widget code following the generated instructions and project conventions |

### KX13 codebase migration

> **Location:** [src/kx13-codebase-migration/](./src/kx13-codebase-migration/)

AI-assisted migration of Kentico Xperience 13 live-site code (pages, widgets, shared components) to Xperience by Kentico. Full instructions are available in the [README](./src/kx13-codebase-migration/README.md).

| Prompt | Description |
|---|---|
| `migrate-global-code` | Sets up XbyK project foundation (code generation, localization, routing, Page Builder) |
| `migrate-page` | Migrates a page's controller, views, repositories, and dependencies |
| `migrate-page-widgets` | Migrates Page Builder widgets and sections for a specified page |
| `migrate-shared-component` | Migrates reusable components (header, footer, etc.) with dependencies |
| `migrate-page-visual` | Compares old and new pages visually with Playwright, fixes discrepancies |

## Requirements

- [Xperience by Kentico](https://docs.kentico.com) 30.6.0 or newer
- An AI coding assistant, for example:
  - [GitHub Copilot](https://github.com/features/copilot)
  - [Cursor](https://cursor.sh/)
  - [Claude Code](https://www.claude.com/product/claude-code)

## Setup

1. Clone this repository:

    ```bash
    git clone https://github.com/Kentico/xperience-by-kentico-kenticopilot.git
    ```

1. Copy the files for your AI assistant to your Xperience project:

    ```bash
    # For GitHub Copilot
    cp -r src/widget-creation/gh-copilot/* YOUR_PROJECT/

    # For Cursor
    cp -r src/widget-creation/cursor/* YOUR_PROJECT/

    # For Claude Code
    cp -r src/widget-creation/claude-code/* YOUR_PROJECT/
    ```

    - Prompts are transferable to other solutions. Follow the conventions of your specific assistant.

1. Follow the use case README in `src/` for specific instructions.

## Full Instructions

View the [Usage Guide](./docs/Usage-Guide.md) for more detailed instructions.

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
