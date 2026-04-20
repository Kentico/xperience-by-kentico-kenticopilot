# KX13 Database Query Patterns

SQL query templates for resolving lookup values from the KX13 source database before generating class mapping code. Use `sqlcmd` with parameters extracted from the connection string: `-S` (server), `-d` (database), `-Q` (inline query), `-W` (trim whitespace), `-h -1` (no headers).

---

## User ID to UserName Mappings

When converters need to resolve KX13 `UserID` foreign keys:

```sql
SELECT UserID, UserName FROM CMS_User WHERE UserID IN (SELECT DISTINCT <FKColumn> FROM <SourceTable> WHERE <FKColumn> IS NOT NULL)
```

## Distinct Field Values (Selectors, Enums, Free-Text Categories)

When the migration plan assumes certain values but actual KX13 data may differ:

```sql
SELECT DISTINCT <FieldName> FROM <SourceTable> WHERE <FieldName> IS NOT NULL ORDER BY <FieldName>
```

This reveals actual values for fields like Specialty, Country, Status, etc., ensuring converter dictionaries have correct keys.

## Country / Composite Selector Values

KX13 `countrySelector` fields may store simple ISO codes (e.g., `US`), full names, or composite values (e.g., `USA;NewHampshire` for country+state). Always query first to discover the actual format before writing a converter.

## Relationship Data

When `CMS_Relationship` entries map to content item references:

```sql
SELECT r.LeftNodeID, dn.DocumentName AS LeftDoc, r.RightNodeID, rn.DocumentName AS RightDoc, rn.ClassName
FROM CMS_Relationship r
JOIN View_CMS_Tree_Joined dn ON dn.NodeID = r.LeftNodeID AND dn.DocumentCulture = 'en-US'
JOIN View_CMS_Tree_Joined rn ON rn.NodeID = r.RightNodeID AND rn.DocumentCulture = 'en-US'
WHERE dn.ClassName = '<SourceClassName>'
```

## Source Field Name Verification

For every `SetFrom` or `ConvertFrom` mapping that references a source field, verify the field actually exists in the KX13 database:

```sql
SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = '<SourceTableName>'
ORDER BY ORDINAL_POSITION
```

The source table name follows the pattern `Namespace_ClassName` (dots replaced with underscores, e.g., `MedioClinic.Doctor` -> `MedioClinic_Doctor`).

- If a source field referenced in `SetFrom("SourceClass", "FieldName")` does not exist in the source table, flag it as an error and ask the user for the correct field name.
- If the migration plan proposes a field rename (e.g., `DocumentPageTitle` -> `SEOMetaTitle`), verify both that the source field exists AND that the generated code includes the corresponding `SetFrom` or `ConvertFrom` with the rename. A planned rename that is not reflected in the generated code will result in empty fields.
