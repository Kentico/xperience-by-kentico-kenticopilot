# Update Xperience

AI-assisted skills for updating an [Xperience by Kentico](https://docs.kentico.com/x/DQKQC) application to the latest available version. The skills analyze your current version, identify required upgrade steps, update NuGet and npm packages, handle database migrations, toggle CI when needed, and guide you through any breaking changes.

## Workflow

This plugin provides two-stage assistance for upgrading your Xperience by Kentico project:

1. **Prep stage** — Validates your development environment (.NET 8 SDK/runtime), installs Microsoft Data API Builder for secure database operations, discovers whether your project uses CI/CD Repository, locates the database connection string, and writes a reusable context file for the update skill.
2. **Update stage** — Analyzes your current Xperience version, identifies the latest version and upgrade path, updates NuGet and npm packages, performs database migrations, temporarily disables CI (if enabled) during the update, re-enables CI, and guides you through any breaking changes that need code adaptation.

## Prerequisites

- .NET 8 SDK (or later)
- .NET 8 runtime (or later)
  - For Apple Silicon (ARM64) Macs: install .NET 8 x64 runtime as well
- Xperience by Kentico project with local git repository
- AI coding assistant installed (for example, GitHub Copilot or Claude Code)
- (Optional) When CI is enabled: SQL Server or compatible database with Xperience schema

### Data API Builder Runtime Notes

DAB exposes a REST API over your Xperience database so the update skill can toggle the `CMSEnableCI` setting without executing raw SQL.

- DAB startup is config-driven. Use `dotnet dab start --config dab-config.json`.
- Do not pass `--rest` to `dab start` for DAB 2.0.0-rc. REST should be enabled in `dab-config.json`.
- The update workflow binds DAB to `http://127.0.0.1:50771` via `ASPNETCORE_URLS` to avoid common conflicts on the default port.
- DAB validation requires `XBK_UPDATE_DB_CONNECTION` to be set when your config uses `@env('XBK_UPDATE_DB_CONNECTION')`.
- Use shell-appropriate environment syntax: Bash/zsh uses inline `VAR=value command`, while PowerShell uses `$env:VAR = "value"; command`.
- On Apple Silicon, if `dotnet dab` fails due to framework or architecture mismatch, run DAB with ARM64 explicitly, for example `arch -arm64 $(which dotnet) dab --version`, and use the same prefix for other DAB commands when needed.

### SQL/Database Access Note

When your Xperience project has Continuous Integration (CI/CD Repository) enabled, the update workflow needs to temporarily disable and re-enable CI in the database.

- For fully autonomous execution, database access is required. The prep skill will attempt to locate your connection string automatically from `appsettings.json`, `appsettings.Development.json`, or .NET user-secrets.
- If the prep skill cannot find a connection string, it will stop and provide configuration instructions.
- If your project does not use CI/CD Repository, database access is not required; the update workflow will skip CI-related operations.
- The developer is responsible for providing secure and correct database access and permissions. Connection strings are never written to disk and are discarded after use.

## Install the plugin

### VS Code (GitHub Copilot)

Add the marketplace to your VS Code settings (`settings.json`), then browse and install from the Extensions sidebar (`@agentPlugins`):

```json
"chat.plugins.marketplaces": [
    "Kentico/xperience-by-kentico-kenticopilot"
]
```

### Copilot CLI

```bash
copilot plugin marketplace add Kentico/xperience-by-kentico-kenticopilot
copilot plugin install update-xperience@xperience-by-kentico-kenticopilot
```

### Claude Code

```bash
/plugin marketplace add Kentico/xperience-by-kentico-kenticopilot
/plugin install update-xperience@xperience-by-kentico-kenticopilot
```

## Usage

### 1. Run the prep stage

The prep skill validates your environment, installs Data API Builder, and discovers your project's CI and database connection context. Run this once per repository before using the update skill.

#### VS Code GitHub Copilot example

```text
/update-xperience-prep
```

The skill will:

- Validate .NET 8 SDK and runtime are installed
- Install Microsoft Data API Builder (pinned to v2.0.0-rc) locally to the project
- Locate your Xperience web project
- Detect whether CI/CD Repository is enabled
- (If CI is enabled) Locate and verify your database connection string
- Write `update-xperience-context.json` to the repository root

#### Copilot CLI example

```bash
copilot plugin marketplace add Kentico/xperience-by-kentico-kenticopilot
copilot plugin install update-xperience@xperience-by-kentico-kenticopilot
copilot skill run update-xperience-prep
```

#### Claude Code example

```bash
/plugin marketplace add Kentico/xperience-by-kentico-kenticopilot
/plugin install update-xperience@xperience-by-kentico-kenticopilot
/update-xperience-prep
```

### 2. Run the update stage

After prep is complete, the update skill analyzes your current Xperience version, identifies the upgrade path, and walks you through all required steps: updating packages, applying migrations, toggling CI when needed, and addressing breaking changes.

#### VS Code GitHub Copilot example

```text
/update-xperience
```

The skill will:

- Analyze your current Xperience version and identify the latest available version
- Update NuGet and npm packages to their latest versions
- Run build and optional test suites
- (If CI is enabled) Temporarily disable CI, apply database migrations, re-enable CI
- Guide you through any code changes required for breaking changes
- Commit the update to git

#### Copilot CLI example

```bash
copilot skill run update-xperience
```

#### Claude Code example

```bash
/update-xperience
```

## Output and Artifacts

### From prep stage

- **update-xperience-context.json** (repository root) — Context file containing paths, CI/connection metadata, and the resolved `CMSEnableCI` key id used by the update stage. Safe to commit.
- **dab-config.json** (repository root, if CI is enabled) — Data API Builder configuration file. Safe to commit; contains no secrets.

### From update stage

- **Updated project files** — NuGet and npm package changes, code adaptations for breaking changes
- **Git commits** — One or more commits capturing the upgrade progress, with clear commit messages
- **Migration artifacts** — Any database migration scripts or backups created during the update

## Best Practices

- Run **prep** once per repository (or after your project structure changes).
- **Re-run prep** if you update your .NET SDK version or change CI/connection settings.
- Keep **both generated files** (`update-xperience-context.json` and `dab-config.json`) in version control; they contain no secrets and enable consistent updates across developers.
- Review the **generated diffs** before committing, especially if code changes were required for breaking changes.
- If prep reports that it **cannot find a connection string**, follow its instructions to configure one before running update (use `appsettings.json`, `appsettings.Development.json`, or .NET user-secrets).
- For **CI-enabled projects**, ensure your database user account has permissions to update `CMS_SettingsKey` (`CMSEnableCI` key).
- Run the update skill from a non-default branch (not `main`/`master`); use a feature branch.

## Skill Reference

### update-xperience-prep

Skill name: **update-xperience-prep**

Validates the development environment (.NET 8 SDK/runtime), installs Microsoft Data API Builder, discovers the Xperience project layout, detects CI/CD Repository usage, and locates the database connection string (if CI is enabled). For CI-enabled projects, it also resolves the `CMSEnableCI` key id through DAB REST and writes it to `update-xperience-context.json` for reuse by the update skill.

**Argument hint:** Optional path to the Xperience web project (auto-discovered if not provided).

**Use when:** Starting to use the update-xperience plugin on a new repository. Run only once per repository.

**Prerequisites:**

- .NET 8 SDK and runtime installed
- Xperience by Kentico project in a git repository
- (If CI is enabled) Database connection string configured in `appsettings.json`, `appsettings.Development.json`, or .NET user-secrets

### update-xperience

Skill name: **update-xperience**

Analyzes the current Xperience version, identifies the latest version and upgrade path, updates NuGet and npm packages, applies database migrations, temporarily disables and re-enables CI (if enabled), and guides you through any code changes required for breaking changes. Commits the upgrade to git.

**Argument hint:** Optional target version to upgrade to (defaults to latest). Pass `AgentMode` to skip interactive confirmations.

**Use when:** You're ready to upgrade your Xperience project to the latest version. Requires `update-xperience-prep` to have been run first.

**Prerequisites:**

- `update-xperience-prep` must have been run successfully
- `update-xperience-context.json` must exist at the repository root
- Clean git working tree (no uncommitted changes) required
- Run from a non-default branch (not `main`/`master`); use a feature branch
- (If CI is enabled) Database connection string must have been located by prep skill
