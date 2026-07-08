# Kentico web development

Skills and references for building Xperience by Kentico websites. The plugin covers an agentic-readiness audit for your project, AI-assisted creation of [Page Builder](https://docs.kentico.com/x/6QWiCQ) widgets, and guidance for [content retrieval](https://docs.kentico.com/documentation/developers-and-admins/development/content-retrieval) in live-site code, with more web-development capabilities planned.

## Skills

| Skill                          | Description                                                                                                                              |
| ------------------------------ | -------------------------------------------------------------------------------------------------------------------------------------- |
| `agentify`                     | Audits an XbyK project for agentic-development readiness, reports gaps, and applies fixes on request                                     |
| `design-to-content`            | Guides content modeling — translating designs/wireframes into an Xperience content model                                                |
| `widget-create-research`       | Analyzes widget requirements and design files, validates them against Xperience documentation, and generates implementation instructions |
| `widget-create-implementation` | Creates the widget code following the generated instructions and project conventions                                                    |
| `content-retrieval`            | Decision rules, a docs/API map, and performance guidance for reading published content (pages, reusable items, reusable-schema items) in live-site/MVC code — prefer `IContentRetriever` |

You invoke `agentify`, `design-to-content`, and the two `widget-create-*` skills explicitly — the `widget-create-*` pair forms the two-stage widget workflow described below. The `content-retrieval` skill is a reference skill that activates automatically when you write or review content-retrieval code — you can also invoke it by name. See [Content retrieval](#content-retrieval) for details.

## Agentic readiness (`agentify`)

`agentify` prepares an Xperience by Kentico project for AI-assisted development. It audits the project against Kentico's agentic-development best practices, writes an `agentic-readiness-report.md`, and — with your confirmation — fixes the gaps it finds.

It checks:

- **Agent instructions** (`AGENTS.md` / `CLAUDE.md`) — project overview, dev-environment setup, run & verify instructions, MCP-usage instructions, and pointers to further guidance.
- **Design / architecture / interactions guidance** — recommending description-driven **passive skills** over static `DESIGN.md` / `ARCHITECTURE.md` / `INTERACTIONS.md` files.
- **Kentico Docs MCP** and **Kentico Management MCP** — configured and accessible to the agent.

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

## Widget creation workflow (`widget-create-research` + `widget-create-implementation`)

These prompts provide two-stage AI assistance for building custom Page Builder widgets:

1. **Research stage** - Analyzes your requirements and design, validates them against Xperience documentation, and generates detailed implementation instructions
2. **Implementation stage** - Creates the widget code following the generated instructions and project conventions

## Prerequisites

- Xperience by Kentico project with Page Builder configured
- AI coding assistant installed (for example, GitHub Copilot or Claude Code)
- Widget requirements file describing the main use cases and behavior
- Widget design file (optional, exported from Figma or similar)

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

### 1. Prepare context files

Create a folder with your widget requirements and design:

- **requirements.md** - Describes the widget functionality, presentation options, and technical requirements.
- (Optional) **design.html** - Visual design and element structure exported from Figma or other design tool.

See the `examples/widget-creation/` directory for samples of these files.

### 2. Run the research stage

The AI analyzes your requirements, validates them against Xperience documentation, and creates a detailed instructions file in your input folder.

In the prompt, provide the path to your requirements folder. Include all supplementary materials that the agent should follow.

**VS Code GitHub Copilot example**

```
/widget-create-research

For the requirements described in: examples/widget-creation/requirements.md
```

### 3. Run the implementation stage

In the prompt, provide the path to the instructions file generated by the research stage. The AI creates the widget following the instructions, project conventions, and Xperience best practices.

_Optional_: The instructions file created in the research stage contains all the information required by the implementation stage. Depending on the scale of your project and the scope of the implementation, consider starting the implementation step from a new conversation to avoid possible LLM context degradation caused by long conversation history and excessive summarization.

**VS Code GitHub Copilot example**

```
/widget-create-implementation

Follow instructions in: widget-creation/ARTICLE_SHOWCASE.instructions.md
```

## Prompt output

The implementation stage generates:

- Widget view component class
- Widget properties class
- Razor view file (`.cshtml`)
- View model class
- Localized resource strings (creates a .resx file and the corresponding registration class if none found in the workspace)

If your project already contains widgets, the prompt also mimics their implementation patterns and filesystem structure.

## Included files

### Instructions

These files provide context to the AI about Xperience by Kentico:

- `base-pagebuilder.md` - Core Page Builder concepts and APIs
- `docs.md` - Links to relevant Xperience documentation
- `example-widgets.md` - Examples of existing widget patterns

### Prompts/Commands

- **Research prompt** - Analyzes requirements and generates implementation instructions
- **Implementation prompt** - Creates the widget code based on instructions

### Template

- `CREATION_TEMPLATE.md` - Template for generating widget implementation instructions

## Best practices for usage

- Provide clear, specific requirements in your requirements file
- Include presentation options and error handling scenarios
- Review the generated instructions before running the implementation stage
- Thoroughly review and test the generated code

## Examples

See `examples/widget-creation/` for a complete example of context files for an article showcase widget, which includes:

- Structured requirements file
- Exported design HTML

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

The widget skill files serve as a baseline for bootstrapping new widgets in Xperience by Kentico solutions. Modify and enhance the files as required by your projects, workflow, and requirements. Place project-specific information into a skill's `references` folder as new files — the skills instruct the agent to read all files in the directory.
