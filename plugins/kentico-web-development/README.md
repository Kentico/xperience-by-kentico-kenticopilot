# Kentico web development

Skills and references for building Xperience by Kentico websites. The plugin covers an agentic-readiness audit for your project, content modeling, AI-assisted [Page Builder](https://docs.kentico.com/x/6QWiCQ) development — building widgets and structuring pages with sections and templates — guidance for [content retrieval](https://docs.kentico.com/documentation/developers-and-admins/development/content-retrieval) in live-site code, and validation of the live site against static HTML designs, with more web-development capabilities planned.

## Skills

| Skill               | Description                                                                                                                              |
| ------------------- | -------------------------------------------------------------------------------------------------------------------------------------- |
| `agentify`          | Audits an XbyK project for agentic-development readiness, reports gaps, and applies fixes on request                                     |
| `design-to-content` | Guides content modeling — translating designs/wireframes into an Xperience content model                                                |
| `page-builder-widgets`   | Builds a custom Page Builder **widget** (view component, properties, view model, Razor view, registration)                          |
| `page-builder-structure` | Builds Page Builder **structure** — sections (widget-zone layouts) and page templates (full-page layouts)                           |
| `content-retrieval` | Decision rules, a docs/API map, and performance guidance for reading published content (pages, reusable items, reusable-schema items) in live-site/MVC code — prefer `IContentRetriever` |
| `design-validation` | Validates a live site against static HTML designs — a deterministic Playwright comparison (content, structure, computed styles) plus AI classification of each finding as a content, serving, or styling issue |

You invoke `agentify` and `design-to-content` explicitly. `page-builder-widgets` and `page-builder-structure` are **passive-knowledge** skills — the AI loads them automatically when you describe the relevant task; just provide your requirements. The `content-retrieval` skill is a reference skill that activates automatically when you write or review content-retrieval code — you can also invoke it by name. See [Content retrieval](#content-retrieval) for details. The `design-validation` skill activates when you ask to validate or compare pages against a design, and the AI also uses it proactively after page-affecting changes when a static design exists — see [Design validation](#design-validation-design-validation).

## Agentic readiness (`agentify`)

`agentify` prepares an Xperience by Kentico project for AI-assisted development. It audits the project against Kentico's agentic-development best practices, writes an `agentic-readiness-report.md`, and — with your confirmation — fixes the gaps it finds.

It checks:

- **Agent instructions** (`AGENTS.md` / `CLAUDE.md`) — project overview, dev-environment setup, run & verify instructions, MCP-usage instructions, and pointers to further guidance.
- **Design / architecture / interactions guidance** — recommending description-driven **passive skills** over static `DESIGN.md` / `ARCHITECTURE.md` / `INTERACTIONS.md` files.
- **Kentico Docs MCP** and **Kentico Management MCP** — configured and accessible to the agent (see [Configure MCP servers](#configure-mcp-servers) below).

When fixing the **Management MCP** gap, the skill can enable the management API in your app (NuGet package + `Program.cs` + a secret) and add the local MCP server. This is a Kentico **preview, local-development-only** feature — `agentify` confirms before editing application code and never enables it for production.

**Claude Code example**

```
/agentify

Project root: C:/my-project
```

## Content modeling (`design-to-content`)

Use the `design-to-content` skill when translating designs, wireframes, or Figma files into an Xperience by Kentico content model. The skill points the AI to the relevant Kentico content-modeling documentation and guides decisions about content types, reusable schemas, taxonomies, relationships, and Page Builder structure.

**VS Code GitHub Copilot example**

```
/design-to-content

I have a Figma design for a news portal. Help me model the content types.
```

## Page Builder development (`page-builder-widgets` + `page-builder-structure`)

`page-builder-widgets` and `page-builder-structure` are **passive-knowledge** skills: the AI loads them automatically when you ask it to build the relevant Page Builder component. You don't run them as explicit commands — just describe what you want and provide your requirements. Each skill instructs the AI to first study the existing components in your project and mirror their conventions, then validate any uncertain APIs against the Xperience documentation via the Kentico Docs MCP before implementing.

## Design validation (`design-validation`)

`design-validation` checks whether the rendered live site actually matches its static HTML design — the development outcome that is otherwise hard to confirm. A bundled Playwright script deterministically compares each design page with the corresponding live page (content text, DOM structure, computed styles) and writes a JSON report; the AI then classifies every difference as a **content** issue (wrong or missing content item, field, or translation), a **serving** issue (missing widget, wrong section or template, unresolved `~/` URLs), or a **styling** issue (CSS), and drives the fix.

It activates when you ask to validate, QA, or compare pages against a design, and the AI also runs it proactively after implementing or changing a page, template, widget, view component, or stylesheet when a static design for the affected page exists.

```
Validate the home and about pages against the designs in ./design — the site runs on https://localhost:5001
```

Requirements: Node.js 22.18+ (24 LTS recommended) and npm, the site running in live mode, and the design as local HTML/CSS files. The first run downloads the Playwright Chromium browser (~115 MB). Reports should be written to a project-local folder (`--out`) so they survive plugin updates.

## Prerequisites

- Xperience by Kentico project with Page Builder configured
- AI coding assistant installed (for example, GitHub Copilot or Claude Code)
- A description of what you want to build — for a widget, a requirements file describing its functionality, presentation options, and error handling; optionally a design file (e.g. `design.html` exported from Figma)
- For `design-validation`: Node.js 22.18+ (24 LTS recommended) and npm

## Configure MCP servers

This plugin requires some MCP servers to be set up in your workspace. See [MCP-setup.md](./MCP-setup.md) for the list and copy-paste-ready configuration.

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
copilot plugin install kentico-web-development@xperience-by-kentico-kenticopilot
```

### Claude Code

```bash
/plugin marketplace add Kentico/xperience-by-kentico-kenticopilot
/plugin install kentico-web-development@xperience-by-kentico-kenticopilot
```

## Usage

Because these are passive-knowledge skills, you trigger them simply by describing the task. The AI recognizes the intent, loads the relevant skill, studies your project, and implements the component.

### Create a widget

1. Prepare your context. Create (or point to) a requirements file describing the widget's functionality, presentation options, and error handling. Optionally include a design file. See [`examples/widget-creation/`](../../examples/widget-creation/) for samples.
2. Ask the AI to build it, referencing your requirements:

   ```
   Create a Page Builder widget based on the requirements in examples/widget-creation/requirements.md
   ```

The AI produces the widget view component, properties class, Razor view, view model, and registration. If your project already contains widgets, it mirrors their patterns and file structure.

### Create a section or page template

Describe the layout you need:

```
Add a two-column Page Builder section with a configurable background color
```

```
Create a landing-page template with a hero editable area and a theme property
```

The AI builds the section (view component / partial view, properties, widget zones, registration) or page template (full-page view, properties, registration, routing notes) following your project's conventions.

> **Tip:** For large implementations, consider starting a fresh conversation for a new component to avoid context degradation from long histories.

## Included files

Each skill carries its own references that the AI reads on demand:

### `page-builder-widgets`

- `references/docs.md` — a documentation map: links to the relevant Xperience widget documentation, each with a "when to read" hint

### `page-builder-structure`

- `references/docs.md` — a documentation map: links to section and page-template documentation, each with a "when to read" hint

### `design-validation`

- `references/cli-guidance.md` — the comparison script's setup, options, and exit codes
- `references/report-template.md` — the JSON report structure, finding kinds, and how to interpret them
- `references/classification.md` — classifying each finding as a content, serving, or styling issue
- `scripts/` — the Playwright comparison CLI (`compare.ts`, TypeScript run natively by Node)

## Best practices

- Provide clear, specific requirements, including presentation options and error-handling scenarios.
- Let the AI study existing components first so new code matches your conventions.
- Thoroughly review and test the generated code in both edit and live mode.

## Examples

See [`examples/widget-creation/`](../../examples/widget-creation/) for a complete example of widget context files (a structured requirements file and an exported design HTML).

## Content retrieval

The `content-retrieval` skill is a **reference skill** — it carries no multi-step workflow and produces no files. It activates automatically when your task involves reading published content in live-site / MVC code: fetching pages or reusable content items, turning a Combined content selector or Page selector value into data, or diagnosing a content query that is slow under load. You can also invoke it explicitly by name.

Instead of generating code from a fixed template, it equips the agent with:

- **Decision rules** — when to use `IContentRetriever` (the default for almost all retrieval) versus the lower-level content item query API, and the single most common bug: Combined content selector GUIDs (`ContentItemGUID`) and Page selector GUIDs (`WebPageItemGUID`) are **not interchangeable**, and crossing them returns an empty result with no exception.
- **A documentation map** (`references/content-retrieval-docs.md`) — every relevant Xperience docs page and API-reference entry, each with a "when to read" hint, so the agent looks up current signatures instead of reconstructing them from memory.
- **A performance model** (`references/performance.md`) — how to keep retrieval fast under load (linked-item depth, the retriever's implicit caching, column projection, paging) and the known API limitations.

If the [Kentico Docs MCP server](./.mcp.json) is configured, the skill uses it to fetch the current content of any referenced documentation page.

### Example

```text
How should I load the items a visitor picked in a Combined content selector on my widget?
```

The skill supplies the retrieval decision rules and points to the exact docs and API reference for the methods involved.

## Skill customization

These skills serve as a baseline for bootstrapping Page Builder components in Xperience by Kentico solutions. Modify and enhance them as your projects and workflow require. Place project-specific information into a skill's `references` folder as new files — the skills instruct the AI to read the reference material.
