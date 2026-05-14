# Update Execution — CI Disabled (Path A)

Use this path when `usesCI = false` in `update-xperience-context.json`.

1. Run the update command from the Xperience web project directory:
   ```
   dotnet run --no-build -- --kxp-update --skip-confirmation
   ```
2. If the command fails: report the failure and **stop**. Do not commit.
3. Do not run `--kxp-ci-store` when CI is disabled.
