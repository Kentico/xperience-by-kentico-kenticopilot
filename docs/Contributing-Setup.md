# Contributing setup

This repository contains AI agent skills, instructions, and related materials for Xperience by Kentico development assistance. This guide explains how to contribute changes to these materials.

## Required software

### Text editor

You need a text editor to work with Markdown and skill files. We recommend [VS Code](https://code.visualstudio.com/) for its Markdown support and helpful extensions.

### AI coding assistants

Test your skill changes with AI assistants. This repository currently provides skills tested with:

- [GitHub Copilot](https://github.com/features/copilot)
- [Claude Code](https://www.claude.com/product/claude-code)

Testing with these tools helps validate that your changes work as intended.

## Repository structure

- `.claude-plugin/marketplace.json` — Claude Code marketplace manifest (lists all plugins)
- `.github/plugin/marketplace.json` — GitHub Copilot / VS Code marketplace manifest (lists all plugins)
- `plugins/` — Plugin folders organized by use case (e.g., `widget-creation/`, `kx13-codebase-migration/`)
  - `.mcp.json` — MCP server configuration for the plugin
  - `skills/` — SKILL.md files defining individual agent skills
- `examples/` — Example files passed to LLMs as context for corresponding scenarios
- `docs/` — Usage and contributing documentation
- `styleguides/` — Kentico documentation style guides

## Contributing to skill files

### Skill engineering best practices

When you create or modify skill files:

- Write clear, specific instructions
- Include context and examples
- Test skills with the target AI assistant
- Follow the structure of existing SKILL.md files (YAML frontmatter with `name`, `description`, `argument-hint`, `compatibility`)

## Development workflow

1. Fork this repository.
2. Create a new branch with one of the following prefixes:

   - `feat/` - new functionality
   - `refactor/` - restructuring of existing features
   - `fix/` - bugfixes

3. Validate your Markdown formatting:

   - Verify proper syntax and link validity
   - Follow existing file organization patterns

4. Commit your changes with a commit message following the [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/#summary) convention.

   - See the [Conventional Commits documentation](https://www.conventionalcommits.org/en/v1.0.0/#summary) for guidelines

5. Create a pull request on GitHub, targeting this repository.

   - Write a clear description of the changes
   - Include screenshots or recordings demonstrating prompt testing results (if applicable)
   - Verify prompt clarity and documentation quality
   - Indicate if new instructions affect existing workflows
   - Resolve all comments before the PR is merged
