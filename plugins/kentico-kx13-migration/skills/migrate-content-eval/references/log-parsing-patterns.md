# Log Parsing Patterns

Concise reference for understanding the two log files produced by the Kentico Migration Tool. For automated parsing, use `scripts/parse-migration-logs.ps1` which produces a structured YAML summary. This document is the fallback reference when the script is unavailable.

---

## Protocol Log (`protocol*.txt`)

Located in the directory specified by `Settings.MigrationProtocolPath` in appsettings.json.

**Entry boundary:** Entries are multi-line (JSON with newlines). Split by timestamp at line start: `^\d{8}_\d{6}:`

**Entry types:**

| Prefix | Meaning | Key data |
|--------|---------|----------|
| `Success: EntityType(ID=N, Guid=GUID Name=NAME)` | Entity migrated successfully | Entity type, ID, GUID, Name |
| `Success: UserRoleInfo({...JSON...})` | User-role link created | UserID, RoleID (multi-line JSON) |
| `NeedManualAction: False, ReferenceName: CODE` | Skipped (expected) | `NotSupportedSkip` (system resource), `CmsClass_CmsRootClassTypeSkip`, `CmsUser_SkipAdminUser` |
| `NeedManualAction: True, ReferenceName: CODE` | Failed — requires fix | `FailedToCreateTargetInstance` + exception + stack trace + source entity JSON |

---

## Console Log (`migration-run.log`)

Located in the same directory as the protocol log.

**Line format:** `HH:MM:SS.mmm level: Logger.Namespace[EventId] Message`

**First line exception:** May contain ANSI escape codes (`\[\d+m`) — strip before parsing.

**Levels:** `info`, `warn`, `fail`

**Command lifecycle:** Use `Source.Behaviors.RequestHandlingBehavior` (outer layer) for timing, not `Core.KX13` (inner):

- Start: `Handling {CommandName}`
- End: `Handled {CommandName} in elapsed: HH:MM:SS.mmmmmmm`

**Expected commands:** `MigrateSitesCommand`, `MigrateCustomModulesCommand`, `MigratePageTypesCommand`, `MigrateUsersCommand`, `MigrateAttachmentsCommand`, `MigratePagesCommand`, `MigrateMediaLibrariesCommand`, `MigrateFormsCommand`, `MigrateCategoriesCommand`

---

## Key Error Categories

| Category | Detection keyword | Meaning |
|----------|-------------------|---------|
| VisualBuilderPatcher crash | `VisualBuilderPatcher` in stack trace | Unregistered widget type — needs `IWidgetMigration` |
| Content item reference | `linked content item with GUID` | Missing referenced content item — fix `IClassMapping` or exclusion |
| Missing source field | `Value is not contained in source, field` | Usually expected (new target-only field) — verify against plan |
| Media file missing | `AssetManager` + `does not exist` | Source file not on disk |
| Form legacy format | `legacy format ClassFormDefinition` | Fields auto-converted to text input — may need manual adjustment |
| Explicit drop | `Content item skipped. Reason:` | Expected if matches plan directives |
| Terminal exception | `Cannot read keys` | Always harmless — artifact of piped stdin |

---

## Multi-Run Merging

The parse script processes all log files in the directory chronologically. Merge semantics:

- **Protocol success**: later run's success overrides earlier failure for the same entity (by GUID)
- **Protocol failure**: only kept if the entity did not succeed in any later run
- **Console commands**: latest execution per command name wins (re-runs re-execute commands)
- **Console errors/warnings**: accumulated across runs, deduplicated by message
- **Drops**: deduplicated by NodeGUID
- **Media**: counts accumulated, missing file paths deduplicated

## Deduplication Rules

- **Content item reference failures** produce 2 `fail:` lines (Importer + CommandHandler) — deduplicate by GUID
- **Media file failures** produce 3 entries (AssetManager + Importer + CommandHandler) — group by media file GUID
- **VisualBuilderPatcher crashes** appear in both console and protocol logs — correlate by timestamp
- **Missing source field warnings** repeat per language variant — count unique field names, not occurrences
