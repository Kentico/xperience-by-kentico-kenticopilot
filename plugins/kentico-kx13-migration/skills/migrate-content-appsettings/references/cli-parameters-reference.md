# CLI Parameters & Key Configuration Options

## Key Configuration Options (appsettings.json)

| Setting                                         | Purpose                                                               |
| ----------------------------------------------- | --------------------------------------------------------------------- |
| `ConvertClassesToContentHub`                    | List of page types/custom tables/module classes to migrate as reusable content items |
| `CreateReusableFieldSchemaForClasses`           | List of page types to convert to reusable field schemas. Cannot be combined with `ReusableSchemaBuilder` in custom class mappings. |
| `CustomModuleClassDisplayNamePatterns`          | Display name patterns for content items migrated from custom module classes (e.g. `"Item-{CustomClassGuid}"`) |
| `EntityConfigurations`                          | Per-object-type config (exclude code names via `ExcludeCodeNames`, explicit PK mapping). Keys are **database table names** with underscores (e.g., `CMS_Class`, `CMS_SettingsKey`), NOT code names with dots |
| `OptInFeatures.QuerySourceInstanceApi`          | Enable Source Instance API Discovery for widget migration             |
| `OptInFeatures.CustomMigration.FieldMigrations` | Convert media selection text fields to content item assets or media library files |
| `MigrateOnlyMediaFileInfo`                      | Skip binary media file transfer (useful for shared/cloud storage)     |
| `MigrateMediaToMediaLibrary`                    | Migrate media as media libraries instead of content item assets (deprecated — media libraries will be removed in future XbyK) |
| `MemberIncludeUserSystemFields`                 | Which user system fields from `CMS_User`/`CMS_UserSettings` to include for Members |
| `IncludeExtendedMetadata`                       | Migrate `DocumentPageTitle`, `DocumentPageDescription`, `DocumentPageKeywords` |
| `UseOmActivityNodeRelationAutofix`              | Handle activity references to non-existing pages: `DiscardData`, `AttemptFix`, or `Error` |
| `UseOmActivitySiteRelationAutofix`              | Handle activity site references: `DiscardData`, `AttemptFix`, or `Error` |
| `TargetWorkspaceName`                           | Code name of the XbyK workspace for migrated content items            |
| `AssetRootFolders`                              | Dictionary defining root content folder per site for asset content items |
| `CommerceConfiguration`                         | Commerce data migration settings (KX13 only): `CommerceSiteNames`, `OrderStatuses`, `IncludeCustomerSystemFields`, `IncludeAddressSystemFields`, `IncludeOrderSystemFields`, `IncludeOrderItemsSystemFields`, `IncludeOrderAddressSystemFields`, `KX13OrderFilter`, `SystemFieldPrefix` |

## CLI Parameter Dependencies

| Parameter              | Dependencies                                                         |
| ---------------------- | -------------------------------------------------------------------- |
| `--sites`              | (none)                                                               |
| `--custom-modules`     | `--sites`                                                            |
| `--custom-tables`      | (none)                                                               |
| `--users`              | `--sites`, `--custom-modules`                                        |
| `--members`            | `--sites`, `--custom-modules`                                        |
| `--settings-keys`      | `--sites`                                                            |
| `--page-types`         | `--sites`                                                            |
| `--pages`              | `--sites`, `--users`, `--page-types`                                 |
| `--type-restrictions`  | `--sites`, `--page-types`, `--pages`                                 |
| `--categories`         | `--sites`, `--users`, `--page-types`, `--pages`                      |
| `--media-libraries`    | `--sites`, `--custom-modules`, `--users`                             |
| `--forms`              | `--sites`, `--custom-modules`, `--users`                             |
| `--contact-management` | `--users`, `--custom-modules`                                        |
| `--data-protection`    | `--sites`, `--users`, `--contact-management`                         |
| `--customers`          | `--sites`, `--custom-modules`, `--users`, `--members`                |
| `--orders`             | `--sites`, `--custom-modules`, `--users`, `--members`, `--customers` |

Use `--bypass-dependency-check` to skip dependency validation on repeated runs when dependencies were already migrated.
