# Appsettings.json Configuration Reference

Comprehensive reference for all `appsettings.json` settings used by the Kentico Migration Tool CLI. Settings are organized into three tiers: infrastructure (always needed), content-related (evaluate per migration), and non-content (include only when explicitly requested).

## Infrastructure Settings

Always include these. Use placeholder values when no workspace projects are available. When KX13/XbyK projects are present in the workspace (see Step 1: Discover Instance Configuration), extract actual values from project configuration files.

### `Settings.KxConnectionString`

- **Type:** string
- **Default:** none (required)
- **Purpose:** SQL Server connection string to the source KX13 database.
- **Example:**

  ```json
  "KxConnectionString": "Data Source=<SERVER>;Initial Catalog=<KX13_DB>;Integrated Security=True;Persist Security Info=False;Connect Timeout=60;Encrypt=False;Current Language=English;"
  ```

- **Auto-discovery:** `appsettings.json` → `ConnectionStrings.CMSConnectionString` (.NET Core), or `web.config`/`ConnectionStrings.config` → `CMSConnectionString` (.NET Framework)

### `Settings.KxCmsDirPath`

- **Type:** string
- **Default:** none
- **Purpose:** Absolute path to the source KX13 CMS root directory. Required when migrating media files or attachments (reads binaries from the local file system).
- **Example:**

  ```json
  "KxCmsDirPath": "C:\\inetpub\\wwwroot\\Xperience13\\CMS"
  ```

- **Auto-discovery:** `CMS` sibling folder of the KX13 MVC project (.NET Core), or folder containing `web.config` (.NET Framework / Portal Engine)

### `Settings.XbyKDirPath`

- **Type:** string
- **Default:** none
- **Purpose:** Absolute path to the target XbyK application root directory. Required when migrating media files or attachments.
- **Example:**

  ```json
  "XbyKDirPath": "C:\\inetpub\\wwwroot\\XbyK\\DancingGoat.Web"
  ```

- **Auto-discovery:** Directory containing the XbyK `.csproj` with `Kentico.Xperience.WebApp` package reference

### `Settings.XbyKApiSettings.ConnectionStrings.CMSConnectionString`

- **Type:** string
- **Default:** none (required)
- **Purpose:** SQL Server connection string to the target XbyK database.
- **Example:**

  ```json
  "XbyKApiSettings": {
    "ConnectionStrings": {
      "CMSConnectionString": "Data Source=<SERVER>;Initial Catalog=<XBYK_DB>;Integrated Security=True;Persist Security Info=False;Connect Timeout=60;Encrypt=False;Current Language=English;"
    }
  }
  ```

- **Auto-discovery:** XbyK `appsettings.json` → `ConnectionStrings.CMSConnectionString`

### `Settings.MigrationProtocolPath`

- **Type:** string (absolute file path)
- **Default:** none (optional but recommended). When omitted, the tool falls back to `MigrationProtocol_<timestamp>.html` in the executable's directory.
- **Purpose:** Absolute file path where the migration tool writes a structured protocol log. This protocol log is required by the migrate-content-eval skill to evaluate migration results — without it, protocol-based evaluation categories (page type verification, widget audit, user/role counts, module data, overall totals) cannot be assessed.
- **Constraints:**
  - Must be an absolute path
  - The tool **auto-creates** the parent directory via `Directory.CreateDirectory()` — no need to create it manually
  - The tool inserts a timestamp between filename and extension on each run (e.g., `protocol.txt` → `protocol20240115_1430.txt`), so previous runs are not overwritten
