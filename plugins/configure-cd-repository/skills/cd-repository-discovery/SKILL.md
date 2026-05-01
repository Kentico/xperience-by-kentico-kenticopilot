---
name: cd-repository-discovery
description: "Discovers Xperience by Kentico deployment context (app path, CI/CD repository paths, and available git tooling) and saves it to a reusable context file for follow-up CD repository configuration."
argument-hint: "Path to a folder where discovery context should be written"
---

You are tasked with discovering repository and environment context required to build CD Repository filters.

## Input Parameters

- **Context Folder Path** - User-provided folder where you must write the discovery context file.

## Primary Goal

Create a machine-readable context file named `cd-repository-context.json` in the provided folder.

## Context File Contract

Write a JSON file with this shape (add additional helpful fields when available):

```json
{
  "generatedAtUtc": "ISO-8601 UTC timestamp",
  "repositoryRoot": "absolute path to git repository root",
  "appPath": "absolute path to Xperience by Kentico app",
  "ciRepositoryPath": "{appPath}/App_Data/CIRepository",
  "cdRepositoryConfigPath": "{appPath}/App_Data/CDRepository/repository.config",
  "tooling": {
    "ghAvailable": true,
    "gitAvailable": true,
    "preferredChangeSource": "gh|local-git"
  },
  "discovery": {
    "appPathSource": "user|discovered",
    "ciPathSource": "user|default|discovered",
    "cdConfigSource": "user|default|discovered",
    "repositoryConfigVersion": "2",
    "notes": [
      "optional notes about assumptions or fallbacks"
    ]
  }
}
```

## Steps To Follow

1. Ask the user for optional known values:
   - Xperience app path
   - CI Repository path
   - CD repository.config path
   - Preferred change source (`gh` or `local-git`)

2. Discover missing values automatically:
   - Search for `App_Data/CDRepository/repository.config`.
   - Search for `App_Data/CIRepository`.
   - Infer `appPath` as the parent folder of `App_Data`.
   - If multiple candidates exist, pick the best match by proximity/consistency and note alternatives in `discovery.notes`.

3. Validate all required paths exist:
   - `appPath`
   - `ciRepositoryPath`
   - `cdRepositoryConfigPath`

4. Detect repository.config syntax version:
   - Open the `repository.config` file and check the root `<RepositoryConfiguration Version="X">` attribute.
   - Record the version ("1" or "2") in `discovery.repositoryConfigVersion`.
   - If Version attribute is missing, assume v1 and record accordingly.

5. Detect tool availability:
   - Check if `gh` CLI is available.
   - Check if local `git` is available.
   - Set `preferredChangeSource`:
     - Use `gh` when `gh` is available and user did not force local git.
     - Otherwise use `local-git`.

6. Ensure the context folder exists, then write `cd-repository-context.json`.

7. Print a concise summary of what was discovered and where the context file was written.

## Rules

- Do not edit `repository.config` in this skill.
- Do not guess silently. If a critical value cannot be determined, ask a focused follow-up question.
- Keep paths absolute in the context file.
- Preserve user-provided values when valid; only fall back to discovery/defaults when needed.
- If both `gh` and `git` are unavailable, still write context with clear error notes and ask user to resolve tooling.
- If `repository.config` uses v1 syntax, record this in the context file and include a warning in the output summary.

## Output Format

Conclude with:

- Context file path
- App path
- CI repository path
- CD repository.config path
- Repository.config syntax version
- Tooling status (`gh`, `git`, chosen source)
- Any assumptions/fallbacks recorded
- **If v1 syntax detected:** Include this warning in the summary:
  > ⚠️ **Note:** The repository.config file uses the legacy v1 syntax. Before running `cd-repository-configure`, you must upgrade to v2 syntax using the `cd-repository-upgrade` skill. See [Migrate CI/CD repository.config to v2](https://docs.kentico.com/documentation/developers-and-admins/ci-cd/configure-ci-cd-repositories/config-v2-migration) for details.
