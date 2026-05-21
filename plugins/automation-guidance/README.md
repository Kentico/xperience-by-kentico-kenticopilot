# Automation guidance

AI-assisted skills for identifying and planning [Marketing Automation](https://docs.kentico.com/documentation/business-users/digital-marketing/automation) use cases in Xperience by Kentico.

## Overview

These skills help both marketing teams and developers discover and design automation workflows that benefit marketing operations and campaigns. Whether you are a marketer looking to automate manual processes or a developer planning automation requirements for a project, this plugin guides you from idea to a structured implementation plan.

The workflow follows two stages:

1. **Exploration stage** — Analyzes your business context, marketing goals, and contact lifecycle to surface high-value automation opportunities, prioritized by impact and complexity.
2. **Scoping stage** — Takes a specific opportunity and produces a structured scope document that maps it to Xperience automation concepts: triggers, steps, conditions, actions, and required dependencies.

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

## Usage

### 1. Explore automation opportunities

The AI analyzes your business context and identifies marketing processes that are good candidates for automation. You can provide context up front or let the skill guide you through a short discovery conversation.

**VS Code GitHub Copilot example**

```
/automation-explore

We are a B2B SaaS company targeting mid-market businesses. Our main goals are to
reduce manual lead follow-up after demo requests and improve trial-to-paid conversion.
```

**Claude Code example**

```
/automation-explore
```

The skill will ask about your business, contact lifecycle, and marketing goals if you do not provide them.

### 2. Scope a specific use case

Once you have identified an automation opportunity — either from `/automation-explore` or your own planning — use the scope skill to produce a detailed implementation plan.

**VS Code GitHub Copilot example**

```
/automation-scope

Use case: Trial conversion nurture sequence
Output folder: docs/automations/
```

**Claude Code example**

```
/automation-scope

Scope the "Re-engagement Campaign" use case from the explore output.
Save to: docs/automations/
```

The skill maps the use case to Xperience automation concepts and saves a structured scope document that both marketing stakeholders and developers can use.

## Skill output

### `/automation-explore`

Produces a prioritized list of automation use-case candidates, each with:

- A plain-language description of the trigger and intended outcome
- Estimated complexity (Simple / Moderate / Complex)
- Business value summary

### `/automation-scope`

Produces a scope document (`[use-case-name]-automation-scope.md`) containing:

- Use case summary and goals
- Trigger type and configuration
- Contact eligibility and re-entry rules
- Step-by-step process with branching logic
- Contact attribute changes
- Email communications required
- Success criteria
- Prerequisites checklist (forms, email templates, consent, contact attributes)

## Included reference files

These files provide context to the AI about Xperience by Kentico automations:

- `skills/_shared/references/automation-concepts.md` — Overview of automation components, capabilities, and limitations
- `skills/_shared/references/docs.md` — Links to official Xperience automation documentation
- `skills/automation-explore/references/use-cases.md` — Curated library of common automation use cases across industries

## Best practices

- In the explore stage, be specific about your contact lifecycle and the manual processes your team currently handles — the more context you provide, the more relevant the suggestions.
- Review the explore output with both marketing and development stakeholders before proceeding to scope.
- Use the scope document as the shared source of truth between marketers (who define the goal) and developers (who implement it).
- Check the Prerequisites section of each scope document before starting implementation — missing forms, email templates, or contact attributes are the most common setup blockers.

## Skill customization

These skill files serve as a baseline. Modify and extend them to fit your team's specific processes, terminology, and Xperience configuration. You can add project-specific reference files to each skill's `references/` folder — the skill will read all files in that directory.
