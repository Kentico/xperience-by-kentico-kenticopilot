# Automation

AI-assisted skills for extending [Automation processes](https://docs.kentico.com/x/automation_xp) with [custom components](https://docs.kentico.com/x/automation_custom_xp) in Xperience by Kentico. Currently supports **custom automation actions** (custom step types in the Automation Builder).

## Prerequisites

- Xperience by Kentico project using version 31.6.0 or newer
- AI coding assistant (for example, GitHub Copilot, Copilot CLI, or Claude Code)
- A short description of what the component should do and what properties (if any) marketers should be able to configure for the step

## Configure MCP servers

This plugin requires some MCP servers to be set up in your workspace. See `MCP-setup.md` for the list and copy-paste-ready configuration.

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
copilot plugin install kentico-digital-experience@xperience-by-kentico-kenticopilot
```

### Claude Code

```bash
/plugin marketplace add Kentico/xperience-by-kentico-kenticopilot
/plugin install kentico-digital-experience@xperience-by-kentico-kenticopilot
```

## Skills

### `automation-action`

Points the agent to the authoritative Xperience documentation for implementing a custom automation action — the action class, the optional `IAutomationActionProperties`-implementing properties class with form-component annotations, and the `RegisterAutomationAction<>` registration. The agent fetches the relevant pages via the Kentico Docs MCP, mirrors the project's existing conventions, confirms the action's design with you, and implements following the documentation's examples and best practices.

## Usage

Describe the action you want. The skill probes the project, asks the questions it needs, and writes the files.

### Claude Code example

```text
/automation-action

I need an action that sends a Slack message to a configured webhook
when a contact reaches this step. Marketers should be able to edit
the webhook URL and the message template on the step.
```

### VS Code GitHub Copilot example

```text
/automation-action

Add an action that resets the lead-scoring counter persisted as
process data. No configurable properties.
```

## Included files

### References (read by the agent)

- `references/docs.md` – links to the live Xperience documentation the agent fetches via the Kentico Docs MCP, including the **Custom automation steps** page that is the authoritative source for the action API, code examples, registration, runtime context, and best practices.

## Skill customization

These files are a baseline. Extend `references/` to capture your team's conventions (resource string organization, namespace structure, common dependencies) – the skill reads every file in that folder.
