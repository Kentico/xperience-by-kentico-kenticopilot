# Contributing setup

This repository contains AI agent skills, instructions, and related materials for Xperience by Kentico development assistance. This guide explains how to contribute changes to these materials.

## What belongs here

These resources are for **Kentico Xperience developers** and are designed to be invoked by AI assistants (e.g., GitHub Copilot, Claude Code) to help developers complete tasks more efficiently. Resources intended for non-developers (e.g., marketing, sales, or support) should not be contributed here.

Resources must not duplicate information already available in the Kentico documentation. Instead, link the relevant documentation pages and provide additional context.

**Before adding anything**, be clear about the feature's purpose — what it accomplishes and how it will be used — then decide whether a new resource is truly needed, or whether the same result can be achieved with an existing resource, a link to our documentation, or a well-written prompt.

## Repository layout

```
.
├── AGENTS.md                       # Conventions (CLAUDE.md points here)
├── CLAUDE.md                       # Pointer to AGENTS.md
├── README.md                       # Marketing-facing plugin catalog + install instructions
├── .claude-plugin/marketplace.json # Claude Code marketplace manifest (lists all plugins)
├── .github/
│   ├── plugin/marketplace.json     # GitHub Copilot / VS Code marketplace manifest
│   └── PULL_REQUEST_TEMPLATE.md    # Reviewer questions every PR answers
├── docs/                           # Usage, contributing, and the KX13 upgrade workflow
└── plugins/
    └── <plugin-name>/
        ├── README.md               # Plugin overview, install, skill reference
        ├── src/                    # Optional bundled tooling (e.g. a .NET CLI)
        ├── agents/                 # Subagent definitions — one Markdown file per subagent (optional)
        └── skills/
            ├── _shared/            # References shared by multiple skills in the plugin (optional)
            │   └── references/
            └── <skill-name>/
                ├── SKILL.md        # Required — the skill definition
                ├── assets/         # Templates and code samples the skill writes/copies (optional)
                ├── references/     # Skill-specific reference docs the skill reads (optional)
                └── scripts/        # Helper scripts the skill runs (optional)
```

## Resource types

There are three basic types of resource you can add. Use this table to help you get started:

| Resource | Lives in                            | Name format                        | `kentico-` prefix     | Touches manifests? |
| -------- | ----------------------------------- | ---------------------------------- | --------------------- | ------------------ |
| Plugin   | `plugins/<name>/`                   | lowercase, hyphenated              | **Yes**               | Register in both   |
| Skill    | `plugins/<plugin>/skills/<name>/`   | lowercase, hyphenated, descriptive | No (scoped by plugin) | No                 |
| Subagent | `plugins/<plugin>/agents/<name>.md` | lowercase, hyphenated, descriptive | No (scoped by plugin) | No                 |

**Shared conventions** (apply to every resource): keep each one lean and focused on a single task; write descriptions that trigger the resource reliably but aren't so long they become overwhelming; and avoid assistant-specific features and fields, since these resources are used by different AI assistants. All files related to a resource live inside that resource's folder.

### Plugin

A plugin is a coherent group of resources for one domain — for example web development, KX13 migration, or project lifecycle. Add a new plugin only when the capability does not fit the theme of any existing plugin.

New plugins must be registered in **both** marketplace manifests.

### Skill

A skill packages a repeatable task as instructions an AI assistant loads on demand.

Follow the [Agent Skills specification](https://agentskills.io/specification) for the `SKILL.md` format, frontmatter fields, and directory layout. Also follow [Skill creation — best practices](https://agentskills.io/skill-creation/best-practices) for scoping, progressive disclosure, and what to put in `references/` vs `assets/`.

### Subagent

A subagent is a focused worker that runs in its own context window with a custom system prompt and a restricted tool set. Each subagent is defined as a single Markdown file inside the plugin's `agents/` directory.

## Versioning the marketplace manifests

### Per-plugin `version` (inside each plugin entry)

Bump when **that plugin's contents** change.

| Bump           | When                                                                                               |
| -------------- | -------------------------------------------------------------------------------------------------- |
| Major          | Breaking change — renamed/removed skill, agent, or command; backward-incompatible behavior change. |
| Minor          | Additive — new skill, agent, command, or hook inside the plugin.                                   |
| Hotfix (patch) | Bug fix, prompt tightening, doc tweak, internal refactor.                                          |

### Marketplace `metadata.version` (top of each marketplace file)

Bump **only when the plugin list itself changes**.

| Bump           | When                                                                 |
| -------------- | -------------------------------------------------------------------- |
| Major          | Plugin removed or renamed.                                           |
| Minor          | Plugin added.                                                        |
| Hotfix (patch) | Metadata-only fix (description typo, keyword change, owner contact). |

Both marketplace files must always have matching versions and matching plugin entries.
