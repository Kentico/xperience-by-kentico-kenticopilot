---
name: "update-xperience"
description: "Updates an Xperience by Kentico application to the latest available version. Identifies the current version, determines upgrade steps, updates NuGet packages, and guides through database migrations and code changes."
argument-hint: "Optional: target version to upgrade to (defaults to latest). Pass 'AgentMode' to skip interactive confirmations."
compatibility: "Requires Kentico Docs MCP"
---

You are tasked with updating an Xperience by Kentico application to the latest available version.

## Useful Documentation

Read these sources before making changes:

- https://docs.kentico.com/documentation/developers-and-admins/installation/update-xperience-by-kentico-projects
- https://docs.kentico.com/documentation/changelog
- https://docs.kentico.com/feeds/xbyk-releases.xml

## Input Parameters

- **Target Version** *(optional)* — The version to upgrade to. If not provided, upgrade to the latest available version.
- **AgentMode** *(optional flag)* — When specified, skips all interactive confirmations and runs non-interactively. Use in automated or scripted contexts.

## Preconditions (MUST validate before proceeding)

1. **Clean working tree**: run `git status --porcelain`. Output MUST be empty. If not, ask the user to commit or stash changes and stop.
2. **Not on the default branch**: confirm the current branch is not `main` (or the project's default branch). If it is, ask the user to create and switch to a feature branch and stop.
3. **Detect package management style**: check whether a `Directory.Packages.props` file exists at the repository root. This determines how NuGet packages are updated (see Step 2).
4. **Locate target web application project**: identify the project that should run the update commands. Use these heuristics together:
   - ASP.NET Core web app indicators: `appsettings*.json`, `wwwroot`, web host startup code in `Program.cs`.
   - The project participates in the target solution being updated.
   - The project references Xperience runtime packages required for update execution.

If any precondition fails: output a clear message explaining the issue and stop.

## Step 1 — Determine Current and Latest Xperience Version

1. Run `dotnet tool restore` at the repository root (if a `.config/dotnet-tools.json` is present).
2. Locate the solution file (`.sln` or `.slnx`) at the repository root.
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

1. Open `Directory.Packages.props`.
2. Identify all `<PackageVersion Include="Kentico.Xperience.*" Version="CURRENT_VERSION" />` entries.
3. Replace their `Version` attribute with the latest version identified in Step 1.
4. If any `Kentico.Xperience.*` package uses a `-preview` or `-prerelease` suffix and its base version matches the current version, update it to the corresponding pre-release of the new version if one is available.
5. Do **not** change any `Kentico.Xperience.*` packages whose version is intentionally pinned to a different value — these are integration packages maintained on their own release cadence. Leave them untouched and note them in the final summary.

### Projects using per-project package references (`.csproj` / `Directory.Build.props`)

1. Search all `.csproj` and `Directory.Build.props` files for `<PackageReference Include="Kentico.Xperience.*" Version="CURRENT_VERSION" />` entries.
2. Update each matching `Version` attribute to the latest version.
3. Apply the same pre-release and intentional-pin rules described above.

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
2. For each `@kentico/*` package found, choose update command by version specifier:
    - If version is pinned/exact (for example `"13.4.1"`):
       ```
       npm install @kentico/<package>@latest --save-exact
       ```
       Use `--save-dev --save-exact` for devDependencies.
    - If version is a range (for example `^13.4.1`, `~13.4.1`):
       ```
       npm update @kentico/<package>
       ```
3. Run `npm install` in each affected directory to update the lockfile.
4. If an `mcp.json` (or equivalent MCP configuration) file in the repository references versioned `@kentico/*` packages, update those version references to match.

## Step 5 — Database Backup Confirmation

Before updating the database, the Xperience application must not be running.

- **If AgentMode is NOT set**: ask the user: *"Have you backed up your Xperience database and project folder? The update cannot be rolled back without a backup."* Wait for confirmation before continuing.
- **If AgentMode is set**: skip the confirmation and proceed.

## Step 6 — Run the Xperience Application Update

The update command applies SQL scripts and file system changes to bring the database schema up to the new package version. The exact steps depend on whether the project uses Continuous Integration (CI).

### Detect whether CI is enabled

Preferred detection method: check whether CI is enabled in the database.

1. Obtain the database connection string from the project's `appsettings.json` (or `appsettings.Development.json`), or via `dotnet user-secrets list` if user secrets are configured.
2. Run the following SQL query against the Xperience database:

```sql
SELECT KeyValue FROM CMS_SettingsKey WHERE KeyName = N'CMSEnableCI'
```

- If the result is `True`: follow the **With CI** path below.
- If the result is `False` or the query returns no row: follow the **Without CI** path below.

If direct SQL access is not available to the agent (ex: via MCP tools):

1. Inspect CI repository presence as a fallback signal (for example `App_Data/CIRepository` and CI-related configuration files).
2. Ask the user to manually confirm whether CI is currently enabled in the database.
3. If CI status cannot be confirmed, stop and request confirmation before proceeding.

### Without CI

Run the update command from the directory containing the Xperience web project's `.csproj` file:

```
dotnet run --no-build -- --kxp-update --skip-confirmation
```

If the command fails: report the failure and **stop**. Do not commit.

### With CI

CI must be disabled before the update runs, then re-enabled and objects re-serialized afterward. Follow these steps in order:

1. **Disable CI** — run the following SQL against the Xperience database:
   ```sql
   UPDATE CMS_SettingsKey SET KeyValue = N'False' WHERE KeyName = N'CMSEnableCI'
   ```

2. **Run the update** — from the Xperience web project directory:
   ```
   dotnet run --no-build -- --kxp-update --skip-confirmation
   ```
   If the command fails: re-enable CI (step 3 below), report the failure, and **stop**. Do not commit.

3. **Re-enable CI** — run the following SQL against the Xperience database:
   ```sql
   UPDATE CMS_SettingsKey SET KeyValue = N'True' WHERE KeyName = N'CMSEnableCI'
   ```

4. **Re-serialize all CI objects** — from the Xperience web project directory:
   ```
   dotnet run --no-build -- --kxp-ci-store
   ```
   This regenerates the contents of the CI repository folder to reflect the updated database state.
5. If the project uses object type filtering (`<IncludedObjectTypes>`) in CI/CD repository configuration, review the changelog section for new CI/CD object types and add required types to configuration.

## Step 7 — Update Project Documentation

1. Locate where the project documents its current Xperience version — typically a `README.md` at the repository root, but may also be a `CHANGELOG.md` or a file in a `docs/` folder.
2. Update the version reference to the new version.
3. Update any changelog or release notes links:
   - Use Kentico Docs MCP to retrieve the Xperience by Kentico changelog, and review the RSS feed at `https://docs.kentico.com/feeds/xbyk-releases.xml`.
   - If project docs use a single release link, replace it with the entry URL for the **target version**.
   - If project docs maintain a version-by-version update log, append entries for all reviewed versions in the update path.

## Step 8 — Final Validation and Commit

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
- CI status and handling: <detected/enabled/disabled/manual confirmation>
- Xperience database update: <success/failure>
- Commit: <hash or not created>

## Notes
- <important warnings or manual follow-up>
```

If stopped early, keep the same structure and set failed or not created fields accordingly.

## Error Handling

| Scenario | Action |
|---|---|
| Dirty working tree | Abort; ask user to commit or stash changes. |
| On default branch | Abort; instruct user to create a feature branch. |
| `dotnet restore` fails after package update | Report error; abort. |
| Build fails post-update | Fix breaking API changes; do not proceed to database update until build succeeds. |
| User declines database backup confirmation | Abort; instruct user to create a backup first. |
| `--kxp-update` command fails with CI enabled | Re-enable CI, report failure, abort. Do not commit. |
| `--kxp-update` command fails without CI | Report failure; abort. Do not commit. |
| npm install/update fails | Report error; ask user before continuing. |
| SQL access unavailable for CI detection | Use CI repository fallback + ask user for manual confirmation; abort if still unknown. |

## Important rules

- **Breaking changes before database update** — all compilation errors from breaking API changes must be resolved before running `--kxp-update`. The database update cannot be rolled back without a backup.
- **Keep changes minimal** — do not update unrelated dependencies.
- **Never force-push or rewrite history**.
- **Do not proceed past any failure point** without explicit user instruction.
- **Intentionally pinned packages** — if a `Kentico.Xperience.*` package is pinned to a version that differs from the main package group, leave it alone and note it in the summary.
- **Direct version jumps are supported** — projects can update from any version directly to the latest; sequential intermediate steps are not required.
