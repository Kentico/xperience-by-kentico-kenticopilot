# Documentation links

Fetch these on demand via the **Kentico Docs MCP**. If a page is not yet published (the custom-action API is being released alongside this plugin), skip silently — `references/automation-customization.md` already contains the authoritative API surface.

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
  - Canonical list of available icons with descriptions. Pick the icon name whose intent matches the action; the default for registration is `xp-cogwheel`.
