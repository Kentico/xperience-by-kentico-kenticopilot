// =============================================================================
// CLASS_MAPPING_EXAMPLE.cs
//
// Complete annotated example showing all IClassMapping patterns for a realistic
// migration scenario (MedioClinic KX13 → XbyK). Use this as a reference when
// generating class mapping code.
//
// This file is NOT meant to be compiled directly — it demonstrates patterns.
// =============================================================================

using CMS.DataEngine;
using CMS.FormEngine;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Migration.Tool.Common.Abstractions;
using Migration.Tool.Common.Builders;
using Migration.Tool.KXP.Api.Auxiliary;
using Migration.Tool.Source;
using Migration.Tool.Source.Model;
using Migration.Tool.Source.Services;

namespace Migration.Tool.Extensions.ClassMappings;

// =============================================================================
// PATTERN: Simple field rename mapping (website content type)
// When to use: 1:1 class mapping with field renames, caption changes, or
//              data type adjustments. Source and target are the same logical type.
// =============================================================================
public static class ContactPageClassMapping
{
    // Source
    private const string Source_ClassName = "MedioClinic.NamePerexText";
    private const string Source_Field_Perex = "Perex";
    private const string Source_Field_Text = "Text";

    // Target
    private const string Target_ClassName = "MedioClinic.ContactPage";
    private const string Target_TableName = "MedioClinic_ContactPage";
    private const string Target_DisplayName = "Contact page";
    private const string Target_Field_ID = "ContactPageID";
    private const string Target_Field_Perex = "ContactPagePerex";
    private const string Target_Field_Text = "ContactPageText";

    private static MultiClassMapping BuildMapping()
    {
        var m = new MultiClassMapping(Target_ClassName, target =>
        {
            target.ClassName = Target_ClassName;
            target.ClassTableName = Target_TableName;
            target.ClassDisplayName = Target_DisplayName;
            target.ClassType = ClassType.CONTENT_TYPE;
            target.ClassContentTypeType = ClassContentTypeType.WEBSITE;
        });

        m.BuildField(Target_Field_ID).AsPrimaryKey();

        // PATTERN: SetFrom with isTemplate: true — copies field definition from KX13
        m.BuildField(Target_Field_Perex)
            .SetFrom(Source_ClassName, Source_Field_Perex, true);

        m.BuildField(Target_Field_Text)
            .SetFrom(Source_ClassName, Source_Field_Text, true);

        return m;
    }

    public static IServiceCollection AddContactPageMapping(
        this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IClassMapping>(BuildMapping());
        return serviceCollection;
    }
}

// =============================================================================
// PATTERN: Reusable content type with value converters
// When to use: Converting a KX13 page type to a Content Hub reusable item,
//              with custom value transformations (name splitting, taxonomy lookup).
// Requires: appsettings.json → ConvertClassesToContentHub includes this class.
// =============================================================================
public static class DoctorClassMapping
{
    // Source
    private const string Source_ClassName = "MedioClinic.Doctor";
    private const string Source_Field_DocumentName = "DocumentName";
    private const string Source_Field_Degree = "Degree";
    private const string Source_Field_Biography = "Biography";
    private const string Source_Field_Specialty = "Specialty";

    // Target
    private const string Target_ClassName = "MedioClinic.Doctor";
    private const string Target_TableName = "MedioClinic_Doctor";
    private const string Target_DisplayName = "Doctor";
    private const string Target_Field_ID = "DoctorID";
    private const string Target_Field_FirstName = "DoctorFirstName";
    private const string Target_Field_LastName = "DoctorLastName";
    private const string Target_Field_Degree = "DoctorDegree";
    private const string Target_Field_Biography = "DoctorBiography";
    private const string Target_Field_Specialty = "DoctorSpecialty";

