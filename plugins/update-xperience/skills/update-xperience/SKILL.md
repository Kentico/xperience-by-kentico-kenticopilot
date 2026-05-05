---
name: "update-xperience"
description: "Updates an Xperience by Kentico application to the latest available version. Identifies the current version, determines upgrade steps, updates NuGet packages, and guides through database migrations and code changes."
argument-hint: "Optional: target version to upgrade to (defaults to latest). Pass 'AgentMode' to skip interactive confirmations."
compatibility: "Requires Kentico Docs MCP; requires Microsoft Data API Builder CLI tool (REST API mode) prepared by update-xperience-prep"
---

You are tasked with updating an Xperience by Kentico application to the latest available version.

## Useful Documentation

Read these sources before making changes:

- https://docs.kentico.com/documentation/developers-and-admins/installation/update-xperience-by-kentico-projects
- https://docs.kentico.com/documentation/changelog
- https://docs.kentico.com/feeds/xbyk-releases.xml

## Input Parameters

- **Target Version** _(optional)_ — The version to upgrade to. If not provided, upgrade to the latest available version.
- **AgentMode** _(optional flag)_ — When specified, skips all interactive confirmations and runs non-interactively. Use in automated or scripted contexts.

## Preconditions (MUST validate before proceeding)

1. **Prep skill completed**: verify that `update-xperience-context.json` exists at the repository root and was created by the `update-xperience-prep` skill. If not, stop and instruct the user to run the prep skill first.
2. **Context file is valid**: parse `update-xperience-context.json` and verify required fields:
   - `xperienceProjectCsprojPath`
   - `usesCI`
   - `connectionString.source`
   - `ciSettingsKeyId` when `usesCI = true`
   - `dabConfigPath` when `usesCI = true`
     If validation fails, stop and report the error.
3. **DAB config is valid (only when CI is enabled)**:
   - If `usesCI = true`, first resolve the connection string from `connectionString.source`.
   - Run validation with `XBK_UPDATE_DB_CONNECTION` set in the command environment (required because `dab-config.json` uses `@env('XBK_UPDATE_DB_CONNECTION')`).
   - Shell examples:
     - Bash/zsh: `XBK_UPDATE_DB_CONNECTION="<resolved-connection-string>" dotnet dab validate --config <dabConfigPath>`
     - PowerShell: `$env:XBK_UPDATE_DB_CONNECTION = "<resolved-connection-string>"; dotnet dab validate --config <dabConfigPath>`
   - Exit code must be 0. If validation fails, stop and report the error.
4. **Clean working tree**: run `git status --porcelain`. Output MUST be empty. If not, ask the user to commit or stash changes and stop.
5. **Not on the default branch**:
   - Detect the default branch: run `git remote show origin` and read the `HEAD branch:` line (or use `git config init.defaultBranch`) to determine the project's default branch (typically `main` or `master`).
   - Confirm the current branch (via `git branch --show-current`) is **not** the default branch. If it is, ask the user to create and switch to a feature branch and stop.
6. **Detect package management style for the target solution/project**:
   - Search recursively from the repository root for all `Directory.Packages.props` files.
   - Determine whether the selected target solution/web project is governed by one of those files (nearest applicable `Directory.Packages.props` in its directory ancestry).
   - If exactly one applicable file is found for the target solution/project, use central package management and record that file path.
   - If multiple applicable candidates remain ambiguous, ask the user which one to use and stop until clarified.
   - If no applicable file governs the target solution/project, treat package management as per-project (`.csproj` / `Directory.Build.props`).
     This determines how NuGet packages are updated (see Step 2).
7. **Locate target solution and web project**:
   - Search recursively for `.sln` or `.slnx` files. If multiple solutions exist, prioritize the one that contains the Xperience web application (identified by `appsettings*.json`, `wwwroot`, and `Kentico.Xperience.*` references). If disambiguation is ambiguous, ask the user to specify the solution file.
   - Identify the web project within the solution that should run the update commands. Use these heuristics together:
     - ASP.NET Core web app indicators: `appsettings*.json`, `wwwroot`, web host startup code in `Program.cs`.
     - The project participates in the target solution being updated.
     - The project references Xperience runtime packages required for update execution.

If any precondition fails: output a clear message explaining the issue and stop.

## Step 1 — Determine Current and Latest Xperience Version

1. Run `dotnet tool restore` at the repository root (if a `.config/dotnet-tools.json` is present).
2. Locate the solution file (`.sln` or `.slnx`):
   - Search recursively from the repository root.
   - If multiple solutions are found, prioritize the one containing the identified web project (from precondition 7).
   - If still ambiguous, ask the user to specify the solution file path.
