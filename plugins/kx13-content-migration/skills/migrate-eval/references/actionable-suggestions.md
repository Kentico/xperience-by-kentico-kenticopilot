# Actionable Suggestions

Maps migration issues to specific remediation. Each entry has: detection pattern, fix, and which skill to re-run.

---

## Configuration Fixes (`appsettings.json`)

### EntityConfigurations Key Misspelling
**Pattern:** Silent — excluded types appear in XbyK when they shouldn't.  
**Fix:** Change `"CMS.Class"` → `"CMS_Class"` (underscores, not dots) in `EntityConfigurations` keys.  
**Skill:** `migrate-appsettings`

### ConvertClassesToContentHub as JSON Array
**Pattern:** Tool fails to start or crashes immediately.  
**Fix:** Change JSON array `["A","B"]` → semicolon string `"A;B"`. Same for `CreateReusableFieldSchemaForClasses`.  
**Skill:** `migrate-appsettings`

### Missing Source Media Files
**Pattern:** `File PATH does not exist` / `Media file 'GUID' not migrated`  
**Fix:** Verify `Settings.KxCmsDirPath` points to the KX13 CMS root (folder containing `media/`). If file genuinely missing, accept the skip.  
**Skill:** `migrate-appsettings`

### Content Item Reference to Excluded Class
**Pattern:** `linked content item with GUID 'GUID' ... does not exist`  
**Fix A:** Remove referenced class from `ExcludeCodeNames` so it gets migrated.  
**Fix B:** In IClassMapping, don't map the reference field (set to null).  
**Skill:** `migrate-appsettings` or `migrate-classes`

### QuerySourceInstanceApi Connection
**Pattern:** `Connection to source instance API failed`  
**Fix:** Start KX13 at configured URL, or set `QuerySourceInstanceApi.Enabled` to `false`.  
**Skill:** `migrate-appsettings`

---

## Code Fixes (Extension Implementations)

### Taxonomy Tag JSON Casing
**Pattern:** Silent — no log error. DB shows `"identifier"` (lowercase) instead of `"Identifier"` (PascalCase).  
**Fix:** In IClassMapping `ConvertFrom` for taxonomy fields, use PascalCase: `new { Identifier = guid }` not `new { identifier = guid }`.  
**Skill:** `migrate-classes`

### Silent Content Duplication (Linked Pages)
**Pattern:** Silent — no error. Target count ≈ source originals + source linked pages.  
**Fix:** Implement `DirectLinkedNode()` in `ContentItemDirectorBase` for affected classes. Choose `StoreReferenceInAncestor()`, `Drop()`, or `Materialize()`.  
**Skill:** `migrate-content-items`

### VisualBuilderPatcher Crash
**Pattern:** `ArgumentNullException` in `VisualBuilderPatcher.WalkWidgets`  
**Fix:** Register `IWidgetMigration` for unregistered widget types. Inspect source page's `DocumentPageBuilderWidgets` to identify widget types.  
**Skill:** `migrate-widgets`

### ContentItemReferencePopulator Failure
**Pattern:** `linked content item with GUID 'GUID' ... does not exist` at `ContentItemReferencePopulator`  
**Fix:** Remove referenced class from exclusions, or null out the reference field in IClassMapping.  
**Skill:** `migrate-classes`

### Missing Source Field Warning
**Pattern:** `Value is not contained in source, field 'FIELD_NAME'`  
**Fix:** Usually expected for new target-only fields (will be null). If it should map from source, add `.ConvertFrom()` or `.MapFrom()` in IClassMapping.  
**Skill:** `migrate-classes` (only if mapping needed)

### NullRef After Drop
**Pattern:** `NullReferenceException` at `MigratePages()` following `Explicit drop directive`  
**Fix:** Change `Drop()` → `Materialize()` in ContentItemDirectorBase, or accept and create the errored page manually in XbyK.  
**Skill:** `migrate-content-items`

---

## Manual Intervention

### Failed Page (NeedManualAction: True)
Fix root cause first (see Code Fixes above), then re-run with `--pages --bypass-dependency-check`. If unfixable, manually create in XbyK Administration using `NodeAliasPath` from entity JSON.

### Legacy BizForm Fields
**Pattern:** `legacy format ClassFormDefinition ... converted to text input`  
Review converted form fields in XbyK Administration > Forms. Adjust form component types to match KX13 originals.

### Orphaned Content Type from Excluded Class
**Pattern:** `Success: DataClassInfo(...)` for a class in the plan's Exclusions list.  
Delete from XbyK Administration > Content types. Cosmetic — won't break anything but creates confusion.

### Administrator User Skip
Expected behavior. Update XbyK admin profile manually if needed.

### Missing Source Files
Restore from backup and re-run `--media-libraries`, or accept absence.

---

## Noise — No Action Required

These are expected entries. Do not flag as errors or warnings.

| Pattern | Reason |
|---------|--------|
| `NotSupportedSkip` for `ICmsResource` | System modules exist natively in XbyK |
| `CmsClass_CmsRootClassTypeSkip` | Root node handled by XbyK |
| `CmsUser_SkipAdminUser` | Admin managed separately |
| `CMS.Folder is deprecated` | Not a content type in XbyK |
| `Unknown element 'schema'` | Benign XML warning |
| `Cannot read keys` (InvalidOperationException) | Artifact of piped stdin, always last entry |
| `Explicit drop directive` | Configured behavior — verify matches plan |
| `Deferring` linked node | Normal — retried after all primary pages |
| `Preliminarily allowing any child content type` | Normal type registration |
| `Root node skipped, V27+` | Expected |
| `Asset migration prerequisite created DataClassModel` | Normal Legacy.* type creation |
| Form `Total=0, TotalCopied=0` | Empty form, no submissions |
| `Patching CMS Resource` name change | Normal module name normalization |
