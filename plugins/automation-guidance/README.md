# Automation guidance

AI-assisted skills for identifying, planning, and refining [Marketing Automation](https://docs.kentico.com/documentation/business-users/digital-marketing/automation) workflows in Xperience by Kentico.

## Overview

These skills help both marketing teams and developers work with Xperience automations — from discovering what to automate, to producing a concrete implementation plan, to reviewing and improving existing processes.

The plugin includes three skills that cover different stages of the automation lifecycle:

| Skill | Use when... |
|-------|-------------|
| [`/automation-explore`](#automation-explore) | You know you want to use automations but aren't sure what to automate |
| [`/automation-scope`](#automation-scope) | You have a specific automation in mind and need to turn it into a plan |
| [`/automation-context`](#automation-context) | You want to review an existing automation, brainstorm ideas freely, or get developer guidance |

A team new to automations would typically run explore → scope to get started, then use the context skill for ongoing Q&A, process reviews, and developer conversations.

## Prerequisites

- Xperience by Kentico project (or planned project) with Contact Management configured
- AI coding assistant installed (for example, GitHub Copilot or Claude Code)

## Install the plugin

### VS Code (GitHub Copilot)

Add the marketplace to your VS Code settings (`settings.json`), then browse and install from the Extensions sidebar (`@agentPlugins`):

```json
"chat.plugins.marketplaces": [
    "Kentico/xperience-by-kentico-kenticopilot"
]
```

### Copilot CLI

```bash
copilot plugin marketplace add Kentico/xperience-by-kentico-kenticopilot
copilot plugin install automation-guidance@xperience-by-kentico-kenticopilot
```

### Claude Code

```bash
/plugin marketplace add Kentico/xperience-by-kentico-kenticopilot
/plugin install automation-guidance@xperience-by-kentico-kenticopilot
```

---

## `/automation-explore`

**Problem it solves:** Most people new to automations know the feature exists but don't know what to automate. This skill acts as a strategic consultant — it surfaces high-value opportunities grounded in your business context, rather than presenting a generic feature overview.

**Who it's for:** Marketing teams who want to adopt automations but need a starting point. Also useful for developers who've been asked to "add automation support" to a project and need to understand what the business actually needs first.

### Usage

Run the skill and either describe your business context upfront, or let the agent guide you through a short discovery conversation covering your industry, contact types, manual processes that feel slow, and marketing goals.

```
/automation-explore

We are a B2B SaaS company. We have a free trial sign-up form and a lot of
manual follow-up happening through sales. We also struggle with re-engaging
people who go quiet after week one of the trial.
```

If you don't provide context, the agent will ask focused questions before generating suggestions.

### Output

A prioritized list of automation opportunities, each with:

- A plain-language trigger and expected outcome
- Complexity rating (Simple / Moderate / Complex)
- Business value summary
- A flag if the use case requires developer involvement

Each entry ends with a prompt to continue with `/automation-scope`.

---

## `/automation-scope`

**Problem it solves:** The gap between "we should automate our welcome series" and "here's exactly what that means in Xperience, what it depends on, and what needs to exist before we build it." Without this, a developer typically has to run a full discovery conversation with the marketer before writing a single line of configuration.

**Who it's for:** Anyone with a specific automation in mind — whether from `/automation-explore` or their own planning. Both marketers and developers use the output: marketers to understand what needs to be built and in what order, developers to understand the required configuration and any custom code.

### Usage

Provide a use case by name or description. If anything is ambiguous — who enters, what triggers it, whether there are branches — the agent asks clarifying questions before producing the plan.

```
/automation-scope

Use case: Trial re-engagement nudge for contacts who go quiet after day 7
Output folder: docs/automations/
```

### Output

A saved scope document (`[use-case-name]-automation-scope.md`) containing:

- Plain-language summary and goals
- Trigger type and configuration
- Contact eligibility and re-entry rules
- Step-by-step process table with branching logic
- Detailed step configuration, with technical notes for developers
- Contact attribute changes
- Required email communications
- Success criteria
- Prerequisites checklist — the forms, email templates, consents, and contact attributes that must exist before building starts

The prerequisites checklist is particularly useful: it makes the build sequence explicit so nothing blocks the marketer mid-implementation.

---

## `/automation-context`

**Problem it solves:** General-purpose automation expertise on demand. The other two skills are sequential and output-oriented. This one is conversational — it handles questions that don't fit neatly into explore or scope, including reviewing existing processes, freeform brainstorming, and developer extensibility guidance.

**Who it's for:** Anyone already working with automations who needs an expert conversation partner — marketers auditing existing processes, developers understanding what to build, or teams planning a new campaign.

### Usage

Run the skill and describe what you need. It handles three distinct patterns:

**Review an existing automation**

Describe a process or paste its step structure, and the agent evaluates it against a review checklist — checking recurrence mode, whether all branches have named Finish steps, whether consent is checked before email sends, whether condition nesting is becoming unmaintainable, and more.

```
/automation-context

Can you review this automation? It's a re-engagement campaign triggered by form submission.
Here's the step sequence: ...
```

**Brainstorm automation ideas**

Describe a campaign brief, contact lifecycle scenario, or marketing initiative, and the agent generates automation ideas — including which ones need developer setup and why.

```
/automation-context

We're launching a loyalty programme next quarter. What automations should we
plan for the tier progression journey?
```

**Get developer extensibility guidance**

Ask what custom code is needed to unlock specific automation capabilities that aren't available out of the box.

```
/automation-context

Our client wants to branch automations based on a loyalty point score.
What do we need to build to make that work?
```

The agent will explain that numeric comparison isn't available in Condition steps, and that the developer needs to own the threshold logic and fire a custom activity at each meaningful level — which the marketer can then use as both a trigger and a condition without any further developer involvement.

---

## Included reference files

These files provide context to the AI about Xperience by Kentico automations:

- `skills/_shared/references/automation-concepts.md` — Trigger types, step types, condition list, critical limitations, developer extensibility model, and marketer vs. developer capability reference
- `skills/_shared/references/docs.md` — Links to official Xperience automation documentation
- `skills/automation-explore/references/use-cases.md` — Curated library of common automation use cases across industries

## Best practices

- In the explore stage, be specific about your contact lifecycle and the manual processes your team handles — the more context you provide, the more relevant the suggestions.
- Review the explore output with both marketing and development stakeholders before proceeding to scope.
- Use the scope document as the shared source of truth between marketers (who define the goal) and developers (who implement it).
- Check the Prerequisites section of each scope document before starting implementation — missing forms, email templates, or contact attributes are the most common setup blockers.

## Skill customization

These skill files serve as a baseline. Modify and extend them to fit your team's specific processes, terminology, and Xperience configuration. You can add project-specific reference files to each skill's `references/` folder — the skill will read all files in that directory.
