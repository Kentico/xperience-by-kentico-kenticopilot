---
name: cd-repository-upgrade
description: "Migrates a CD Repository configuration file from v1 syntax to v2, enabling advanced content item filtering and improving CD restore performance. Automatically locates the config file from the discovery context if not provided."
argument-hint: "(Optional) Path to repository.config file, or path to folder containing cd-repository-context.json"
---

You are tasked with migrating a CD Repository `repository.config` file from v1 to v2 syntax.

## Input Parameters

- **Repository Config Path** - *(Optional)* Absolute or relative path to the `repository.config` file to migrate.

If no path is provided, the skill will attempt to locate the context file (`cd-repository-context.json`) in the current directory or its parent folders. If found, it will extract the `cdRepositoryConfigPath` from the context file.

## Discovery Logic

If no repository config path is provided as an argument:

1. Search for `cd-repository-context.json` in the current directory and parent directories (up to 3 levels).
2. If found, read the file and extract `cdRepositoryConfigPath`.
3. If not found, ask the user to provide either:
   - The path to the `repository.config` file directly, or
   - The path to the folder containing `cd-repository-context.json`

## Primary Goal

Transform the config file to v2 syntax in-place, with a backup of the original, enabling advanced content item filtering and improved CD restore performance.

## Transformation Steps

### 1. Locate and Validate Input

**If repository config path was provided as argument:**
- Validate the file exists and is valid XML.

**If repository config path was NOT provided:**
- Use the discovery logic from the "Discovery Logic" section above to locate `cd-repository-context.json`.
- Extract `cdRepositoryConfigPath` from the context file.
- If context file cannot be found, ask the user to provide the config path or context folder path.

**For all cases, after locating the file:**
- Check that the file exists and is valid XML.
- Detect the current version:
  - If `<RepositoryConfiguration>` lacks a `Version` attribute or has `Version="1"`, it's v1.
  - If `Version="2"`, inform the user it's already v2 and stop.
  - If any other version, ask the user to verify.

### 2. Create Backup

Create a backup file named `repository.config.v1.backup` in the same directory.

### 3. Migrate RepositoryConfiguration Element

Update the root element to include `Version="2"`:

```xml
<!-- Before -->
<RepositoryConfiguration
  xmlns:xsd="http://www.w3.org/2001/XMLSchema"
  xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">

<!-- After -->
<RepositoryConfiguration 
  Version="2"
  xmlns:xsd="http://www.w3.org/2001/XMLSchema"
  xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
```

### 4. Ensure IncludedObjectTypes Exists and Is Explicit

**Behavior change:** In v1, `IncludedObjectTypes` is optional (defaults to all objects). In v2, it must be explicit.

- If `<IncludedObjectTypes>` is missing or empty, add/replace with `<IncludeAll />`.
- If `<IncludedObjectTypes>` already contains specific object types, keep them as-is.
- Preserve any `<ExcludedObjectTypes>` section unchanged.

### 5. Remove Content Item Object Types from ObjectFilters

The following object types can **no longer** appear in `<ObjectFilters>` with `<IncludedCodeNames>` or `<ExcludedCodeNames>`:

- `cms.contentitem`
- `cms.contentitemcommondata`
- `cms.contentitemlanguagemetadata`
- `cms.contentitemreference`
- `cms.webpageitem`
- `cms.webpageformerurlpath`
- `cms.webpageurlpath`
- `emaillibrary.emailconfiguration`
- `emaillibrary.sendconfiguration`
- `cms.headlessitem`

**Migration approach:**

- Search `ObjectFilters` for any filters applied to these object types.
- **For content item code name filters (e.g., `<IncludedCodeNames ObjectType="cms.contentitem">` or `<ExcludedCodeNames ObjectType="cms.contentitem">`):**
  - Extract the code name patterns (both included and excluded).
  - **Do NOT remove them yet** – they will be migrated to the new `ContentItemFilters` element in the next step.
  - Remove only the ObjectFilter entries for these object types from `ObjectFilters`.
  - Make a note of the extracted patterns for step 6.
