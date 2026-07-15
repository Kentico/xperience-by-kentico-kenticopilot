---
name: cd-repository-configure
description: "Builds a scoped CD Repository configuration from CI Repository changes selected by PR numbers or a commit range, excluding Xperience version-update noise by default. Use when the user wants to deploy specific features, PRs, or commits to another Xperience by Kentico environment, scope repository.config for a Continuous Deployment run, or prepare a CD deployment package."
argument-hint: "[change-selector]"
compatibility: "Requires local git, PowerShell, and Kentico Docs MCP; PR selectors additionally require tooling for the repository host (CLI or MCP server, e.g., GitHub CLI or Azure DevOps MCP)."
---

You are tasked with creating a scoped CD Repository configuration from CI Repository changes.

## Input parameters

- **Change selector** – PR number(s) (e.g., `PR 312`, `PR 310, PR 311, PR 312`) or a git commit range (e.g., `abc123..def456`), not both.

## Workflow

### 1. Discover the environment

Resolve the following values, preferring any the user provided.

- **CI Repository path** – the `App_Data/CIRepository` folder.
- **CD config path** – the `App_Data/CDRepository/repository.config` file.

Read the `Version` attribute of the `<RepositoryConfiguration>` root element in the CD config file. **If the attribute is missing or has value `"1"`, stop** and inform the user:

> The repository.config file uses the legacy v1 syntax. The `cd-repository-configure` skill requires v2 syntax. Migrate the file following [Migrate CI/CD repository.config to v2](https://docs.kentico.com/documentation/developers-and-admins/ci-cd/configure-ci-cd-repositories/config-v2-migration), then run this skill again.

Offer to apply the documented migration steps (back up the original file first).

### 2. Collect and classify CI changes

For a PR selector, read each PR's title, description, and changed files using whatever tooling is available for the repository's host (its CLI or an MCP server exposing pull requests). If none is available, stop and ask the user to connect one or provide a commit range instead.

For a commit range, walk the commits oldest to newest and inspect each one's changes under the CI Repository path.

Classify each PR or commit as **business/feature** or **Xperience update-only** (see Change classification) and track its changes. Keep only files under the CI Repository path. Exclude every PR or commit classified as Xperience update-only unless the user explicitly opts in.

### 3. Write the scoped configuration

1. Map the remaining CI paths to object types and code names following `references/ci-path-mapping.md`.
2. Regenerate the config file at the CD config path following `references/repository-config-guidelines.md`. Rebuild the filter sections from scratch for the current deployment scope — do not merge with filters left over from previous deployments.
3. Validate that the XML is well-formed, remove duplicate or contradicting filters, and run the quality checklist from the guidelines reference.
4. Track the exact changes made to `repository.config`, to report in the deployment summary.

### 4. Generate and verify the deployment content

1. Run the project's `Export-DeploymentPackage.ps1` if present in the Xperience app root; otherwise run the store operation directly – see [Store objects to a CD Repository](https://docs.kentico.com/documentation/developers-and-admins/ci-cd/continuous-deployment#store-objects-to-a-cd-repository).
2. Verify the generated CD Repository against the configured scope with this skill's bundled `Verify-CdRepository.ps1` script (in its `scripts/` folder), passing the folder containing `repository.config` as `-RepositoryPath`.
3. If the script reports failures, re-check the mapping against `references/ci-path-mapping.md`, fix the config, and re-run the store and the verification. Include the script's report in the deployment summary.

## Change classification

**Xperience update-only** means platform/package updates, hotfixes, or version migrations with no feature intent. If classification is uncertain, exclude and report it in the summary so the user can opt in.

If only content item data changed (no content type or other configuration changes), suggest [Content sync](https://docs.kentico.com/documentation/business-users/content-sync) as a simpler alternative before continuing with CD.

## Output format

Finish with a deployment summary that follows `assets/DEPLOYMENT_SUMMARY_TEMPLATE.md`. Fill in every placeholder and keep every section.

## References

- `references/ci-path-mapping.md` – CI path → config entry translations and the special cases (content items, forms, reusable field schemas, workspaces). Read before mapping CI paths — several CI folder names do not map 1:1 to object types.
- `references/repository-config-guidelines.md` – allowlist rules, content item filter dependencies, `RestoreMode` selection, formatting, and the quality checklist. Read before regenerating the config.
- `references/documentation-links.md` – map of the relevant Kentico documentation pages with when-to-read hints. Fetch pages via the Kentico Docs MCP when a topic goes beyond the bundled references.
