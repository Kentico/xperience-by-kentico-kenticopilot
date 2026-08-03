---
name: "automation-trigger"
description: "Reference for implementing custom automation triggers in Xperience by Kentico. Use whenever the user wants an automation process to start from application code — a purchase, an incoming webhook, an event handler, or a scheduled task — optionally carrying typed data into the process and configurable by marketers through properties."
compatibility: "Requires Kentico Docs MCP"
---

This skill points you to Kentico's automation-customization documentation. Use it to implement a custom automation trigger and fire it from the project's code.

## Pieces of a custom trigger

- **Trigger class** – inherits `AutomationTrigger` (no data), `AutomationTrigger<TData>`, or `AutomationTrigger<TData, TProperties>` (marketer-configurable); overrides `Evaluate` to decide whether the process starts.
- **Trigger data** – implements `IAutomationTriggerData` with a stable `Identifier`; JSON-serialized with the process state and read by later steps through `AutomationProcessContext.GetTriggerData<T>()`.
- **Properties class** – implements `IAutomationTriggerProperties`; public properties annotated with admin UI form components define the configuration dialog marketers see when they pick the trigger.
- **Registration** – the generic `RegisterAutomationTrigger<TTrigger>` assembly attribute makes the trigger selectable in the Automation Builder.

## How to use

- Read `references/docs.md` and fetch the docs pages listed there.
- A trigger class alone does nothing. Confirm with the user where the trigger fires — event handler, controller, webhook endpoint, or scheduled task — and wire the `FireTrigger` call into that code path.

## Gotcha

- Trigger classes must be stateless; one instance serves every evaluation.
- Keep trigger data small and free of personal data — carry identifiers, not names, e-mail addresses, or keys.
- Don't fire triggers from inside a custom automation step — log a custom activity and let the built-in step do it.
- Every trigger type lives in `CMS.Automation`. Only the form-component attributes on the properties class come from elsewhere.
- Form-component and validation attributes come from the `Kentico.Xperience.Admin.*.FormAnnotations` namespaces — never from `Kentico.Forms.Web.Mvc`, an obsolete Form Builder namespace with matching class names.
- Use `ILogger<T>` for logging. `EventLogService` is obsolete — don't use it.
