---
name: automation-context
description: Loads complete Xperience by Kentico (XbK) Automation feature context into the agent, enabling expert assistance with automation review, design, brainstorming, and developer extensibility guidance. Trigger whenever the user asks about XbK automations, wants to review an existing automation process, brainstorm new automation ideas, identify improvement opportunities, or understand what developers can build to unlock more automation value for marketers. Also trigger for any Xperience by Kentico digital marketing workflow that involves triggers, steps, conditions, contacts, or custom activities.
---

# Xperience by Kentico — Automations Context

You now have complete context on the Automation feature in Xperience by Kentico. Use this knowledge to assist with:

- **Review** — auditing existing automation processes for configuration issues, missing best practices, or logic errors
- **Brainstorm** — generating automation ideas given a marketing strategy, campaign, or contact lifecycle scenario
- **Developer unlock** — identifying where custom code creates new automation capabilities for marketers

## Load knowledge

Read `../_shared/references/automation-concepts.md` for the complete Xperience automation knowledge base: trigger types, step types and their limitations, process recurrence modes, developer extensibility patterns, and the marketer vs. developer capability reference.

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

Campaigns provide two additional capabilities worth knowing:

- **Customer Journey view** — a Campaign automatically generates a Customer Journey that maps contact progression across the campaign's assets. This gives marketers a measurement layer beyond the per-automation Statistics view, making it possible to identify where contacts drop off across the full program.
- **Campaign brief** — the Campaign stores a brief describing the initiative's goals and context. This brief is available to AI agents working on the campaign, giving them strategic context without requiring the user to re-explain the program in every conversation.

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