    // PATTERN: Taxonomy tag GUID lookup dictionary
    // Keys populated from KX13 DB query:
    //   SELECT DISTINCT Specialty FROM MedioClinic_Doctor WHERE Specialty IS NOT NULL
    // Values: TODO — replace with actual taxonomy tag GUIDs after creating taxonomy in XbyK.
    //   Query XbyK: SELECT TagName, TagGUID FROM CMS_Tag
    //     WHERE TagTaxonomyID = (SELECT TaxonomyID FROM CMS_Taxonomy WHERE TaxonomyName = 'MedicalSpecialty')
    private static readonly Dictionary<string, Guid> SpecialtyLookup = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Cardiology"] = new Guid("00000000-0000-0000-0000-000000000001"),
        ["Emergency Medicine"] = new Guid("00000000-0000-0000-0000-000000000002"),
        ["General Practice"] = new Guid("00000000-0000-0000-0000-000000000003"),
    };

    private static MultiClassMapping BuildMapping()
    {
        var m = new MultiClassMapping(Target_ClassName, target =>
        {
            target.ClassName = Target_ClassName;
            target.ClassTableName = Target_TableName;
            target.ClassDisplayName = Target_DisplayName;
            target.ClassType = ClassType.CONTENT_TYPE;
            // PATTERN: REUSABLE content type — migrates to Content Hub
            target.ClassContentTypeType = ClassContentTypeType.REUSABLE;
            target.ClassWebPageHasUrl = false;
        });

        m.BuildField(Target_Field_ID).AsPrimaryKey();

        // PATTERN: ConvertFrom — split DocumentName into first/last name
        m.BuildField(Target_Field_FirstName)
            .ConvertFrom(Source_ClassName, Source_Field_DocumentName, false, (value, context) =>
            {
                if (value is not string name || string.IsNullOrWhiteSpace(name))
                    return null;
                var lastSpace = name.LastIndexOf(' ');
                return lastSpace > 0 ? name[..lastSpace].Trim() : name.Trim();
            })
            .WithFieldPatch(f =>
            {
                f.Caption = "First name";
                f.Visible = true;      // Required — FormDefinitionPatcher may reset visibility
                f.Enabled = true;      // Required — ensures the field is editable
                f.AllowEmpty = true;
                f.DataType = FieldDataType.Text;
                f.Size = 200;
            });

        m.BuildField(Target_Field_LastName)
            .ConvertFrom(Source_ClassName, Source_Field_DocumentName, false, (value, context) =>
            {
                if (value is not string name || string.IsNullOrWhiteSpace(name))
                    return null;
                var lastSpace = name.LastIndexOf(' ');
                return lastSpace > 0 ? name[(lastSpace + 1)..].Trim() : null;
            })
            .WithFieldPatch(f =>
            {
                f.Caption = "Last name";
                f.Visible = true;      // Required — FormDefinitionPatcher may reset visibility
                f.Enabled = true;      // Required — ensures the field is editable
                f.AllowEmpty = true;
                f.DataType = FieldDataType.Text;
                f.Size = 200;
            });

        // PATTERN: Simple rename with isTemplate
        m.BuildField(Target_Field_Degree)
            .SetFrom(Source_ClassName, Source_Field_Degree, true);

        m.BuildField(Target_Field_Biography)
            .SetFrom(Source_ClassName, Source_Field_Biography, true);

        // PATTERN: ConvertFrom — free-text to taxonomy tag GUID
        m.BuildField(Target_Field_Specialty)
            .ConvertFrom(Source_ClassName, Source_Field_Specialty, false, (value, context) =>
            {
                if (value is not string specialty || string.IsNullOrWhiteSpace(specialty))
                    return null;
                if (!SpecialtyLookup.TryGetValue(specialty.Trim(), out var guid))
                    return null;
                // PascalCase "Identifier" — matches XbyK TagReference property
                return $"[{{\"Identifier\":\"{guid}\"}}]";
            })
            .WithFieldPatch(f =>
            {
                f.Caption = "Medical specialty";
                f.Visible = true;      // Required — FormDefinitionPatcher may reset visibility
                f.Enabled = true;      // Required — ensures the field is editable
                f.AllowEmpty = true;
                f.DataType = "taxonomy";
                f.Settings["controlname"] = "Kentico.Administration.TagSelector";
                // TODO: Replace with actual MedicalSpecialty taxonomy group GUID
                f.Settings["TaxonomyGroup"] = "[\"TODO-TAXONOMY-GROUP-GUID\"]";
            });

        return m;
    }

    public static IServiceCollection AddDoctorMapping(
        this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IClassMapping>(BuildMapping());
        return serviceCollection;
    }
}

// =============================================================================
// PATTERN: Merge mapping — multiple source classes into one target
// When to use: Two or more KX13 page types consolidate into a single XbyK
//              content type. Each source provides different fields.
// =============================================================================
public static class OfficeLocationClassMapping
{
    // Source 1 — Company (address fields)
    private const string Source_Company = "MedioClinic.Company";
    private const string Source_Company_Street = "Street";
    private const string Source_Company_City = "City";
    private const string Source_Company_Country = "Country";
    private const string Source_Company_DocumentName = "DocumentName";

