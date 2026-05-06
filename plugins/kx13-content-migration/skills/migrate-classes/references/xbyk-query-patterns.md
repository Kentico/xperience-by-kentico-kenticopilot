# XbyK Database Query Patterns

SQL query templates for resolving lookup values from the XbyK target database before generating class mapping code. Use `sqlcmd` with parameters extracted from the connection string (`Settings.XbyKApiSettings.ConnectionStrings.CMSConnectionString`): `-S` (server), `-d` (database), `-Q` (inline query), `-W` (trim whitespace).

---

## Taxonomy Group GUIDs

When a converter field uses `f.Settings["TaxonomyGroup"]` in a `WithFieldPatch`, resolve the taxonomy GUID:

```sql
SELECT TaxonomyName, CAST(TaxonomyGUID AS CHAR(36)) AS TaxonomyGUID
FROM CMS_Taxonomy
WHERE TaxonomyName = '<TaxonomyName>'
```

Use the returned `TaxonomyGUID` in the field patch: `f.Settings["TaxonomyGroup"] = "[\"<TaxonomyGUID>\"]"`.

To list all available taxonomies:

```sql
SELECT TaxonomyName, CAST(TaxonomyGUID AS CHAR(36)) AS TaxonomyGUID
FROM CMS_Taxonomy
ORDER BY TaxonomyName
```

## Taxonomy Tag GUIDs

When a converter maps source values (NodeGUIDs, free-text values, relationship data) to taxonomy tags, resolve the tag GUIDs:

```sql
SELECT TagName, CAST(TagGUID AS CHAR(36)) AS TagGUID
FROM CMS_Tag
WHERE TagTaxonomyID = (SELECT TaxonomyID FROM CMS_Taxonomy WHERE TaxonomyName = '<TaxonomyName>')
ORDER BY TagName
```

Use the returned `TagGUID` values as dictionary values in the converter lookup. The dictionary keys come from KX13 data (e.g., NodeGUIDs from relationship queries, or distinct field values).

## Content Item GUIDs

When a converter needs to reference content items that were migrated in a previous step:

```sql
SELECT ContentItemName, CAST(ContentItemGUID AS CHAR(36)) AS ContentItemGUID
FROM CMS_ContentItem
WHERE ContentItemName IN ('<Name1>', '<Name2>')
```

## Verifying Taxonomy/Tag Existence

Before using taxonomy or tag GUIDs, verify they exist. If a query returns no rows, the taxonomy or tags have not been created yet — fall back to TODO placeholders.

```sql
-- Check if a taxonomy exists
SELECT COUNT(*) AS TaxonomyExists FROM CMS_Taxonomy WHERE TaxonomyName = '<TaxonomyName>'

-- Check if tags exist for a taxonomy
SELECT COUNT(*) AS TagCount FROM CMS_Tag
WHERE TagTaxonomyID = (SELECT TaxonomyID FROM CMS_Taxonomy WHERE TaxonomyName = '<TaxonomyName>')
```
