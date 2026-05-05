# Email Builder category guidance

## In scope

- email templates
- editable areas
- email sections
- email widgets
- markup strategy and layout rules

## Discovery anchors

- email builder component registrations
- email template and editable area definitions
- MJML or HTML email component views
- property models and validation definitions

## Consistency priorities

- one markup strategy across the project unless there is a clearly documented exception
- stable identifiers and registration metadata
- file, folder, and class naming follows one repeatable convention across similar component types
- consistent use of typed property models and validation
- reusable section and widget layout patterns
- editable areas and widget zones structured consistently

## Evidence to capture

- whether the project mixes MJML and HTML email strategies
- component registration patterns
- file paths and class names for equivalent components to verify naming convention alignment
- examples of section and widget layout composition
- property model and default value patterns

## Common high-value findings

- mixed markup strategies without clear boundaries
- repeated but structurally inconsistent section layouts
- inconsistent registration naming or metadata
- property models present for some email components but omitted for peers

## Recommendation style

Favor a single predictable email component model that minimizes rendering surprises and makes future AI-generated email components match existing project patterns.

---

## Platform-specific checks

> **Docs source**: Email Builder documentation is not available at a stable hardcoded URL. Use the Kentico Docs MCP server (`kentico.docs.mcp`) to retrieve current documentation before performing this category's analysis. Search for "Email Builder", "email widgets", "email sections", and "email templates".

### General structural checks (apply regardless of markup strategy)

- **Markup strategy consistency**: the project should use either MJML-based email markup or plain HTML — mixing both without a documented architectural boundary is a significant risk. Inspect email component views to determine the active strategy and flag any deviations.
- **Email components NOT in Areas**: like Page Builder components, email builder components must be at application root scope. Files in Areas may cause view resolution failures.
- **Identifier prefix consistency**: check all email widget, section, and template registrations for a stable `CompanyName.ComponentName` prefix pattern. Inconsistent prefixes complicate deployability.
- **Property models present where needed**: email sections and widgets that have configurable content should have a typed properties model. Flag components with configurable behavior that lack a properties class while comparable components have one.
- **Registration metadata completeness**: check that `Name`, `Description`, and `IconClass` are consistent across all email component registrations. Missing descriptions degrade the editor experience.

### Zone and layout structure checks

- **Every email section must contain at least one editable zone**: equivalent to the Page Builder requirement. Sections without zones cannot hold content and are unusable.
- **Named zones for consistent transfer**: if the project has multiple section types, zones should use consistent names across sections so content transfers predictably when editors change section types.
- **Widget zone restrictions**: check whether any email sections define widget allow-lists and whether those allow-lists are intentional and consistent across equivalent sections.

### Editing component checks (shared with Page Builder)

- **Properties dialogs use correct namespace**: editing component attributes on email component properties classes must come from `Kentico.Xperience.Admin.Base.FormAnnotations` (same as Page Builder). Check that no email component properties accidentally reference `Kentico.Forms.Web.Mvc`.
- **Custom form components with content links**: if email component properties use custom selectors that link to content items, a reference extractor is required for usage tracking. Flag any such components that lack one.

### Asset and script checks

- **No `DOMContentLoaded`-only script initialization**: email builder components rendered in the editor may be inserted dynamically. Script initialization must use the same guard pattern as Page Builder: check `document.readyState` before binding to `DOMContentLoaded`.
- **No duplicate scripts in views**: linking scripts in component views causes duplication when multiple component instances appear in the same email.
