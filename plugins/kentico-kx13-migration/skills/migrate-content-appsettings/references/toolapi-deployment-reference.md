# ToolApiController Deployment Reference

Deploying the `ToolApiController` to the KX13 source instance enables Source Instance API Discovery. The migration tool queries this endpoint during `--pages` to discover widget, page template, and section form component metadata that is not stored in the database.

Two controller variants exist — one for .NET Core KX13 projects and one for .NET Framework 4.8 projects. The correct variant must be selected based on the KX13 project's target framework.

## Framework Detection

Read the KX13 project's `.csproj` file and check the `<TargetFramework>` element:

| `<TargetFramework>` value | Framework | Controller variant |
|---------------------------|-----------|-------------------|
| `net6.0`, `net5.0`, `netcoreapp3.1` | .NET Core | `KX13.Extensions/ToolApiController.cs` |
| `v4.8`, `v4.7`, `v4.7.2` | .NET Framework 4.8 | `KX13.NET48.Extensions/ToolApiController.NET48.cs` |

If the `<TargetFramework>` element is missing or ambiguous, ask the user.

## .NET Core Variant

**Source:** `KX13.Extensions/ToolApiController.cs` in the migration tool repository.

**Target:** `Controllers/ToolApiController.cs` in the KX13 MVC project.

**Steps:**

1. Copy the controller file to the KX13 project's `Controllers` folder.
2. Replace the `Secret` constant value with the generated secret (see [Secret Generation](#secret-generation)).
3. Register the `ToolExtendedFeatures` route in `Startup.cs` inside `UseEndpoints`, **before** other catch-all routes:

```csharp
endpoints.MapControllerRoute(
    name: "ToolExtendedFeatures",
    pattern: "{controller}/{action}",
    constraints: new { controller = "ToolApi" });
```

**Key details:**

- The controller uses ASP.NET Core `Microsoft.AspNetCore.Mvc.Controller` base class.
- It requires `IHttpContextAccessor` via constructor injection (typically already registered in KX13 projects).
- The `IsLocal` check restricts access to localhost requests only.

## .NET Framework 4.8 Variant

**Source:** `KX13.NET48.Extensions/ToolApiController.NET48.cs` in the migration tool repository.

**Target:** `Controllers/ToolApiController.cs` in the KX13 project (rename on copy).

**Steps:**

1. Copy the controller file to the KX13 project's `Controllers` folder, renaming to `ToolApiController.cs`.
2. Replace the `Secret` constant value with the generated secret.
3. Register the `ToolExtendedFeatures` route in `App_Start/RouteConfig.cs` (or equivalent), **before** other catch-all routes:

```csharp
routes.MapRoute(
    name: "ToolExtendedFeatures",
    url: "{controller}/{action}",
    defaults: new { },
    constraints: new { controller = "ToolApi" });
```

**Key details:**

- The controller uses `System.Web.Mvc.Controller` base class.
- Locality check uses `HttpContext.Request.IsLocal`.
- JSON serialization uses `Newtonsoft.Json` via a custom `ToJsonResult` helper.

## Secret Generation

The secret authenticates requests between the migration tool and the KX13 instance.

**Requirements:**

- Cryptographically random, minimum 32 bytes
- Base64-encoded (produces ~44 characters)
- The **same secret** must appear in both:
  1. The `ToolApiController` source code — the `Secret` constant
  2. `appsettings.json` — `Settings.OptInFeatures.QuerySourceInstanceApi.Connections[].Secret`

**Generation example (PowerShell):**

```powershell
[System.Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
```

**Generation example (C#):**

```csharp
Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
```

Never reuse the default placeholder secret from the migration tool repository.

## Testing the Endpoint

After deploying the controller, building, and starting the KX13 instance:

```http
POST http://localhost:<PORT>/ToolApi/Test
Content-Type: application/json

{"secret": "<GENERATED_SECRET>"}
```

**Expected response:** `{"pong": true}`

**Troubleshooting:**

- **404 Not Found** — route not registered, or registered after a catch-all route that intercepts the request first. Verify the `ToolExtendedFeatures` route is placed before other routes.
- **403 Forbidden** — secret mismatch or request is not from localhost. Verify the secret matches and the request originates from the local machine.

## Deployment Checklist

1. Detect framework version from `.csproj` `<TargetFramework>`
2. Copy the correct controller variant to the KX13 `Controllers/` folder
3. Replace the `Secret` constant with a freshly generated value
4. Register the `ToolExtendedFeatures` route (before catch-all routes)
5. Build the KX13 project to verify compilation
6. Start the KX13 instance
7. Test with `POST /ToolApi/Test`
8. Use the same secret in the migration tool's `appsettings.json`
