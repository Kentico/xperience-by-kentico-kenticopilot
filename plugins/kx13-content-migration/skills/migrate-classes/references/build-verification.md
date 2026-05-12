# Post-Build Verification

After a successful build of the `Migration.Tool.Extensions` project, perform these verification steps.

## Verify Registration Completeness

1. Read the `ServiceCollectionExtensions` class and confirm every `IClassMapping` has a corresponding `AddSingleton<IClassMapping>` call.
2. Read the `ServiceCollectionExtensions` class and confirm every `IReusableSchemaBuilder` has a corresponding `AddSingleton<IReusableSchemaBuilder>` call.
3. Cross-reference against the migration plan's "Code Extensions to Implement" table — every row where Type = `IClassMapping` must have a generated file and a registration.
4. If any mapping is missing registration, add it and rebuild.

## Verify WithFieldPatch Safety

Scan **every** generated file for the `ConvertFrom` + `WithFieldPatch` anti-pattern that causes a runtime `NullReferenceException`:

1. For each `WithFieldPatch` call, trace backwards to confirm that the field has a prior definition source — one of: `WithoutSource(dataType)`, `SetFrom(isTemplate: true)`, or `ConvertFrom(includeDefinition: true)`.
2. Flag any `WithFieldPatch` that follows only `ConvertFrom(includeDefinition: false)` or `ConvertFrom(..., false, ...)` without a preceding `WithoutSource`. This applies to **all** field types — not just taxonomy or docrelationships fields. Simple fields like DocumentName → new text field are equally affected.
3. For each flagged instance, insert `WithoutSource(dataType)` before the `ConvertFrom` call and convert the fluent chain to the three-step local variable pattern:

   ```csharp
   var field = m.BuildField("Target");
   field.WithoutSource("text");
   field.ConvertFrom(source, "Field", false, converter);
   field.WithFieldPatch(f => { /* safe */ });
   ```

4. If any fixes were applied, rebuild and re-verify.
