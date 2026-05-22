---
name: automation-context
description: Loads complete Xperience by Kentico (XbK) Automation feature context into the agent, enabling expert assistance with automation review, design, brainstorming, and developer extensibility guidance. Trigger whenever the user asks about XbK automations, wants to review an existing automation process, brainstorm new automation ideas, identify improvement opportunities, or understand what developers can build to unlock more automation value for marketers. Also trigger for any Xperience by Kentico digital marketing workflow that involves triggers, steps, conditions, contacts, or custom activities.
---

# Xperience by Kentico — Automations Context

You now have complete context on the Automation feature in Xperience by Kentico. Use this knowledge to assist with:

- **Review** — auditing existing automation processes for configuration issues, missing best practices, or logic errors
- **Brainstorm** — generating automation ideas given a marketing strategy, campaign, or contact lifecycle scenario
- **Developer unlock** — identifying where custom code creates new automation capabilities for marketers

---

## What Automations Are

Automation processes dynamically interact with contacts via a visual Automation Builder. Each process has a single trigger, a sequence of steps, and terminates at one or more Finish steps. Processes run **per contact**, not globally.

---

## Triggers (3 types)

| Trigger             | When it fires                                        | Notes                                                                                                                                                                                                                                               |
| ------------------- | ---------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Form submission** | A specific form is submitted                         | Most common; supports Form autoresponder email purpose                                                                                                                                                                                              |
| **Registration**    | A member becomes active/enabled                      | Fires after email confirmation if double opt-in is enabled; cannot coexist with Form autoresponder email step                                                                                                                                       |
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
- Available conditions (full list):
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
- Primary use: triggering a downstream automation process, or feeding contact group conditions for personalization

### Finish

- Represents a terminal state (successful or not)
- Multiple Finish steps allowed — use distinct names for reporting clarity (Finish steps differentiate "completed" from "in-progress" in Statistics and Contacts views)
- **Critical for recurrence:** for processes with **"If not already running"** recurrence, the contact must reach a Finish step before the process can re-trigger

---

## Process Recurrence

| Mode                       | Behavior                                                                                   |
| -------------------------- | ------------------------------------------------------------------------------------------ |
| **Always**                 | Triggers every time conditions are met, even if already running. Risk: duplicate emails.   |
| **Only once**              | Runs once per contact, ever.                                                               |
| **If not already running** | Re-triggers only after the contact reaches a Finish step. Most balanced for re-engagement. |

---

## Known Limitations (critical for review and design)

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

## Developer Extensibility — Where Code Unlocks Marketer Value

**Important constraint:** The Automation Builder step palette is fixed by the platform. Developers **cannot add custom step types** to the UI. All extensibility works _around_ the built-in steps via custom activities, custom contact fields, and external application extensions that programmatically initiate triggers automations can respond to, or read state that is set in automations (contact data or custom activities).

The highest-leverage developer contributions for automations:

### 1. Custom Contact Fields

- **What:** Extend `Contact management - Contact` via Modules → Classes → Field Editor
- **Marketer unlock:** Contact field conditions and Set Contact Field Value steps become far more powerful with domain-specific fields (e.g., `LoyaltyTier`, `BrewerModel`, `EventAttended`)
- **Required dev steps:**
  - Add DB column to the Contact class
  - Add field to `Contact edit` UI form (exposes to Condition steps)
  - Add field to `Set contact field value automation step` UI form (exposes to Set step)
  - For custom data types, implement empty value detection for `Contact field is empty` condition

### 2. Custom Activity Types

- **What:** Activity type definitions are created in the Xperience admin UI (Contact Management → Activity types) — no code required for the definition itself. The developer's job is to **log** the activity at the right moment, via `IActivityLogService` server-side or [client-side via JavaScript](https://docs.kentico.com/documentation/developers-and-admins/digital-marketing-setup/set-up-activities/custom-activities#client-side-code).
- **Marketer unlock:** Once an activity is being logged, marketers can use it as an automation trigger and in Condition steps without any further developer involvement.
- **Patterns:**
  - Threshold events (e.g., loyalty points crossing 400 → log `approaching_roaster`)
  - Physical-world bridging (e.g., event attendance via QR → form → lightweight automation → logs `brew_lab_attended`)
  - Commerce events (first purchase, repeat purchase, cart abandonment)
  - Third-party system events via webhook → API → activity log

### 3. Custom Page Builder Widget for Activity Logging

