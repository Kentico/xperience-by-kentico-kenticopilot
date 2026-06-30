# Documentation links

Fetch these on demand via the **Kentico Docs MCP**. Treat the live pages below as the authoritative source for the contract (base classes, registration attribute, identifier constraints, runtime context, process data). Do not rely on a bundled snapshot.

## Automation customization (primary API reference)

- Custom automation steps: <https://docs.kentico.com/documentation/developers-and-admins/digital-marketing-setup/automation-customization/automation-custom-steps>
  - Authoritative API surface: the `AutomationAction` / `AutomationAction<TProperties>` base classes and their `Execute` overrides, the `RegisterAutomationAction<TAction>` assembly attribute and its parameters (`identifier`, `displayName`, optional `IconName`, `Description`), identifier constraints, the `AutomationProcessContext` (processed contact via `GetProcessedObject`, `Process.DisplayName`, trigger data via `GetTriggerData<T>`, cross-step data via `GetProcessData<T>` / `SetProcessData<T>`), and `IAutomationProcessData`. Fetch this first when implementing an action.
- Automation customization overview: <https://docs.kentico.com/documentation/developers-and-admins/digital-marketing-setup/automation-customization>
  - The kinds of custom automation components and where actions fit.

## Automation processes

- Automation overview: <https://docs.kentico.com/documentation/business-users/digital-marketing/automation>
  - Trigger types, step types, contact mapping. Read this to understand the marketer's mental model before designing an action.

## Admin form components (for `TProperties`)

- Form components reference: <https://docs.kentico.com/documentation/developers-and-admins/customization/extend-the-administration-interface/ui-form-components/reference-admin-ui-form-components>
  - Canonical catalog of attributes — fetch when picking attributes for `TProperties`.
- Validation rules: <https://docs.kentico.com/documentation/developers-and-admins/customization/extend-the-administration-interface/ui-form-components/ui-form-component-validation-rules.html>
  - Built-in validation attributes (`RequiredValidationRule`, `MaxLengthValidationRule`, ...) and how to define custom ones. Also the authoritative source for the rule "do not use `Kentico.Forms.Web.Mvc`" — that namespace contains obsolete / Form-Builder-side classes with matching names.
- Visibility conditions: <https://docs.kentico.com/documentation/developers-and-admins/customization/extend-the-administration-interface/ui-form-components/ui-form-component-visibility-conditions.html>
- Configure editing component state: <https://docs.kentico.com/documentation/developers-and-admins/customization/extend-the-administration-interface/ui-form-components/editing-components/configure-editing-component-state>
  - Cross-field dependencies via component configurators — e.g. a dropdown whose options change based on another property's value.

## Icon catalog (for `IconName`)

- Annotated Xperience icons library: <https://github.com/Kentico/xperience-by-kentico-component-icons>
  - Canonical list of available icons with descriptions. Reference icons through the `Kentico.Xperience.Admin.Base.Icons` constants (e.g. `Icons.Bell`) rather than raw `xp-` strings; the default for registration is `Icons.Cogwheel` (`xp-cogwheel`).
