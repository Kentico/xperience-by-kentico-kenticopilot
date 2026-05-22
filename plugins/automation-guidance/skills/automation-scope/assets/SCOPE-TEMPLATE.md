# Automation Scope: [Use Case Name]

## Summary

**Goal**: [One sentence: what this automation achieves for the business]
**Primary Audience**: [Who benefits — marketing team, sales team, contacts, or all three]
**Complexity**: [Simple / Moderate / Complex]

---

## Trigger

**Trigger Type**: [Form submission / Registration / Custom activity]
**Trigger Details**: [Specific form name, or custom activity type name and what causes it to be logged]
**Developer setup required?** [No — for Form submission and Registration triggers / Yes — describe what the developer needs to build]

---

## Process Recurrence

**Recurrence Mode**: [Always / Only once / If not already running]
**Rationale**: [Why this mode is appropriate — e.g., "Only once, because this is a one-time onboarding sequence" or "If not already running, to allow re-entry after completing the process without risking duplicate emails"]

---

## Contact Scope

**Who enters this automation?**
[Description of which contacts are eligible — e.g., "Contacts who submit the Newsletter Sign-Up form and do not already have the Subscriber attribute set to true"]

**Re-entry allowed?** [Yes / No / After X days]

---

## Steps Overview

| # | Step Name | Type | Description |
|---|-----------|------|-------------|
| 1 | [Name] | [Send Email / Set Contact Field Value / Wait / Condition / Log Custom Activity / Finish] | [Brief description] |
| 2 | [Name] | | |
| 3 | ... | | |

---

## Step Detail

### Step 1 — [Name]

- **Type**: [Send Email / Set Contact Field Value / Wait / Condition / Log Custom Activity / Finish]
- **Configuration**: [What to configure — email template name, wait duration, field name and value, condition type and parameters, etc.]
- **True / On success**: Proceed to Step 2
- **False / On failure**: [Proceed to Step X / End at Finish step — name it]

### Step 2 — [Name]

- **Type**: ...
- **Configuration**: ...
- **Next**: ...

---

## Contact Attribute Changes

| Attribute Name | Changed At Step | New Value |
|----------------|-----------------|-----------|
| [Name] | Step [#] | [Value] |

---

## Email Communications

| # | Template Name | Sent At Step | Email Purpose | Notes |
|---|---------------|--------------|---------------|-------|
| 1 | [Name] | Step [#] | [Automation / Form autoresponder] | [e.g., Requires marketing consent] |

---

## Chained Automations

[Leave blank if this automation stands alone. Otherwise, describe any automations this process triggers via Log Custom Activity, or any upstream automation that triggers this one.]

---

## Success Criteria

[How will you measure whether this automation is working? Examples: email open rate, percentage of contacts completing the full process, reduction in manual follow-up effort, conversion rate improvement.]

---

## Prerequisites

Before implementing this automation, the following must exist in Xperience:

- [ ] [Required form — e.g., "Newsletter Sign-Up form created in Forms"]
- [ ] [Required email templates — e.g., "Welcome Email template created in Email marketing"]
- [ ] [Required contact attributes — e.g., "Subscriber (boolean) attribute added to the contact model and exposed to automation steps"]
- [ ] [Required consent — e.g., "Newsletter consent configured and linked to email sending"]
- [ ] [Required custom activity type — e.g., "`purchase_completed` activity type registered and logging implemented by developer" — only if trigger or steps use a custom activity]
