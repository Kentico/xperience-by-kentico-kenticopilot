# Kentico digital experience

Skills for extending [Automation processes](https://docs.kentico.com/x/automation_xp) with [custom components](https://docs.kentico.com/x/automation_custom_xp). The plugin currently supports custom Automation actions: custom step types available in the Automation Builder.

## Skills

| Skill | Use it to | Activation |
|---|---|---|
| `automation-action` | Implement and register a custom Automation action, including optional marketer-configurable properties | Invoke by name or describe the action |

The skill studies the existing project conventions, reads the current Xperience API documentation, confirms the proposed action design, and then implements it.

## Requirements

- An AI coding assistant with this plugin installed
- A description of the action's behavior and any properties marketers need to configure
- The MCP servers listed in [MCP setup](./MCP-setup.md)

## Install

Follow the marketplace instructions in the [usage guide](../../docs/Usage-Guide.md#install-the-selected-plugin), using the plugin name `kentico-digital-experience`.

## Use the plugin

Describe the action and its configurable properties. The agent inspects the project, asks only for missing design decisions, and writes the implementation.

**Example**

```
/automation-action

Create an action that sends a Slack message to a configured webhook
when a contact reaches this step. Marketers should be able to edit the
webhook URL and message template.
```

## Output

Depending on the requested action, the agent creates:

- The action class
- An optional properties class implementing `IAutomationActionProperties`, including form-component annotations
- Assembly-level `RegisterAutomationAction<>` registration

The agent follows the project's namespace, localization, dependency-injection, and logging conventions.

## Included resources

- [`skills/automation-action/references/docs.md`](./skills/automation-action/references/docs.md) maps the task to the current Xperience documentation and API examples.

## Customize the skill

Add project-specific conventions, such as resource-string organization, namespace structure, or common dependencies, to the skill's `references/` directory.
