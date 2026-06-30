# Implement agentic conventions

This reference describes how to implement agentic conventions in an Xperience by Kentico (XbyK) project, so that it is ready for agentic-development (AI-assisted development).

## 1. Agent instructions

- Based on the AI tool the user has, create a file `AGENTS.md` (or `CLAUDE.md`) with instructions for AI coding assistants on how to work with the project.
- Use the `assets/AGENTS_TEMPLATE.md` as a template for this file, filling in all placeholder fields with project specific information.
- Ask user to provide reference to his preferred coding conventions, do not fill this part on your own.

## 2. Guidance skills

- Based on the AI tool the user has, create skills in correct location that provide guidance on how to handle design related questions and tasks.
- Use the `assets/design-guidance-template.md` as templates for these skills, filling in all placeholder fields with project specific information.
- Ask user to provide reference to his preferred design guidance, do not fill those skill files on your own.

## 3. Kentico MCPs

- Setup docs MCP following: https://docs.kentico.com/documentation/developers-and-admins/installation/mcp-server
- Setup Kentico Management MCP following: https://docs.kentico.com/documentation/developers-and-admins/api/management-api/configure-management-mcp-server

## 4. Code quality

- Based on the gaps identified in the readiness report and user's decision on what to fix, apply those fixes to improve code quality.
