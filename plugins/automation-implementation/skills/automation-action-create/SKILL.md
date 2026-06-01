---
name: "automation-action-create"
description: "Implements a custom automation process action in Xperience by Kentico. Reviews project conventions and the action API, confirms the action's behavior and properties with the user, then emits the action class, the optional properties class with form-component annotations, and the assembly-level RegisterAutomationAction registration."
argument-hint: "Free-form description of the action to build (purpose, configurable properties, side effects)"
compatibility: "Requires Kentico Docs MCP"
---

You implement a custom **automation process action** in an Xperience by Kentico project — a step type that appears in the Automation Builder and runs `Execute` for every contact (or other processed object) that reaches it.

## What you must produce

1. A class extending **`CMS.Automation.AutomationAction`** (no properties) or **`CMS.Automation.AutomationAction<TProperties>`** (with properties).
2. An assembly-level **`[assembly: RegisterAutomationAction<TAction>(identifier, displayName, IconName = ..., Tooltip = ...)]`**.
3. If properties: a **`TProperties`** class implementing `CMS.Automation.IAutomationActionProperties` with form-component-annotated public read/write properties.
4. If the action shares cross-step state: an **`IAutomationProcessData`** implementation with a unique `static abstract string Identifier`.
5. If the project uses `.resx` localization in `Register*` attributes (look for `"{$...$}"` strings): the new display name, tooltip, and labels in the existing `.resx`, referenced via the `{$...$}` syntax.

## Steps

### 1. Read context

- Read **`references/automation-customization.md`** — base classes, registration, `AutomationProcessContext`, `IAutomationProcessData`, form components, validation rules, best practices.
- Read **`references/guardrails.md`** — code quality guardrails beyond the API spec.
- Fetch supplementary docs listed in **`references/docs.md`** via the Kentico Docs MCP if needed (form-component reference, visibility conditions, validation rules — see `references/docs.md` for the catalog of pages worth fetching on demand).

### 2. Discover the project and the surrounding APIs

- Search for `AutomationAction<` and `RegisterAutomationAction<`. If existing actions are found, mirror their folder, namespace, and registration style.
- If none exist, follow the project's conventions for similar extensibility (widgets, page templates). Default to an `Automation/` folder at the project root.
- Note DI registration patterns and whether the project uses `.resx` localization in component metadata.
- **Verify the API surface of any Xperience service you plan to inject** (e.g. `IConsentAgreementService`, `IInfoProvider<TInfo>`, `INotificationEmailMessageProvider`) before calling its methods — check the actual method signatures, return types, and whether they are sync or async. Do not assume.
- **If the action integrates with an external service** (Twilio, Slack, HubSpot, Salesforce, a webhook, an SDK, etc.) — briefly research that service's documentation and request shape (via Kentico Docs MCP for Kentico-side concerns, and via WebSearch/WebFetch for the third-party API). Enough to make the SDK call or HTTP payload look realistic, including: the canonical SDK entry point or HTTP endpoint, required authentication shape, the request body, and how the provider signals errors / duplicates. Capture credentials in a typed `*Options` class bound to `appsettings.json` — never on `TProperties`.

### 3. Confirm the design

Walk through **`assets/ACTION_TEMPLATE.md`** with the user in chat — identifier, display name, icon, tooltip, base class, properties (name, type, form component, default, validation rules, visibility conditions), runtime behavior (inputs, side effects, failure handling), injected dependencies, and any `IAutomationProcessData` types the action reads or writes. **Do not save the template to disk** — it is an in-chat scaffold, not an artifact.

Ask only what you cannot reasonably infer. Propose defaults the user can override.

### 4. Implement

Write the files following these rules (full detail in `guardrails.md`):

- Constructor injection for dependencies; verify every injected service is registered with the DI container.
- Use `ILogger<T>` for logging — not `IEventLogService`.
- Never `.Result`/`.Wait()`; no static mutable state; no per-execution state in instance fields.
- Cast `context.ProcessedObject` to `ContactInfo` and short-circuit on mismatch — see the canonical guard in `guardrails.md`.
- External calls keyed by a stable identifier on the cast contact (typically `ContactInfo.ContactID`; prefer `ContactInfo.ContactGUID` when the external system needs a globally stable key) for idempotency.
- No secrets in `TProperties` — read them from `IConfiguration` / `IOptions<T>`.
- Outbound HTTP uses typed `HttpClient` registered with `services.AddHttpClient<TAction>()` — this binds a factory-pooled `HttpClient` to the action class, avoiding both socket exhaustion (from `new`-ing `HttpClient` per call) and stale DNS (from a long-lived singleton). The typed form also lets you centralise base address, default headers, and `DelegatingHandler`s (retry / auth / logging) without leaking that wiring into `Execute`.
- Prefer declarative validation attributes (`RequiredValidationRule`, `MaxLengthValidationRule`, `MinimumIntegerValueValidationRule`, `MaximumIntegerValueValidationRule`, `MinimumDecimalValueValidationRule`, ...) on `TProperties` instead of hand-rolled checks in `Execute`. Reserve `Execute` validation for cross-property or runtime conditions only.
- Form-component attributes and validation rules come from `Kentico.Xperience.Admin.Base.FormAnnotations` (plus `Kentico.Xperience.Admin.Content.FormAnnotations` and `Kentico.Xperience.Admin.DigitalMarketing.FormAnnotations` for content- and marketing-specific selectors). Do **not** import `Kentico.Forms.Web.Mvc` — that namespace contains obsolete classes with matching names (`ValidationRule`, `RegisterFormValidationRule`, ...) for the live-site Form Builder, which is the wrong API surface here.

### 5. Verify

- Run `dotnet build` on the web project.
- Confirm the identifier is unique in the solution (grep the identifier string).
- If you added `.resx` strings, confirm the `.resx` and its registration class compile.

Report what you produced and the manual steps (if any) the user still owes — typically registering a new dependency in DI or adding a configuration value to `appsettings.json`.
