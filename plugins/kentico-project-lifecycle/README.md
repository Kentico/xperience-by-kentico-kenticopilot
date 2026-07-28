# Kentico project lifecycle

Keep an Xperience by Kentico solution current and move changes between environments in a controlled way.

The skills in this plugin cover project lifecycle management such as applying [Xperience updates](https://docs.kentico.com/x/DwKQC) and the [Continuous Deployment](https://docs.kentico.com/x/YgaiCQ) side of a [CI/CD](https://docs.kentico.com/x/YAaiCQ) setup.

## Choose a skill

| Skill | Use it to |
|---|---|
| `update-xperience` | Update an Xperience project to a target version, or to the latest release |
| `cd-repository-configure` | Build a `repository.config` scoped to the changes you deploy, then export and verify the deployment content |

For enabling the repositories themselves, and for the full filtering reference, see [Configure CI/CD repositories](https://docs.kentico.com/x/ygAcCQ).

> [!TIP]
> New to agent skills? Agents activate skills as necessary based on the assigned task. Alternatively, you can use slash commands and other methods depending on your assistant. For what that means in practice, read [Invoke a skill](../../docs/Usage-Guide.md#invoke-a-skill).

## Requirements

- An Xperience by Kentico project
- An AI coding assistant with this plugin installed
- The [Documentation MCP server](https://docs.kentico.com/x/mcp_server_xp), configured as described in [MCP setup](./MCP-setup.md)

## Install

Follow the marketplace instructions in the [usage guide](../../docs/Usage-Guide.md#install-the-selected-plugin), using the plugin name `kentico-project-lifecycle`.

## Run your first update

This example shows how to use the skills in this plugin to update an Xperience by Kentico project to the latest version.

1. Use the `update-xperience` skill in your coding assistant to start the project update.

   ```text
   /update-xperience
   ```

   The agent reads your `Kentico.Xperience.*` package references to establish the current version, then reads every [Changelog](https://docs.kentico.com/x/6wocCQ) entry between that version and the latest release, together with the update guides those entries link to. The agent follows the documented [update procedure](https://docs.kentico.com/x/DwKQC) rather than a procedure of its own.

2. The agent performs the update and outputs a final report. The report states what changed and which release-note items required action, including any manual follow-up that stays unapplied until you do it yourself.

3. Review all changes.

4. Start the application, then confirm the administration and the live site both come up on the new version.

The project now runs the target version with the code changes its release notes called for, and the report records everything the release notes left to you.

## Common tasks

### Update to a specific version

Pass the version when you need a known target, such as the version another environment already runs.

```text
/update-xperience 31.2.0
```

### Configure a scoped deployment

The skill rebuilds `repository.config` for the changes you select. For restoring the package on the target environment, see [Deploy to the SaaS environment](https://docs.kentico.com/x/IgKQC).

```text
/cd-repository-configure

Changes: PR 312
```

### Deploy several pull requests together

One configuration describes one deployment, so pull requests that travel together get selected together.

```text
/cd-repository-configure

Changes: PR 310, PR 311, PR 312
```

### Select changes by commit range

The `..` operator follows standard git syntax, so the start commit is **exclusive** and the end commit is **inclusive**. Use the commit before your first feature commit as the range start. To include `abc1234` itself, use its parent, as in `abc1234^..def5678`. To deploy one commit on its own, use `abc1234^..abc1234`.

```text
/cd-repository-configure

Changes: abc1234..def5678
```

## Write effective prompts

Both of these prompts produce a working output. However, providing the agent with more context significantly reduces guesswork and increases output quality and standards adherence. Compare:

| Prompt | What happens |
|---|---|
| `/cd-repository-configure Changes: PR 312` | The agent discovers the repository paths and reads the pull request. Expect questions about which project to configure and about anything the pull request leaves ambiguous. |
| `/cd-repository-configure Changes: PR 312. The Xperience project is ./src/DancingGoat, and the PR adds a content type together with the content items that use it.` | The agent maps the changed CI paths straight away, and includes the content items on purpose instead of asking whether you want them. |

> [!TIP]
> The same recommendations apply to every KentiCopilot skill. See [Write specific prompts](../../docs/Usage-Guide.md#write-specific-prompts) for the general guidance, including the habits that slow a session down.

## Customize

Record project-specific conventions in your project's agent instruction files. The agent otherwise infers your update and deployment conventions from the repository each time. Instructions kept in the project apply to every task and survive plugin updates.

> [!TIP]
> Durable project context, exploring before generating, and verifying against the running site all improve task outcomes. See [Work effectively with KentiCopilot](https://docs.kentico.com/x/work_effectively_kenticopilot_guides) for details.
