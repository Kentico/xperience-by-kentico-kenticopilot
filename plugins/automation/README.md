# Automation

AI-assisted skills for extending [Automation processes](https://docs.kentico.com/documentation/business-users/digital-marketing/automation) in Xperience by Kentico — starting with **custom automation actions** (custom step types in the Automation Builder).

## What the skill does

The plugin ships one skill, `automation-action-create`, that walks the agent through a single conversation:

1. **Reads** the action API reference and project guardrails bundled with the skill, and pulls supplementary Xperience docs via the Kentico Docs MCP.
2. **Inspects** the target Xperience by Kentico project for existing actions, namespace conventions, DI patterns, and `.resx` localization.
3. **Confirms** the missing pieces with you in chat: identifier, display name, icon, tooltip, configurable properties (each with type, form component, default, visibility conditions), runtime behavior, and dependencies.
4. **Generates** the action class, the optional `TProperties` class with form-component annotations, the assembly-level `RegisterAutomationAction<>` attribute, and any `.resx` strings the project already uses.
5. **Verifies** by building and grepping for identifier collisions.

## Prerequisites

- Xperience by Kentico project with the custom automation action API available
- AI coding assistant (for example, GitHub Copilot or Claude Code)
- A short description of what the action should do and what — if anything — marketers should be able to configure on the step

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
copilot plugin install automation@xperience-by-kentico-kenticopilot
```

### Claude Code

```bash
/plugin marketplace add Kentico/xperience-by-kentico-kenticopilot
/plugin install automation@xperience-by-kentico-kenticopilot
```

## Available skills

| Skill                       | Description                                                                                                                                                    |
| --------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `automation-action-create`  | Researches the project and the action API, confirms requirements, and implements a registered custom automation action and (optionally) its properties model. |

## Usage

Describe the action you want. The skill probes the project, asks the questions it needs, and writes the files.

**Claude Code example**

```
/automation-action-create

I need an action that sends a Slack message to a configured webhook
when a contact reaches this step. Marketers should be able to edit
the webhook URL and the message template on the step.
```

**VS Code GitHub Copilot example**

```
/automation-action-create

Add an action that resets the lead-scoring counter persisted as
process data. No configurable properties.
```

## Examples

The `examples/automation/` folder contains canonical action samples covering distinct patterns:

| Example                          | Pattern                                                                          |
| -------------------------------- | -------------------------------------------------------------------------------- |
| `SendContactSmsAction`           | Outbound channel integration via Twilio; templated message; reads contact field. |
| `NotifySalesOnSlackAction`       | Internal-facing webhook POST with templated card.                                |
| `SyncContactToHubSpotAction`     | Outbound data sync to external CRM; DI-injected `HttpClient`; idempotency.       |
| `UpdateContactConsentAction`     | Service-based internal write using `IConsentAgreementService`.                   |
| `UpdateLeadScoreAction`          | Cross-step custom process data via `GetProcessData` / `SetProcessData`.          |
| `ResetLeadScoreAction`           | No-properties base class shape (pairs with `UpdateLeadScoreAction`).             |

The first five extend `AutomationAction<TProperties>`; `ResetLeadScoreAction` uses the no-properties `AutomationAction` base class.

## Included files

### References (read by the agent)

- `references/automation-customization.md` — snapshot of the official **Automation customization** documentation page (base classes, registration, `AutomationProcessContext`, `IAutomationProcessData`, form components, validation rules, best practices). Will be replaced by a link to the live docs page once it is published.
- `references/guardrails.md` — team rules and security/concurrency conventions beyond the API spec (no secrets in `TProperties`, `ILogger<T>` over `IEventLogService`, typed `HttpClient`, idempotency, marketer-experience conventions).
- `references/docs.md` — supplementary Xperience documentation pages the agent fetches via the Kentico Docs MCP.

### Templates

- `skills/automation-action-create/assets/ACTION_TEMPLATE.md` — in-chat scaffold the agent uses when proposing the action's design (not written to disk).

## Skill customization

These files are a baseline. Extend `references/` to capture your team's conventions (resource string organization, namespace structure, common dependencies) — the skill reads every file in that folder.
