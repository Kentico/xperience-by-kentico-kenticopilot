# Pre-Flight Checks

Before running any migration command, verify the environment is ready by completing all checks below.

## 2a. Locate the Migration Tool CLI

Find the `Migration.Tool.CLI` project directory:

- Search the workspace for `Migration.Tool.CLI.csproj` or `Kentico.Migration.Tool.csproj`
- Verify the project exists and note its path

## 2b. Validate appsettings.json

Read the `appsettings.json` in the CLI project directory and verify:

- **Connection strings** are real values (not `<PLACEHOLDER>` or `[TODO]` — flag these to the user)
- **ConvertClassesToContentHub** is a semicolon-separated string (not a JSON array — fix if needed)
- **CreateReusableFieldSchemaForClasses** is a semicolon-separated string (not a JSON array — fix if needed)
- **EntityConfigurations** matches exclusions from the migration plan
- **QuerySourceInstanceApi** — if `Enabled: true`, note that the KX13 source instance must be running. **Test reachability:** attempt an HTTP GET to the configured `SourceInstanceUri` (e.g., `Invoke-WebRequest -Uri "<SourceInstanceUri>/ToolApi/Test" -Method POST -Body '{"secret":"<secret>"}' -ContentType 'application/json' -ErrorAction SilentlyContinue`). If unreachable, warn that `--pages` will block until the API responds and recommend either starting the KX13 instance or setting `Enabled: false` to fall back to legacy widget migration mode
- **MigrationProtocolPath** — if not set, warn the user: "Protocol log path is not configured — migration logs won't be available for `migrate-eval`. Set `Settings.MigrationProtocolPath` in appsettings.json to enable post-migration evaluation." Stop and confirm before proceeding.

## 2c. Validate Build

Run `dotnet build` on the migration tool solution or the Extensions project to confirm it compiles:

- If the build fails, diagnose and fix compilation errors before proceeding
- Pay attention to NuGet package version warnings

## 2d. Validate NuGet Package Versions

Check that the Kentico NuGet package versions in the migration tool project match the target XbyK database version:

- Read the migration tool `.csproj` files and extract Kentico package versions
- If the XbyK connection string is available, query: `SELECT KeyValue FROM CMS_SettingsKey WHERE KeyName = 'CMSDBVersion'` using `sqlcmd`
- If versions don't match, **warn the user** and recommend updating NuGet packages before proceeding (version mismatch causes runtime startup failures)

## 2e. Check for TODO Placeholders in Code

Search the `Migration.Tool.Extensions` source files for `TODO` comments:

- If any TODO placeholders exist in GUID-valued positions (e.g., taxonomy group GUIDs, taxonomy tag GUIDs), warn the user that these will cause runtime parse errors
- Recommend replacing with `Guid.Empty` (`00000000-0000-0000-0000-000000000000`) as safe defaults, or resolving the actual GUIDs from the XbyK database

## 2f. Confirm Pre-Migration Steps

Check the migration plan's **Pre-Migration** section for any manual steps. Ask the user to confirm these have been completed (e.g., "Have you created the DayOfWeek taxonomy in XbyK?").
