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

### Web development

> **Location:** [plugins/kentico-web-development/](./plugins/kentico-web-development/)

Skills and references for building Xperience by Kentico websites. Includes content modeling guidance for translating designs into a content model, and a two-stage workflow for building [Page Builder](https://docs.kentico.com/x/6QWiCQ) widgets: the AI researches your requirements against your project structure and the Xperience documentation, then generates the full widget implementation (view component, properties, Razor view, view model, localization). Full instructions are available in the [README](./plugins/kentico-web-development/README.md).

| Skill                          | Description                                                                                          |
| ------------------------------ | ---------------------------------------------------------------------------------------------------- |
| `agentify`                     | Audits an XbyK project for agentic-development readiness, reports gaps, and applies fixes on request |
| `design-to-content`            | Guides content modeling — translating designs/wireframes into an Xperience content model             |
| `widget-create-research`       | Analyzes requirements and design files, generates implementation instructions                        |
| `widget-create-implementation` | Creates widget code following the generated instructions and project conventions                     |

### KX13 → Xperience by Kentico migration

> **Location:** [plugins/kentico-kx13-migration/](./plugins/kentico-kx13-migration/)

The complete toolkit for upgrading a Kentico Xperience 13 project to Xperience by Kentico — content-model auditing, database **content** migration (driving the [Kentico Migration Tool](https://github.com/Kentico/xperience-by-kentico-kentico-migration-tool)), and live-site **codebase** migration. Full instructions are available in the [README](./plugins/kentico-kx13-migration/README.md). See also [KX13 upgrade plugins](./docs/KX13-Upgrade-Plugins.md) for the end-to-end path.

| Skill                         | Description                                                                                                                                                     |
| ----------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `migrate-content-audit`       | Runs a bundled .NET 8 CLI that reads a KX13 database and exports the content model as JSON + a Markdown report (the canonical input for `migrate-content-plan`) |
| `migrate-content-plan`        | Produces a Migration Overview and Migration Detail document from the source content model                                                                       |
| `migrate-content-appsettings` | Generates the Migration Tool's `appsettings.json`                                                                                                               |
| `migrate-content-classes`     | Generates `IClassMapping` / `ReusableSchemaBuilder` C# extensions                                                                                               |
| `migrate-content-fields`      | Generates `IFieldMigration` C# extensions for field value and definition transforms                                                                             |
| `migrate-content-widgets`     | Generates `IWidgetMigration` / `IWidgetPropertyMigration` C# extensions                                                                                         |
| `migrate-content-items`       | Generates `ContentItemDirectorBase` C# for linked pages, child references, page-to-widget conversions                                                           |
| `migrate-content-run`         | Executes a single combined `migrate` CLI invocation with all required flags (the tool orders them internally), monitors output, applies fixes                   |
| `migrate-content-eval`        | Evaluates the migrated XbyK database against the plan and produces an HTML report                                                                               |
| `migrate-code-global`         | Sets up the Xperience by Kentico project foundation (code generation, localization, routing, Page Builder)                                                      |
| `migrate-code-page`           | Migrates a page's controller, views, repositories, and dependencies                                                                                             |
| `migrate-code-page-widgets`   | Migrates Page Builder widgets and sections for a specified page                                                                                                 |
| `migrate-code-component`      | Migrates reusable components (header, footer, etc.) with dependencies                                                                                           |
| `migrate-code-page-visual`    | Compares old and new pages visually with Playwright, fixes discrepancies                                                                                        |

### Project lifecycle

> **Location:** [plugins/kentico-project-lifecycle/](./plugins/kentico-project-lifecycle/)

Skills for managing the lifecycle of an Xperience by Kentico solution. The plugin updates projects to newer Xperience versions by following the official release notes and update documentation, and builds scoped [Continuous Deployment Repository](https://docs.kentico.com/x/continuous_deployment) filters from CI Repository changes: the AI discovers your project layout and tooling, then inspects changed CI Repository files from specified PRs or commit ranges and writes a minimal `IncludedObjectTypes` / `ObjectFilters` allowlist — automatically excluding noise from Xperience version updates. Full instructions are available in the [README](./plugins/kentico-project-lifecycle/README.md).

| Skill                     | Description                                                                                          |
| ------------------------- | ---------------------------------------------------------------------------------------------------- |
| `update-xperience`        | Updates the project to a newer Xperience version, driven by the release notes and official docs      |
| `cd-repository-discovery` | Locates the Xperience app, CI/CD repository paths, and git tooling; saves context to a reusable file |
| `cd-repository-configure` | Reads the context file and PR/commit changes, then writes a scoped `repository.config`               |
| `cd-repository-upgrade`   | Migrates a `repository.config` from v1 to v2 syntax                                                  |

## Upgrading from Kentico Xperience 13?

If you are upgrading a KX13 project to Xperience by Kentico, see [KX13 upgrade plugins](./docs/KX13-Upgrade-Plugins.md) for the recommended end-to-end path and where each plugin slots into the [official upgrade walkthrough](https://docs.kentico.com/x/upgrade_walkthrough_guides).

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
copilot plugin install kentico-web-development@xperience-by-kentico-kenticopilot
copilot plugin install kentico-kx13-migration@xperience-by-kentico-kenticopilot
copilot plugin install kentico-project-lifecycle@xperience-by-kentico-kenticopilot
```

### Claude Code

```bash
/plugin marketplace add Kentico/xperience-by-kentico-kenticopilot
/plugin install kentico-web-development@xperience-by-kentico-kenticopilot
/plugin install kentico-kx13-migration@xperience-by-kentico-kenticopilot
/plugin install kentico-project-lifecycle@xperience-by-kentico-kenticopilot
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
