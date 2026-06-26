# Post-Step Validation Queries

Run these validation queries against the XbyK database after each major migration step succeeds. Use the XbyK connection string from `appsettings.json` → `XbyKApiSettings.ConnectionStrings.CMSConnectionString` with `sqlcmd`.

## After `--page-types`

```sql
-- Verify all planned content types were created
SELECT ClassName, ClassContentTypeType FROM CMS_Class
WHERE ClassContentTypeType IN ('Content', 'Website') ORDER BY ClassName
```

Cross-reference against the migration plan's target content types. Flag any missing types — they may be target-only types that need manual creation in XbyK Administration.

```sql
-- Verify excluded classes were NOT created
SELECT ClassName FROM CMS_Class WHERE ClassName IN ('<excluded1>', '<excluded2>')
```

If excluded classes appear, the `EntityConfigurations.ExcludeCodeNames` setting may not be working correctly. Flag to the user.

## After `--pages`

```sql
-- Verify webpage content items have web page entries (detect orphans)
SELECT ci.ContentItemName, cc.ClassName
FROM CMS_ContentItem ci
JOIN CMS_Class cc ON ci.ContentItemContentTypeID = cc.ClassID
WHERE cc.ClassContentTypeType = 'Website'
AND NOT EXISTS (
    SELECT 1 FROM CMS_WebPageItem wp
    WHERE wp.WebPageItemContentItemID = ci.ContentItemID
)
```

Flag any webpage-type content items without web page entries — these are orphan pages that exist as content items but are not in the page tree.

## After `--categories`

```sql
-- Verify planned taxonomies were created
SELECT TaxonomyName FROM CMS_Taxonomy ORDER BY TaxonomyName
```

Cross-reference against the migration plan's taxonomy section. Flag any missing taxonomies — they may be new taxonomies requiring manual creation.

## After `--custom-modules`

```sql
-- Verify module classes were created
SELECT ClassName FROM CMS_Class WHERE ClassName IN ('<module_class1>', '<module_class2>')
```

Flag any missing module classes.
