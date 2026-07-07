# Custom automation action — design scaffold

Use this as a mental checklist when proposing the action design with the user in chat. **Do not save a filled-in copy to disk** — the conversation is the artifact, the code is the deliverable.

## Identity

- Action class name (PascalCase, ends in `Action`).
- Registration identifier (`Company_Module_Action`; letters, digits, underscores, dots; unique in solution).
- Display name (short, marketer-facing).
- Icon (`Icons.*` constant from `Kentico.Xperience.Admin.Base`, e.g. `Icons.Bell`).
- Description (one sentence — hover text shown in the step selector).

## Base class

- `AutomationAction` (no properties) — or — `AutomationAction<TProperties>` (with properties).

## Configurable properties (skip if no properties)

For each property, decide:

- Name + .NET type.
- Form component attribute (e.g. `TextInputComponent`, `DropDownComponent`, `CheckBoxComponent`).
- Validation rules to attach (`RequiredValidationRule`, `MaxLengthValidationRule`, `Minimum/MaximumIntegerValueValidationRule`, ...).
- Default value.
- Form category for UI grouping, if any.
- Visibility condition, if any (`[VisibleIfTrue(nameof(...))]`).
- Marketer-facing label, plus optional explanation and/or watermark text (e.g. `Label = "Webhook URL"`, `ExplanationText = "Slack incoming webhook (https://hooks.slack.com/...)"`, `WatermarkText = "https://hooks.slack.com/services/..."`).

Only when the action has `TProperties`: optionally override the default **Step name** input by declaring a property named `StepDisplayName` on the properties class with its own form-component annotation (label, validation, ...). The step name always appears first in the dialog — its order cannot be changed. For no-properties actions this override isn't available — the marketer enters the step name in the default input.

## Runtime behavior

- The processed contact, retrieved via `await context.GetProcessedObject(ct)` (returns the `ContactInfo`; throws if the processed object is not a contact).
- Inputs from `TProperties`.
- Cross-step state read with `await context.GetProcessData<T>(ct)` — list each `IAutomationProcessData` type, including its `Identifier`. Remember `GetProcessData` returns `null` if the data has never been written.
- Inputs from configuration / `IOptions<T>` (secrets, environment-scoped values).
- Side effects (HTTP calls, service invocations, `await context.SetProcessData(...)` writes, log entries).
- Failure modes — transient vs. permanent — and what `Execute` does for each.

## Dependencies (constructor injection)

For each service: type, where it's registered, lifestyle. Confirm any that aren't built into Xperience.

## File plan

- `<Project>/Automation/<ActionClass>.cs` — action + assembly registration attribute.
- `<Project>/Automation/<ActionClass>Properties.cs` — only if there are properties.
- `<Project>/Automation/<DataClass>.cs` — one per `IAutomationProcessData` implementation introduced.
- `<Project>/Automation/<OptionsClass>.cs` — only if the action binds typed options from `appsettings.json`.
- DI registration in `Program.cs` (or wherever the project registers services).
- `appsettings.json` sample values for any new options.

## Pre-flight checks

- Action extends the right base class and overrides `Execute`.
- Each class (action, `TProperties`, each `IAutomationProcessData`, each `*Options`) lives in its own file named after the class — the properties class is not co-located in the action's file.
- `[assembly: RegisterAutomationAction<...>]` is either above the `namespace` in the action's file or in a central registration class.
- Identifier is letters/digits/underscores/dots only and unique in the solution.
- `TProperties` implements `IAutomationActionProperties`; public properties with both getter and setter; sensible defaults; validation rules attached where appropriate.
- Every annotated property has a `Label` and an explicit `Order`.
- `IAutomationProcessData` implementations declare a unique `Identifier` and store non-personal data only.
- All injected dependencies are registered.
- `CancellationToken` is forwarded to every async call (including `GetProcessData` / `SetProcessData`).
- No secrets on `TProperties`.
- `dotnet build` succeeds.
