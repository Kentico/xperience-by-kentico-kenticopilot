# Xperience by Kentico — Automation Concepts

## What Automations Are

Automation processes dynamically interact with contacts via a visual Automation Builder. Each process has a single trigger, a sequence of steps, and terminates at one or more Finish steps. Processes run **per contact**, not globally.

---

## Triggers (3 types)

| Trigger | When it fires | Notes |
|---------|---------------|-------|
| **Form submission** | A specific form is submitted | Most common; supports Form autoresponder email purpose |
| **Registration** | A member becomes active/enabled | Fires after email confirmation if double opt-in is enabled; cannot coexist with Form autoresponder email step |
| **Custom activity** | A specific custom activity is logged for the contact | Requires developer setup; enables chaining and external event bridging. **The trigger matches on activity type only — the activity value is not accessible in the trigger condition, so the activity type itself must carry the semantic meaning.** |

**Key nuance — Registration trigger + member-to-contact mapping:** Automation runs for a _contact_, not the member. XbK maps by email. If the registering member's email doesn't match the current session contact, the process may run for a different or newly created contact — meaning subsequent page visits won't be tracked for the contact in the process.

---

## Steps

### Send Email

- Supported email purposes:
  - **Form autoresponder** — only valid in Form-triggered processes; typically used for double opt-in confirmation links
  - **Automation** — all other automation emails; cannot be used as a form autoresponder in Form configuration (requires an automation process instead)
- Limitation: multilingual emails require a separate form and separate automation process per language

### Wait

- Holds a contact for a fixed duration or until a specific date
- Contacts in Wait steps continue executing even after a process is disabled (potentially hours/days later)

### Condition

- Splits the process into **Yes / No** branches based on a single condition
- **Critical limitation:** only one condition check per step. If/else-if chains require nested branching — becomes complex and hard to manage past 2–3 levels
- Available condition types:
  - `Contact has value in field` (string matching only — contains, starts with, equals, etc. — **no numeric comparison**)
  - `Contact field value is empty`
  - `Contact has visited a page in the last X days`
  - `Contact has performed specific activities in the last X days`
  - `Contact has performed activity with value in the last X days`
  - `Contact has agreed with consent`
  - `Contact is in recipient list`
  - `Contact has clicked on a link in a specific email in the last X days`
  - `Contact has clicked on an email link with a specific URL in the last X days`
  - `Contact is member`

### Set Contact Field Value

- Sets a single **fixed** value on a contact field; not dynamic based on contact state or automation context
- Supports non-string field types (checkboxes, dropdowns, dates render appropriate controls)
- **Dev required:** custom contact fields must be added to the `Set contact field value automation step` UI form in Modules → Contact management → Contact → UI Forms before they appear in this step

### Log Custom Activity

- Logs a specified custom activity on the contact
- **Limitations:** activity type, title, and value are all static — no dynamic values
- Primary uses:
  - **Triggering a downstream automation process** — the logged activity fires another automation's Custom activity trigger, enabling chained sequences
  - **Feeding contact group conditions** — logged activities can drive contact group membership for personalization across channels
  - **Building measurable Customer Journey stages** — logging a named activity at each key step (e.g., `newsletter_reminder_sent`) creates a stage in the Customer Journey view, enabling dropout analysis and AI-assisted journey optimization

### Finish

- Represents a terminal state (successful or not)
- Multiple Finish steps allowed — use distinct names for reporting clarity
- **Critical for recurrence:** for processes with **"If not already running"** recurrence, the contact must reach a Finish step before the process can re-trigger

---

## Process Recurrence

| Mode | Behavior |
|------|----------|
| **Always** | Triggers every time conditions are met, even if already running. Risk: duplicate emails. |
| **Only once** | Runs once per contact, ever. |
| **If not already running** | Re-triggers only after the contact reaches a Finish step. Most balanced for re-engagement. |

---

## Critical Limitations

