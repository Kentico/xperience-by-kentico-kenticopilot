# Infrastructure Discovery

Detailed procedures for automatically discovering KX13, XbyK, and Migration Tool infrastructure values from workspace projects.

## 1a. Locate Projects

Search the workspace for KX13 and XbyK projects:

- **KX13 indicators:** a `.csproj` with Kentico 13 package references (`Kentico.Xperience.AspNet.Mvc5` or `Kentico.Xperience.WebApp` for KX13), a `CMSApp.csproj` (Portal Engine), or a `web.config` with a CMS connection string
- **XbyK indicators:** a `.csproj` with `Kentico.Xperience.WebApp` package reference and XbyK-era version (27+)
- If multiple candidates exist for either, ask the user to confirm which project to use

## 1b. Extract Infrastructure Values

### KX13 source instance

- **Connection string:** read `appsettings.json` for `ConnectionStrings.CMSConnectionString` (.NET Core). If not found (Portal Engine / .NET Framework), read `web.config` for `<connectionStrings>` containing `CMSConnectionString`, or check `ConnectionStrings.config` if referenced via `configSource`
- **CMS root directory:** for .NET Core KX13, the `CMS` sibling folder of the MVC project. For Portal Engine (.NET Framework), the folder containing `web.config`. After discovering the CMS root, check whether it contains `media/` and/or `files/` subfolders. If those subfolders are missing or empty, search the workspace for alternative directories that do contain them (e.g., a separate media export folder). If an alternative is found, **ask the user** which path to use for `KxCmsDirPath` — present both the standard CMS root and the alternative with a note about which one actually contains media files. **Never override `KxCmsDirPath` away from the standard CMS root without explicit user confirmation.**
- **Development port / SourceInstanceUri:** read `Properties/launchSettings.json` — extract the `applicationUrl` from the available profiles (iisExpress, Kestrel, etc.). Also check the migration tool's existing `appsettings.json` (if present) for a pre-configured `SourceInstanceUri`. If `launchSettings.json` has multiple profiles with different URLs/ports, list all candidates and ask the user to confirm which URL the KX13 instance actually runs on. Do not assume https or a standard port — always verify the discovered URL's protocol (http vs https) and port match the actual running instance.
- **Framework detection:** read the `.csproj` and extract `<TargetFramework>`. Values `net6.0`, `net5.0`, `netcoreapp3.1` = .NET Core. Values containing `v4.8` or `v4.7` = .NET Framework 4.8

### XbyK target instance

- **Connection string:** read `appsettings.json` and extract `ConnectionStrings.CMSConnectionString`
- **Project root:** the directory containing the XbyK `.csproj`

### Migration Tool Extensions project

- **NuGet version check:** read the `Migration.Tool.Extensions.csproj` (or the main migration tool `.csproj`) and extract the version of `Kentico.Xperience.WebApp` or other Kentico packages. Then read the XbyK target project's `.csproj` (located in step 1a) and extract the version of the same `Kentico.Xperience.WebApp` package reference. If the two NuGet package versions do not match, **warn the user** and recommend aligning them (e.g., `dotnet add package Kentico.Xperience.WebApp --version <XBYK_PROJECT_VERSION>` in the Migration Tool Extensions project). A version mismatch between the migration tool extensions and the target XbyK project causes runtime startup failures.

## 1c. Prepare ToolApiController Deployment (conditional)

This sub-step applies only when `QuerySourceInstanceApi` will be needed (determined in Step 4). Gather the information now but defer actual file writes to Step 5:

- Read `toolapi-deployment-reference.md` for deployment procedures
- Check if `ToolApiController.cs` already exists in the KX13 project's `Controllers` folder
- If not present, prepare deployment:
  - Based on framework detection from 1b, select the correct controller variant:
    - .NET Core → `KX13.Extensions/ToolApiController.cs` from the migration tool repository
    - .NET Framework 4.8 → `KX13.NET48.Extensions/ToolApiController.NET48.cs`
  - Generate a cryptographically secure random secret (32+ bytes, base64-encoded)
  - Identify the route registration location (`Startup.cs` for .NET Core, `RouteConfig.cs` for .NET Framework)
- Store the generated secret and discovered port for use in `OptInFeatures.QuerySourceInstanceApi`