3. Run `dotnet list <solution-file> package --outdated` and analyze the output to identify whether `Kentico.Xperience.*` packages are outdated.
4. If no `Kentico.Xperience.*` packages are outdated: report "No new Xperience version available" and **stop**.
5. Record the **current version** and **latest version** for use in subsequent steps.
6. Build a release-impact checklist by reviewing changelog and RSS entries for **every version between current and target** (inclusive of the target):
   - Breaking API changes and deprecations
   - CI/CD changes (including newly introduced object types)
   - Behavior changes and preview-feature notes that may affect the project
     Use this checklist to drive Step 3 fixes and Step 6 CI/CD validation.

## Step 2 — Update NuGet Packages

### Projects using Central Package Management (`Directory.Packages.props`)

1. Open the applicable `Directory.Packages.props` file selected in precondition 6 (do not assume repository root).
2. Identify all `<PackageVersion Include="Kentico.Xperience.*" Version="*" />` entries (any version).
3. For each `Kentico.Xperience.*` package:
   - **If the version matches the current version identified in Step 1**: replace it with the target version.
   - **If the version differs from the current version** (e.g., intentionally pinned to an older or newer value, or at a different patch level): leave it untouched and note it in the final summary as an "intentionally pinned" package.
4. If any `Kentico.Xperience.*` package uses a `-preview` or `-prerelease` suffix and its base version matches the current version, update it to the corresponding pre-release of the new version if one is available.
5. Note: do not update packages outside the `Kentico.Xperience.*` namespace, even if they appear outdated.

### Projects using per-project package references (`.csproj` / `Directory.Build.props`)

1. Search all `.csproj` and `Directory.Build.props` files for `<PackageReference Include="Kentico.Xperience.*" Version="*" />` entries (any version).
2. For each `Kentico.Xperience.*` package:
   - **If the version matches the current version identified in Step 1**: update it to the target version.
   - **If the version differs from the current version**: leave it untouched and note it in the final summary as an "intentionally pinned" package.
3. Apply the same pre-release rules described above.

### After updating package versions (both styles)

Run `dotnet restore` to confirm the updated references resolve successfully. If restore fails, report the error and stop.

## Step 3 — Build and Resolve Breaking Changes

**All breaking API changes MUST be resolved before updating the database. Do not proceed to Step 5 until the build succeeds.**
**Step 6 uses `--no-build`, so this successful build output is required before update execution.**

