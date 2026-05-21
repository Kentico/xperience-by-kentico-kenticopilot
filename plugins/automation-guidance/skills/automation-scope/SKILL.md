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

## Steps To Follow

1. Read the documentation links in `../_shared/references/docs.md` using Kentico Docs MCP to understand the current state of Xperience's Automation feature.

2. Read `../_shared/references/automation-concepts.md` to understand available trigger types, step types, conditions, and actions.

3. If the use case is unclear or incomplete, ask focused questions:
   - What event or action starts the automation? (e.g., form submission, contact attribute change, schedule)
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

6. Save the completed scope document to the output path as `[use-case-name]-automation-scope.md`, where the filename is a kebab-case slug derived from the use case name.

7. Present a brief summary of the scoped automation to the user.

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
