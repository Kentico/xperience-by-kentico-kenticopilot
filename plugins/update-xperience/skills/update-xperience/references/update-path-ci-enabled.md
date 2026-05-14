# Update Execution — CI Enabled (Path B)

Use this path when `usesCI = true` in `update-xperience-context.json`.

When CI is enabled, it must be disabled before the update and re-enabled before `--kxp-ci-store`.

1. Retrieve the connection string according to `connectionString.source`:
   - `appsettings.Development.json`: read `ConnectionStrings:CMSConnectionString` from that file.
   - `appsettings.json`: read `ConnectionStrings:CMSConnectionString` from that file.
   - `user-secrets`: run `dotnet user-secrets list --project <xperienceProjectCsprojPath>` and read `ConnectionStrings:CMSConnectionString`.
2. Read `dabPort` from `update-xperience-context.json`. Start DAB with `ASPNETCORE_URLS=http://127.0.0.1:<dabPort>`, `XBK_UPDATE_DB_CONNECTION=<resolved-connection-string>`, and `dotnet dab start --config <dabConfigPath>`.
3. Read `ciSettingsKeyId` from `update-xperience-context.json`.
   - Use this value as the `CMSEnableCI` row key.
   - If missing or invalid, stop and instruct the user to rerun `update-xperience-prep`.
4. Disable CI via HTTP PATCH:
   - `PATCH http://127.0.0.1:<dabPort>/api/SettingsKey/KeyID/<ciSettingsKeyId>`
   - Request body: `{"KeyValue": "0"}`
   - Expect: 200
   - Verify with GET: `GET http://127.0.0.1:<dabPort>/api/SettingsKey/KeyID/<ciSettingsKeyId>` and confirm `KeyValue = "0"`
5. Run the update command from the Xperience web project directory:
   ```
   dotnet run --no-build -- --kxp-update --skip-confirmation
   ```
6. Wrap step 5 in try/finally so CI re-enable is guaranteed.
7. Re-enable CI via HTTP PATCH in finally:
   - `PATCH http://127.0.0.1:<dabPort>/api/SettingsKey/KeyID/<ciSettingsKeyId>`
   - Request body: `{"KeyValue": "1"}`
   - Expect: 200
   - Verify with GET: `GET http://127.0.0.1:<dabPort>/api/SettingsKey/KeyID/<ciSettingsKeyId>` and confirm `KeyValue = "1"`
8. If update succeeded, run:
   ```
   dotnet run --no-build -- --kxp-ci-store
   ```
   If this command fails: report the failure and **stop**. Do not commit.
9. Kill the DAB subprocess after all CI operations complete (successful or failed).
