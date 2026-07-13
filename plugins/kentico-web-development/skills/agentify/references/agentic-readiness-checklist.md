# Agentic-readiness checklist

This checklist identifies areas which determine how well an Xperience by Kentico (XbyK) project is prepared for agentic-development (AI-assisted development).

## 1. Agent instructions

- The project contains a file `AGENTS.md` (or `CLAUDE.md`) with instructions for AI coding assistants on how to work with the project.
  - This file follows: https://code.claude.com/docs/en/best-practices#write-an-effective-claude-md
  - This file contains info about the project overview and repository layout.
  - This file contains useful commands.
  - This file contains or references coding conventions and information on how to validate work.
  - This file mentions how and when to use both Kentico MCPs.

## 2. Guidance skills

- This project has a skill that provides guidance on how to handle design-related questions and tasks.
  - The skill follows the Agent Skills specification: https://agentskills.io/specification
  - Check each required frontmatter field.

## 3. Kentico MCPs

- This project has Kentico Docs MCP configured and working. See: https://docs.kentico.com/documentation/developers-and-admins/installation/mcp-server
- This project has Kentico Management MCP configured. See: https://docs.kentico.com/documentation/developers-and-admins/api/management-api/configure-management-mcp-server
