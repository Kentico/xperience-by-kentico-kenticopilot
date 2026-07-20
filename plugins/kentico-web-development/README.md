# Kentico web development

Skills for building and maintaining Xperience by Kentico websites, from project preparation and content modeling through Page Builder implementation, content retrieval, and design QA.

## Choose a skill

| Skill | Use it to | Activation |
|---|---|---|
| `agentify` | Audit a project for AI-assisted-development readiness and optionally apply fixes | Invoke by name |
| `design-to-content` | Turn a design, wireframe, or Figma file into an Xperience content model | Invoke by name |
| `page-builder-widgets` | Build a Page Builder widget | Describe the component |
| `page-builder-structure` | Build a section or page template | Describe the component |
| `content-retrieval` | Write, review, or troubleshoot code that reads published content | Loads for relevant code tasks; can also be invoked by name |
| `design-validation` | Compare local design HTML with a running site and classify differences | Ask to validate or compare pages |

## How the capabilities fit together

A design-led implementation typically uses the skills in this order:

1. Run `agentify` once to prepare the project and its agent instructions.
2. Use `design-to-content` to model structured content from the design.
3. Ask the agent to build the required widgets, sections, and templates.
4. Let `content-retrieval` guide any code that loads the modeled content.
5. Run `design-validation` against the live implementation.

Use only the steps relevant to your task; the skills also work independently.

### Agentic readiness

`agentify` first asks which AI tool the project uses, then checks project instructions, development and verification guidance, a design-guidance skill, and Kentico Docs/Management MCP configuration. It writes a readiness report and asks before applying recommended fixes.

With confirmation, it can create tool-appropriate agent instructions and a project-specific design-guidance skill, and follow the official MCP setup procedures. Management MCP setup may require local application integration; the skill follows the linked Kentico procedure rather than inventing setup steps.

### Content modeling

`design-to-content` guides decisions about content types, reusable field schemas, taxonomies, relationships, and Page Builder structure. It points the agent to current Kentico documentation instead of embedding API guidance in the skill.

### Page Builder development

`page-builder-widgets` and `page-builder-structure` load when you describe a matching implementation task. The agent first studies existing components, mirrors project conventions, and verifies uncertain APIs against current Xperience documentation.

### Content retrieval

`content-retrieval` is a reference skill rather than a file-generating workflow. It helps the agent choose between `IContentRetriever` and the lower-level content item query API, use the correct selector identifier type, and account for caching, linked-item depth, projection, and paging.

### Design validation

`design-validation` runs a bundled Playwright comparison between static HTML and a running site. It writes one JSON report per page, compares content, meaningful structure, and selected computed styles, then helps the agent classify findings with a root cause and fix location as:

- **Content**: wrong or missing content, fields, or translations
- **Serving**: missing widgets, incorrect sections/templates, or unresolved asset URLs
- **Styling**: CSS differences

This is not a screenshot or pixel-regression tool. It does not establish pixel fidelity or compare every layout dimension and background image.

## Requirements

- An Xperience by Kentico project
- An AI coding assistant with this plugin installed
- Kentico Docs MCP for documentation-backed skills; see [MCP setup](./MCP-setup.md)
- Kentico Management MCP for `agentify` readiness and design-validation content investigation
- Page Builder configured when creating widgets, sections, or templates
- Clear requirements and, when relevant, local design files
- For `design-validation`: Node.js 22.18+ (24 LTS recommended), npm 11.10+, local HTML/CSS designs, and the site running in live mode

The first design-validation run downloads Playwright Chromium.

## Install

Follow the marketplace instructions in the [usage guide](../../docs/Usage-Guide.md#install-the-selected-plugin), using the plugin name `kentico-web-development`.

## Use the plugin

Provide the task and the most concrete inputs available:

```text
/agentify

Audit the project in the current workspace.
```

```text
/design-to-content

Model the news portal represented by ./design/home.html.
```

```text
Create a Page Builder widget from ./requirements/product-card.md and
follow the conventions of the existing widgets.
```

```text
How should this widget load the items selected through a Combined
content selector?
```

```text
Validate the home and about pages against ./design. The live site is
running at https://localhost:5001.
```

For implementation work, the agent produces code matching the project. Audit and validation skills produce reports with recommended next steps.

## Included resources

- `agentify` includes readiness criteria, implementation guidance, and report templates.
- `design-to-content` includes a map to the content-modeling documentation.
- `page-builder-widgets` and `page-builder-structure` include documentation maps for their APIs.
- `content-retrieval` includes [API/documentation guidance](./skills/content-retrieval/references/content-retrieval-docs.md) and a [performance model](./skills/content-retrieval/references/performance.md).
- `design-validation` includes [CLI guidance](./skills/design-validation/references/cli-guidance.md), [finding classification](./skills/design-validation/references/classification.md), a [report template](./skills/design-validation/references/report-template.md), and the Playwright scripts.

## Limits and review guidance

- Generated Page Builder code must be reviewed and tested in both edit and live mode.
- Content-modeling output is a design proposal; validate it against editorial and governance requirements.
- Content-retrieval guidance does not replace profiling representative production workloads.
- Design validation compares text, meaningful structure, URLs, and selected computed styles. A clean report does not guarantee pixel fidelity.
- Write validation reports to a project-owned output folder so plugin updates do not replace them.

## Customize the skills

Add project-specific conventions to the relevant skill's `references/` directory. Keep reusable Xperience guidance linked to the authoritative documentation instead of copying it into the plugin.
