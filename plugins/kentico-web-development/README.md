# Kentico web development

Build Xperience websites with AI assistance. Model content from designs, build [Page Builder](https://docs.kentico.com/x/6QWiCQ) widgets and templates, write content retrieval code, and validate the result against the original design.

## Choose a skill

| Skill | Use it to |
|---|---|
| `agentify` | Audit a project for AI-assisted-development readiness, and apply the fixes on request |
| `design-to-content` | Turn a design, wireframe, or Figma file into an Xperience content model |
| `page-builder-widgets` | Build a Page Builder widget |
| `page-builder-structure` | Build a section or a page template |
| `content-retrieval` | Write, review, or troubleshoot code that reads published content |
| `design-validation` | Compare local design HTML with a running site and classify the differences |

The skills are designed to work independently. However, a project that starts from a design usually needs them in the order listed, beginning with an `agentify` audit and ending with a validation run against the design.

> [!TIP]
> New to agent skills? Agents activate skills as necessary based on the assigned task. Alternatively, you can use slash commands and other methods depending on your assistant. For what that means in practice, read [Invoke a skill](../../docs/Usage-Guide.md#invoke-a-skill).

## Requirements

- An Xperience by Kentico project
- An AI coding assistant with this plugin installed
- MCP servers listed on [MCP-setup.md](./MCP-setup.md)
- Task descriptions and design files or wireframes (exported from Figma, generated via Claude Design, or from other sources)

Additionally, `design-validation` requires:

- Node.js 22.18 or newer
- npm 11.10 or newer
- Local HTML and CSS designs, and the site running in live mode
- The first run of the skill downloads Playwright Chromium browsers

## Install

Follow the marketplace instructions in the [usage guide](../../docs/Usage-Guide.md#install-the-selected-plugin), using the plugin name `kentico-web-development`.

## Build your first page

This sequence takes a design through a content model, the components that render it, and the code that loads the content, to a page an editor can maintain. Each stage also works on its own, and [Common tasks](#common-tasks) covers the stages separately.

1. Open the solution containing your Xperience web project in your AI coding assistant. To create the project first, see [Installation](https://docs.kentico.com/x/DQKQC).

2. Prepare the project for agentic development.

   ```text
   /agentify

   Audit the project in the current workspace.
   ```

   The skill asks which assistant you work with and reports what the project is missing. Nothing in your repository changes until you approve the recommendations.

3. Model the content behind the design.

   ```text
   /design-to-content

   Model the pages represented by ./design/home.html and
   ./design/product-detail.html.
   ```

   The agent proposes [content types](https://docs.kentico.com/x/gYHWCQ), [reusable field schemas](https://docs.kentico.com/x/D4_OD), [taxonomies](https://docs.kentico.com/x/taxonomies_xp), and the relationships between them, and recommends a [content model](https://docs.kentico.com/x/f4HWCQ) for the project.

4. Review the proposal, then ask the agent to apply the approved model and generate the model classes your code binds to.

   ```text
   Create the approved content types, reusable field schemas, and
   taxonomies in my local instance, then generate the model classes.
   Use the Management MCP server.
   ```

5. Describe the components the design calls for, together with the design file and an existing component to follow. The more specific you are, the fewer questions the agent needs to ask. For examples, see [Write effective prompts](#write-effective-prompts).

   ```text
   Create a page template for ./design/product-detail.html with an
   editable area for the marketing content, and a product card widget
   matching ./design/product-card.html. Follow the conventions of the
   components already in this project.
   ```

6. Ask for the code that loads the modeled content into those components. For what to look at in the generated code, see [Review the output](#review-the-output).

   ```text
   Retrieve the products for the listing page, including the linked
   brand item, and cache the result.
   ```

7. Build the project, create a page from the new template in your website channel, and add content to the page.

8. Compare the rendered page with the design you started from.

   ```text
   Validate the product detail page against ./design. The live site is
   running at https://localhost:5001.
   ```

The page now renders modeled content through components an editor rearranges in Page Builder, and the validation report describes each difference between the rendered page and the design.

## Common tasks

### Add a widget to an existing project

Point the assistant to a design and existing [widgets](https://docs.kentico.com/x/7gWiCQ) to follow for project conventions.

```text
Create a Page Builder widget from ./requirements/product-card.md,
matching ./design/product-card.html and following the conventions of
the widgets already in this project.
```

### Write code that retrieves content

The `content-retrieval` skill loads on its own and helps the agent use the API recommended for your ask.

```text
Have this widget load its items via a Combined content selector.
```

## Write effective prompts

Both of these prompts produce a working output. However, providing the agent with more context significantly reduces guesswork and increases output quality and standards adherence. Compare:

| Prompt | What happens |
|---|---|
| `Build a hero widget` | The agent works out the markup, the properties an editor configures, and the styling. Expect several rounds of questions. |
| `Build a hero widget for the home page matching ./design/home.html, following the structure of the banner widget in ./Components/Widgets/Banner` | The agent has a design to match and a convention to copy so the final output can immediately be compared against the initial design. |

> [!TIP]
> The same applies to every KentiCopilot skill. See [Write specific prompts](../../docs/Usage-Guide.md#write-specific-prompts) for the general guidance, including the habits that slow a session down.

## Review the output

Carefully review all generated output, paying particular attention to:

**Generated content model** – Unless given specific and detailed instructions, a content model generated from the provided design is the agent's best estimate that satisfies the existing constraints. Validate it against your editorial and governance requirements before adopting it fully into your codebase.

**Both Page Builder modes** – A widget that renders correctly on the live site can still fail in the editor, where properties, inline editors, and empty states behave differently. Add the component to a page and test it in edit mode as well as on the live site.

**Retrieval under load** – Linked-item depth, projection, and caching decisions may look correct against development data, but degrade against production volumes. Raising linked-items depth above zero is the usual cause. Before you accept a generated query, read [Reference - ContentRetriever API](https://docs.kentico.com/x/reference_content_retriever_api_xp).

## Customize

Record project-specific conventions in your project's agent instruction files. The agent otherwise infers coding conventions from surrounding code each time. Instructions kept in the project apply to every task and survive plugin updates. `agentify` can scaffold them.

> [!TIP]
> Durable project context, exploring before generating, and verifying against the running site all improve task outcomes. See [Work effectively with KentiCopilot](https://docs.kentico.com/x/work_effectively_kenticopilot_guides) for details.
