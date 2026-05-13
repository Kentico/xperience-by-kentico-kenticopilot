# Admin UI category guidance

## In scope

- applications
- UI pages and page templates
- page commands
- page extenders
- admin UI form components
- admin UI validation rules and visibility conditions
- custom client React modules used by the administration interface

## Discovery anchors

- `RegisterFormComponent`
- `ClientComponentName`
- `FormComponent`
- `FormComponentCommand`
- UI page registrations and page template registrations
- admin client module package names and exported React components

## Consistency priorities

- server-side definitions and client-side React components are coherently paired
- `ClientComponentName` values follow one stable naming convention
- file, folder, and class naming follows one repeatable convention across similar component types
- exported React component names match Kentico loading conventions, including required suffixes where applicable
- form component properties, client properties, and back-end component types follow repeatable patterns
- editing components, validation rules, and visibility conditions use a consistent registration strategy
- page commands and extenders keep logic focused and do not become cross-cutting dumping grounds
- admin client code uses consistent typing and module contracts

## Evidence to capture

- registration attributes and registration files
- paired back-end and front-end file paths
- file paths and class names for equivalent components to verify naming convention alignment
- examples of command handlers or client command invocations
- repeated naming patterns and outliers

## Common high-value findings

- mixed conventions for pairing server classes with React modules
- inconsistent module naming that makes component loading harder to predict
- form components without typed property classes where peers have them
- command handlers with inconsistent async patterns or response shapes
- similar admin customizations implemented through different extension points without a clear rule

## Recommendation style

Favor target patterns that make future admin customizations easy for an agent to copy, such as one registration location, one naming convention, one file layout, and one server-client pairing model.

---

## Platform-specific checks (sourced from Kentico docs)

### Form components — class structure

- **TType generic must match exactly**: `FormComponent<TProperties, TClientProperties, TType>` — `TType` must be identical to the C# type used by the editing component, including nullability (e.g., `int` vs `int?`). Mismatch causes runtime failures.
- **TClientProperties is a DTO only**: should contain only properties needed on the client; no logic. If no client properties are needed, inherit `FormComponent<TProperties, TType>` instead.
- **`ConfigureClientProperties()` must be implemented**: responsible for mapping back-end properties to what the client receives. Missing or incomplete implementations cause client-side UI failures.
- **`ComponentAttribute` ties the class to its attribute**: the component class must be decorated with `[ComponentAttribute(typeof(MyComponentAttribute))]` for use as an editing component.
- **Identifier format**: `CompanyName.ModuleName.ComponentName` — check for consistent prefix across all registered form components.
- **Namespace**: must use `Kentico.Xperience.Admin.Base.Forms` and `Kentico.Xperience.Admin.Base.FormAnnotations`. **Do NOT use `Kentico.Forms.Web.Mvc`** — it contains identically named classes (`FormComponent`, `FormComponentAttribute`) that serve a different purpose (Form Builder). Using the wrong namespace causes silent behavior differences.

### Form components — client module pairing

- **`ClientComponentName` format**: must be `@orgName/projectName/componentName`. The admin client application auto-appends the `FormComponent` suffix when loading, so the exported React component name must match `<componentName>FormComponent` exactly.
- **All components exported from `entry.tsx`**: every custom form component, validation rule client, visibility condition, and UI page template must be exported via the module's `entry.tsx`. Missing exports cause 404 failures at runtime with no compile-time warning.
- **No dynamic imports (`import()`)**: webpack code-splitting via dynamic imports is not supported in Xperience admin client modules. All component code must be in the main bundle. Dynamic imports may load silently in dev but fail unpredictably in production.
- **`@kentico/xperience-admin-*` package versions**: `@kentico/xperience-admin-base`, `@kentico/xperience-admin-components`, and `@kentico/xperience-webpack-config` must be kept in sync with each other and with the Xperience version. Flag version drift across multiple admin client modules.
- **`RegisterClientModule("orgName", "projectName")` in `Module.cs`**: required for the client module to be recognized. The org/project names must match `AdminOrgName` and `ProjectName` in the `.csproj` and `orgName`/`projectName` in `webpack.config.js` exactly.

### Validation rules — structure and pairing

