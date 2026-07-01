# Automation

AI-assisted skills for extending [Automation processes](https://docs.kentico.com/x/automation_xp) with [custom components](https://docs.kentico.com/x/automation_custom_xp) in Xperience by Kentico. Currently supports **custom automation actions** (custom step types in the Automation Builder).

## Prerequisites

- Xperience by Kentico project using version 31.6.0 or newer
- AI coding assistant (for example, GitHub Copilot, Copilot CLI, or Claude Code)
- A short description of what the component should do and what properties (if any) marketers should be able to configure for the step

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

### `automation-action-create`

Researches the project and the action API, then implements and registers a custom automation action and (optionally) its `IAutomationActionProperties`-implementing properties class. The skill walks the agent through a single conversation:

1. **Reads** the code-quality guardrails bundled with the skill, and fetches the action API contract and supplementary Xperience docs from the live documentation via the Kentico Docs MCP.
2. **Inspects** the target Xperience by Kentico project for existing actions, namespace conventions, DI patterns, and `.resx` localization.
3. **Confirms** the missing pieces with you in chat: identifier, display name, icon, tooltip, configurable properties (each with type, form component, default, validation rules, visibility conditions), runtime behavior, and dependencies. Default values may be inferred without confirmation during the initial generation – you can always adjust the output or ask the agent to make any required changes.
4. **Generates** the action class, the optional `TProperties` class (implementing `IAutomationActionProperties`) with form-component annotations, the assembly-level `RegisterAutomationAction<>` attribute, and any `.resx` strings the project already uses.
5. **Verifies** by building and grepping for identifier collisions.

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

The [`references/example-actions.md`](skills/automation-action-create/references/example-actions.md) file collects canonical action samples covering distinct patterns:

| Example                          | Pattern                                                                          |
| -------------------------------- | -------------------------------------------------------------------------------- |
| `SendContactSmsAction`           | Outbound channel integration via Twilio; templated message; reads contact field. |
| `NotifySalesOnSlackAction`       | Internal-facing webhook POST with templated card.                                |
| `SyncContactToHubSpotAction`     | Outbound data sync to external CRM; DI-injected `HttpClient`; idempotency.       |
| `UpdateContactConsentAction`     | Service-based internal write using `IConsentAgreementService`.                   |
| `UpdateLeadScoreAction`          | Cross-step custom process data sharing via `GetProcessData` / `SetProcessData`.  |
| `ResetLeadScoreAction`           | No-properties base class pattern (pairs with `UpdateLeadScoreAction`).             |

The first five inherit from `AutomationAction<TProperties>`. `ResetLeadScoreAction` uses the no-properties `AutomationAction` base class.

## Included files

### References (read by the agent)

- `references/guardrails.md` – code-quality guardrails beyond the API specification (no secrets in `TProperties`, `ILogger<T>` over `IEventLogService`, typed `HttpClient`, idempotency, marketer-experience conventions).
- `references/docs.md` – links to the live Xperience documentation the agent fetches via the Kentico Docs MCP, including the **Custom automation steps** page that is the authoritative source for the action API (base classes, registration, `AutomationProcessContext`, `IAutomationProcessData`, form components, validation rules).
- `references/example-actions.md` – canonical custom action samples (one section per action, with the action and its properties class shown as separate files) that the skill mirrors when generating code.

### Templates

- `skills/automation-action-create/assets/ACTION_TEMPLATE.md` – in-chat scaffold the agent uses when proposing the action's design (not written to disk).

## Skill customization

These files are a baseline. Extend `references/` to capture your team's conventions (resource string organization, namespace structure, common dependencies) – the skill reads every file in that folder.