- **For other object type filters:** Keep them unchanged in `ObjectFilters`.

See [Configure content item filters](https://docs.kentico.com/documentation/developers-and-admins/ci-cd/configure-ci-cd-repositories#configure-content-item-filters) in the Kentico documentation for details on the new approach.

### 6. Migrate Content Item Code Name Filters to ContentItemFilters

If the v1 config had any `<IncludedCodeNames ObjectType="cms.contentitem">` or `<ExcludedCodeNames ObjectType="cms.contentitem">` entries, create a new `<ContentItemFilters>` section to preserve this filtering.

**Example migration:**

```xml
<!-- v1 config -->
<ObjectFilters>
  <IncludedCodeNames ObjectType="cms.contentitem">article.spring-launch;coffee.espresso;offer.summer</IncludedCodeNames>
  <ExcludedCodeNames ObjectType="cms.contentitem">article.draft%;offer.internal%</ExcludedCodeNames>
</ObjectFilters>

<!-- Migrates to v2 config -->
<ContentItemFilters>
  <IncludedContentItemNames>
    article.spring-launch;coffee.espresso;offer.summer
  </IncludedContentItemNames>
  <ExcludedContentItemNames>
    article.draft%;offer.internal%
  </ExcludedContentItemNames>
</ContentItemFilters>
```

**Placement:** Add `<ContentItemFilters>` **after** `<IncludedContentItemsOfType>` and before `<ObjectFilters>`.

**If no content item code name filters existed:** Omit this section. Users can add it later if needed.

See [Configure content item filters](https://docs.kentico.com/documentation/developers-and-admins/ci-cd/configure-ci-cd-repositories#configure-content-item-filters) for full details.

### 7. Add Content Items Section (If Not Present)

If no `<IncludedContentItemsOfType>` exists, add a new section **after** `IncludedObjectTypes` and **before** `ContentItemFilters` or `ExcludedObjectTypes` (if present):

```xml
<IncludedContentItemsOfType>
  <!-- Explicitly list content types or use <IncludeAll /> -->
  <IncludeAll />
</IncludedContentItemsOfType>
```

**Note:** Use `<IncludeAll />` as the safe default migration choice. The user can refine this post-migration by specifying individual content types.

If `<IncludedContentItemsOfType>` already exists, preserve it as-is.

### 9. Update Discovery Context File (If Applicable)

If the config path was discovered from a `cd-repository-context.json` file (rather than provided as an argument):

- Read the context file again to ensure you have the latest version.
- Update the `discovery.repositoryConfigVersion` field from `"1"` to `"2"`.
- Write the updated context file back to disk.
- Include a note in the output that the context file was updated.

If the config path was provided explicitly as an argument, skip this step.

### 10. Validate XML

Parse the resulting XML to ensure it is well-formed. If validation fails, report the error and do not overwrite the original file.

### 11. Write Migrated File

- Overwrite the original `repository.config` with the migrated v2 syntax.
- Preserve formatting and indentation where possible.

## Output Format

Conclude with a clear summary:

```
✅ Migration Complete

Config File: [absolute path to repository.config]
Backup: [absolute path to backup file]
[If discovered from context: Located via cd-repository-context.json]
[If context file was updated: Updated discovery context at [path]]

Changes Made:
- Added Version="2" to RepositoryConfiguration element
- [List specific changes made]

Content Item Configuration:
- [Describe IncludedContentItemsOfType changes]
- [If content item code name filters were found and migrated to ContentItemFilters, note them explicitly]
- [Note if user needs to manually review or refine IncludedContentItemsOfType with specific types instead of IncludeAll]

Next Steps:
1. Review the migrated repository.config, especially:
   - IncludedContentItemsOfType configuration (consider replacing `<IncludeAll />` with specific content types for better performance)
   - ContentItemFilters if present (verify the migrated code name patterns are correct)
   - ObjectFilters to ensure non-content-item filters were preserved correctly
2. Test the migrated config by running a CD store operation:
   dotnet run --no-build -- --kxp-cd-store --repository-path "C:\path\to\CDRepository" --config-path "C:\path\to\repository.config"
   See [Store objects to a CD Repository](https://docs.kentico.com/documentation/developers-and-admins/ci-cd/continuous-deployment#store-objects-to-a-cd-repository) for details.
3. Use 'cd-repository-configure' to build scoped deployment filters from PR/commit changes

References:
- [Migrate CI/CD repository.config to v2](https://docs.kentico.com/documentation/developers-and-admins/ci-cd/configure-ci-cd-repositories/config-v2-migration)
- [Configure content item filters](https://docs.kentico.com/documentation/developers-and-admins/ci-cd/configure-ci-cd-repositories#configure-content-item-filters)
- [Store objects to a CD Repository](https://docs.kentico.com/documentation/developers-and-admins/ci-cd/continuous-deployment#store-objects-to-a-cd-repository)
```

## Rules

- **Discover from context when available.** If no config path is provided, automatically search for and use `cd-repository-context.json` to locate the config file.
- **Update context file if used.** If the config was discovered from a context file, update `discovery.repositoryConfigVersion` from `"1"` to `"2"` in that file. This eliminates the need for the user to run discovery again.
- **Backup first.** Always create a backup before modifying the original file.
- **Preserve non-content-item filters.** Do not remove or modify `ObjectFilters` for non-content-item object types.
- **Migrate content item code name filters.** If the v1 config has `<IncludedCodeNames ObjectType="cms.contentitem">` or `<ExcludedCodeNames ObjectType="cms.contentitem">`, migrate these to the new `<ContentItemFilters>` element with `<IncludedContentItemNames>` and `<ExcludedContentItemNames>`. See [Configure content item filters](https://docs.kentico.com/documentation/developers-and-admins/ci-cd/configure-ci-cd-repositories#configure-content-item-filters) for details.
- **Be explicit about content item filtering.** In the summary, explain what happened with content item filters and guide the user on fine-tuning `IncludedContentItemsOfType` (e.g., replacing `<IncludeAll />` with specific content types for better performance).
- **Use correct CD commands.** When referencing testing steps, use `--kxp-cd-store` for CD Repository operations (not `--kxp-ci-store`, which is for CI Repository).
- **Validate before writing.** Ensure the migrated XML is well-formed before overwriting the original.
- **Idempotent.** If the file is already v2, do not process it again.
- **Preserve existing IncludedContentItemsOfType.** If the user already has this section configured, do not replace it.

## Decision Tree

**Is the file already v2?**
→ Stop. Inform the user it's already v2 and no migration is needed.

**Does IncludedObjectTypes exist and is not empty?**
→ Keep as-is.

**Does IncludedObjectTypes not exist or is empty?**
→ Replace with `<IncludeAll />`.

**Does ObjectFilters contain cms.contentitem or related types (cms.contentitem, cms.contentitemcommondata, etc.)?**
→ Extract code name patterns from IncludedCodeNames/ExcludedCodeNames for cms.contentitem.
→ If patterns exist, create a new `<ContentItemFilters>` section with `<IncludedContentItemNames>` and `<ExcludedContentItemNames>` using the extracted patterns.
→ Remove the cms.contentitem-related filter entries from `ObjectFilters`.
→ Note in the summary which content item code name patterns were migrated.

**Does IncludedContentItemsOfType not exist?**
→ Add `<IncludedContentItemsOfType><IncludeAll /></IncludedContentItemsOfType>` as safe default.

**Does the migrated XML validate?**
→ If yes, write the file. If no, report the error and do not overwrite.