    // Source 2 — MapLocation (coordinate fields)
    private const string Source_MapLocation = "MedioClinic.MapLocation";
    private const string Source_MapLocation_Latitude = "Latitude";
    private const string Source_MapLocation_Longitude = "Longitude";
    private const string Source_MapLocation_DocumentName = "DocumentName";

    // Target
    private const string Target_ClassName = "MedioClinic.OfficeLocation";
    private const string Target_TableName = "MedioClinic_OfficeLocation";
    private const string Target_DisplayName = "Office location";
    private const string Target_Field_ID = "OfficeLocationID";
    private const string Target_Field_Name = "OfficeLocationName";
    private const string Target_Field_Street = "OfficeLocationStreet";
    private const string Target_Field_City = "OfficeLocationCity";
    private const string Target_Field_Country = "OfficeLocationCountry";
    private const string Target_Field_Latitude = "OfficeLocationLatitude";
    private const string Target_Field_Longitude = "OfficeLocationLongitude";

    // PATTERN: Country code lookup
    // Query KX13 first to discover actual format:
    //   SELECT DISTINCT Country FROM MedioClinic_Company WHERE Country IS NOT NULL
    // KX13 countrySelector may store ISO codes, full names, or composite "Country;State" values.
    private static readonly Dictionary<string, string> CountryLookup = new(StringComparer.OrdinalIgnoreCase)
    {
        ["US"] = "United States",
        ["GB"] = "United Kingdom",
        ["CZ"] = "Czech Republic",
    };

    private static MultiClassMapping BuildMapping()
    {
        var m = new MultiClassMapping(Target_ClassName, target =>
        {
            target.ClassName = Target_ClassName;
            target.ClassTableName = Target_TableName;
            target.ClassDisplayName = Target_DisplayName;
            target.ClassType = ClassType.CONTENT_TYPE;
            target.ClassContentTypeType = ClassContentTypeType.REUSABLE;
            target.ClassWebPageHasUrl = false;
        });

        m.BuildField(Target_Field_ID).AsPrimaryKey();

        // PATTERN: Merge — field populated by both sources via ConvertFrom
        // Each source's DocumentName maps to the same target field
        m.BuildField(Target_Field_Name)
            .ConvertFrom(Source_Company, Source_Company_DocumentName, false,
                (value, _) => value?.ToString()?.Trim())
            .ConvertFrom(Source_MapLocation, Source_MapLocation_DocumentName, false,
                (value, _) => value?.ToString()?.Replace("-", " ").Trim());

        // PATTERN: isTemplate: true on the Company source — it provides field definitions
        m.BuildField(Target_Field_Street)
            .SetFrom(Source_Company, Source_Company_Street, true);

        m.BuildField(Target_Field_City)
            .SetFrom(Source_Company, Source_Company_City, true);

        // PATTERN: ConvertFrom with value transformation (country code → name)
        m.BuildField(Target_Field_Country)
            .ConvertFrom(Source_Company, Source_Company_Country, true, (value, context) =>
            {
                if (value is not string code || string.IsNullOrWhiteSpace(code))
                    return null;
                return CountryLookup.TryGetValue(code.Trim(), out var name) ? name : code;
            });

        // PATTERN: ConvertFrom with data type change (double → decimal)
        m.BuildField(Target_Field_Latitude)
            .ConvertFrom(Source_MapLocation, Source_MapLocation_Latitude, true, (value, context) =>
            {
                return value switch
                {
                    double d => (decimal)d,
                    null => null,
                    _ => Convert.ToDecimal(value)
                };
            });

        m.BuildField(Target_Field_Longitude)
            .ConvertFrom(Source_MapLocation, Source_MapLocation_Longitude, true, (value, context) =>
            {
                return value switch
                {
                    double d => (decimal)d,
                    null => null,
                    _ => Convert.ToDecimal(value)
                };
            });

        return m;
    }

    public static IServiceCollection AddOfficeLocationMapping(
        this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IClassMapping>(BuildMapping());
        return serviceCollection;
    }
}

