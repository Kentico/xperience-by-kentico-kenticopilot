# docrelationships Field Handling Guide

When the migration plan identifies `docrelationships` fields (relationship-based fields that store data in `CMS_Relationship` rather than the coupled data table):

1. **Use factory DI registration** — the mapping's `AddSingleton` call must use a factory delegate that injects `ModelFacade` and `CmsRelationshipService`:

   ```csharp
   serviceCollection.AddSingleton<IClassMapping>(sp =>
       BuildMapping(sp.GetRequiredService<ModelFacade>(), sp.GetRequiredService<CmsRelationshipService>()));
   ```

2. **Use the `WithoutSource` + `ConvertFrom` + `WithFieldPatch` pattern** — `WithoutSource("taxonomy")` creates the field definition, `ConvertFrom` provides the converter, and `WithFieldPatch` sets taxonomy form control settings with `Visible = true` and `Enabled = true`.
3. **Use a non-relationship source field in `ConvertFrom` when the target is NOT a page reference** — when the target field is a taxonomy, object code name, or any non-page type, do NOT use the `docrelationships` field name as the `ConvertFrom` source field. The built-in `ConvertToPages` pipeline detects `docrelationships`/`Pages` source fields and overwrites the converter output with `ContentItemReference` GUIDs. Use the primary key field or another non-relationship field instead. The converter ignores the incoming value anyway. **However**, when the target field IS a `contentitemreference` (Pages) field that should link to migrated pages, keep the original `docrelationships` source field so that `ConvertToPages` correctly resolves the page references.
4. **Extract relationship lookup data from the migration plan** — the plan's Custom Value Transforms section documents KX13 NodeGUIDs for related pages. Use these as dictionary keys mapping to XbyK taxonomy tag GUIDs.
5. **Use PascalCase `Identifier` in taxonomy JSON** — `[{"Identifier":"guid"}]`, not lowercase.

See Sample 11 in `class-mapping-api.md` for the complete annotated pattern.
