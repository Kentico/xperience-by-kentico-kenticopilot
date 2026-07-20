# Kentico project lifecycle

Skills for maintaining an Xperience by Kentico solution after initial development: updating Xperience and creating deployment-scoped [Continuous Deployment Repository](https://docs.kentico.com/x/continuous_deployment) configuration.

## Choose a skill

| Skill | Use it to | Activation |
|---|---|---|
| `update-xperience` | Update an Xperience project to a target or latest version | Invoke by name |
| `cd-repository-configure` | Generate and verify `repository.config` filters from selected PRs or commits | Invoke by name with change selectors |

### Xperience updates

`update-xperience` determines the current and target versions, reviews every intervening [Changelog](https://docs.kentico.com/changelog) entry and linked update guide, and follows the [official update procedure](https://docs.kentico.com/documentation/developers-and-admins/installation/update-xperience-by-kentico-projects).

### CD Repository configuration

`cd-repository-configure`:

1. Discovers the application, CI Repository, and CD Repository configuration.
2. Reads and classifies changes from selected PRs or a git commit range.
3. Regenerates deployment-scoped filters in `repository.config`.
4. Exports and verifies the serialized deployment content when project tooling permits.

Xperience-version-only changes are excluded by default. The skill requires v2 `repository.config` syntax; for v1, it directs you to the [v2 migration guide](https://docs.kentico.com/documentation/developers-and-admins/ci-cd/configure-ci-cd-repositories/config-v2-migration).

## Requirements

- An Xperience by Kentico project
- An AI coding assistant with this plugin installed
- Kentico Docs MCP, configured as described in [MCP setup](./MCP-setup.md)
- For CD Repository work:
  - CI/CD Repository enabled with v2 `repository.config` syntax
  - Local git
  - Repository-host tooling for PR selectors, such as `gh`, `az repos`, or a suitable MCP server

## Install

Follow the marketplace instructions in the [usage guide](../../docs/Usage-Guide.md#install-the-selected-plugin), using the plugin name `kentico-project-lifecycle`.

## Use the plugin

### Update your Xperience project

The update skill identifies your current and target Xperience versions, reviews the release notes for every version in between (including the feature-specific update guides they link to), and follows the official update documentation.

```text
/update-xperience
```

To update to a specific version instead of the latest:

```text
/update-xperience <target-version>
```

### Configure the CD Repository

Provide the PR numbers or the git commit range you want to deploy. When your workspace contains more than one Xperience app, also mention the app path.

```text
/cd-repository-configure

Changes: PR 312
```

```text
/cd-repository-configure

Changes: PR 310, PR 311, PR 312
```

The `..` range operator follows standard git syntax: the start commit is **exclusive** and the end commit is **inclusive**. Use the commit just before your first feature commit as the range start.

```text
/cd-repository-configure

Changes: abc1234..def5678
```

To include `abc1234` itself, use its parent as the range start (`abc1234^..def5678`). To deploy exactly one commit in isolation, use `abc1234^..abc1234`.

## Output

`update-xperience` produces the project and dependency changes required for the selected version and reports any manual follow-up from the version-specific guidance.

`cd-repository-configure` produces an updated `repository.config` plus a deployment summary covering:

- Analyzed selectors with per-commit/PR classification — included (business/feature) vs. excluded (Xperience update-only), with reasons
- The chosen `RestoreMode` and the selected object types, code names, and content item filters
- Exactly what changed in `repository.config`
- Results of the deployment package export and the verification script, when run

## Best practices

- Keep Xperience version-update PRs separate from feature PRs where possible — this makes classification unambiguous and exclusion automatic.
- Review the generated `repository.config` diff before deploying, especially for the first run on a project.
- The skill rebuilds the deployment filters from scratch on every run; it asks before removing entries it did not create (for example, standing manual exclusions).

## Included resources

- [`update-docs.md`](./skills/update-xperience/references/update-docs.md) maps the Changelog and update procedure.
- [`ci-path-mapping.md`](./skills/cd-repository-configure/references/ci-path-mapping.md) maps CI Repository paths to CD configuration.
- [`repository-config-guidelines.md`](./skills/cd-repository-configure/references/repository-config-guidelines.md) defines filter and formatting rules.
- [`documentation-links.md`](./skills/cd-repository-configure/references/documentation-links.md) maps the current CD documentation.
- [`DEPLOYMENT_SUMMARY_TEMPLATE.md`](./skills/cd-repository-configure/assets/DEPLOYMENT_SUMMARY_TEMPLATE.md) defines the final report.
- [`Verify-CdRepository.ps1`](./skills/cd-repository-configure/scripts/Verify-CdRepository.ps1) checks that configured objects were serialized.