// =============================================================================
// PATTERN: Reusable field schema + mapping with schema assignment
// When to use: Extracting common fields (e.g., SEO metadata) into a shared
//              schema used by multiple content types.
// =============================================================================
public static class HomePageClassMapping
{
    // Source
    private const string Source_ClassName = "MedioClinic.HomePage";
    private const string Source_Field_Perex = "Perex";
    private const string Source_Field_Text = "Text";

    // Target
    private const string Target_ClassName = "MedioClinic.HomePage";
    private const string Target_TableName = "MedioClinic_HomePage";
    private const string Target_DisplayName = "Home page";
    private const string Target_Field_ID = "HomePageID";
    private const string Target_Field_Perex = "HomePagePerex";
    private const string Target_Field_Text = "HomePageText";

    // SEO Schema (shared across all webpage content types)
    public const string SEOSchemaName = "MedioClinic.SEOMetadataSchema";
    private const string SEOSchema_DisplayName = "SEO Metadata";
    private const string SEOSchema_Description = "Common SEO metadata fields for all webpage content types";
    private const string SEOSchema_Field_Title = "SEOMetaTitle";
    private const string SEOSchema_Field_Description = "SEOMetaDescription";

    // PATTERN: Define the reusable field schema with WithFactory for full control
    private static ReusableSchemaBuilder BuildSEOSchema()
    {
        var sb = new ReusableSchemaBuilder(SEOSchemaName, SEOSchema_DisplayName, SEOSchema_Description);

        sb.BuildField(SEOSchema_Field_Title)
            .WithFactory(() => new FormFieldInfo
            {
                Name = SEOSchema_Field_Title,
                Caption = "SEO Meta Title",
                Guid = new Guid("A1B2C3D4-E5F6-7890-ABCD-EF1234567890"),
                DataType = FieldDataType.Text,
                Size = 200,
                AllowEmpty = true,
                Settings =
                {
                    ["controlname"] = FormComponents.AdminTextInputComponent
                }
            });

        sb.BuildField(SEOSchema_Field_Description)
            .WithFactory(() => new FormFieldInfo
            {
                Name = SEOSchema_Field_Description,
                Caption = "SEO Meta Description",
                Guid = new Guid("B2C3D4E5-F6A7-8901-BCDE-F12345678901"),
                DataType = FieldDataType.Text,
                Size = 300,
                AllowEmpty = true,
                Settings =
                {
                    ["controlname"] = FormComponents.AdminTextAreaComponent
                }
            });

        return sb;
    }

    private static MultiClassMapping BuildMapping()
    {
        var m = new MultiClassMapping(Target_ClassName, target =>
        {
            target.ClassName = Target_ClassName;
            target.ClassTableName = Target_TableName;
            target.ClassDisplayName = Target_DisplayName;
            target.ClassType = ClassType.CONTENT_TYPE;
            target.ClassContentTypeType = ClassContentTypeType.WEBSITE;
        });

        m.BuildField(Target_Field_ID).AsPrimaryKey();

        // PATTERN: Assign reusable schema — note the typo in the API method name
        m.UseResusableSchema(SEOSchemaName);

        // Map content-type-specific fields with isTemplate: true
        m.BuildField(Target_Field_Perex)
            .SetFrom(Source_ClassName, Source_Field_Perex, true);

        m.BuildField(Target_Field_Text)
            .SetFrom(Source_ClassName, Source_Field_Text, true);

        // PATTERN: Map SEO schema fields from KX13 extended metadata
        // No isTemplate — the field definition comes from the schema builder
        // Requires appsettings.json: "IncludeExtendedMetadata": true
        m.BuildField(SEOSchema_Field_Title)
            .SetFrom(Source_ClassName, "DocumentPageTitle");

        m.BuildField(SEOSchema_Field_Description)
            .SetFrom(Source_ClassName, "DocumentPageDescription");

        return m;
    }

    public static IServiceCollection AddHomePageMapping(
        this IServiceCollection serviceCollection)
    {
        var schemaBuilder = BuildSEOSchema();
        var mapping = BuildMapping();

        serviceCollection.AddSingleton<IClassMapping>(mapping);
        // PATTERN: Register schema builder as singleton
        serviceCollection.AddSingleton<IReusableSchemaBuilder>(schemaBuilder);

        return serviceCollection;
    }
}

