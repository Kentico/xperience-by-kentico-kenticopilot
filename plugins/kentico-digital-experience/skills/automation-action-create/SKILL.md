---
name: "automation-action-create"
description: "Reference for implementing custom automation process actions in Xperience by Kentico. Use whenever the user wants to add a custom step to marketing automation — a class that runs for every contact reaching the step in the Automation Builder, optionally configurable by marketers through properties."
compatibility: "Requires Kentico Docs MCP"
---

This skill points you to Kentico's automation-customization documentation. Use it to implement a custom automation process action.

## Pieces of a custom action

- **Action class** – inherits `AutomationAction` (no configuration) or `AutomationAction<TProperties>` (marketer-configurable); implements the logic in `Execute`.
- **Properties class** – implements `IAutomationActionProperties`; public properties annotated with admin UI form components define the configuration dialog.
- **Registration** – the `RegisterAutomationAction` assembly attribute (identifier, display name, icon, description) makes the action appear in the Automation Builder.
- **Runtime context** – `AutomationProcessContext` gives access to the processed contact, the process, and trigger data.
- **Process data** – `IAutomationProcessData` implementations share typed data between steps of the same process.

## How to use

- Read `references/docs.md` and fetch the docs pages listed there.

## Gotcha

Form-component and validation attributes come from the `Kentico.Xperience.Admin.*.FormAnnotations` namespaces — never from `Kentico.Forms.Web.Mvc`, an obsolete Form Builder namespace with matching class names.
