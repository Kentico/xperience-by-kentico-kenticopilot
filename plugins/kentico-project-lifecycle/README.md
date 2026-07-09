# Kentico project lifecycle

Skills for managing the lifecycle of an Xperience by Kentico solution. The plugin currently covers two areas:

- **Updating your Xperience project** — the `update-xperience` skill updates a project to a newer Xperience by Kentico version.
- **Configuring the [Continuous Deployment (CD) Repository](https://docs.kentico.com/x/continuous_deployment)** — the `cd-repository-*` skills discover your project layout, inspect CI Repository changes from one or more pull requests or commit ranges, and produce a scoped `repository.config` that captures only your feature changes — while automatically excluding noise from Xperience version updates.

More project-lifecycle capabilities are planned.

## Workflow

The CD Repository skills provide three-stage assistance for building CD Repository filters:

1. **Discovery stage** – Locates the Xperience app folder, CI and CD Repository paths, and available git tooling. Detects the `repository.config` syntax version. Saves everything to a reusable context file so you don't have to re-enter paths for every deployment.
2. **Upgrade stage** (if needed) – If discovery detects a legacy v1 `repository.config`, migrates it to v2 syntax to enable advanced content item filtering and improved CD restore performance. Automatically updates the context file when complete.
3. **Configure stage** – Reads the context file, collects changed CI Repository files from the specified PRs or commit range, classifies them (feature vs. Xperience-update noise), and writes a minimal `IncludedObjectTypes` / `ObjectFilters` allowlist to `repository.config`.

## Prerequisites

- Xperience by Kentico project
- AI coding assistant installed (for example, GitHub Copilot or Claude Code)
- For the CD Repository skills: CI/CD Repository enabled and `gh` CLI (recommended) or local `git` available in your terminal

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

The CD Repository skills run in the three stages described in the [Workflow](#workflow) section above.

#### 1. Run the discovery stage

The discovery skill finds your Xperience app path, CI and CD Repository locations, and detects whether `gh` or local `git` should be used to retrieve change information. It also detects the current `repository.config` syntax version. It writes this context to a `cd-repository-context.json` file in a folder you specify.

You only need to run this once per project (or after your project structure changes).

##### VS Code GitHub Copilot example

```text
/cd-repository-discovery

Save context to: C:/my-project/.cd-context
```

The skill will ask for any values it cannot discover automatically (for example, the Xperience app path if multiple candidates exist in the workspace). It will also report the `repository.config` syntax version detected.

#### 2. Upgrade config syntax (if needed)

If discovery detected a legacy v1 `repository.config`, upgrade it to v2 first to enable advanced content item filtering and improve CD restore performance.

##### VS Code GitHub Copilot example — with explicit config path

```text
/cd-repository-upgrade

Repository config path: C:/my-project/App_Data/CDRepository/repository.config
```

##### VS Code GitHub Copilot example — auto-discover from context

If you've already run `cd-repository-discovery`, you can run the upgrade skill without arguments and it will automatically locate the config file from the context file:

```text
/cd-repository-upgrade
```

The skill will create a backup (`repository.config.v1.backup`) and migrate the file to v2 syntax. If it discovered the config from a context file, it will automatically update the context to reflect v2 syntax. Proceed to step 3 (configure stage).

#### 3. Run the configure stage

Provide the folder containing the context file written in step 1, along with the PR numbers or git commit range you want to deploy.

##### VS Code GitHub Copilot example — single PR

```text
/cd-repository-configure

Context folder: C:/my-project/.cd-context
Changes: PR 312
```

##### VS Code GitHub Copilot example — multiple PRs

```text
/cd-repository-configure

Context folder: C:/my-project/.cd-context
Changes: PR 310, PR 311, PR 312
```

##### VS Code GitHub Copilot example — commit range

The `..` range operator follows standard git syntax: the start commit is **exclusive** and the end commit is **inclusive**. Use the commit just before your first feature commit as the start of the range.

```text
/cd-repository-configure

Context folder: C:/my-project/.cd-context
Changes: abc1234..def5678
```

To include `abc1234` itself, use its parent (`abc1234^`) as the range start:

```text
/cd-repository-configure

Context folder: C:/my-project/.cd-context
Changes: abc1234^..def5678
```

To deploy exactly one commit in isolation:

```text
/cd-repository-configure

Context folder: C:/my-project/.cd-context
Changes: abc1234^..abc1234
```

The skill will:

- Filter changed files to only those inside the CI Repository
- Classify them as feature changes or Xperience update-only changes
- Exclude Xperience update-only changes from the deployment filters (unless you ask to include them)
- Write a minimal `IncludedObjectTypes` allowlist and scoped `ObjectFilters` to `repository.config`
- Diff and explain every change it makes

## Prompt output

The configure stage produces an updated `repository.config` containing:

- `IncludedObjectTypes` — allowlist of only the object types touched by your feature changes
- `ObjectFilters` with `IncludedCodeNames` — precise code name scope per object type

It also prints a deployment summary covering reviewed change selectors, selected object types and code names, excluded update-only groups and the reason for exclusion, and a diff of exactly what changed in `repository.config`.

If `Export-DeploymentPackage.ps1` is present in the repository, the skill runs it and validates that the exported package contains the expected scoped content.

## Best practices

- Re-run **discovery** whenever your project structure changes or you move to a new machine.
- Use the **same context folder** across multiple configure runs for the same project so you never have to re-enter paths.
- Keep Xperience version-update PRs separate from feature PRs where possible — this makes classification unambiguous and exclusion automatic.
- Review the generated `repository.config` diff before deploying, especially for the first run on a project.
- Keep the generated `cd-repository-context.json` local to your machine because it contains absolute, machine-specific paths. Optionally add the context folder to `.gitignore`. If you want to share the expected context structure with teammates, commit a template or example context file instead of the generated one.

## Skill reference

### update-xperience

Skill name: **update-xperience**

Updates an Xperience by Kentico project to a newer version. Reviews the [Changelog](https://docs.kentico.com/changelog) for every version in the update path, follows the feature-specific update guides linked from the release notes, and performs the update per the [official update procedure](https://docs.kentico.com/documentation/developers-and-admins/installation/update-xperience-by-kentico-projects).

**Argument hint:** Optional target version (defaults to the latest available).

**Use when:** You want to update your Xperience project to a newer version, apply a hotfix, or move to a newer refresh.

### cd-repository-upgrade

Skill name: **cd-repository-upgrade**

Migrates a CD Repository `repository.config` file from legacy v1 syntax to v2 syntax, enabling advanced content item filtering and improved CD restore performance. Creates a backup (`repository.config.v1.backup`) before migration.

**Argument hint:** Path to the `repository.config` file to migrate.

**Use when:** After running discovery, if `repository.config` syntax version is reported as v1. Run before `cd-repository-configure` to ensure your configuration uses the v2 format. See [Migrate CI/CD repository.config to v2](https://docs.kentico.com/documentation/developers-and-admins/ci-cd/configure-ci-cd-repositories/config-v2-migration) for details on the v1 to v2 changes.

### cd-repository-discovery

Skill name: **cd-repository-discovery**

Discovers the Xperience app path, CI Repository path, CD `repository.config` path, git repository root, and tooling availability (`gh` / `git`). Saves all values to `cd-repository-context.json` in the folder you provide.

**Argument hint:** Path to a folder where the discovery context should be written.

### cd-repository-configure

Skill name: **cd-repository-configure**

Reads `cd-repository-context.json` and a set of change selectors (PR numbers and/or a commit range), collects CI Repository file changes, excludes Xperience update noise, and writes a scoped CD Repository configuration.

**Argument hint:** Path to the discovery context folder and PR number(s) or commit hash range.
