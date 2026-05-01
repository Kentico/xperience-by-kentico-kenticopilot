# Update Xperience

AI-assisted skills for updating an [Xperience by Kentico](https://docs.kentico.com/x/DQKQC) application to the latest available version.

## Workflow

This plugin provides AI assistance for upgrading your Xperience by Kentico project:

1. **Update stage** - Analyzes the current version of your project, identifies the required upgrade steps, and guides you through updating NuGet packages, applying database migrations, and adapting your code to breaking changes.

## Prerequisites

- Xperience by Kentico project
- AI coding assistant installed (for example, GitHub Copilot or Claude Code)
- Access to the project database if you want fully autonomous CI handling during update (for example, SQL access provided through local tools, scripts, or an MCP-enabled SQL workflow)

## SQL Access Note

The update workflow includes CI-aware database operations when Continuous Integration is enabled.

- For fully autonomous execution, the agent needs a way to run SQL queries against the Xperience database.
- If SQL access is unavailable, the agent can still continue with guidance, but will require manual confirmation or manual execution of CI enable/disable SQL steps.
- The developer is responsible for providing secure and correct database access and permissions.

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

### Run the update skill

The AI analyzes your project's current Xperience by Kentico version, determines the latest available version, and walks you through all required upgrade steps including package updates, database migrations, and code changes.

**VS Code GitHub Copilot example**

```
/update-xperience
```

**Claude Code example**

```
/update-xperience
```
