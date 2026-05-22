---
name: "automation-scope"
description: "Scopes a specific marketing automation use case into a structured plan aligned to Xperience by Kentico's Automation feature. Maps the use case to triggers, conditions, steps, and actions."
argument-hint: "Describe the automation use case to scope, or reference a use case from /automation-explore output"
compatibility: "Requires Kentico Docs MCP"
---

You are tasked with scoping a specific automation use case into a structured implementation plan for Xperience by Kentico's Automation feature.

## Input Parameters

- **Use Case** — A description of the automation to scope. Can be a use case from `/automation-explore`, a free-form description, or a marketing workflow the user wants to automate.
- **Output Path** _(optional)_ — Folder where the scope document should be saved. Defaults to the current directory.

## Gotchas

- **Only 3 trigger types exist: Form submission, Registration, and Custom activity.** There is no scheduled trigger, no contact attribute change trigger, and no page visit trigger. If the use case implies any of these, it requires a developer to log a custom activity at the right moment before the marketer can build the automation.
- **Custom activity trigger matches on activity type only.** The activity value is not accessible at the trigger — the activity type name must carry the full semantic meaning (e.g., `purchase_completed`, not a generic `ecommerce_event`).
- **Condition steps evaluate one condition at a time.** Each Condition step produces a Yes/No branch. Multi-condition logic requires nesting, which becomes unmaintainable past 2–3 levels. Flag heavily nested designs explicitly in the scope document.
- **Set Contact Field Value is static.** The value is fixed at design time. Scenarios requiring a runtime-computed value (e.g., set a date to today, copy one field's value to another) are not achievable with the built-in step and require developer involvement.
- **No numeric comparison in conditions.** `Contact has value in field` supports string matching only. Score thresholds and date arithmetic require a developer to encode the logic as a custom activity.

## Steps To Follow

1. Verify that Kentico Docs MCP is available by attempting to fetch the automation overview page listed in `../_shared/references/docs.md`. If the MCP tool is not available or the request fails, stop immediately and tell the user:

   > This skill requires the Kentico Docs MCP to be configured. Ensure the plugin's `.mcp.json` is loaded by your AI assistant, or add the following server to your MCP configuration manually:
   > ```json
   > "kentico.docs.mcp": {
   >   "type": "http",
   >   "url": "https://docs.kentico.com/mcp"
   > }
   > ```

   Do not proceed until the MCP is available and the fetch succeeds.

2. Read `../_shared/references/automation-concepts.md` to understand available trigger types, step types, conditions, and actions.

3. If the use case is unclear or incomplete, ask focused questions:
   - What event or action starts the automation? (e.g., form submission, member registration, or a non-form event such as a purchase or page visit — the latter requires a developer to log a custom activity)
   - Which contacts should enter — all contacts, or those matching specific attributes?
   - What should happen at each stage of the process?
   - Are there decision points? (e.g., "if they opened the email, do X; otherwise, do Y")
   - What contact data should be updated along the way?
   - What emails or internal notifications need to be sent?
   - Should contacts be able to re-enter the process?

4. Map the use case to Xperience automation concepts:
   - Select the most appropriate trigger type
   - Define each step in sequence (action, condition/branch, wait)
   - Identify all contact attribute reads and writes
   - List required dependencies: email templates, forms, consent configurations, contact attributes

5. Read `assets/SCOPE-TEMPLATE.md` and fill in all sections. Keep plain-language descriptions in the Summary and Steps Overview. Technical implementation notes belong in the Steps Detail section.

6. Before saving, verify:
   - The trigger type is exactly one of: Form submission, Registration, or Custom activity. If any other trigger type appears, correct it or add an explicit developer-requirement note.
   - Every branch path in the Steps Detail ends at a named Finish step.
   - No unfilled placeholder text (`[...]`) remains in the template.
   - Every contact field, custom activity type, and email template referenced in the steps appears in the Prerequisites checklist.

7. Save the completed scope document to the output path as `[use-case-name]-automation-scope.md`, where the filename is a kebab-case slug derived from the use case name.

8. Present a brief summary of the scoped automation to the user.

## Rules

- Use plain language in the Summary, Contact Scope, and Steps Overview sections — these must be readable by marketing stakeholders.
- The Steps Detail section may include notes relevant to Xperience configuration and implementation.
- Flag any required dependency (email template, form, consent, contact attribute) that does not yet exist in Xperience.
- If the use case cannot be fully achieved with built-in Xperience automation capabilities, state this clearly and describe what custom code extension would be required.
- Do not produce a scope document for a use case that is fundamentally incompatible with Xperience automations — instead, explain why and suggest the closest alternative approach.

## Output Format

Conclude with:

```
Scope document saved to: [file path]

Summary:
- Trigger: [type and condition]
- Steps: [count and types]
- Dependencies: [list or "none"]
- Complexity: [Simple / Moderate / Complex]
```
