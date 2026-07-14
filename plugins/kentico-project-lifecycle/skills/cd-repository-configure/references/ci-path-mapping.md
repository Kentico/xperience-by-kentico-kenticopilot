# Mapping CI Repository paths to repository.config entries

CI Repository folder names mostly match `repository.config` object type names, but several do not map 1:1, and content items use dedicated filter elements instead of `ObjectFilters`. Use this reference when translating changed CI file paths into config entries. The authoritative object type list is [Reference - CI/CD object types](https://docs.kentico.com/documentation/developers-and-admins/ci-cd/reference-ci-cd-object-types).

## Common mappings

| CI path | Config object type | Note |
| --- | --- | --- |
| `@global/cms.contenttype` | `cms.contenttype` | |
| `@global/cms.user` | `cms.systemtable` | Unintuitive: the folder is `cms.user` but customizable system classes are configured via `cms.systemtable` |
| `@global/cms.member` | `cms.systemtable` | |
| `@global/cms.contact` | `cms.systemtable` | |
| `@global/cms.class` | `cms.class` | Ambiguous – see below |
| `@global/emaillibrary.emailtemplate` | `emaillibrary.emailtemplate` | |
| `@global/cms.settingskey` | `cms.settingskey` | Excluded from CI/CD by default due to potentially sensitive data; flag this to the user if it appears in a diff |

## Content items (channel-scoped and @global)

**Channel-scoped content (pages, emails, headless items):** These are stored under `<ChannelName>/` rather than `@global/`. For example, `DancingGoat/cms.contentitem/` contains pages for the DancingGoat website channel. When these paths appear in CI changes, use `IncludedContentItemsOfType` and `ContentItemFilters` rather than `ObjectFilters`. See [Reference - CI/CD object types](https://docs.kentico.com/documentation/developers-and-admins/ci-cd/reference-ci-cd-object-types#content-management) for details.

**Resolving content item code names from channel-scoped diffs:** When a diff shows a change under `<ChannelName>/contentitemdata.<type>/<folder>/`, the folder name encodes the item as `<itemcodename>-<guid_prefix>` but is not directly usable as a `ContentItemFilters` code name. Always resolve the canonical code name by reading the corresponding `<ChannelName>/cms.contentitem/<itemcodename-guid>.xml` file and extracting the `<ContentItemName>` element. This is the value to use in `<IncludedContentItemNames>`.

**Content type not changed but new items of that type were added:** When a CI diff adds new content item files under `@global/contentitemdata.<type>/` or `@global/cms.contentitem/` for a type whose `@global/cms.contenttype/<type>.xml` was **not** changed in any of the included commits, that content type must still be included in **both** `IncludedContentItemsOfType` and `ObjectFilters/IncludedCodeNames ObjectType="cms.contenttype"`. The type definition is unchanged but the type must be present in both places for `kxp-cd-store` to include the new items in the deployment package.

**Website channel folders:** Folders that group pages in the content tree are content items with no backing content type. To track them, add the dedicated `##WebPageFolders##` placeholder as a `<ContentType>` inside `IncludedContentItemsOfType`. Include it whenever the diff shows new or moved page-tree folders.

```xml
<IncludedContentItemsOfType>
  <ContentType>##WebPageFolders##</ContentType>
</IncludedContentItemsOfType>
```

## Special cases

**Forms require two object types:** Both `@global/cms.form/` and `@global/cms.formclass/` must be included together. The `cms.formclass` files use a `bizform.` code name prefix (e.g., `bizform.userfeedback.xml`). Include both in `IncludedObjectTypes`:

```xml
<IncludedObjectTypes>
  <ObjectType>cms.form</ObjectType>
  <ObjectType>cms.formclass</ObjectType>
</IncludedObjectTypes>
```

**`@global/cms.class` ambiguity:** This folder covers both module class definitions and reusable field schema definitions. Inspect the file code names to determine which is present before scoping `ObjectFilters`.

**`@global/cms.contentitemcommondata/` vs `@global/cms.class/cms.contentitemcommondata.xml` — do not confuse these two paths:**

- `@global/cms.contentitemcommondata/<itemcodename>/` — these are **child data files** for individual content items (language variants, draft state, etc.). They are covered automatically when the parent content type is included via `IncludedContentItemsOfType`. Do **not** add `cms.contentitemcommondata` to `ObjectFilters`; the v2 format disallows it and will throw an exception.
- `@global/cms.class/cms.contentitemcommondata.xml` — this single file is the **reusable field schema definition** and must be explicitly included via `cms.class` in `ObjectFilters` when changed.

**Reusable field schemas** → Object type `cms.class`. Reusable field schema definitions are tracked via the `CMS.ContentItemCommonData` code name (file path `App_Data\CIRepository\@global\cms.class\cms.contentitemcommondata.xml`). To include them, add both an `IncludedObjectTypes` entry and an `ObjectFilters` entry:

```xml
<IncludedObjectTypes>
  <ObjectType>cms.class</ObjectType>
</IncludedObjectTypes>
<ObjectFilters>
  <!-- Contains reusable field schema definitions -->
  <IncludedCodeNames ObjectType="cms.class">
    CMS.ContentItemCommonData
  </IncludedCodeNames>
</ObjectFilters>
```

**`cms.resource` allowlists:** If the config includes or excludes a subset of `cms.resource` objects, the `CMS.ContentEngine` resource must be explicitly included, otherwise `cms.contentitemcommondata` (and with it reusable field schema data) is not serialized:

```xml
<IncludedCodeNames ObjectType="cms.resource">
  CMS.ContentEngine
</IncludedCodeNames>
```

**Workspaces:** When the diff adds a `cms.workspace` object, also include the `cms.contentfolder` object type. Each workspace requires a root content folder for the Content hub to work correctly.
