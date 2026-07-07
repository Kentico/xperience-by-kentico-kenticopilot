---
name: "update-xperience"
description: "Updates an Xperience by Kentico project to a newer version by following the official release notes and update documentation. Use when the user wants to update or upgrade their Xperience by Kentico project, apply a hotfix, or move to a newer refresh."
argument-hint: "[target-version]"
compatibility: "Requires Kentico Docs MCP"
---

You are tasked with updating an Xperience by Kentico project to a newer version. The official release notes and update documentation are the source of truth — follow them, do not invent update steps.

## Workflow

### 1. Determine the versions

- Identify the current version from the `Kentico.Xperience.*` NuGet package references in the project.
- Identify the target version: use the version passed as an argument if provided, otherwise the latest available.

### 2. Review the release notes

- Read the release notes in the [Changelog](https://docs.kentico.com/changelog) for **every version between the current and target version**.
- Note breaking changes, deprecations, new object types for CI/CD, and any manual steps.
- **Follow the pages linked from the release notes** that guide through specific feature updates — they contain the instructions for adapting the project.

### 3. Follow the official update procedure

- Read `references/update-docs.md` and fetch the relevant documentation pages via the Kentico Docs MCP.
- Perform the update as documented — do not substitute your own procedure or external tools; everything needed (including CI handling) is built into the product's CLI.

### 4. Resolve breaking changes

- Apply the code and configuration changes required by the release notes and their linked guides.
- Build the solution and fix remaining errors caused by the update.

### 5. Verify and report

- Verify the result (build, tests if present).
- Summarize what was updated and which release-note items required action.
