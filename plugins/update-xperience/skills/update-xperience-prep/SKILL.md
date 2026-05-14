---
name: "update-xperience-prep"
description: "One-time setup for XbK update workflow: validates .NET SDK, installs Data API Builder (configured for REST API access), discovers CI/connection-string context, writes update-xperience-context.json, and generates dab-config.json for CI toggling."
argument-hint: "Optional: path to XbK web project (defaults to auto-discovery)"
compatibility: "Requires .NET 8 SDK or later"
---

You are tasked with preparing an Xperience by Kentico project for autonomous update operations that require secure database access.

This skill is a **one-time setup**. Run it once per repository before running the main `update-xperience` skill.

## Prerequisites

- .NET 8 SDK (or later) installed
- Xperience by Kentico project

## Overview

This skill:

1. Validates the .NET SDK environment
2. Installs Microsoft's Data API Builder (local to the project, pinned version)
3. Locates the XbK web project and discovers CI usage
4. Detects where the connection string is sourced when CI is enabled (`appsettings.json`, `appsettings.Development.json`, or user-secrets)
5. Retrieves the connection string value when CI is enabled
6. Generates `dab-config.json` from a template with REST API enabled (CI-enabled projects only)
7. Validates `dab-config.json` with `dotnet dab validate` (CI-enabled projects only)
8. Selects a DAB listener port (CI-enabled projects only)
9. Runs a DAB REST API smoke test, resolves `CMSEnableCI` `KeyID`, and records it for the main skill (CI-enabled projects only)
10. Collects update preferences: documentation paths and whether to run tests
11. Writes `update-xperience-context.json` to the repository root for the main skill

Output:

- `update-xperience-context.json` at the repository root (no secrets)
- `dab-config.json` at the repository root when CI is enabled (template-derived, no secrets)
- these files can be committed for reuse by the main skill

## Step 1 — Validate .NET SDK

1. Run `dotnet --list-sdks`.
2. Verify at least one installed SDK is version 8.0 or later.
   - If no SDK 8.0+ is installed: report the installed SDK list and stop; instruct the user to install .NET 8 SDK (or later).
3. Run `dotnet --list-runtimes`.
4. Verify `Microsoft.NETCore.App` runtime version 8.0 or later is installed.
   - If runtime 8.0+ is missing: report the installed runtimes and stop; instruct the user to install .NET runtime 8 (or later).

## Step 2 — Verify or Install Data API Builder

DAB will be installed **locally to the project** (in `.config/dotnet-tools.json`) to ensure version consistency across developers and CI.
Pin DAB to `2.0.0-rc`.

1. Check if `.config/dotnet-tools.json` exists at the repository root.
   - If it does **not** exist, run `dotnet new tool-manifest` to create it.
2. Run `dotnet tool list --local` and search for `Microsoft.DataApiBuilder`.
   - If already installed: run `dotnet tool update Microsoft.DataApiBuilder --local --version 2.0.0-rc`.
   - If not installed: run `dotnet tool install Microsoft.DataApiBuilder --local --version 2.0.0-rc`.
3. Run `dotnet tool restore` to confirm the tool is restorable.
4. Verify: run `dotnet dab --version` and report the version installed.

## Context File Contract

See [references/context-file-contract.md](references/context-file-contract.md) for the full JSON schema and field descriptions.

## Step 3 — Locate XbK Web Project

1. Search for `.csproj` files in the repository that contain `appsettings.json` and `Kentico.Xperience` package references.
2. For the identified XbK web project, record:
   - `xperienceProjectCsprojPath` (absolute path)
   - `appPath` (directory containing the `.csproj`)
3. Detect CI usage:
   - Check whether `{appPath}/App_Data/CIRepository/repository.config` exists.
   - If it exists: `usesCI = true`.
   - If it does not exist: `usesCI = false`.

## Step 4 — Detect Connection String Source (Only if CI is enabled)

If `usesCI = false`:

- Set `connectionString.source = not-required`
- Set `ciSettingsKeyId = null`
- Set `dabConfigPath = null`
- Set `dabPort = null`
- Skip to Step 10.

If `usesCI = true`, detect where the connection string is sourced, in this order:

1. Check `appsettings.Development.json` in `appPath`:
   - If `ConnectionStrings:CMSConnectionString` exists and is non-empty, set source to `appsettings.Development.json`.
2. Else check `appsettings.json` in `appPath`:
   - If `ConnectionStrings:CMSConnectionString` exists and is non-empty, set source to `appsettings.json`.
3. Else check user-secrets:
   - Open the `.csproj` and locate `<UserSecretsId>`.
   - If `<UserSecretsId>` is missing, continue to step 4.
   - If `<UserSecretsId>` exists, run `dotnet user-secrets list --project <xperienceProjectCsprojPath>`.
   - If `ConnectionStrings:CMSConnectionString` exists, set source to `user-secrets`.
4. If none of the above locations provides a connection string, stop and ask the user to configure one.

Record:

- `connectionString.source`

## Step 5 — Retrieve Connection String (Only if CI is enabled)

When `usesCI = true`, retrieve the actual connection string from the selected source.

1. Retrieve the value from detected source:
   - `appsettings.Development.json` or `appsettings.json`: read `ConnectionStrings:CMSConnectionString`.
   - `user-secrets`: run `dotnet user-secrets list --project <xperienceProjectCsprojPath>` and read `ConnectionStrings:CMSConnectionString`.

## Step 6 — Write dab-config.json from Template (Only if CI is enabled)

If `usesCI = false`, skip this step.

1. Locate `dab-config-template.json` in the prep skill assets folder: `skills/update-xperience-prep/assets/dab-config-template.json`.
2. Copy it to the repository root: `./dab-config.json`.
3. The file uses `@env('XBK_UPDATE_DB_CONNECTION')` as a placeholder — **do not edit it**. The main skill will inject the connection string at runtime via subprocess environment variables.

If `dab-config.json` already exists at the root and was previously generated by this skill, you may overwrite it (it is safe; it contains no secrets).

Set `dabConfigPath` in the context file to the absolute path of this generated file.

## Step 7 — Validate Config (Only if CI is enabled)

If `usesCI = false`, skip this step.

Inject the connection string retrieved in Step 5 into the DAB process environment, then run validation.

```powershell
$env:XBK_UPDATE_DB_CONNECTION = "<connection-string-from-step-5>"
dotnet dab validate --config dab-config.json
```

**Important:** The connection string is passed to the subprocess environment only; never add it to the parent shell or logs.

Expected output: exit code 0, confirmation message. If validation fails:

- Report the error.
- **Do not commit**; stop and ask the user to review the error.

After validation, run:

```powershell
dotnet dab configure --config dab-config.json --show-effective-permissions
```

Confirm `SettingsKey` includes `create` and `update` for `anonymous` (DAB REST PATCH authorization evaluates both create and update permissions for PATCH/PUT).
If either action is missing, regenerate `dab-config.json` from the prep template and re-validate.

## Step 8 — Select DAB Port (Only if CI is enabled)

If `usesCI = false`, skip this step.

1. Generate a random candidate port in the range 49152–65535.
2. Present the candidate port to the user:
   > "DAB will listen on port `<candidate-port>`. Press Enter to accept or type a different port number:"
3. Accept the user's input (or use the candidate if they accept).
4. Record the accepted value as `dabPort` for the context file.

## Step 9 — DAB REST Smoke Test (Only if CI is enabled)

Use DAB's REST API to verify end-to-end readiness for the main `update-xperience` skill.

1. Start DAB as a REST server subprocess using the generated config, injecting the connection string retrieved in Step 5:

   ```powershell
   $env:ASPNETCORE_URLS = "http://127.0.0.1:<dabPort>"
   $env:XBK_UPDATE_DB_CONNECTION = "<connection-string-from-step-5>"
   dotnet dab start --config dab-config.json
   ```

   DAB will listen on `http://127.0.0.1:<dabPort>` (where `<dabPort>` is the port selected in Step 8). **Important:** The connection string is passed to the subprocess environment only; never add it to the parent shell or logs.