// =============================================================================
// PATTERN: docrelationships field → taxonomy tags via CMS_Relationship
// When to use: A KX13 docrelationships field stores related node references in
//              CMS_Relationship (not in the coupled data table). The target is a
//              taxonomy field in XbyK. Requires factory DI registration to inject
//              ModelFacade and CmsRelationshipService.
//
// KEY RULES:
// 1. Factory DI: Use serviceCollection.AddSingleton<IClassMapping>(sp => BuildMapping(...))
//    to inject ModelFacade and CmsRelationshipService.
// 2. WithoutSource + ConvertFrom + WithFieldPatch: Use WithoutSource("taxonomy")
//    to create the field, ConvertFrom for conversion, WithFieldPatch for form settings.
// 3. Non-relationship source field: When the target is NOT a page reference
//    (e.g., taxonomy, object code name), do NOT use the docrelationships field
//    name in ConvertFrom — the ConvertToPages pipeline will overwrite the output.
//    Use a non-relationship field (e.g., primary key) instead.
//    HOWEVER, when the target IS a contentitemreference (Pages) field linking to
//    migrated pages, keep the original docrelationships source field so that
//    ConvertToPages correctly resolves page references.
// 4. PascalCase Identifier: Use [{"Identifier":"guid"}] for taxonomy tag JSON.
// 5. Visible + Enabled: Set f.Visible = true and f.Enabled = true in WithFieldPatch.
// =============================================================================
public static class DoctorProfileClassMapping
{
    // Source
    private const string Source_ClassName = "MedioClinic.Doctor";
    private const string Source_Field_Degree = "Degree";
    private const string Source_Field_Biography = "Biography";
    private const string Source_Field_EmergencyShift = "EmergencyShift"; // docrelationships type

    // Target
    private const string Target_ClassName = "MedioClinic.DoctorProfile";
    private const string Target_TableName = "MedioClinic_DoctorProfile";
    private const string Target_DisplayName = "Doctor profile";
    private const string Target_Field_ID = "DoctorProfileID";
    private const string Target_Field_Degree = "DoctorProfileDegree";
    private const string Target_Field_Biography = "DoctorProfileBiography";
    private const string Target_Field_EmergencyShift = "DoctorProfileEmergencyShift";

    // KX13 NodeGUID → XbyK Tag GUID lookup
    // Keys: KX13 NodeGUIDs from the migration plan's Custom Value Transforms table
    // Values: Actual taxonomy tag GUIDs from XbyK after creating the DayOfWeek taxonomy.
    //   Query XbyK: SELECT TagName, CAST(TagGUID AS CHAR(36)) FROM CMS_Tag
    //     WHERE TagTaxonomyID = (SELECT TaxonomyID FROM CMS_Taxonomy WHERE TaxonomyName = 'DayOfWeek')
    private static readonly Dictionary<Guid, Guid> DayOfWeekNodeGuidToTagGuid = new()
    {
        [new Guid("KX13-MONDAY-NODE-GUID")] = new Guid("XBYK-MONDAY-TAG-GUID"),
        [new Guid("KX13-TUESDAY-NODE-GUID")] = new Guid("XBYK-TUESDAY-TAG-GUID"),
        // ... one entry per related node
    };

    // Taxonomy group GUID from XbyK
    //   Query: SELECT CAST(TaxonomyGUID AS CHAR(36)) FROM CMS_Taxonomy WHERE TaxonomyName = 'DayOfWeek'
    private const string DayOfWeekTaxonomyGroupGuid = "TODO-TAXONOMY-GROUP-GUID";