- **Both server class and TypeScript client component are required**: `ValidationRule<TProperties, TClientProperties, TType>` on the back end, plus a matching `ValidationRule<TProps, TType>` TypeScript export. Rules without a client component will fail client-side validation silently.
- **`TType` must match exactly including nullability**: a rule defined for `int?` cannot be applied to `int` fields.
- **`ClientRuleName` format**: same `@orgName/projectName/Name` convention as form components. The value must match the exported TypeScript component name exactly.
- **`GetDescriptionText()` must be implemented** on `TProperties` (i.e., the class derived from `ValidationRuleProperties`). Missing implementation causes a compile error; a placeholder but unhelpful string is a quality issue.
- **Error message priority**: `Validate()` return value overrides `Properties.ErrorMessage` which overrides `DefaultErrorMessage`. Check that the strategy is consistent — if rules return error messages directly from `Validate()`, hiding `ErrorMessage` from the properties dialog is the correct pattern.
- **`ValidationRuleAttribute(typeof(AttrClass))` binding**: the rule class must be decorated with this attribute when a corresponding attribute class exists. Attribute class property names must exactly mirror rule properties — the system uses property names for instantiation. Name mismatches cause silent runtime errors.
- **Namespace**: must use `Kentico.Xperience.Admin.Base.Forms` / `Kentico.Xperience.Admin.Base.FormAnnotations`. **Do NOT use `Kentico.Forms.Web.Mvc`** — it contains `ValidationRule` and `RegisterFormValidationRule` with matching names but different semantics.

### Visibility conditions — structure and dependencies

- **No-field-dependency vs. field-dependency**: conditions without field dependencies inherit `VisibilityCondition` or `VisibilityCondition<TProperties>`; conditions with field dependencies use `VisibilityConditionWithDependency<TProperties>` and `VisibilityConditionWithDependencyProperties`.
- **`DependsOnFields` must be overridden** for multi-field dependencies. For single-field dependencies, using `VisibilityConditionWithDependency<TProperties>` handles this automatically via `VisibilityConditionWithDependencyProperties.PropertyName`.
- **Field-dep conditions can only reference preceding fields**: conditions evaluate fields in `Order` sequence. A condition assigned to property C cannot depend on a property with `Order` greater than C's `Order`. Flag conditions that may violate ordering.
- **Performance**: field-dep conditions re-evaluate on every change to the linked field. Flag any `Evaluate()` methods with non-trivial logic (database calls, network I/O) — these must be cached externally or refactored to a lighter check.
- **`TargetFieldType` required for field-dep conditions** in `RegisterFormVisibilityCondition`: limits which field types are offered as dependencies in the field editor. Missing `TargetFieldType` allows incompatible type assignments.
- **`VisibilityConditionAttribute(typeof(AttrClass))` binding**: required on the condition class when a corresponding attribute class exists. Attribute class property names and types must mirror the condition's properties class exactly.
- **Multiple conditions = AND logic**: a property with multiple visibility condition attributes is visible only when ALL conditions pass. Check for contradictory rulesets that permanently hide fields.
- **Namespace**: must use `Kentico.Xperience.Admin.Base.Forms` and `Kentico.Xperience.Admin.Base.FormAnnotations`. **Do NOT use `Kentico.Forms.Web.Mvc`** — contains same-named classes for a different subsystem.

### UI pages — registration and structure

- **`UIPage(parentType, slug, uiPageType, name, templateName, order)` assembly attribute**: all six positional parameters are required. Missing `parentType` breaks the URL hierarchy; missing `templateName` causes a blank page render.
- **`TClientProperties` must extend `TemplateClientProperties`**: `Page<TClientProperties>` — if the properties class doesn't extend `TemplateClientProperties`, the system cannot hydrate client properties.
- **Template name format**: `@orgName/projectName/templateName` — the client app auto-appends `Template` when loading, so the exported React component must be named `<templateName>Template`. Component name mismatch causes a blank page with no server error.
- **`ConfigurePage()` vs. `ConfigureTemplateProperties()` separation**: `ConfigurePage()` is called on initial load and before every page command — it is the right place for page state and permission checks. `ConfigureTemplateProperties()` is called only once (initial load) to set client property defaults. Putting data fetching in the wrong method causes stale UI or performance issues.
- **`UIPageLocation`, `UIBreadcrumbs`, `UINavigation` used consistently for dialogs**: pages opened as modal dialogs should typically use `[UIPageLocation(PageLocationEnum.Dialog)]`, `[UIBreadcrumbs(false)]`, and `[UINavigation(false)]`. Check for dialog pages missing these attributes, which produces confusing navigation states.
- **Parameterized slugs use `PageParameterConstants.PARAMETERIZED_SLUG`**: hardcoding parameter patterns in slugs is not supported.
- **`IPageLinkGenerator.GetPath()` for URL generation**: check for hardcoded admin URL strings. Workspace-scoped pages require including `WorkspaceID` in the `PageParameterValues` passed to `GetPath`.
- **Use System → UI tree to find `uiPageType`** when extending default system pages rather than guessing class names.
