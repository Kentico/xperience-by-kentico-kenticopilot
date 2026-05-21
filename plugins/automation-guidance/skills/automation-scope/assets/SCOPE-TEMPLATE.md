# Automation Scope: [Use Case Name]

## Summary

**Goal**: [One sentence: what this automation achieves for the business]
**Primary Audience**: [Who benefits — marketing team, sales team, contacts, or all three]
**Complexity**: [Simple / Moderate / Complex]

---

## Trigger

**Trigger Type**: [Form submission / Contact attribute change / Scheduled / Manual]
**Trigger Details**: [Specific form name, attribute and value, schedule, or condition that starts the automation]

---

## Contact Scope

**Who enters this automation?**
[Description of which contacts are eligible — e.g., "Contacts who submit the Newsletter Sign-Up form and do not already have the Subscriber attribute set to true"]

**Re-entry allowed?** [Yes / No / After X days]

---

## Steps Overview

| # | Step Name | Type | Description |
|---|-----------|------|-------------|
| 1 | [Name] | [Action / Condition / Wait] | [Brief description] |
| 2 | [Name] | [Action / Condition / Wait] | [Brief description] |
| 3 | ... | | |

---

## Step Detail

### Step 1 — [Name]

- **Type**: [Action / Wait / Condition]
- **Configuration**: [What to configure — email template name, wait duration, attribute name and value to check, etc.]
- **True / On success**: Proceed to Step 2
- **False / On failure**: [End process / Jump to Step X / Alternate path description]

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

| # | Template Name | Sent At Step | Notes |
|---|---------------|--------------|-------|
| 1 | [Name] | Step [#] | [e.g., Requires marketing consent] |

---

## Success Criteria

[How will you measure whether this automation is working? Examples: email open rate, percentage of contacts completing the full process, reduction in manual follow-up effort, conversion rate improvement.]

---

## Prerequisites

Before implementing this automation, the following must exist in Xperience:

- [ ] [Required form — e.g., "Newsletter Sign-Up form created in Forms"]
- [ ] [Required email templates — e.g., "Welcome Email template created in Email marketing"]
- [ ] [Required contact attributes — e.g., "Subscriber (boolean) attribute added to the contact model"]
- [ ] [Required consent — e.g., "Newsletter consent configured and linked to email sending"]