- **What:** A configurable Page Builder widget that conditionally logs a custom activity when a contact visits a page. See the reference implementation: [ConditionalCustomActivityWidget](https://github.com/Kentico/xperience-by-kentico-labs-automations-exploration/tree/v1.0.0/examples/DancingGoat/Components/Widgets/ConditionalCustomActivityWidget)
- **Widget properties (all marketer-configurable once installed):**
  - **Condition type** — what must be true before the activity is logged:
    - `Custom activity`: the contact has previously performed a specific activity type (optionally filtered by activity value)
    - `Contact group`: the contact is a member of a specific group
  - **Action activity type** — which custom activity to log when the condition is met (with optional value to attach)
  - **Execute when** — `Always` (log on every qualifying visit), `Once` (log only the first time the condition is met, then skip), or `Disabled` (suppress logging without removing the widget)
- **Marketer unlock:** Once installed, marketers place the widget on any page and configure all properties without further developer involvement. The widget is invisible to visitors and only fires on live page views — not in edit or preview mode.
- **Patterns this enables:**
  - Log `pricing_page_visited` when any contact lands on the pricing page → triggers a sales notification automation
  - Log `re_engaged_with_product` only when the contact is already in the "Active Trial" contact group → advances a trial nurture chain without spamming non-trial contacts
  - Log `deep_content_consumer` only after the contact has already performed `watched_demo` → gates a content drip on confirmed prior engagement
  - Use `Once` to ensure each contact triggers the downstream automation exactly once, regardless of how many times they visit the page

### 4. Member Field → Contact Field Mapping

- **What:** Configure `MemberFieldMappings` to automatically copy member registration fields to contact fields
- **Marketer unlock:** Data collected at registration (preferences, product interests, referral source) becomes immediately available in automation conditions without manual steps

### 5. Webhook / API-triggered Activity Logging

- **What:** External systems (CRM, e-commerce, event platforms) call an XbK API endpoint that logs a custom activity
- **Marketer unlock:** Automation responds to real-world events (purchase completed in Shopify, support ticket resolved in Zendesk) without the marketer needing to touch external systems after setup

---

## Proven Scenario Patterns

### Pattern A — Double Opt-In Reminder (marketer-only)

Trigger: Form submission → Wait (2 days) → Condition: in recipient list? → No: Send reminder email → Finish

### Pattern B — Post-Purchase Onboarding (dev: custom contact field + form mapping)

Trigger: Registration form → Wait (1 day) → Condition: Brewer model field contains "X" → Send product-specific guide → Wait (5 days) → Condition: clicked guide email? → Branch: deep content vs. FAQ → Finish

### Pattern C — Engagement-Gated Content Drip (dev: custom activities + page widget)

Chain of automations, each triggered by a custom activity logged by the previous one when engagement is confirmed. Non-engagement sends a nudge; engaging with the nudge logs the same activity. The chain advances only on real engagement.

### Pattern D — Physical Event Attendance (dev: custom activity; marketer builds QR page)

Trigger: RSVP form → Send confirmation → Wait (until day before) → Send reminder → Wait (until day after) → Condition: `brew_lab_attended` activity logged? → Attendee: thank-you + discount / No-show: next event promo → Finish

### Pattern E — Loyalty Tier Progression (dev: threshold activity hooks)

Developer owns numeric logic and fires activity at thresholds (`approaching_roaster` at 400pts, `tier_roaster_reached` at 500pts, etc.). Marketer builds one automation per activity. Tier-achievement automations use Set Contact Field Value to write confirmed tier to the contact record, making it available for personalization and contact group segmentation across all channels.

---

## Best Practices

### Name every step descriptively

Default step names describe the step _type_. Good names describe what the step _does in this automation_. This is the difference between a process you have to open and trace vs. one you can read at a glance.

| Default name | Good name                    |
| ------------ | ---------------------------- |
| Wait         | Wait 2 days for confirmation |
| Condition    | Is confirmed?                |
| Send email   | Send double opt-in reminder  |
| Finish       | Already confirmed            |
| Finish       | Reminder sent                |

This pays off immediately in the Statistics view — step names are displayed there directly, so named steps let you interpret outcomes without opening the builder. It also matters increasingly as AIRA and AI agents use automation structure as context for evaluation and recommendations: a well-named process is self-documenting for both humans and AI.

### Every branch must end with a Finish step

The Finish step is not cosmetic. It's how Xperience records that a contact has _completed_ the automation rather than simply stopped progressing. This distinction is visible in the Statistics view and in individual contact records.

Named Finish steps turn raw counts into actionable data:

> "172 contacts triggered this automation" → meaningless
> "117 Already confirmed / 55 Reminder sent" → actionable

Missing Finish steps on any branch leave contacts in an indeterminate state — they appear neither complete nor in-progress. For **"If not already running"** recurrence, a contact that never reaches a Finish step can never re-trigger the process. Treat every branch terminus as a required Finish step, named for the outcome it represents.

### Name related automations to reflect their relationship

If automations are connected — sharing a trigger type, or where one automation's Log Custom Activity step fires another — use process names that make this relationship visible. Reviewers should be able to identify chains and sibling processes without opening each one. (e.g., `Onboarding — Step 1: Welcome`, `Onboarding — Step 2: Engagement Check`)

### Use Campaigns to group related automations

[Campaigns](https://docs.kentico.com/documentation/business-users/digital-marketing/campaigns) let you logically group digital marketing assets — including automations — under a shared initiative. If a set of automations represents a coordinated program (a launch, a loyalty ladder, an onboarding sequence), associating them with a Campaign makes the relationship explicit and keeps assets organized.

---

## Automation Review Checklist

When reviewing an existing automation, evaluate:

- [ ] **Recurrence mode** appropriate for the scenario? (Always = high risk of duplicates)
- [ ] **Every step named descriptively?** (Default names describe type; good names describe purpose in this automation)
- [ ] **All branches terminated** at a Finish step? (Unconnected terminals = stuck contacts; missing Finish on any branch is a bug, not a style choice)
- [ ] **Finish step names** distinct and outcome-specific? (e.g., "Already confirmed" / "Reminder sent" — not just "Finish 1" / "Finish 2")
- [ ] **Wait durations** appropriate? (Too short: competes with transactional emails. Too long: loses relevance)
- [ ] **Condition nesting depth** manageable? (>3 levels = maintenance risk)
- [ ] **Email purposes** correct? (Form autoresponder only in Form-triggered; Automation purpose for all others)
- [ ] **Consent / recipient list membership** checked before sending marketing email?
- [ ] **Custom activity triggers** — is the activity type specific enough? (Overly generic types may fire unintentionally)
- [ ] **Set Contact Field Value** — is a static value truly sufficient, or does this scenario need dynamic logic a developer should provide?
- [ ] **Multilingual requirements** — if the site is multilingual, is a separate process per language needed?
- [ ] **Re-engagement after disable** — if disabling soon, are contacts in Wait steps accounted for?

---

## Brainstorm Prompt Guide

When generating automation ideas from a marketing brief, consider:

1. **What contact action or event starts the journey?** → determines trigger type
2. **What developer-provided signals exist or could exist?** → custom activities unlock the most interesting triggers
3. **What information distinguishes one contact path from another?** → maps to Condition steps; flag where numeric logic needs developer help
4. **What contact data should be written back for downstream use?** → Set Contact Field Value steps; identify custom fields the developer needs to expose
5. **Where does the journey end meaningfully?** → define distinct Finish step labels per outcome
6. **Could this automation fire another?** → Log Custom Activity chaining pattern
7. **What physical or external-system events matter?** → identify webhook/API activity logging opportunities

---

## Quick Reference — What Marketers Can Do Without Developers

- Create and configure any automation process
- Set triggers (Form, Registration, Custom activity — if activity type already exists)
- Use any built-in condition (contact field string match, recipient list membership, activity logged, consent, email engagement)
- Send any email with correct purpose
- Add Wait steps with fixed durations
- Log pre-existing custom activities
- Set pre-existing contact field values
- Build QR-code attendance pages using existing forms and automations
- Configure all process recurrence and naming
- Create new custom activity types
- Add custom contact fields (and expose them to automation condition and Set Contact Field Value steps via the Modules UI)
- Map member fields to contact fields via Form Builder form field configuration

## Quick Reference — What Requires a Developer

- Enable numeric Contact field comparisons (must be encoded as threshold activities — developer owns the numeric logic and fires a custom activity at the threshold)
- Build reusable activity-logging Page Builder widgets
- Integrate external system events via webhook → custom activity
- Map member fields to contact fields automatically in custom code (beyond what Form Builder configuration supports)