1. **Condition steps are binary** — one check, yes/no. Complex branching requires nesting, which degrades maintainability quickly.
2. **No numeric comparison in Contact has value in field** — string matching only. Cannot check "points > 400". Workaround: developer logs a custom activity at the threshold.
3. **Set Contact Field Value is static** — the value is configured at build time, not computed at runtime.
4. **Log Custom Activity is static** — type, title, and value cannot be dynamic.
5. **No versioning** — editing a live process with active contacts immediately affects those contacts. Steps with logged statistics cannot be deleted.
6. **Stuck contacts** — if a contact reaches an unconnected step (non-Finish terminal), they stop permanently. Adding a new step later does not resume them.
7. **Not for long-term newsletters** — automation email sequences are fixed at build time. Use recipient lists + regular email sends for ongoing newsletter cadences.
8. **Form autoresponder emails are scoped** — a Form autoresponder purpose email can only be used in a Form-triggered automation. An Automation purpose email cannot serve as a form autoresponder without an automation process.
9. **Disabling doesn't stop in-progress** — contacts in Wait steps continue after disabling. Allow days before assuming a process is fully stopped.
10. **Statistics lag** — step statistics update every 30 minutes (manual refresh available).

---

## Developer Extensibility

**Important constraint:** The Automation Builder step palette is fixed by the platform. Developers **cannot add custom step types** to the UI. All extensibility works _around_ the built-in steps via custom activities, custom contact fields, and external integrations that programmatically log activities automations can respond to.

### Custom Contact Fields

- Extend `Contact management - Contact` via Modules → Classes → Field Editor
- **Marketer unlock:** Contact field conditions and Set Contact Field Value steps become far more powerful with domain-specific fields (e.g., `LoyaltyTier`, `BrewerModel`, `EventAttended`)
- Required dev steps: add DB column, add to `Contact edit` UI form (exposes to Condition steps), add to `Set contact field value automation step` UI form (exposes to Set step)

### Custom Activity Types

- Activity type definitions are created in the Xperience admin UI (Contact Management → Activity types) — no code required. The developer's job is to **log** the activity at the right moment, via `IActivityLogService` server-side or client-side via JavaScript.
- **Marketer unlock:** Once an activity is being logged, marketers can use it as an automation trigger and in Condition steps without any further developer involvement.
- Common patterns: threshold events (loyalty points crossing a level), physical-world bridging (event attendance via QR → form), commerce events, third-party system events via webhook → API → activity log

### Custom Page Builder Widget for Activity Logging

- A configurable Page Builder widget that **conditionally** logs a custom activity when a contact visits a page. Reference implementation: [ConditionalCustomActivityWidget](https://github.com/Kentico/xperience-by-kentico-labs-automations-exploration/tree/v1.0.0/examples/DancingGoat/Components/Widgets/ConditionalCustomActivityWidget)
- Condition types: contact has previously performed a specific activity, or contact is in a specific contact group
- Execution modes: Always, Once (log only the first time the condition is met), or Disabled
- **Marketer unlock:** Once installed, marketers place and configure the widget on any page without further developer involvement. Invisible to visitors; only fires on live page views, not in edit or preview mode.

### Member Field → Contact Field Mapping

- Configure `MemberFieldMappings` to automatically copy member registration fields to contact fields
- **Marketer unlock:** Data collected at registration becomes immediately available in automation conditions

### Webhook / API-triggered Activity Logging

- External systems (CRM, e-commerce, event platforms) call an XbK API endpoint that logs a custom activity
- **Marketer unlock:** Automation responds to real-world external events after one-time developer setup

---

## Quick Reference — Marketer Can Do Without a Developer

- Create and configure any automation process
- Set triggers (Form, Registration, Custom activity — if activity type already exists)
- Use any built-in condition type
- Send emails, add Wait steps, log pre-existing custom activities, set pre-existing contact field values
- Create new custom activity types
- Add custom contact fields (and expose them to automation steps via the Modules UI)
- Map member fields to contact fields via Form Builder form field configuration

## Quick Reference — Requires a Developer

- Enable numeric contact field comparisons (must encode as threshold activities — developer owns the numeric logic)
- Build reusable activity-logging Page Builder widgets
- Integrate external system events via webhook → custom activity
- Automatically map member fields to contact fields in custom code (beyond Form Builder configuration)
