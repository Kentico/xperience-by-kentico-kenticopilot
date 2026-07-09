# Kentico project lifecycle

Skills for managing the lifecycle of an Xperience by Kentico solution. The plugin currently covers two areas:

- **Updating your Xperience project** — the `update-xperience` skill updates a project to a newer Xperience by Kentico version.
- **Configuring the [Continuous Deployment (CD) Repository](https://docs.kentico.com/x/continuous_deployment)** — the `cd-repository-configure` skill discovers your project layout, inspects CI Repository changes from one or more pull requests or commit ranges, and writes a `repository.config` that captures only your feature changes — while automatically excluding noise from Xperience version updates.

More project-lifecycle capabilities are planned.

## Prerequisites

- Xperience by Kentico project
- AI coding assistant installed (for example, GitHub Copilot or Claude Code)
- For the CD Repository skill: CI/CD Repository enabled (v2 `repository.config` syntax), and local `git` available in your terminal; for PR selectors, tooling that can read your repository host's pull requests (for example, the `gh` CLI for GitHub, or the `az repos` CLI or an MCP server for Azure DevOps)

## Configure MCP servers

This plugin works best with some MCP servers set up in your workspace. See [MCP-setup.md](./MCP-setup.md) for the list and copy-paste-ready configuration.

## Install the plugin

### VS Code (GitHub Copilot)

Add the marketplace to your VS Code settings (`settings.json`), then browse and install from the Extensions sidebar (`@agentPlugins`):

```json
"chat.plugins.marketplaces": [
    "Kentico/xperience-by-kentico-kenticopilot"
]
```

For more information, see: [VS Code plugin marketplace](https://code.visualstudio.com/docs/copilot/customization/agent-plugins#_configure-plugin-marketplaces)

### Copilot CLI

```bash
copilot plugin marketplace add Kentico/xperience-by-kentico-kenticopilot
copilot plugin install kentico-project-lifecycle@xperience-by-kentico-kenticopilot
```

### Claude Code

```bash
/plugin marketplace add Kentico/xperience-by-kentico-kenticopilot
/plugin install kentico-project-lifecycle@xperience-by-kentico-kenticopilot
```

## Skills

### `update-xperience`

Updates an Xperience by Kentico project to a newer version. Reviews the [Changelog](https://docs.kentico.com/changelog) for every version in the update path, follows the feature-specific update guides linked from the release notes, and performs the update per the [official update procedure](https://docs.kentico.com/documentation/developers-and-admins/installation/update-xperience-by-kentico-projects).

### `cd-repository-configure`

Builds a scoped CD Repository configuration from the CI Repository changes in selected PRs or a commit range. The skill walks through four stages in a single conversation:

1. **Discovers the project** – locates the Xperience app, the CI Repository, and the CD `repository.config`. Asks only when a value is ambiguous.
2. **Collects and classifies changes** – reads the CI Repository files changed by the selected PRs (via your repository host's CLI or MCP tooling) or commit range (via local git) and classifies each PR or commit as a business/feature change or Xperience update-only noise (excluded by default).
3. **Writes the scoped config** – regenerates the `repository.config` filter sections from scratch for the current deployment scope: `RestoreMode`, an `IncludedObjectTypes` allowlist, `IncludedContentItemsOfType`, `ContentItemFilters`, and `ObjectFilters` with precise code names.
4. **Generates and verifies the deployment content** – runs your `Export-DeploymentPackage.ps1` (or points to the [CD store command](https://docs.kentico.com/documentation/developers-and-admins/ci-cd/continuous-deployment#store-objects-to-a-cd-repository)), then checks the generated CD Repository with the bundled `Verify-CdRepository.ps1` script, which flags configured objects that are missing from the serialized output.

The skill requires the v2 `repository.config` syntax. If your project still uses v1, the skill stops and points you to [Migrate CI/CD repository.config to v2](https://docs.kentico.com/documentation/developers-and-admins/ci-cd/configure-ci-cd-repositories/config-v2-migration) — and can help you apply the documented steps.

## Usage

### Update your Xperience project

The update skill identifies your current and target Xperience versions, reviews the release notes for every version in between (including the feature-specific update guides they link to), and follows the official update documentation.

#### VS Code GitHub Copilot example

```text
/update-xperience
```

To update to a specific version instead of the latest:

```text
/update-xperience 30.9.2
```

### Configure the CD Repository

Provide the PR numbers or the git commit range you want to deploy. When your workspace contains more than one Xperience app, also mention the app path.

#### VS Code GitHub Copilot example — single PR

```text
/cd-repository-configure

Changes: PR 312
```

#### VS Code GitHub Copilot example — multiple PRs

```text
/cd-repository-configure

Changes: PR 310, PR 311, PR 312
```

#### VS Code GitHub Copilot example — commit range

The `..` range operator follows standard git syntax: the start commit is **exclusive** and the end commit is **inclusive**. Use the commit just before your first feature commit as the range start.

```text
/cd-repository-configure

Changes: abc1234..def5678
```

To include `abc1234` itself, use its parent as the range start (`abc1234^..def5678`). To deploy exactly one commit in isolation, use `abc1234^..abc1234`.

## Prompt output

An updated `repository.config` scoped to the selected changes, plus a deployment summary covering:

- Analyzed selectors with per-commit/PR classification — included (business/feature) vs. excluded (Xperience update-only), with reasons
- The chosen `RestoreMode` and the selected object types, code names, and content item filters
- Exactly what changed in `repository.config`
- Results of the deployment package export and the verification script, when run

## Best practices

- Keep Xperience version-update PRs separate from feature PRs where possible — this makes classification unambiguous and exclusion automatic.
- Review the generated `repository.config` diff before deploying, especially for the first run on a project.
- The skill rebuilds the deployment filters from scratch on every run; it asks before removing entries it did not create (for example, standing manual exclusions).

## Included files

### update-xperience references

- `skills/update-xperience/references/update-docs.md` – map of the Changelog and official update-procedure documentation pages, fetched on demand via the Kentico Docs MCP.

### cd-repository-configure references (read by the agent)

- `references/ci-path-mapping.md` – translations from CI Repository folder names to `repository.config` object types and content item filter elements, plus the known special cases (forms, reusable field schemas, workspaces, `cms.class` ambiguity).
- `references/repository-config-guidelines.md` – rules for regenerating a minimal deployment-scoped config: allowlist decisions, content item filter dependencies, `RestoreMode` selection, formatting, and the final quality checklist.
- `references/documentation-links.md` – map of the relevant Kentico documentation pages with when-to-read hints, fetched on demand via the Kentico Docs MCP.

### Templates

- `skills/cd-repository-configure/assets/DEPLOYMENT_SUMMARY_TEMPLATE.md` – the deployment summary the skill fills in at the end of every run.

### Scripts

- `skills/cd-repository-configure/scripts/Verify-CdRepository.ps1` – compares the generated CD Repository against the filters in `repository.config` and fails when a configured object or content item has no serialized file (catches silent suppression).
