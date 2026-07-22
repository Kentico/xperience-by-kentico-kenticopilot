# Evaluation Categories

Per-category evaluation logic. For each category, compare gathered data against plan expectations. Assign status: **PASS**, **FAIL**, **WARN**, or **N/A**.

---

## Category 1: Configuration & Run Overview

**Sources:** YAML `run` section

- All expected migration commands completed → PASS
- Any command crashed → FAIL, any missing → WARN
- Note per-command elapsed times and re-run context (`isReRun`, `runNumber`)

On re-runs: "already exists" messages → INFO (normal). DataClassInfo "updated" → expected.

---

## Category 2: Content Types

**Sources:** XbyK `CMS_Class` query + protocol log `DataClassInfo` entries + plan

For each planned type:

- Migration tool type missing → FAIL
- Manual type missing → WARN
- Type exists with wrong `ClassContentTypeType` → FAIL

Excluded classes that still exist as content types → WARN (orphaned, should be deleted — see Gotchas in SKILL.md).

---

## Category 3: Reusable Field Schemas

**Sources:** XbyK `CMS_ContentItemCommonData` columns + plan

For each planned schema, verify expected fields exist on `CMS_ContentItemCommonData` (NOT on per-type data tables):

- Schema fields missing → FAIL
- All present → PASS

---

## Category 4: Taxonomies & Tags

**Sources:** XbyK `CMS_Taxonomy` / `CMS_Tag` queries + plan

For each planned taxonomy:

- Missing taxonomy from `--categories` → FAIL
- Missing manual pre-migration taxonomy → WARN
- Missing post-migration taxonomy → INFO
- Missing expected tags → WARN

**Critical — tag data quality:** For content types with taxonomy fields, query the data table:

- `"Identifier"` (PascalCase) → PASS
- `"identifier"` (lowercase) → **FAIL** — tags won't render in XbyK, no log error produced

---

## Category 5: Content Item Counts & Orphans

**Sources:** KX13 `View_CMS_Tree_Joined` + XbyK `CMS_ContentItem` counts

Per mapped class:

- Zero target when source > 0 → FAIL
- Count mismatch → WARN
- Match → PASS

**Orphan detection:** Website items without web page entries → FAIL. Items without language data → WARN.

**Linked page duplication:** Compare target count vs source originals (excluding linked). If target ≈ originals + linked count → **WARN** (linked pages silently materialized as duplicates).

---

## Category 6: Field Verification

**Sources:** XbyK `INFORMATION_SCHEMA.COLUMNS` + plan's Field Mappings

Per content type:

- Expected target field missing → FAIL
- All present → PASS
- Skip system fields: `ContentItemDataID`, `ContentItemDataCommonDataID`, `ContentItemDataGUID`

**SEO metadata:** If plan indicates extended metadata, verify columns exist and spot-check population (TOP 5 non-null rows).

---

## Category 7: Page Migration Issues

**Sources:** YAML errors/warnings + protocol log `NeedManualAction: True` entries + XbyK Page Builder data

Page error classification:

| Sub-type | Detection | Status |
|---|---|---|
| VisualBuilderPatcher crash | `ArgumentNullException` in `WalkWidgets` | FAIL |
| Content item reference failure | `linked content item with GUID` | FAIL |
| NullRef after drop | `NullReferenceException` at `MigratePages()` | WARN |
| Missing source field | `Value is not contained in source` | Usually INFO |
| Explicit drop | `Content item skipped. Reason:` | INFO if matches plan |

**Widget verification:** Query widget type distribution from Page Builder JSON. Cross-reference plan's widget transformations:

- Source widget type still present in XbyK → FAIL (transformation not applied)
- Target widget type present → PASS

Deduplicate — a single root cause produces entries in both logs.

---

## Category 8: Users & Roles

**Sources:** Protocol log user/role entries + KX13 vs XbyK user counts

- Zero target when source > 0 → FAIL
- Count mismatch → WARN
- Match → PASS

Admin user skip is expected (INFO).

---

## Category 9: Media & Attachments

**Sources:** YAML media stats + KX13 vs XbyK counts

Compare `Media_File` → `Legacy.MediaFile` and `CMS_Attachment` → `Legacy.Attachment`:

- Zero target when source > 0 → FAIL
- >10% discrepancy → WARN
- Close match → PASS

---

## Category 10: Forms

**Sources:** KX13 vs XbyK `CMS_Form` names

- Source form missing in target → FAIL
- All present → PASS

`Total=0, TotalCopied=0` is NOT an error (empty form, no submissions).

---

## Category 11: Custom Modules

**Sources:** Protocol log `ResourceInfo` entries + XbyK data counts

- Module class missing → FAIL
- Class exists but zero data when source had data → WARN
- All present → PASS

---

## Category 12: Overall Health

Aggregate from all categories:

- Protocol totals: success / skip / fail
- DB verification totals: PASS / FAIL / WARN counts
- Per-command elapsed times
- Status = worst status from all other categories
