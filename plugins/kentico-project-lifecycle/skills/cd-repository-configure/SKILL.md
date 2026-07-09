---
name: cd-repository-configure
description: "Builds a scoped CD Repository configuration from CI Repository changes selected by PR numbers or a commit range, excluding Xperience version-update noise by default. Use when the user wants to deploy specific features, PRs, or commits to another Xperience by Kentico environment, scope repository.config for a Continuous Deployment run, or prepare a CD deployment package."
argument-hint: "PR number(s) or commit hash range"
compatibility: "Requires local git and Kentico Docs MCP; PR selectors additionally require tooling for the repository host (CLI or MCP server, e.g., GitHub CLI or Azure DevOps MCP)."
---

You are tasked with creating a scoped CD Repository configuration from CI Repository changes.

## Input parameters

- **Change selectors** – **either** PR number(s) **or** a git commit range (not both):
  - **PR mode:** one or more PR numbers (e.g., `PR 312` or `PR 310, PR 311, PR 312`) — for deploying specific, discrete changes.
  - **Commit mode:** one git commit range (e.g., `abc123..def456` or `abc123^..def456`) — for deploying all changes between two commits.
- **Paths** *(optional)* – the Xperience app path and/or the CD `repository.config` path.

## Workflow

### 1. Discover the environment

Resolve the following values, preferring any the user provided. If a value is ambiguous (for example, multiple Xperience apps in the workspace), ask the user instead of guessing.

- **CI Repository path** – the `App_Data/CIRepository` folder.
- **CD config path** – the `App_Data/CDRepository/repository.config` file (the Xperience app root is the parent folder of `App_Data`).

Read the `Version` attribute of the `<RepositoryConfiguration>` root element in the CD config file. **If the attribute is missing or `"1"`, stop** and inform the user:

> The repository.config file uses the legacy v1 syntax. The `cd-repository-configure` skill requires v2 syntax. Migrate the file following [Migrate CI/CD repository.config to v2](https://docs.kentico.com/documentation/developers-and-admins/ci-cd/configure-ci-cd-repositories/config-v2-migration), then run this skill again.

Offer to apply the documented migration steps (back up the original file first).

### 2. Collect and classify CI changes

- **PR mode** — needs tooling that can read pull requests on the repository's host:
  1. Identify the host from `git remote get-url origin` (GitHub, Azure DevOps, GitLab, ...).
  2. Pick an available tool for that host — its CLI (e.g., `gh pr view <number> --json title,body,files` for GitHub, `az repos pr show --id <number>` for Azure DevOps) or an MCP server that exposes its pull requests. If none is available, stop and ask the user to connect one or provide a commit range instead.
  3. For each PR, get its title, description, and changed files. Classify each PR as **business/feature** or **Xperience update-only** (see Change classification). Track changes per PR.
- **Commit mode** — uses local git:
  1. List the commits in the range: `git log <range> --pretty=format:"%H %s"`.
  2. For each commit, oldest to newest: get its CI changes with `git show <commit-hash> --name-status -- <ciRepositoryPath>` and classify the commit as **business/feature** or **Xperience update-only** (see Change classification). Track changes per commit.

Keep only files under the CI Repository path. Exclude every PR or commit classified as Xperience update-only unless the user explicitly opts in.

### 3. Write the scoped configuration

1. Map the remaining CI paths to object types and code names following `references/ci-path-mapping.md`.
2. Regenerate the config file at the CD config path following `references/repository-config-guidelines.md`. Rebuild the filter sections from scratch for the current deployment scope — do not merge with filters left over from previous deployments.
3. Validate that the XML is well-formed, remove duplicate or contradicting filters, and run the quality checklist from the guidelines reference.
4. Diff and explain the exact changes made to `repository.config`.

### 4. Generate and verify the deployment content

1. Run `Export-DeploymentPackage.ps1` if it exists in the Xperience app root (it wraps the CD store operation). Otherwise run the store directly – see [Store objects to a CD Repository](https://docs.kentico.com/documentation/developers-and-admins/ci-cd/continuous-deployment#store-objects-to-a-cd-repository).
2. Verify the generated CD Repository against the configured scope (the store operation fills the CD Repository folder with serialized XML):

   ```powershell
   skills/cd-repository-configure/scripts/Verify-CdRepository.ps1 -RepositoryPath "<folder containing repository.config>"
   ```

3. If the script fails (exit code 1 – an included code name or content item has no serialized file): re-check the mapping against `references/ci-path-mapping.md`, fix the config, and re-run the store and the verification. Include the script's report in the deployment summary.

## Change classification

A change is **Xperience update-only** when it comes from platform or package update work rather than intentional configuration or content modeling changes. Signals:

- The PR/commit title or description indicates an Xperience update, hotfix, NuGet/package bump, or version migration.
- Bulk CI churn tied to upgrade commits with no explicit feature intent.

Decision rules:

- **Classification uncertain?** Exclude the group and report it in the summary so the user can opt in.
- **Only content item data changed** (no content type, schema, or other configuration changes)? Suggest [Content sync](https://docs.kentico.com/documentation/business-users/content-sync) as a simpler alternative; continue if the user still wants CD.

## Output format

Finish with a deployment summary that follows `assets/DEPLOYMENT_SUMMARY_TEMPLATE.md`. Fill in every placeholder and keep every section; the template's comments explain the per-mode (PR vs. commit range) adjustments.

## References

- `references/ci-path-mapping.md` – CI path → config entry translations and the special cases (content items, forms, reusable field schemas, workspaces). Read before mapping CI paths — several CI folder names do not map 1:1 to object types.
- `references/repository-config-guidelines.md` – allowlist rules, content item filter dependencies, `RestoreMode` selection, formatting, and the quality checklist. Read before regenerating the config.
- `references/documentation-links.md` – map of the relevant Kentico documentation pages with when-to-read hints. Fetch pages via the Kentico Docs MCP when a topic goes beyond the bundled references.
