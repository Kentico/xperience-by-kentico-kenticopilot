# Writing the scoped repository.config

Guidelines for building the `IncludedObjectTypes` allowlist, object and content item filters, and `RestoreMode`. Filtering semantics are documented in [Exclude objects from CI/CD](https://docs.kentico.com/documentation/developers-and-admins/ci-cd/configure-ci-cd-repositories); this file adds the rules for producing a **minimal deployment-scoped** config.

## Regenerate, don't merge

Each run produces a config scoped to the **current deployment only**. Rebuild `IncludedObjectTypes`, `IncludedContentItemsOfType`, `ContentItemFilters`, and `ObjectFilters` from scratch based on the current change selectors, and re-determine `RestoreMode`. Do not carry over filter entries from the previous config.

- If the existing config contains entries that clearly did not come from a previous run of this skill (for example, manually maintained `ExcludedObjectTypes` or standing exclusions like `cms.settingskey`), surface them explicitly in the diff and ask the user whether to keep them before removing.

## Decision rules

- If `IncludedObjectTypes` is populated, it is an allowlist: include every required main object type.
- Child and binding objects follow parent inclusion rules — do not list them separately.
- Prefer explicit object types over broad `IncludeAll` patterns.
- Add content-item-specific filters only when content item deployment is intentionally requested.
- Keep code name filters minimal and precise.
- Do not add multiple `IncludedCodeNames`/`ExcludedCodeNames` elements for the same object type — use one element with semicolon-separated values.
- **When `IncludedObjectTypes` contains `cms.contenttype` and `ObjectFilters` has an `IncludedCodeNames ObjectType="cms.contenttype"` entry, every content type listed in `IncludedContentItemsOfType` must also appear in that `ObjectFilters` code name list** — regardless of whether the type definition itself was changed in the included commits. If a type is missing from `ObjectFilters`, `kxp-cd-store` will silently suppress all content items of that type from the deployment package. See [Content type filtering impact on content items](https://docs.kentico.com/documentation/developers-and-admins/ci-cd/configure-ci-cd-repositories#scope-filtering-with-the-objecttype-attribute).

## RestoreMode selection

The deployment config **must** contain a `<RestoreMode>` element. Choose between the two modes:

- Analyze git history: if **all** CI `.xml` files under `ciRepositoryPath` in the deployment scope are **new** (created), use `Create` mode.
- If **any** CI `.xml` files are **modified** (updated), use `CreateUpdate` mode to preserve existing objects.
- Create mode has significant performance benefits, especially for content item deployments. Use CreateUpdate only when necessary for file updates.

## Formatting

Write code names one per line for readability. Within a single `<IncludedCodeNames>` element, list each code name on its own indented line with a semicolon separator (no trailing semicolon on the last entry):

```xml
<ObjectFilters>
  <IncludedCodeNames ObjectType="cms.contenttype">
    DancingGoat.Cafe;
    DancingGoat.FAQItem;
    DancingGoat.BuilderEmail
  </IncludedCodeNames>
  <IncludedCodeNames ObjectType="cms.class">
    CMS.ContentItemCommonData
  </IncludedCodeNames>
</ObjectFilters>
```

For `ContentItemFilters`, use a separate `<IncludedContentItemNames>` element per item (not semicolons), one per line:

```xml
<ContentItemFilters>
  <IncludedContentItemNames>BostonCoffeePlace-034jwdxo</IncludedContentItemNames>
  <IncludedContentItemNames>CafePhoto-nu2tjd9a</IncludedContentItemNames>
</ContentItemFilters>
```

## Quality checklist

Verify all of the following before finishing:

- XML is well-formed.
- No repeated `IncludedCodeNames` entries for the same object type.
- Code names match actual CI object code names from the XML files.
- Code names are listed one per line within elements (see Formatting above).
- Update-only groups are excluded (unless the user opted in).
- The config contains **only** entries justified by the current deployment scope — no leftovers from previous runs (see Regenerate, don't merge above).
- **Every `<ContentType>` listed in `IncludedContentItemsOfType` also appears in `ObjectFilters/IncludedCodeNames ObjectType="cms.contenttype"`** (when that filter element is present). Any content type absent from `ObjectFilters` will have its content items silently excluded from the deployment package by `kxp-cd-store`.
- The final config diff is concise and justified.
