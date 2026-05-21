# Update Execution — CI Disabled (Path A)

Use this path when `usesCI = false` in `update-xperience-context.json`.

1. Run the update command from the Xperience web project directory:
   - If **AgentMode** is NOT set: ask the user to confirm before running the update command.
   - `dotnet run --no-build -- --kxp-update --skip-confirmation`
2. If the command fails: report the failure and **stop**.
3. Do not run `--kxp-ci-store` when CI is disabled.
