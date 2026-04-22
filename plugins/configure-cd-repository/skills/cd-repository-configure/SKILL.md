---
name: cd-repository-configure
description: "Builds or updates CD Repository filters from CI Repository changes using a discovery context file and PR numbers or commit ranges, while excluding Xperience update-only noise by default."
argument-hint: "Path to discovery context folder and PR number(s) or commit hash range"
compatibility: "Works with GitHub CLI or local git"
---

You are tasked with creating a scoped CD Repository configuration from CI Repository changes.

## Input Parameters

- **Context Folder Path** - Folder that contains `cd-repository-context.json` produced by `cd-repository-discover`.
- **Change Selectors** - One or more PR numbers and/or one git commit range (for example `abc123..def456`).

## Prerequisite

Read `cd-repository-context.json` from the provided folder and validate:

- `appPath`
- `repositoryRoot`
- `ciRepositoryPath`
- `cdRepositoryConfigPath`
- `tooling.preferredChangeSource`

If the context file is missing or invalid, stop and ask the user to run `cd-repository-discover` (or fix the file).

## Workflow

1. Load discovery context and confirm paths still exist.
2. Resolve change source strategy:
   - Prefer `gh` when context says `gh-pr` and `gh` is available.
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
   - Preserve existing `RestoreMode` unless user asks to change it.
9. Validate XML and remove duplicate/contradicting filters.
10. Diff and explain exact changes.
11. If available in the repository, run deployment package export validation (`Export-DeploymentPackage.ps1`) and verify expected scoped output.

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
- `@global/cms.user` -> `cms.systemtable`
- `@global/cms.member` -> `cms.systemtable`
- `@global/cms.contact` -> `cms.systemtable`
- `@global/cms.class` -> `cms.class`
- `@global/emaillibrary.emailtemplate` -> `emaillibrary.emailtemplate`
- `@global/cms.settingskey` -> `cms.settingskey`
- `*/contentitemdata.*` or `*/cms.contentitemcommondata*` -> content item related (include only when intentionally in deployment scope)

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
- Exact impact on `repository.config`
- Validation result for deployment package export (if executed)