- **Deprecation note:** Marked `[Obsolete("Use logging instead")]` in the migration tool source (`ConfigurationNames.cs`). The setting remains functional but may be removed in a future version. Continue including it for protocol-based evaluation compatibility.
- **Example:**

  ```json
  "MigrationProtocolPath": "C:\\repos\\MedioClinic-Migration\\MigrationProtocol\\protocol.txt"
  ```

  In this example, `C:\repos\MedioClinic-Migration\` is the workspace root containing the KX13 source, XbyK target, and migration tool projects as siblings.
- **Auto-discovery:** Constructed from the **migration workspace root** — the common parent directory of the KX13 source, XbyK target, and migration tool projects. Default pattern: `<WorkspaceRoot>/MigrationProtocol/protocol.txt`. The workspace root is identified during Step 1 by finding the common ancestor of the discovered KX13 and XbyK projects (or the parent of the Migration Tool CLI directory). Resolve to an absolute path during generation. Placing the protocol log at the workspace root keeps it independent of any single project and accessible to all skills that consume it.

---

## Content-Related Settings

Evaluate each of these against the migration plan. Only include settings that are relevant.

### 1. `Settings.ConvertClassesToContentHub`

- **Type:** string (semicolon-separated class code names)
- **Default:** `""` (empty — no conversions)
- **Purpose:** Lists page types, custom tables, or module classes to migrate as Content Hub reusable content items instead of web page content types.
- **Constraints:**
  - Only include classes that should become **reusable content items**, not webpage types
  - Classes listed here are migrated as reusable items during `--page-types`, `--custom-modules`, or `--custom-tables`
  - Classes with `IClassMapping` code extensions can have their fields customized; classes without mappings get a 1:1 field transfer
  - **IMPORTANT:** Must be a semicolon-separated string, NOT a JSON array. The `ToolConfiguration` class binds this as a single string and splits on semicolons internally. Using a JSON array causes a runtime binding error.
- **Example:**

  ```json
  "ConvertClassesToContentHub": "MedioClinic.Doctor;MedioClinic.CompanyService;MedioClinic.SocialLink"
  ```

### 2. `Settings.CreateReusableFieldSchemaForClasses`

- **Type:** string (semicolon-separated class code names)
- **Default:** `""` (empty)
- **Purpose:** Converts specified page types to reusable field schemas. Fields from these classes become shared schemas that multiple content types can reference.
- **Constraints:**
  - **Mutually exclusive** with `ReusableSchemaBuilder` code for the same class — if a class has a code-based `ReusableSchemaBuilder`, do NOT include it here
  - This setting creates the schema automatically from existing fields; `ReusableSchemaBuilder` allows custom field definitions
- **Example:**

  ```json
  "CreateReusableFieldSchemaForClasses": "Acme.SeoFields;Acme.ArticleFields"
  ```

  **IMPORTANT:** Like `ConvertClassesToContentHub`, this must be a semicolon-separated string, NOT a JSON array. Using a JSON array causes a runtime configuration binding error.

### 3. `Settings.CustomModuleClassDisplayNamePatterns`

- **Type:** dictionary `{ "ClassName": "Pattern" }`
- **Default:** none
- **Purpose:** Defines display name patterns for content items migrated from custom module classes. Module classes don't have a `DocumentName` field, so this pattern generates display names.
- **Constraints:**
  - Keys are the module class code names
  - Pattern placeholders: `{CustomClassGuid}` for the item GUID, or any column name from the source table (e.g., `{AirportCode}`, `{ItemName}`)
- **Example:**

  ```json
  "CustomModuleClassDisplayNamePatterns": {
    "MedioClinic.Airports": "Airport-{AirportCode}",
    "Acme.CustomClass": "Item-{CustomClassGuid}"
  }
  ```

### 4. `Settings.IncludeExtendedMetadata`

- **Type:** boolean
- **Default:** `false`
- **Purpose:** When `true`, migrates `DocumentPageTitle`, `DocumentPageDescription`, and `DocumentPageKeywords` fields to the target content types.
- **Constraints:**
  - These fields are added to the target content type automatically when enabled
  - Relevant for SEO — include when the migration plan mentions SEO metadata or extended metadata fields
- **Example:**

  ```json
  "IncludeExtendedMetadata": true
  ```

### 5. `Settings.EntityConfigurations`

- **Type:** nested dictionary `{ "ObjectType": { "ExcludeCodeNames": [...], "ExplicitPrimaryKeyMapping": {...} } }`
- **Default:** none
- **Purpose:** Per-object-type configuration for excluding specific objects by code name or providing explicit primary key mappings.
- **Constraints:**
  - Common keys: `CMS_Class` (page types/classes), `CMS_SettingsKey` (settings), `CMS_Site` (sites)
  - `ExcludeCodeNames` is an array of code names to skip during migration
  - `ExplicitPrimaryKeyMapping` maps source PKs to target PKs (rarely needed)
  - Must include ALL classes explicitly marked as excluded in the migration plan
- **Example:**

  ```json
  "EntityConfigurations": {
    "CMS_Class": {
      "ExcludeCodeNames": [
        "MedioClinic.DayOfWeek",
        "MedioClinic.BasicPage",
        "MedioClinic.BigUsCities"
      ]
    },
    "CMS_SettingsKey": {
      "ExcludeCodeNames": [
        "CMSHomePagePath"
      ]
    }
  }
  ```

### 6. `Settings.OptInFeatures.QuerySourceInstanceApi`

- **Type:** object with `Enabled` (boolean) and `Connections` (array)
- **Default:** `Enabled: false`
- **Purpose:** Enables Source Instance API Discovery for widget migration. The migration tool queries the running KX13 instance to automatically map widget properties to native XbyK UI form components.
- **Constraints:**
  - Requires a `ToolApiController` endpoint deployed on the KX13 source instance
  - `Connections` is an array supporting multiple source instances, each with `SourceInstanceUri` and `Secret`
  - `SourceInstanceUri` is the base URL of the running KX13 instance (no `/getkentico11` suffix — that's the controller route added automatically)
  - `Secret` must match the secret configured in the `ToolApiController` on the source instance
  - Recommended baseline for cleanest widget migration results
- **Auto-discovery:** When Step 1 discovers the KX13 project, `SourceInstanceUri` can be extracted from `Properties/launchSettings.json` (IIS Express or Kestrel URL). The `Secret` is generated during ToolApiController deployment. See `toolapi-deployment-reference.md`.
- **Example:**

  ```json
  "OptInFeatures": {
    "QuerySourceInstanceApi": {
      "Enabled": true,
      "Connections": [
        {
          "SourceInstanceUri": "http://localhost:<SOURCE_INSTANCE_PORT>/",
          "Secret": "<SECRET_STRING>"
        }
      ]
    }
  }
  ```

### 7. `Settings.OptInFeatures.CustomMigration.FieldMigrations`

- **Type:** array of objects
- **Default:** `[]` (empty)
- **Purpose:** Converts text fields containing media links (e.g., `MediaSelectionControl` values) to content item asset references or media library file references.
- **Constraints:**
  - Each object requires: `SourceDataType`, `TargetDataType`, `SourceFormControl`, `TargetFormComponent`, `Actions`, `FieldNameRegex`
  - `SourceDataType`: typically `"text"`
  - `TargetDataType`: `"contentitemreference"` (for content item assets) or `"assets"` (for asset selector)
  - `SourceFormControl`: the KX13 form control name (e.g., `"MediaSelectionControl"`)
  - `TargetFormComponent`: XbyK form component identifier (e.g., `"Kentico.Administration.ContentItemSelector"` or `"Kentico.Administration.AssetSelector"`)
  - `Actions`: array of action strings, typically `["convert to asset"]`
  - `FieldNameRegex`: regex pattern to match field names (use `".*"` for all fields with the specified form control)
- **Example:**

  ```json
  "OptInFeatures": {
    "CustomMigration": {
      "FieldMigrations": [
        {
          "SourceDataType": "text",
          "TargetDataType": "contentitemreference",
          "SourceFormControl": "MediaSelectionControl",
          "TargetFormComponent": "Kentico.Administration.ContentItemSelector",
          "Actions": ["convert to asset"],
          "FieldNameRegex": ".*"
        }
      ]
    }
  }
  ```

### 8. `Settings.AssetRootFolders`

- **Type:** dictionary `{ "SiteName": "/FolderPath" }`
- **Default:** `{}` (empty)
- **Purpose:** Defines root content folders for asset content items per site. Controls where media assets are placed in the Content hub folder structure.
- **Constraints:**
  - Keys **must match the KX13 site code name exactly**
  - Values are the root folder paths in the target Content hub
- **Example:**

  ```json
  "AssetRootFolders": {
    "MedioClinic": "/MedioClinic",
    "DancingGoatCore": "/media/core"
  }
  ```

### 9. `Settings.TargetWorkspaceName`

- **Type:** string
- **Default:** `""` (empty — uses default workspace)
- **Purpose:** Code name of the XbyK workspace where migrated content items are placed. Only needed for multi-workspace XbyK instances.
- **Example:**

  ```json
  "TargetWorkspaceName": "MainWorkspace"
  ```

---

## Non-Content Settings

Document these for reference but omit from generated output unless explicitly requested.

### `Settings.MigrateOnlyMediaFileInfo`

- **Type:** boolean
- **Default:** `false`
- **Purpose:** When `true`, migrates only media file database records without transferring binary files. Useful when media files are stored in cloud/remote storage and will be migrated separately.

### `Settings.MigrateMediaToMediaLibrary`

- **Type:** boolean
- **Default:** `false`
- **Purpose:** When `true`, migrates media as media library items instead of content item assets. **Deprecated** — media libraries will be removed in a future XbyK version. Default behavior (content item assets) is recommended.

### `Settings.MemberIncludeUserSystemFields`

- **Type:** string (pipe-separated field code names)
- **Default:** `""` (empty)
- **Purpose:** Specifies which system fields from `CMS_User` and `CMS_UserSettings` tables to include when migrating Members.
- **Example:** `"FirstName|MiddleName|LastName|UserPrivilegeLevel|UserGender|UserDateOfBirth"`

### `Settings.UseOmActivityNodeRelationAutofix`

- **Type:** string enum
- **Valid values:** `"DiscardData"`, `"AttemptFix"`, `"Error"`
- **Default:** none
- **Purpose:** How to handle activity records referencing non-existing pages.

### `Settings.UseOmActivitySiteRelationAutofix`

- **Type:** string enum
- **Valid values:** `"DiscardData"`, `"AttemptFix"`, `"Error"`
- **Default:** none
- **Purpose:** How to handle activity records with broken site references.

### `Settings.CommerceConfiguration`

- **Type:** object
- **Purpose:** Commerce data migration settings (KX13 only). Sub-keys: `CommerceSiteNames` (array), `OrderStatuses` (mapping object), `IncludeCustomerSystemFields` (array), `IncludeAddressSystemFields` (array), `IncludeOrderSystemFields` (array), `IncludeOrderItemsSystemFields` (array), `IncludeOrderAddressSystemFields` (array), `KX13OrderFilter` (object with `OrderFromDate`, `OrderToDate`, `OrderStatusCodeNames`), `SystemFieldPrefix` (string, default `"KX13_"`).

---

## MedioClinic Example Walkthrough

The following explains each setting chosen for the MedioClinic migration scenario (see `APPSETTINGS_EXAMPLE.json`):

| Setting | Value | Why |
|---------|-------|-----|
| `ConvertClassesToContentHub` | 6 classes (Doctor, CompanyService, SocialLink, Company, MapLocation, Airports) | These custom tables and module classes become reusable Content Hub items per the migration plan |
| `EntityConfigurations.CMS_Class.ExcludeCodeNames` | 5 classes (DayOfWeek, BasicPage, BigUsCities, ConsentCookieLevel, User) | Excluded because they have no XbyK equivalent or are not needed in the target model |
| `IncludeExtendedMetadata` | `true` | Migration plan specifies SEO metadata fields should be preserved |
| `MigrationProtocolPath` | `<WorkspaceRoot>/MigrationProtocol/protocol.txt` | Infrastructure — protocol log at workspace root, required for post-migration evaluation (migrate-content-eval skill) |
| `QuerySourceInstanceApi` | Enabled with placeholder | MedioClinic uses Page Builder widgets that benefit from API Discovery |
| `AssetRootFolders` | `"MedioClinic": "/MedioClinic"` | Single-site project; assets organized under site name folder |