    // PATTERN: BuildMapping accepts injected services for CMS_Relationship queries
    private static MultiClassMapping BuildMapping(
        ModelFacade modelFacade,
        CmsRelationshipService relationshipService)
    {
        // Pre-load the source field GUID from the class form definition
        // (needed by CmsRelationshipService.GetNodeRelationships)
        var doctorClass = modelFacade.SelectWhere<ICmsClass>(
            "ClassName = @className",
            new SqlParameter("className", Source_ClassName)).FirstOrDefault();
        Guid emergencyShiftFieldGuid = Guid.Empty;
        if (doctorClass != null && !string.IsNullOrWhiteSpace(doctorClass.ClassFormDefinition))
        {
            var fi = new FormInfo(doctorClass.ClassFormDefinition);
            var field = fi.GetFormField(Source_Field_EmergencyShift);
            if (field != null)
                emergencyShiftFieldGuid = field.Guid;
        }

        var m = new MultiClassMapping(Target_ClassName, target =>
        {
            target.ClassName = Target_ClassName;
            target.ClassTableName = Target_TableName;
            target.ClassDisplayName = Target_DisplayName;
            target.ClassType = ClassType.CONTENT_TYPE;
            target.ClassContentTypeType = ClassContentTypeType.REUSABLE;
            target.ClassWebPageHasUrl = false;
        });

        m.BuildField(Target_Field_ID).AsPrimaryKey();

        m.BuildField(Target_Field_Degree)
            .SetFrom(Source_ClassName, Source_Field_Degree, true);

        m.BuildField(Target_Field_Biography)
            .SetFrom(Source_ClassName, Source_Field_Biography, true);

        // PATTERN: docrelationships → taxonomy via CMS_Relationship
        // Step 1: WithoutSource("taxonomy") — creates the taxonomy field definition
        // Step 2: ConvertFrom with NON-RELATIONSHIP source field — avoids ConvertToPages override
        //         We use "DoctorID" (primary key) instead of "EmergencyShift" (docrelationships)
        // Step 3: WithFieldPatch — sets taxonomy form control with Visible + Enabled
        var emergencyShiftField = m.BuildField(Target_Field_EmergencyShift);
        emergencyShiftField.WithoutSource("taxonomy");
        emergencyShiftField.ConvertFrom(Source_ClassName, "DoctorID", false, (value, context) =>
        {
            // The source value (DoctorID) is irrelevant — we query CMS_Relationship directly.
            if (context is not ConvertorTreeNodeContext treeNodeContext)
                return null;
            if (emergencyShiftFieldGuid == Guid.Empty)
                return null;

            // Look up the source node by GUID to get NodeID
            var doctorNode = modelFacade.SelectWhere<ICmsTree>(
                "NodeGUID = @nodeGuid",
                new SqlParameter("nodeGuid", treeNodeContext.NodeGuid)).FirstOrDefault();
            if (doctorNode == null)
                return null;

            // Query CMS_Relationship for this Doctor's EmergencyShift relationships
            var relations = relationshipService.GetNodeRelationships(
                doctorNode.NodeID, Source_ClassName, emergencyShiftFieldGuid);

            var tagReferences = new List<object>();
            foreach (var relation in relations)
            {
                if (relation.RightNode is not null &&
                    DayOfWeekNodeGuidToTagGuid.TryGetValue(relation.RightNode.NodeGUID, out var tagGuid))
                {
                    // PascalCase "Identifier" — matches XbyK TagReference property
                    tagReferences.Add(new { Identifier = tagGuid });
                }
            }

            return tagReferences.Count > 0
                ? System.Text.Json.JsonSerializer.Serialize(tagReferences)
                : null;
        });
        emergencyShiftField.WithFieldPatch(f =>
        {
            f.Caption = "Emergency shift";
            f.Visible = true;   // Required — FormDefinitionHelper resets visibility for "taxonomy" type
            f.Enabled = true;   // Required — ensures field is editable in XbyK admin form
            f.Settings["controlname"] = "Kentico.Administration.TagSelector";
            f.Settings["TaxonomyGroup"] = $"[\"{DayOfWeekTaxonomyGroupGuid}\"]";
        });

        return m;
    }

    // PATTERN: Factory DI registration — inject ModelFacade and CmsRelationshipService
    public static IServiceCollection AddDoctorProfileMapping(
        this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IClassMapping>(sp =>
            BuildMapping(
                sp.GetRequiredService<ModelFacade>(),
                sp.GetRequiredService<CmsRelationshipService>()));
        return serviceCollection;
    }
}

// =============================================================================
// PATTERN: ServiceCollectionExtensions — central registration for all mappings
// Every mapping file has its own extension method. This class calls them all.
// =============================================================================
public static class ServiceCollectionExtensions
{
    public static IServiceCollection UseCustomizations(this IServiceCollection services)
    {
        // IClassMapping registrations
        services.AddHomePageMapping();          // also registers SEOMetadataSchema
        services.AddDoctorMapping();
        services.AddContactPageMapping();
        services.AddOfficeLocationMapping();
        services.AddDoctorProfileMapping();     // factory DI — docrelationships pattern
        // ... add all other mappings here ...

        // Note: appsettings.json must also be configured with:
        // - ConvertClassesToContentHub (for reusable types)
        // - EntityConfigurations.ExcludeCodeNames (for excluded classes)
        // - IncludeExtendedMetadata: true (for SEO fields)

        return services;
    }
}
