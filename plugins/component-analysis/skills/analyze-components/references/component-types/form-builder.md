# Form Builder category guidance

## In scope

- form components
- form sections
- form validation rules
- form visibility conditions
- supporting scripts or styles used by custom builder components

## Discovery anchors

- form component registrations
- form section registrations
- validation rule registrations
- visibility condition registrations
- properties classes and editing component annotations
- client assets paired with custom builder components

## Consistency priorities

- stable unique identifiers and consistent registration metadata
- file, folder, and class naming follows one repeatable convention across similar component types
- typed properties and validation patterns are used consistently
- view responsibilities are separated from business logic in a repeatable way
- editor and live-site behavior stay aligned for comparable components
- scripts and styles are included and scoped using one strategy

## Evidence to capture

- registration attributes and registry locations
- file paths and class names for equivalent components to verify naming convention alignment
- property model shapes and defaults
- validation rule usage and rule coverage for required inputs
- evidence of editor versus live-site handling

## Common high-value findings

- similar components using different registration or file layout conventions
- inconsistent null-safety and default values across form components
- validation rules applied unevenly to comparable component types
- front-end assets loaded inconsistently across builder components

## Recommendation style

Prefer a repeatable creation pattern for every new Form Builder component family so future implementations look mechanically similar.

---

## Platform-specific checks (sourced from Kentico docs)

> **Critical namespace distinction**: Form Builder components use the `Kentico.Forms.Web.Mvc` namespace. Admin UI form components use `Kentico.Xperience.Admin.Base.Forms` / `Kentico.Xperience.Admin.Base.FormAnnotations`. These are completely separate class hierarchies with identically named types (`FormComponent`, `ValidationRule`, etc.). **Mixing namespaces is a common and consequential error** — flag any file that mixes them.

### Form components — class structure

- **Base class**: `FormComponent<TProperties, TValue>` from `Kentico.Forms.Web.Mvc`. `TProperties` must derive from `FormComponentProperties<TValue>` with the same `TValue` type. Type mismatch causes a runtime error.
- **`FormComponentProperties<TValue>` constructor**: must call the base constructor with a `FieldDataType` value (`FieldDataType.Text`, `FieldDataType.Integer`, etc.). For `Text`, a `size` parameter is also required (e.g., `base(FieldDataType.Text, size: 200)`). Missing size for text fields causes database column misconfiguration.
- **`[BindableProperty]` attribute**: properties used for model binding from input elements must be decorated with `[BindableProperty]`. Properties without it will not be bound.
- **`GetValue()` and `SetValue()` must be overridden**: `GetValue()` composes the final field value (potentially from multiple bindable properties); `SetValue()` restores state. Omitting overrides causes data loss on form submission or re-render.
- **`DefaultValue` property**: should be overridden in the properties class with an appropriate editing component attribute. Omitting `DefaultValue` means editors cannot configure a default in the Form Builder UI.
- **`CustomAutopostHandling` property**: for components that construct their value from multiple inputs, override `CustomAutopostHandling` to `true` to prevent premature form auto-submission. Then manually trigger evaluation via `window.kentico.updatableFormHelper.updateForm(this.form)` at the right time.
- **NOT in Areas**: same constraint as Page Builder components — files must be at application root scope.
- **Identifier format**: `CompanyName.ModuleName.ComponentName`. Check all form component identifiers for a consistent company prefix.

### Form components — view

- **Partial view location**: `~/Views/Shared/FormComponents/_<Identifier>.cshtml` (with `.` replaced by `_` in the default convention). Specifying a custom `ViewName` in the registration attribute is valid, but check for consistency across components.
- **`ViewData.Kentico().GetEditorHtmlAttributes()` must be called**: this retrieves system HTML attributes required for correct functionality. The attributes must be applied to every input element. Omitting this breaks admin UI rendering and field tracking.
- **Asset placement**: scripts and styles for live site go under `~/wwwroot/FormBuilder/Public/`; admin-specific assets under `~/wwwroot/FormBuilder/Admin/`. Use component-identifier subfolders or a `Shared` subfolder. Check that projects follow one placement strategy consistently.
- **Script initialization pattern**: same `DOMContentLoaded` + dynamic-insertion guard required as for Page Builder components. Flag any component views that only bind to `DOMContentLoaded`.
- **No duplicate scripts in views**: linking scripts directly in component views causes duplication when multiple form fields use the same component.

### Form sections — structure and zones

- **`FormZoneAsync()` or `<form-zone />` required**: every section must contain at least one zone. Sections without a zone cannot hold fields and are unusable.
- **Named zones for consistent transfer**: use `FormZoneAsync("zone-name")` and reuse the same identifiers across comparable section types so fields transfer predictably when a user changes section type in the editor.
- **`RegisterFormSection` assembly attribute**: required parameters are `identifier` and `name`; `CustomViewName` is required unless the default view convention is followed (`_<Identifier>.cshtml` in `~/Views/Shared/Sections/`). `PropertiesType` is required when properties are used.
- **File organization**: recommended layout is `~/Components/FormSections/<SectionName>/` for view, view component, and properties class together.
- **View component signature for sections with properties**: `InvokeAsync(FormSectionViewModel<TSectionPropertiesClass> sectionProperties)` — not a plain `InvokeAsync()`. Missing generic parameter means properties are unavailable in the view.
- **NOT in Areas**: file must be at application root scope.
- **Asset placement**: `~/wwwroot/FormBuilder/Public/<SectionName>/` and `~/wwwroot/FormBuilder/Admin/<SectionName>/`.

### Validation rules — Form Builder vs. Admin UI

- **Form Builder validation rules** are separate from Admin UI validation rules. Form Builder uses classes from `Kentico.Forms.Web.Mvc`; Admin UI uses `Kentico.Xperience.Admin.Base.Forms`. They are not interchangeable.
- **Registration**: `RegisterFormComponent` (not `RegisterFormValidationRule`) is used for Form Builder components. Validation rules for Form Builder fields are a separate concept registered via the Form Builder's own validation rule system — check that any custom validation rules are registered against the correct subsystem.
- **Editor-applied vs. attribute-applied validation**: for Form Builder components, editors configure validation via the Form Builder UI. In Page Builder and Admin UI model-based forms, validation is applied via C# attribute notation. Check that projects use the appropriate strategy for their context and apply it consistently.

### Visibility conditions — Form Builder vs. Admin UI

- **Form Builder visibility conditions** also live in the `Kentico.Forms.Web.Mvc` namespace and are separate from Admin UI visibility conditions. The architectural pattern is similar (field dependencies, `Evaluate()` method) but the base classes are different.
- **Field dependency ordering**: visibility conditions on a Form Builder field can only depend on fields that precede the current field. Check `Order` values to confirm ordering is intentional and consistent.
- **Performance**: field-dep conditions re-evaluate on every input change. Flag `Evaluate()` methods with heavy logic.
