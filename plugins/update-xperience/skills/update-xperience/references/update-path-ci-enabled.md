# Update Execution — CI Enabled (Path B)

Use this path when `usesCI = true` in `update-xperience-context.json`.

When CI is enabled, it must be disabled before the update and re-enabled before `--kxp-ci-store`.

1. Retrieve the connection string according to `connectionString.source`:
   - `appsettings.Development.json`: read `ConnectionStrings:CMSConnectionString` from that file.
   - `appsettings.json`: read `ConnectionStrings:CMSConnectionString` from that file.
   - `user-secrets`: run `dotnet user-secrets list --project <xperienceProjectCsprojPath>` and read `ConnectionStrings:CMSConnectionString`.
2. Read `dabPort` from `update-xperience-context.json`. Start DAB with `ASPNETCORE_URLS=http://127.0.0.1:<dabPort>`, `XBK_UPDATE_DB_CONNECTION=<resolved-connection-string>`, and `dotnet dab start --config <dabConfigPath>`.
3. Look up the `CMSEnableCI` settings key at runtime:
   - `GET http://127.0.0.1:<dabPort>/api/SettingsKey?$filter=KeyName eq 'CMSEnableCI'`
   - Expect: 200 with exactly one record in the `value` array.
   - Extract `KeyID` from the response; use it for all subsequent PATCH and GET requests in this run.
   - If the response is not 200, or the `value` array has zero or multiple records, stop and instruct the user to verify database state before retrying.
4. Disable CI via HTTP PATCH:
   - `PATCH http://127.0.0.1:<dabPort>/api/SettingsKey/KeyID/<KeyID>`
   - Request body: `{"KeyValue": "0"}`
   - Expect: 200
   - Verify with GET: `GET http://127.0.0.1:<dabPort>/api/SettingsKey/KeyID/<KeyID>` and confirm `KeyValue = "0"`
5. Run the update command from the Xperience web project directory:
   - If **AgentMode** is NOT set: ask the user to confirm before running the update command.
   - `dotnet run --no-build -- --kxp-update --skip-confirmation`
6. Wrap step 5 in try/finally so CI re-enable is guaranteed.
7. Re-enable CI via HTTP PATCH in finally:
   - `PATCH http://127.0.0.1:<dabPort>/api/SettingsKey/KeyID/<KeyID>`
   - Request body: `{"KeyValue": "1"}`
   - Expect: 200
   - Verify with GET: `GET http://127.0.0.1:<dabPort>/api/SettingsKey/KeyID/<KeyID>` and confirm `KeyValue = "1"`
8. If update succeeded, run:
   ```
   dotnet run --no-build -- --kxp-ci-store
   ```
   If this command fails: report the failure and stop.
9. Kill the DAB subprocess after all CI operations complete (successful or failed).
