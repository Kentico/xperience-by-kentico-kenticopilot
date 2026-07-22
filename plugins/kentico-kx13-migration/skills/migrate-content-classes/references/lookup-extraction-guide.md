# Lookup Value Extraction Guide

How to extract all lookup values that converters need from the migration plan document. The plan is the trusted source of truth — do not query databases when the plan already provides the information. If the plan is contradictory, incomplete, or silent on a value, use `sqlcmd` to query the KX13 or XbyK database to resolve the gap.

## 4a. Extract Source Field Names and Data Types

For every `SetFrom` or `ConvertFrom` mapping, read the source field name, data type, and form control from the migration plan's **Source Content Model** field tables. The plan documents every source page type's fields with their exact names and types — trust these as authoritative.

- If the plan references a source field that is not documented in its own field tables, flag it as ambiguous and use `sqlcmd` to query the KX13 `CMS_ClassFormDefinition` or the coupled data table to verify the field name. Ask the user for clarification only if the query result is also ambiguous.
- **Cross-reference every source field name against the migration plan**. The migration plan is the ground truth for column names — subtle differences like `LeaderCTALinkText1` vs. `LeaderCTAText1` cause hard failures at runtime.
- Do not query the KX13 database when the plan already documents the field — trust the plan as the verified source.

## 4b. Extract Converter Lookup Keys

For converters that need lookup dictionaries (e.g., NodeGUID→TagGUID mappings, country code→name mappings), extract the dictionary **keys** from the migration plan:

- **Relationship NodeGUIDs** — the plan's **Custom Value Transforms** section documents KX13 NodeGUIDs for each related page (e.g., DayOfWeek NodeGUIDs). Use these directly as dictionary keys.
- **Distinct field values** — the plan documents expected values for selector/enum fields. Use these as dictionary keys.
- **Country / composite values** — the plan documents the field's form control and expected behavior. Use the plan's notes to determine the value format.

## 4c. Handle Values That Depend on XbyK Target Instance

For lookup dictionary **values** that depend on XbyK runtime state (taxonomy tag GUIDs, taxonomy group GUIDs, content item GUIDs):

1. **Check the migration plan first** — the plan's lookup dictionary tables (e.g., Custom Value Transforms) are the trusted source of truth. If the plan contains actual GUIDs (not `TODO`), use them directly in the generated code. Do not ask the user, do not query any database — trust the plan.
2. **If the plan contains `TODO` placeholders**, use `sqlcmd` to query the XbyK database to resolve them (e.g., `SELECT TagName, CAST(TagGUID AS CHAR(36)) FROM CMS_Tag ...`). If the query succeeds, use the resolved values directly. If the database is unreachable or the taxonomy has not been created yet, generate TODO placeholders in the code with the specific SQL query the user can run later, using templates from [xbyk-query-patterns.md](xbyk-query-patterns.md).

## 4d. Resolve Gaps and Ask the User Only When Necessary

When the plan is contradictory, incomplete, or silent on a value:

1. **Try resolving via `sqlcmd` first** — query the KX13 database (for source field names, data types, class IDs) or XbyK database (for taxonomy GUIDs, content item GUIDs) to resolve the gap.
2. **Ask the user only when** the gap cannot be resolved from the plan or from a database query:
   - The plan is contradictory about a field name, data type, or mapping direction and the database query returns ambiguous results.
   - The database is unreachable and a TODO placeholder is not acceptable.
   - The plan references a source class or field that does not appear in any of its own documentation tables and is not found in the KX13 database.

Do **not** ask the user for taxonomy tag GUIDs, taxonomy group GUIDs, or other lookup dictionary values that are documented in the plan's Custom Value Transforms tables — use whatever the plan contains (actual GUIDs or `TODO` placeholders) directly.

When asking about genuinely ambiguous items, always include a **suggested resolution** — for example:

```text
The plan's Field Changes table lists SpecialtyText as "text" but the Source Content Model
field table shows it as "longtext". Which data type should the ConvertFrom converter expect?
```
