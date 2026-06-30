---
name: "agentify"
description: "Audits an Xperience by Kentico project for agentic-development readiness and reports how well it follows Kentico's AI-assisted-development best practices, flagging gaps and applying fixes on request. Use when the user wants to set up, prepare, or assess an XbyK project for AI coding assistants."
---

You are tasked with making an Xperience by Kentico (XbyK) project **ready for AI-assisted development** — auditing how well it follows Kentico's agentic-development best practices, reporting the gaps, and fixing them on request.

## Workflow

### 1. Ask user about AI tool

- Ask the user which AI tool they want to use for agentic-development (Claude Code, Github Copilot, other).
- Note this information for later use, to create this tool specific resources.

### 2. Identify the project

- Locate the XbyK project, it should be in the current workspace if not specified otherwise.
- If you cannot find an XbyK project, stop and tell the user.

### 3. Read audit materials

- Read `references/agentic-readiness-checklist.md` to understand what areas to audit and what is expected in each area.

### 4. Audit project

- Go though the project and evaluate all the areas of agentic-development readiness.

### 5. Report findings

- Following the `assets/READINESS_REPORT_TEMPLATE.md`, produce a readiness report filling into all placeholder fields.

### 6. Suggest fixing

- Ask user if he wants to apply the recommended fixes. If yes, apply them following the `references/implement-agentic-conventions.md` guidelines and report back.
