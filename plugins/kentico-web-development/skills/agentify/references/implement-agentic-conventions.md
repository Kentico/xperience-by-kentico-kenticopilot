# Implement agentic conventions

This reference describes how to implement agentic conventions in an Xperience by Kentico (XbyK) project, so that it is ready for agentic-development (AI-assisted development).

## 1. Agent instructions

- Based on the AI tool the user has, create a file `AGENTS.md` (or `CLAUDE.md`) with instructions for AI coding assistants on how to work with the project.
- Use the `assets/AGENTS_TEMPLATE.md` as a template for this file, filling in all placeholder fields with project-specific information.
- Ask the user to provide a reference to their preferred coding conventions; do not fill this part in on your own.

## 2. Guidance skills

- Create an agent Skill that provides guidance on how to handle design related questions and tasks.
- Make sure the skill follows https://agentskills.io/specification
- Place the skill where the user's AI tool discovers skills (its skills directory); consult that tool's documentation for the exact location.
- Use the `assets/design-conventions.TEMPLATE.md` as a template for these skills, filling in all placeholder fields with project-specific information.
- Ask the user to provide a reference to their preferred design guidance; do not fill those skill files in on your own.

## 3. Kentico MCPs

- Set up the Kentico Docs MCP following: https://docs.kentico.com/documentation/developers-and-admins/installation/mcp-server
- Set up the Kentico Management MCP following: https://docs.kentico.com/documentation/developers-and-admins/api/management-api/configure-management-mcp-server
