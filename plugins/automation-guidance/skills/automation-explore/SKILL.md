---
name: "automation-explore"
description: "Discovers marketing automation opportunities by analyzing business goals, contact journeys, and current workflows. Suitable for both marketers and developers planning automation strategies in Xperience by Kentico."
argument-hint: "Optional: describe your business, industry, marketing goals, or a specific workflow you want to automate"
compatibility: "Requires Kentico Docs MCP"
---

You are tasked with helping the user discover high-value use cases for the Automation feature in Xperience by Kentico.

## Input Parameters

- **Business Context** _(optional)_ — A description of the user's business, industry, marketing goals, current workflows, or a specific process they want to automate.

## Gotchas

- **Scheduled and time-based triggers do not exist.** There is no built-in way to trigger an automation on a schedule or after a period of inactivity. Use cases like "send a reminder if they haven't acted in N days" or "run weekly for dormant contacts" require a developer to build a background job that logs a custom activity. Always flag these as requiring developer involvement.
- **Contact attribute changes are not triggers.** There is no "when a contact field changes" trigger. Use cases that conceptually start from a status change (e.g., "when a contact becomes a customer") require the system making that change — a form, a purchase flow, or an integration — to also log a custom activity. Developer involvement required.
- **Page visits are not triggers.** Tracking a page visit in an automation requires a developer-built activity-logging Page Builder widget or server-side code on the target page.

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

2. Read `../_shared/references/automation-concepts.md` to understand the available trigger types, step types, conditions, and actions.

3. Read `references/use-cases.md` to familiarize yourself with common automation patterns.

4. If the user has not described their context, ask focused questions to understand:
   - What industry or type of business do they operate in?
   - Who are their primary contacts — prospects, customers, event attendees, subscribers?
   - What manual marketing processes are repeated often or feel slow?
   - What contact lifecycle stages do they manage (e.g., lead → trial → customer → renewal)?
   - Are there specific goals: increase conversions, reduce churn, improve engagement, save team time?

5. Analyze the business context and identify automation opportunities. For each opportunity, evaluate:
   - What event or condition would trigger the automation?
   - What outcome does it deliver for the business or the contact?
   - Is it achievable with Xperience's built-in automation capabilities?
   - What is the likely business impact (High / Medium / Low)?

6. Present your findings as a prioritized list of automation use-case candidates.

## Rules

- Use plain language throughout — avoid Xperience-specific class names or API terms.
- Prioritize use cases that deliver clear business value and are achievable without custom development.
- If a valuable use case requires custom code extensions (custom triggers, conditions, or actions), include it but flag it clearly.
- Do not assume the user has any prior knowledge of Xperience's Automation feature.

## Output Format

Conclude with the following structure:

```markdown
# Automation Opportunities for [Business/Project Name]

## Recommended Use Cases

### 1. [Use Case Name] — [Simple / Moderate / Complex]
**Trigger**: [What starts this automation, in plain language]
**Outcome**: [What it achieves for the business or the contact]
**Value**: [Why this matters — quantify if possible]

### 2. [Use Case Name] — ...

...

## Next Steps

To design any of these automations in detail, run:

/automation-scope [use case name or description]
```
