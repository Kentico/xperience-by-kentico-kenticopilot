---
name: cd-repository-configure
description: "Builds or updates CD Repository filters from CI Repository changes using a discovery context file and PR numbers or commit ranges, while excluding Xperience update-only noise by default."
argument-hint: "Path to discovery context folder and PR number(s) or commit hash range"
compatibility: "Works with GitHub CLI or local git"
---

You are tasked with creating a scoped CD Repository configuration from CI Repository changes.

## Input Parameters

- **Context Folder Path** - Folder that contains `cd-repository-context.json` produced by `cd-repository-discovery`.
- **Change Selectors** - Provide **either** PR number(s) **or** a git commit range (not both):
  - **PR mode:** One or more PR numbers (e.g., `PR 312` or `PR 310, PR 311, PR 312`)
  - **Commit mode:** One git commit range (e.g., `abc123..def456`)

Choose PR mode when deploying specific, discrete changes; choose commit mode when deploying all changes between two commits.

## Prerequisite

Read `cd-repository-context.json` from the provided folder and validate:

- `appPath`
- `repositoryRoot`
- `ciRepositoryPath`
- `cdRepositoryConfigPath`
- `tooling.preferredChangeSource`
- `discovery.repositoryConfigVersion` must be `"2"`

If the context file is missing or invalid, stop and ask the user to run `cd-repository-discovery` (or fix the file).

**If `discovery.repositoryConfigVersion` is `"1"` or missing:** Stop and inform the user:
> "The repository.config file uses the legacy v1 syntax. The `cd-repository-configure` skill requires v2 syntax. Please run the `cd-repository-upgrade` skill to migrate your configuration to v2, then run `cd-repository-discovery` again to generate an updated context file. See [Migrate CI/CD repository.config to v2](https://docs.kentico.com/documentation/developers-and-admins/ci-cd/configure-ci-cd-repositories/config-v2-migration) for details."

## Workflow

1. Load discovery context and confirm paths still exist.
2. Resolve change source strategy:
   - Prefer `gh` when context says `gh` and `gh` is available.
   - Otherwise use local git commands.
3. Collect changed files for each selector:
   - PR mode: collect file list from each PR.
   - Commit range mode: run `git diff --name-only` from `repositoryRoot`.
4. Keep only files under `ciRepositoryPath`.
5. Classify CI changes into:
   - **Business/feature changes** (include in deployment filtering)
   - **Xperience update-only changes** (exclude by default)
6. Exclude update-only changes unless user explicitly requests inclusion.
7. Map remaining CI paths to object types and code names.
8. Update `cdRepositoryConfigPath`:
   - Build minimal `IncludedObjectTypes` allowlist (main object types only).
   - Add/merge `ObjectFilters` with one `IncludedCodeNames` entry per object type using semicolon-separated code names.
   - **Determine RestoreMode:**
     - Analyze git history: if **all** CI `.xml` files under `ciRepositoryPath` are **new** (created), use `Create` mode for better performance.
     - If **any** CI `.xml` files are **modified** (updated), use `CreateUpdate` mode to preserve existing objects.
     - **Note:** Create mode has significant performance benefits, especially for content item deployments. Use CreateUpdate only when necessary for file updates.
9. Validate XML and remove duplicate/contradicting filters.
10. Diff and explain exact changes.
11. If available in the repository, run `Export-DeploymentPackage.ps1` to generate the deployment package. **Manually verify** that the generated package contains the expected scoped objects. (The script copies your CD Repository into the package; it does not validate CD filters.)

## Change Classification Guidance

Treat changes as **Xperience update-only** when they originate from platform/package update work (for example hotfix/version bump PRs or commits) and do not represent intentional business configuration/content modeling changes.

Common signals:

- PR/commit title or description indicates Xperience update, hotfix, NuGet/package bump, or version migration.
- Bulk CI churn tied to upgrade commits with no explicit feature intent.

When uncertain, default to safety:

- Exclude ambiguous update-related groups.
- Report exclusions clearly so user can opt in.

## Mapping Hints

Common CI paths to object types:

- `@global/cms.contenttype` -> `cms.contenttype`
- `@global/cms.user` -> `cms.systemtable` *(unintuitive: folder is `cms.user` but config uses `cms.systemtable`)*
- `@global/cms.member` -> `cms.systemtable`
- `@global/cms.contact` -> `cms.systemtable`
- `@global/cms.class` -> `cms.class` *(see note below about ambiguity)*
- `@global/emaillibrary.emailtemplate` -> `emaillibrary.emailtemplate`
- `@global/cms.settingskey` -> `cms.settingskey` *(excluded from CI/CD by default due to potentially sensitive data; flag this to the user if it appears in a diff)*

**Channel-scoped content (pages, emails, headless items):** These are stored under `<ChannelName>/` rather than `@global/`. For example, `DancingGoat/cms.contentitem/` contains pages for the DancingGoat website channel. When these paths appear in CI changes, use `IncludedContentItemsOfType` / `ContentItemFilters` (v2) rather than `ObjectFilters`. See [CI/CD object type reference](https://docs.kentico.com/documentation/developers-and-admins/ci-cd/reference-ci-cd-object-types#content-management) for details.

**Forms require two object types:** Both `@global/cms.form/` and `@global/cms.formclass/` must be included together. The `cms.formclass` files use a `bizform.` code name prefix (e.g., `bizform.userfeedback.xml`). Include both in `IncludedObjectTypes`:

  ```xml
  <IncludedObjectTypes>
    <ObjectType>cms.form</ObjectType>
    <ObjectType>cms.formclass</ObjectType>
  </IncludedObjectTypes>
  ```

**`@global/cms.class` ambiguity:** This folder covers both module class definitions and reusable field schema definitions. Inspect the file code names to determine which is present before scoping `ObjectFilters`.

**Reusable field schemas** -> Object type `cms.class`. Reusable field schema definitions are tracked via the `CMS.ContentItemCommonData` code name (file path `App_Data\CIRepository\@global\cms.class\cms.contentitemcommondata.xml`). To include them, add both an `IncludedObjectTypes` entry and an `ObjectFilters` entry:

  ```xml
  <IncludedObjectTypes>
    <ObjectType>cms.class</ObjectType>
  </IncludedObjectTypes>
  <ObjectFilters>
    <!-- Contains reusable field schema definitions -->
    <IncludedCodeNames ObjectType="cms.class">
      CMS.ContentItemCommonData
    </IncludedCodeNames>
  </ObjectFilters>
  ```

  See [CI/CD object type reference](https://docs.kentico.com/documentation/developers-and-admins/ci-cd/reference-ci-cd-object-types#content-management) for details.

## Decision Rules

- If `IncludedObjectTypes` is populated, it is an allowlist: include every required main object type.
- Child and binding objects follow parent inclusion rules.
- Prefer explicit object types over broad `IncludeAll` patterns.
- Add content-item-specific filters only when content item deployment is intentionally requested.
- Keep code name filters minimal and precise.

## Quality Checks

- XML is well-formed.
- No repeated `IncludedCodeNames` entries for the same object type.
- Code names match actual CI object code names from XML files.
- Update-only groups are excluded (unless user opted in).
- Final config diff is concise and justified.

## Output Format

Finish with a concise deployment summary:

- Reviewed source selectors (PRs / commit range)
- Selected object types for deployment
- Selected code names by object type
- Explicitly excluded update-only groups and why
- Determined RestoreMode and reasoning (Create vs. CreateUpdate based on git history)
- Exact impact on `repository.config`
- Validation result for deployment package export (if executed)

## Leveraging Kentico Documentation

This skill has access to the Kentico Docs MCP server. If you need deeper information on any topic (content item filtering, CD Repository configuration, object types, etc.) use the MCP server to search for "Repository configuration templates" or "Reference - CI/CD object types" for specific syntax and configuration examples.