2. Run an HTTP GET request against the `SettingsKey` entity REST endpoint:
   ```powershell
   curl "http://127.0.0.1:<dabPort>/api/SettingsKey?`$filter=KeyName eq 'CMSEnableCI'"
   ```
3. Confirm:
   - DAB process starts successfully (HTTP server is listening).
   - HTTP GET request returns a 200 status code.
   - Response JSON includes exactly one record in the `value` array.
   - The returned row includes `KeyID`; capture this value as `ciSettingsKeyId` for Step 9.
4. Stop/kill the DAB subprocess (SIGTERM or equivalent).

If any check fails, report the error and stop. This indicates DAB is not ready for the main skill.

## Step 10 — Collect Update Preferences

Ask the user two questions before writing the context file.

**Documentation paths:**

> "Which files reference the Xperience version and should be updated automatically (e.g., README.md, docs/CHANGELOG.md)? Enter paths relative to the repository root, one per line. Leave blank to skip automatic documentation updates."

1. Accept zero or more paths from the user.
2. Validate that each provided path exists in the repository.
   - If a path does not exist, warn the user and exclude it from the list.
3. Record the accepted paths as `docPaths` (an array of relative paths, or an empty array if none provided).

**Run tests:**

> "Should the update skill run `dotnet test` after a successful build? (yes/no)"

4. Record the answer as `runTests` (boolean).

## Step 11 — Write update-xperience-context.json

1. Ensure repository root is known (`git rev-parse --show-toplevel`).
2. Write `update-xperience-context.json` at repository root with the contract above.
3. Ensure paths are absolute and booleans are actual JSON booleans.

## Output

When done, output using this exact structure:

```
# Xperience Update Prep Complete

## Result
- .NET SDK validation: <pass/fail, versions>
- .NET runtime validation: <pass/fail, versions>
- Data API Builder version: <version>
- Data API Builder location: .config/dotnet-tools.json
- XbK project identified: <path-to-.csproj>
- CI enabled: <true/false>
- Connection string source: <appsettings.json | appsettings.Development.json | user-secrets | not-required>
- CMSEnableCI KeyID: <integer | not-required>
- DAB port: <port number | not-required>
- Database connectivity: <OK or skipped>
- dab-config.json: <created and validated | skipped>
- Documentation paths: <list of paths | none>
- Run tests: <yes/no>
- update-xperience-context.json: <path>
- Status: ready for update-xperience skill

## Notes
- update-xperience-context.json is safe to commit (contains no secrets)
- dab-config.json is safe to commit (contains no secrets)
- Developers cloning the repo should run: dotnet tool restore
- Connection string value is never written to disk
```

If stopped early, set failed fields accordingly:

```
# Xperience Update Prep — Incomplete

## Result
- <step-name>: FAILED or NOT ATTEMPTED
- Reason: <error message or user instruction>

## Notes
- <any recovery steps>
```

## Error Handling

| Scenario                                                | Action                                                                                                                                        |
| ------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------- |
| .NET SDK 8.0+ is missing                                | Abort; instruct user to install .NET 8 SDK (or later).                                                                                        |
| .NET runtime 8.0+ is missing                            | Abort; instruct user to install .NET runtime 8 (or later).                                                                                    |
| DAB install fails                                       | Report error; abort.                                                                                                                          |
| XbK project not found                                   | Abort; ask user to specify project path or check repository structure.                                                                        |
| Connection string source not found (CI enabled)         | Abort; instruct user to configure `ConnectionStrings:CMSConnectionString` in appsettings or user-secrets.                                     |
| UserSecretsId not configured when needed                | Do not initialize user-secrets. Continue checking other sources; if none exist, ask the user to provide a supported connection string source. |
| DAB smoke test fails (startup, HTTP call, or DB access) | Abort; report connection/configuration error.                                                                                                 |
| CMSEnableCI lookup returns zero or multiple rows        | Abort; report ambiguity/error and ask user to verify database state before update.                                                            |
| dab validate fails                                      | Abort; report validation error (usually config syntax or DAB version issue).                                                                  |
| Context file write fails                                | Abort; report error and ask user to fix path/permissions.                                                                                     |

## Important rules

- **DAB REST enabled.** The template configures REST API and the skills bind DAB to `http://127.0.0.1:<dabPort>` via `ASPNETCORE_URLS`; the port is selected during prep and stored in `update-xperience-context.json` for use by the main skill.
- **Spawned inline.** The main skill launches and terminates the DAB process per update cycle.
