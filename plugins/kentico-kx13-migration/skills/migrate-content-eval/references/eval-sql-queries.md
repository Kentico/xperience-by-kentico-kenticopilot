# Evaluation SQL Queries

SQL query catalog for database verification, organized by evaluation category.

**Execution:** `sqlcmd -S <server> -d <database> -Q "<query>" -W -s "|" -h -1`

**Database key:**

- **KX13** = source (`Settings.KxConnectionString`)
- **XbyK** = target (`Settings.XbyKApiSettings.ConnectionStrings.CMSConnectionString`)

## Parameter Resolution

Queries use placeholders. Resolve them from the migration plan detail before execution.

| Placeholder | Plan Section | Transform |
|---|---|---|
| `<excluded1>`, `<excluded2>` ... | Content Model Mapping > Exclusions table → Source Class column | Use as-is |
| `<Namespace_ClassName>` | Target Content Model > Webpage/Content Hub types → Class Name column | Replace `.` with `_` |
| `<TaxonomyFieldName>` | Field Mappings > Field Changes table → rows where Target Field is a taxonomy field | Use Target Field name |
| `<SchemaField1>`, `<SchemaField2>` ... | Target Content Model > Reusable Field Schemas → Description column field names | Use as-is |
| `<module_class1>`, `<module_class2>` ... | Source Content Model > Module Classes → Class Name column | Use as-is |

---

## Category 2: Content Types

### XbyK — All content types with classification

```sql
SELECT ClassName, ClassDisplayName, ClassContentTypeType
FROM CMS_Class
WHERE ClassContentTypeType IN ('Content', 'Website')
ORDER BY ClassContentTypeType, ClassName
```

Cross-reference: plan's "Webpage Content Types" → expect `Website`, "Content Hub Types" → expect `Content`.

### XbyK — Verify excluded classes have no content items

```sql
SELECT cc.ClassName, COUNT(*) AS ItemCount
FROM CMS_ContentItem ci
JOIN CMS_Class cc ON ci.ContentItemContentTypeID = cc.ClassID
WHERE cc.ClassName IN ('<excluded1>', '<excluded2>')
GROUP BY cc.ClassName
```

Any results → exclusion failed (likely `EntityConfigurations` key uses dots instead of underscores).

---

## Category 3: Reusable Field Schemas

### XbyK — Verify schema fields on CMS_ContentItemCommonData

Reusable schema fields are stored on `CMS_ContentItemCommonData`, NOT on per-content-type data tables.

```sql
SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'CMS_ContentItemCommonData'
  AND COLUMN_NAME IN ('<SchemaField1>', '<SchemaField2>')
ORDER BY COLUMN_NAME
```

Substitute field names from the plan's reusable schema definition.

---

## Category 4: Taxonomies & Tags

### XbyK — All taxonomies with tags

```sql
SELECT t.TaxonomyName, t.TaxonomyTitle, tag.TagName, tag.TagTitle
FROM CMS_Taxonomy t
LEFT JOIN CMS_Tag tag ON tag.TagTaxonomyID = t.TaxonomyID
ORDER BY t.TaxonomyName, tag.TagName
```

### XbyK — Taxonomy field data casing verification

Spot-check raw JSON values:

```sql
SELECT TOP 5 <TaxonomyFieldName>
FROM <Namespace_ClassName>
WHERE <TaxonomyFieldName> IS NOT NULL AND <TaxonomyFieldName> <> ''
```

Automated lowercase detection:

```sql
SELECT COUNT(*) AS LowercaseCount
FROM <Namespace_ClassName>
WHERE <TaxonomyFieldName> LIKE '%"identifier"%'
  AND <TaxonomyFieldName> NOT LIKE '%"Identifier"%'
```

`LowercaseCount > 0` → ConvertFrom produces wrong casing. Tags won't render.

---

## Category 5: Content Item Counts & Orphans

### KX13 — Source page counts (excluding linked pages)

```sql
SELECT ClassName, COUNT(*) AS SourceCount
FROM View_CMS_Tree_Joined
WHERE NodeLinkedNodeID IS NULL
GROUP BY ClassName
ORDER BY ClassName
```

### KX13 — Linked page counts (for duplication detection)

```sql
SELECT ClassName, COUNT(*) AS LinkedPageCount
FROM View_CMS_Tree_Joined
WHERE NodeLinkedNodeID IS NOT NULL
GROUP BY ClassName
ORDER BY ClassName
```

### XbyK — Target content item counts per class

```sql
SELECT cc.ClassName, COUNT(*) AS TargetCount
FROM CMS_ContentItem ci
JOIN CMS_Class cc ON ci.ContentItemContentTypeID = cc.ClassID
GROUP BY cc.ClassName
ORDER BY cc.ClassName
```

### XbyK — Website items without web page entries (orphans)

```sql
SELECT ci.ContentItemName, ci.ContentItemGUID, cc.ClassName
FROM CMS_ContentItem ci
JOIN CMS_Class cc ON ci.ContentItemContentTypeID = cc.ClassID
WHERE cc.ClassContentTypeType = 'Website'
AND NOT EXISTS (
    SELECT 1 FROM CMS_WebPageItem wp
    WHERE wp.WebPageItemContentItemID = ci.ContentItemID
)
```