1. Build the solution: `dotnet build <solution-file>`
2. If the build **succeeds**: proceed to Step 4.
3. If the build **fails**:
   - Identify whether errors reference removed, renamed, or changed Kentico APIs (check the Breaking changes sections of the [Kentico changelog](https://docs.kentico.com/documentation/changelog) using Kentico Docs MCP).
   - Fix all compilation errors caused by breaking API changes before continuing.
   - Re-build until the solution compiles cleanly.
   - If errors appear unrelated to the version update, report them and ask the user how to proceed.

## Step 4 — Update npm Packages (if applicable)

Skip this step if no `package.json` files in the repository declare `@kentico/*` dependencies.

1. Search all `package.json` files in the repository (at any depth) for dependencies or devDependencies starting with `@kentico/`.
2. For each `@kentico/*` package found, resolve it to the absolute latest stable version on npm:
   - **Always use `npm install @kentico/<package>@latest`** to fetch the absolute latest version regardless of the current specifier (pinned, range, or prerelease).
   - **Do not use `npm update @kentico/<package>`**, because it typically stays within the existing semver range and may not update the declared version in `package.json`.
   - Use `--save-exact` to pin the exact resolved version.
   - Use `--save-dev --save-exact` for devDependencies.
   - Example:
     ```bash
     npm install @kentico/base-ui@latest --save-exact
     npm install @kentico/test-utils@latest --save-exact --save-dev
     ```
3. Run `npm install` in each affected directory to update the lockfile.
4. If an `mcp.json` (or equivalent MCP configuration) file in the repository references versioned `@kentico/*` packages, update those version references to the newly resolved versions.

## Step 5 — Database Backup Confirmation

Before updating the database, the Xperience application must not be running.

- **If AgentMode is NOT set**: ask the user: _"Have you backed up your Xperience database and project folder? The update cannot be rolled back without a backup."_ Wait for confirmation before continuing.
- **If AgentMode is set**: skip the confirmation and proceed.

## Step 6 — Run the Xperience Application Update

The update command applies SQL scripts and file system changes to bring the database schema up to the new package version.

1. Read `update-xperience-context.json` and use:
   - `usesCI` to decide whether CI toggling is required
   - `xperienceProjectCsprojPath` to determine the web app directory
   - `connectionString.source` to retrieve connection string (only if `usesCI = true`)
   - `ciSettingsKeyId` to target the `CMSEnableCI` row (only if `usesCI = true`)
   - `dabConfigPath` to start DAB REST server (only if `usesCI = true`)

### Path A — CI not enabled (`usesCI = false`)

1. Run the update command from the Xperience web project directory:
   ```
   dotnet run --no-build -- --kxp-update --skip-confirmation
   ```
2. If the command fails: report the failure and **stop**. Do not commit.
3. Do not run `--kxp-ci-store` when CI is disabled.

### Path B — CI enabled (`usesCI = true`)

When CI is enabled, it must be disabled before update and re-enabled before `--kxp-ci-store`.

1. Retrieve the connection string according to `connectionString.source`:
   - `appsettings.Development.json`: read `ConnectionStrings:CMSConnectionString` from that file.
   - `appsettings.json`: read `ConnectionStrings:CMSConnectionString` from that file.
   - `user-secrets`: run `dotnet user-secrets list --project <xperienceProjectCsprojPath>` and read `ConnectionStrings:CMSConnectionString`.
2. Start DAB with `ASPNETCORE_URLS=http://127.0.0.1:50771`, `XBK_UPDATE_DB_CONNECTION=<resolved-connection-string>`, and `dotnet dab start --config <dabConfigPath>`.
3. Read `ciSettingsKeyId` from `update-xperience-context.json`.
   - Use this value as the `CMSEnableCI` row key.
   - If missing or invalid, stop and instruct the user to rerun `update-xperience-prep`.
4. Disable CI via HTTP PATCH:
   - `PATCH http://127.0.0.1:50771/api/SettingsKey/KeyID/<ciSettingsKeyId>`
   - Request body: `{\"KeyValue\": \"0\"}`
   - Expect: 200

   - Verify with GET:
     - `GET http://127.0.0.1:50771/api/SettingsKey/KeyID/<ciSettingsKeyId>` and confirm `KeyValue = \"0\"`

5. Run the update command from the Xperience web project directory:
   ```
   dotnet run --no-build -- --kxp-update --skip-confirmation
   ```
6. Wrap step 5 in try/finally so CI re-enable is guaranteed.
7. Re-enable CI via HTTP PATCH in finally:
   - `PATCH http://127.0.0.1:50771/api/SettingsKey/KeyID/<ciSettingsKeyId>`
   - Request body: `{\"KeyValue\": \"1\"}`
   - Expect: 200

   - Verify with GET:
     - `GET http://127.0.0.1:50771/api/SettingsKey/KeyID/<ciSettingsKeyId>` and confirm `KeyValue = \"1\"`

8. If update succeeded, run:
   ```
   dotnet run --no-build -- --kxp-ci-store
   ```
   If this command fails: report the failure and **stop**. Do not commit.
9. Kill the DAB subprocess after all CI operations complete (successful or failed).

## Step 7 — Post-Update Validation

Before committing, validate that the updated application builds and runs correctly.

1. **Build the solution**: run `dotnet build <solution-file>` from the repository root.
   - If the build **fails**: report the error and **stop**. Do not commit. Fix the compilation errors and rebuild until successful.
   - If the build **succeeds**: continue.

2. **Optional smoke tests**: if the repository includes test projects (`.csproj` files with test frameworks), run them:

   ```bash
   dotnet test <solution-file>
   ```

   If tests fail: report the failure, investigate, and repair before committing. (Optional: if tests are not critical, you may ask the user whether to proceed.)

3. **Optional startup verification**: if the Xperience application is configured to start quickly and safely in a CI/development environment, attempt a dry-run startup to verify the application boots without errors (e.g., with a timeout and graceful shutdown). This is optional but recommended for early detection of runtime issues.

## Step 8 — Update Project Documentation

1. Locate where the project documents its current Xperience version — typically a `README.md` at the repository root, but may also be a `CHANGELOG.md` or a file in a `docs/` folder.
2. Update the version reference to the new version.
3. Update any changelog or release notes links:
   - Use Kentico Docs MCP to retrieve the Xperience by Kentico changelog, and review the RSS feed at `https://docs.kentico.com/feeds/xbyk-releases.xml`.
   - If project docs use a single release link, replace it with the entry URL for the **target version**.
   - If project docs maintain a version-by-version update log, append entries for all reviewed versions in the update path.

## Step 9 — Final Validation and Commit

1. Run `git diff --name-only` and verify the changed files are consistent with the steps performed (package files, lockfiles, README/docs, CI repository files, generated files). Flag any unexpected changes for user review.
2. Stage the relevant changed files:
   ```pwsh
   git add <package-management-file> <readme/docs> <package.json files> <lockfiles> <CI repository files>
   ```
3. Commit using Conventional Commit format:
   ```
   build: update to Xperience by Kentico v{latestVersion}
   ```

## Output

When done, output using this exact structure:

```
# Update Complete

## Result
- Previous version: <old>
- Updated version: <new>
- NuGet packages updated: <count>
- NuGet packages intentionally skipped: <list or none>
- npm @kentico packages updated: <list or none>
- CI status and handling: <usesCI=true|false; toggled/skipped>
- Xperience database update: <success/failure>
- Commit: <hash or not created>

## Notes
- <important warnings or manual follow-up>
```

If stopped early, keep the same structure and set failed or not created fields accordingly.

## Error Handling

| Scenario                                                 | Action                                                                                                              |
| -------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------- |
| update-xperience-context.json missing or invalid         | Abort; instruct user to run update-xperience-prep skill first.                                                      |
| dab-config.json missing or invalid when CI enabled       | Abort; report validation error and stop.                                                                            |
| Dirty working tree                                       | Abort; ask user to commit or stash changes.                                                                         |
| On default branch                                        | Abort; instruct user to create a feature branch.                                                                    |
| `dotnet restore` fails after package update              | Report error; abort.                                                                                                |
| Build fails post-update                                  | Fix breaking API changes; do not proceed to database update until build succeeds.                                   |
| User declines database backup confirmation               | Abort; instruct user to create a backup first.                                                                      |
| DAB REST server fails to start                           | Report error; abort.                                                                                                |
| Connection string cannot be resolved from context source | Report error; abort.                                                                                                |
| `ciSettingsKeyId` missing/invalid in context             | Abort; instruct user to rerun `update-xperience-prep` to regenerate context and DAB artifacts.                      |
| DAB HTTP PATCH fails (disable CI)                        | Abort; report error and stop.                                                                                       |
| `--kxp-update` command fails with CI enabled             | Re-enable CI via HTTP PATCH (mandatory), report failure, abort. Do not commit.                                      |
| `--kxp-update` command fails without CI                  | Report failure; abort. Do not run `--kxp-ci-store`. Do not commit.                                                  |
| DAB HTTP PATCH fails (re-enable CI)                      | **CRITICAL**: CI is stuck off. Report prominently, abort, do not commit. User intervention required.                |
| DAB HTTP requests fail (due to network/port issues)      | Report error; check that port 50771 is available and not blocked by firewall. Ask user to verify port availability. |
| npm install/update fails                                 | Report error; ask user before continuing.                                                                           |

## Important rules

- **Prep skill required** — `update-xperience-prep` must be run once before this skill. It generates `update-xperience-context.json` and (when CI is enabled) `dab-config.json`.
- **Breaking changes before database update** — all compilation errors from breaking API changes must be resolved before running `--kxp-update`. The database update cannot be rolled back without a backup.
- **CI behavior is context-driven** — use `usesCI` from `update-xperience-context.json`. If false, skip DAB toggle and skip `--kxp-ci-store`. If true, disable CI before update and re-enable before `--kxp-ci-store`.
- **Try/finally on CI toggle** — when CI is enabled, the re-enable CI step (Step 6, step 6) **must** execute even if the update command fails. If re-enable fails, CI is stuck off and user intervention is required.
- **No secrets in files** — the context file stores only source metadata. The connection string value is read at runtime from the configured source and passed only to the DAB subprocess environment. Never write it to `.env`, files, or the parent shell.
- **Kill DAB subprocess** — after all steps (successful or failed), terminate the DAB process.
- **Keep changes minimal** — do not update unrelated dependencies.
- **Never force-push or rewrite history**.
- **Do not proceed past any failure point** without explicit user instruction.
- **Intentionally pinned packages** — if a `Kentico.Xperience.*` package version differs from the current version (e.g., a different patch level or major version), leave it unchanged and document it in the final summary as intentionally pinned. This typically applies to integration packages on independent release cadences.
- **Direct version jumps are supported** — projects can update from any version directly to the latest; sequential intermediate steps are not required.
