# Configure CD Repository

AI-assisted skills for configuring the [Continuous Deployment (CD) Repository](https://docs.kentico.com/x/continuous_deployment) in Xperience by Kentico. The skills discover your project layout, inspect CI Repository changes from one or more pull requests or commit ranges, and produce a scoped `repository.config` that captures only your feature changes — while automatically excluding noise from Xperience version updates.

## Workflow

These skills provide two-stage assistance for building CD Repository filters:

1. **Discovery stage** – Locates the Xperience app folder, CI and CD Repository paths, and available git tooling. Saves everything to a reusable context file so you don't have to re-enter paths for every deployment.
2. **Configure stage** – Reads the context file, collects changed CI Repository files from the specified PRs or commit range, classifies them (feature vs. Xperience-update noise), and writes a minimal `IncludedObjectTypes` / `ObjectFilters` allowlist to `repository.config`.

## Prerequisites

- Xperience by Kentico project with CI/CD Repository enabled
- AI coding assistant installed (for example, GitHub Copilot or Claude Code)
- `gh` CLI (recommended) or local `git` available in your terminal

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
copilot plugin install configure-cd-repository@xperience-by-kentico-kenticopilot
```

### Claude Code

```bash
/plugin marketplace add Kentico/xperience-by-kentico-kenticopilot
/plugin install configure-cd-repository@xperience-by-kentico-kenticopilot
```

## Usage

### 1. Run the discovery stage

The discovery skill finds your Xperience app path, CI and CD Repository locations, and detects whether `gh` or local `git` should be used to retrieve change information. It writes this context to a `cd-repository-context.json` file in a folder you specify.

You only need to run this once per project (or after your project structure changes).

#### VS Code GitHub Copilot example

```text
/cd-repository-discover

Save context to: C:/my-project/.cd-context
```

The skill will ask for any values it cannot discover automatically (for example, the Xperience app path if multiple candidates exist in the workspace).

### 2. Run the configure stage

Provide the folder containing the context file written in step 1, along with the PR numbers or git commit range you want to deploy.

#### VS Code GitHub Copilot example — single PR

```text
/cd-repository-configure

Context folder: C:/my-project/.cd-context
Changes: PR 312
```

#### VS Code GitHub Copilot example — multiple PRs

```text
/cd-repository-configure

Context folder: C:/my-project/.cd-context
Changes: PR 310, PR 311, PR 312
```

#### VS Code GitHub Copilot example — commit range

```text
/cd-repository-configure

Context folder: C:/my-project/.cd-context
Changes: abc1234..def5678
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
- Commit `cd-repository-context.json` to your repository if your team shares the same project layout, so teammates can skip the discovery stage.

## Skill reference

### cd-repository-discover

Skill name: **cd-repository-discover**

Discovers the Xperience app path, CI Repository path, CD `repository.config` path, git repository root, and tooling availability (`gh` / `git`). Saves all values to `cd-repository-context.json` in the folder you provide.

**Argument hint:** Path to a folder where the discovery context should be written.

### cd-repository-configure

Skill name: **cd-repository-configure**

Reads `cd-repository-context.json` and a set of change selectors (PR numbers and/or a commit range), collects CI Repository file changes, excludes Xperience update noise, and writes a scoped CD Repository configuration.

**Argument hint:** Path to the discovery context folder and PR number(s) or commit hash range.
