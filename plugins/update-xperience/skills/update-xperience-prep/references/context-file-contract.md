# Context File Contract

`update-xperience-context.json` is written to the repository root by the `update-xperience-prep` skill. It contains no secrets and is safe to commit.

```json
{
  "xperienceProjectCsprojPath": "absolute path to ASP.NET Core Xperience .csproj",
  "usesCI": true,
  "connectionString": {
    "source": "appsettings.json|appsettings.Development.json|user-secrets|not-required"
  },
  "ciSettingsKeyId": 69816,
  "dabConfigPath": "absolute path to dab-config.json when usesCI=true, otherwise null",
  "dabPort": 52341,
  "docPaths": ["README.md", "docs/CHANGELOG.md"],
  "runTests": true
}
```

| Field | Type | Description |
| ----- | ---- | ----------- |
| `xperienceProjectCsprojPath` | string | Absolute path to the ASP.NET Core Xperience `.csproj` file |
| `usesCI` | boolean | Whether the project uses Xperience's CI feature |
| `connectionString.source` | string | Where the connection string is stored: `appsettings.json`, `appsettings.Development.json`, `user-secrets`, or `not-required` |
| `ciSettingsKeyId` | integer\|null | The `KeyID` of the `CMSEnableCI` row in `CMS_SettingsKey`. `null` when `usesCI = false` |
| `dabConfigPath` | string\|null | Absolute path to the generated `dab-config.json`. `null` when `usesCI = false` |
| `dabPort` | integer\|null | The port DAB listens on for HTTP requests. `null` when `usesCI = false` |
| `docPaths` | string[] | Repository-relative paths to files that reference the Xperience version. Empty array skips Step 8 (documentation update) in the main skill. |
| `runTests` | boolean | Whether the main skill should run `dotnet test` after a successful build in Step 7. |