### XbyK — Content items without language data (orphans)

```sql
SELECT ci.ContentItemName, cc.ClassName
FROM CMS_ContentItem ci
JOIN CMS_Class cc ON ci.ContentItemContentTypeID = cc.ClassID
WHERE NOT EXISTS (
    SELECT 1 FROM CMS_ContentItemCommonData cd
    WHERE cd.ContentItemCommonDataContentItemID = ci.ContentItemID
)
```

---

## Category 6: Field Verification

### XbyK — Columns for a content type data table

```sql
SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = '<Namespace_ClassName>'
ORDER BY ORDINAL_POSITION
```

Replace dots with underscores (e.g., `MedioClinic.Doctor` → `MedioClinic_Doctor`). Skip system fields: `ContentItemDataID`, `ContentItemDataCommonDataID`, `ContentItemDataGUID`.

Reusable schema fields are on `CMS_ContentItemCommonData` — see Category 3.

### XbyK — SEO metadata population spot-check

```sql
SELECT TOP 5 DocumentPageTitle, DocumentPageDescription
FROM <Namespace_ClassName>
WHERE DocumentPageTitle IS NOT NULL OR DocumentPageDescription IS NOT NULL
```

---

## Category 7: Page Builder

### XbyK — Widget data coverage

```sql
SELECT
    COUNT(*) AS TotalWebPages,
    SUM(CASE
        WHEN cd.ContentItemCommonDataPageBuilderWidgets IS NOT NULL
         AND cd.ContentItemCommonDataPageBuilderWidgets <> ''
         AND cd.ContentItemCommonDataPageBuilderWidgets <> '{"editableAreas":[]}'
        THEN 1 ELSE 0
    END) AS PagesWithWidgets
FROM CMS_WebPageItem wp
JOIN CMS_ContentItem ci ON wp.WebPageItemContentItemID = ci.ContentItemID
JOIN CMS_Class cc ON ci.ContentItemContentTypeID = cc.ClassID
JOIN CMS_ContentItemCommonData cd ON cd.ContentItemCommonDataContentItemID = ci.ContentItemID
WHERE cc.ClassContentTypeType = 'Website'
```

### XbyK — Widget type distribution

```sql
SELECT
    JSON_VALUE(w.value, '$.type') AS WidgetType,
    COUNT(DISTINCT wp.WebPageItemID) AS PageCount
FROM CMS_WebPageItem wp
JOIN CMS_ContentItem ci ON wp.WebPageItemContentItemID = ci.ContentItemID
JOIN CMS_ContentItemCommonData cd ON cd.ContentItemCommonDataContentItemID = ci.ContentItemID
CROSS APPLY OPENJSON(cd.ContentItemCommonDataPageBuilderWidgets, '$.editableAreas') ea
CROSS APPLY OPENJSON(ea.value, '$.sections') s
CROSS APPLY OPENJSON(s.value, '$.zones') z
CROSS APPLY OPENJSON(z.value, '$.widgets') w
WHERE cd.ContentItemCommonDataPageBuilderWidgets IS NOT NULL
GROUP BY JSON_VALUE(w.value, '$.type')
```

Cross-reference: source widget type still present → FAIL (transformation not applied).

---

## Category 8: Users

### KX13 — Source user count (Editor privilege and above)

```sql
SELECT COUNT(*) AS SourceUserCount FROM CMS_User WHERE UserPrivilegeLevel >= 2
```

### XbyK — Target users

```sql
SELECT UserName, Email FROM CMS_User ORDER BY UserName
```

---

## Category 9: Media

### KX13 — Source media file count

```sql
SELECT COUNT(*) AS SourceMediaCount FROM Media_File
```

### KX13 — Source attachment count

```sql
SELECT COUNT(*) AS SourceAttachmentCount FROM CMS_Attachment
```

### XbyK — Legacy.MediaFile count

```sql
SELECT COUNT(*) AS MediaFileCount
FROM CMS_ContentItem ci
JOIN CMS_Class cc ON ci.ContentItemContentTypeID = cc.ClassID
WHERE cc.ClassName = 'Legacy.MediaFile'
```

### XbyK — Legacy.Attachment count

```sql
SELECT COUNT(*) AS AttachmentCount
FROM CMS_ContentItem ci
JOIN CMS_Class cc ON ci.ContentItemContentTypeID = cc.ClassID
WHERE cc.ClassName = 'Legacy.Attachment'
```

---

## Category 10: Forms

### KX13 — Source forms

```sql
SELECT FormName, FormDisplayName FROM CMS_Form ORDER BY FormName
```

### XbyK — Target forms

```sql
SELECT FormName, FormDisplayName FROM CMS_Form ORDER BY FormName
```

---

## Category 11: Custom Modules

### KX13 — Source module data counts

```sql
SELECT COUNT(*) AS SourceCount FROM <Namespace_ClassName>
```

### XbyK — Module classes with data counts

```sql
SELECT cc.ClassName, COUNT(ci.ContentItemID) AS ItemCount
FROM CMS_Class cc
LEFT JOIN CMS_ContentItem ci ON ci.ContentItemContentTypeID = cc.ClassID
WHERE cc.ClassName IN ('<module_class1>', '<module_class2>')
GROUP BY cc.ClassName
```
