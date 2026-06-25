// =============================================================================
// FIELD_MIGRATION_EXAMPLE.cs
//
// Complete annotated example showing all IFieldMigration patterns for a
// realistic migration scenario (MedioClinic KX13 → XbyK). Use this as a
// reference when generating field migration code.
//
// This file is NOT meant to be compiled directly — it demonstrates patterns.
// =============================================================================

using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using Migration.Tool.Common.Enumerations;
using Migration.Tool.KXP.Api.Services.CmsClass;

namespace Migration.Tool.Extensions.FieldMigrations;

// =============================================================================
// PATTERN: Simple form control replacement
// When to use: A custom KX13 form control has no XbyK equivalent and needs
//              replacement with a standard component. Also transforms the value
//              to match the new control's expected format.
// =============================================================================
public class CommunityTextEditorFieldMigration : IFieldMigration
{
    // Source form control name in KX13
    private const string Source_FormControl = "CommunityTextEditor";

    // Target XbyK form component
    private const string Target_FormComponent = "Kentico.Administration.RichTextEditor";

    // PATTERN: Rank < 100,000 for custom migrations (lower = higher priority)
    // Built-in defaults use 100,000+. Leave gaps for future insertions.
    public int Rank => 1000;

    // PATTERN: ShallMigrate matches on form control name — lightweight check
    public bool ShallMigrate(FieldMigrationContext context)
        => context.SourceFormControl.Equals(Source_FormControl, StringComparison.OrdinalIgnoreCase);

    // PATTERN: MigrateFieldDefinition uses XElement API to patch XML definition
    public void MigrateFieldDefinition(
        FormDefinitionPatcher formDefinitionPatcher,
        XElement field,
        XAttribute? columnTypeAttr,
        string fieldDescriptor)
    {
        // Find or create the settings element
        var settings = field.Element("settings") ?? new XElement("settings");
        if (settings.Parent == null) field.Add(settings);

        // Replace custom control with standard RichTextEditor
        var controlName = settings.Element("controlname");
        if (controlName != null)
            controlName.Value = Target_FormComponent;
        else
            settings.Add(new XElement("controlname", Target_FormComponent));

        // Update column type to longtext for rich text content
        if (columnTypeAttr != null)
            columnTypeAttr.Value = "longtext";
    }

    // PATTERN: MigrateValue with null handling and context check
    public void MigrateValue(
        object? sourceValue,
        FieldMigrationContext context,
        out object? targetValue)
    {
        if (sourceValue is null or DBNull)
        {
            targetValue = null;
            return;
        }

        if (sourceValue is string s && context.SourceObjectContext == SourceObjectContext.TreeNode)
        {
            // Wrap plain text in paragraph tags for rich text editor
            targetValue = s.StartsWith("<") ? s : $"<p>{s}</p>";
            return;
        }

        targetValue = sourceValue;
    }

    // PATTERN: Per-migration extension method for DI registration
    public static IServiceCollection AddCommunityTextEditorMigration(
        this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IFieldMigration>(new CommunityTextEditorFieldMigration());
        return serviceCollection;
    }
}

// =============================================================================
// PATTERN: Data type conversion (text → datetime)
// When to use: A field stores dates as text in KX13 and should become a proper
//              datetime in XbyK. Both the definition and value must change.
// =============================================================================
public class DateTextFieldMigration : IFieldMigration
{
    // Source: specific field + form control combination
    private const string Source_FieldName = "EventDateText";
    private const string Source_FormControl = "TextBoxControl";

    // Target XbyK form component
    private const string Target_FormComponent = "Kentico.Administration.DateTimeInput";
    private const string Target_DataType = "datetime";

    public int Rank => 2000;

    // PATTERN: ShallMigrate matches on field name + form control for precision
    public bool ShallMigrate(FieldMigrationContext context)
        => context.FieldName.Equals(Source_FieldName, StringComparison.OrdinalIgnoreCase)
        && context.SourceFormControl.Equals(Source_FormControl, StringComparison.OrdinalIgnoreCase);

    // PATTERN: Definition change — update data type and form control together
    public void MigrateFieldDefinition(
        FormDefinitionPatcher formDefinitionPatcher,
        XElement field,
        XAttribute? columnTypeAttr,
        string fieldDescriptor)
    {
        // Change column type to datetime
        if (columnTypeAttr != null)
            columnTypeAttr.Value = Target_DataType;

        // Change the data type attribute
        var dataType = field.Attribute("datatype");
        if (dataType != null)
            dataType.Value = Target_DataType;

        // Update form control to datetime picker
        var settings = field.Element("settings") ?? new XElement("settings");
        if (settings.Parent == null) field.Add(settings);

        var controlName = settings.Element("controlname");
        if (controlName != null)
            controlName.Value = Target_FormComponent;
        else
            settings.Add(new XElement("controlname", Target_FormComponent));
    }

    // PATTERN: Value conversion — parse text to DateTime defensively
    public void MigrateValue(
        object? sourceValue,
        FieldMigrationContext context,
        out object? targetValue)
    {
        if (sourceValue is null or DBNull)
        {
            targetValue = null;
            return;
        }

        if (sourceValue is string s && DateTime.TryParse(s, out var dt))
        {
            targetValue = dt;
            return;
        }

        // Already a DateTime or unparseable — pass through
        targetValue = sourceValue;
    }

    public static IServiceCollection AddDateTextMigration(
        this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IFieldMigration>(new DateTextFieldMigration());
        return serviceCollection;
    }
}

// =============================================================================
// PATTERN: HTML cleanup / sanitization
// When to use: Legacy HTML fields contain deprecated tags (<font>, <b>, <i>)
//              that need cleaning across all content types. Definition stays
//              the same — only value transformation.
// =============================================================================
public class HtmlCleanupFieldMigration : IFieldMigration
{
    // Source: match all HtmlArea longtext fields
    private const string Source_DataType = "longtext";
    private const string Source_FormControl = "HtmlAreaControl";

    public int Rank => 3000;

    // PATTERN: ShallMigrate on data type + form control — applies across all classes
    public bool ShallMigrate(FieldMigrationContext context)
        => context.SourceDataType.Equals(Source_DataType, StringComparison.OrdinalIgnoreCase)
        && context.SourceFormControl.Equals(Source_FormControl, StringComparison.OrdinalIgnoreCase);

    // PATTERN: Pass-through definition — no changes needed
    public void MigrateFieldDefinition(
        FormDefinitionPatcher formDefinitionPatcher,
        XElement field,
        XAttribute? columnTypeAttr,
        string fieldDescriptor)
    {
        // No definition changes — value-only migration
    }

    // PATTERN: Regex-based HTML cleanup
    public void MigrateValue(
        object? sourceValue,
        FieldMigrationContext context,
        out object? targetValue)
    {
        if (sourceValue is null or DBNull)
        {
            targetValue = null;
            return;
        }

        if (sourceValue is string html)
        {
            // Remove <font> tags but keep content
            html = System.Text.RegularExpressions.Regex.Replace(
                html, @"</?font[^>]*>", string.Empty,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Replace <b> with <strong>
            html = System.Text.RegularExpressions.Regex.Replace(
                html, @"<b(\s|>)", "<strong$1",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            html = System.Text.RegularExpressions.Regex.Replace(
                html, @"</b>", "</strong>",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Replace <i> with <em>
            html = System.Text.RegularExpressions.Regex.Replace(
                html, @"<i(\s|>)", "<em$1",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            html = System.Text.RegularExpressions.Regex.Replace(
                html, @"</i>", "</em>",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            targetValue = html;
            return;
        }

        targetValue = sourceValue;
    }

    public static IServiceCollection AddHtmlCleanupMigration(
        this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IFieldMigration>(new HtmlCleanupFieldMigration());
        return serviceCollection;
    }
}

// =============================================================================
// PATTERN: Path/URL transformation
// When to use: Fields contain KX13 media paths (e.g., /getmedia/) that need
//              rewriting for XbyK. Value-only migration — definition unchanged.
// =============================================================================
public class UrlPathFieldMigration : IFieldMigration
{
    // Source form control
    private const string Source_FormControl = "UrlSelector";

    public int Rank => 4000;

    public bool ShallMigrate(FieldMigrationContext context)
        => context.SourceFormControl.Equals(Source_FormControl, StringComparison.OrdinalIgnoreCase);

    // PATTERN: No definition change — URL fields keep the same structure
    public void MigrateFieldDefinition(
        FormDefinitionPatcher formDefinitionPatcher,
        XElement field,
        XAttribute? columnTypeAttr,
        string fieldDescriptor)
    {
        // No definition changes needed
    }

    // PATTERN: Value-only migration — rewrite KX13 media paths
    public void MigrateValue(
        object? sourceValue,
        FieldMigrationContext context,
        out object? targetValue)
    {
        if (sourceValue is null or DBNull)
        {
            targetValue = null;
            return;
        }

        if (sourceValue is string url)
        {
            // Convert KX13 /getmedia/{guid}/filename paths to XbyK format
            if (url.Contains("/getmedia/", StringComparison.OrdinalIgnoreCase))
            {
                var match = System.Text.RegularExpressions.Regex.Match(
                    url, @"/getmedia/([0-9a-fA-F-]+)/");
                if (match.Success)
                {
                    targetValue = $"/assets/{match.Groups[1].Value}";
                    return;
                }
            }

            targetValue = url;
            return;
        }

        targetValue = sourceValue;
    }

    public static IServiceCollection AddUrlPathMigration(
        this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IFieldMigration>(new UrlPathFieldMigration());
        return serviceCollection;
    }
}

// =============================================================================
// PATTERN: Context-specific migration
// When to use: The same form control needs different transformation depending
//              on whether it's on a page, custom table, or form. Uses
//              SourceObjectContext to branch behavior.
// =============================================================================
public class CustomDropdownFieldMigration : IFieldMigration
{
    // Source form control
    private const string Source_FormControl = "CustomDropDown";

    // Target XbyK form component
    private const string Target_FormComponent = "Kentico.Administration.DropDownSelector";

    public int Rank => 5000;

    public bool ShallMigrate(FieldMigrationContext context)
        => context.SourceFormControl.Equals(Source_FormControl, StringComparison.OrdinalIgnoreCase);

    // PATTERN: Definition change — replace form control
    public void MigrateFieldDefinition(
        FormDefinitionPatcher formDefinitionPatcher,
        XElement field,
        XAttribute? columnTypeAttr,
        string fieldDescriptor)
    {
        var settings = field.Element("settings") ?? new XElement("settings");
        if (settings.Parent == null) field.Add(settings);

        var controlName = settings.Element("controlname");
        if (controlName != null)
            controlName.Value = Target_FormComponent;
        else
            settings.Add(new XElement("controlname", Target_FormComponent));
    }

    // PATTERN: Different value behavior per SourceObjectContext
    public void MigrateValue(
        object? sourceValue,
        FieldMigrationContext context,
        out object? targetValue)
    {
        if (sourceValue is null or DBNull)
        {
            targetValue = null;
            return;
        }

        if (sourceValue is not string s)
        {
            targetValue = sourceValue;
            return;
        }

        targetValue = context.SourceObjectContext switch
        {
            // Pages: convert semicolon-separated values to JSON array
            SourceObjectContext.TreeNode => ConvertToJsonArray(s),

            // Custom tables: keep as-is (already in correct format)
            SourceObjectContext.CustomTable => s,

            // Forms: convert pipe-separated to semicolon-separated
            SourceObjectContext.Form => s.Replace("|", ";"),

            _ => s
        };
    }

    private static string ConvertToJsonArray(string semicolonSeparated)
    {
        var values = semicolonSeparated
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(v => $"\"{v.Trim()}\"");
        return $"[{string.Join(",", values)}]";
    }

    public static IServiceCollection AddCustomDropdownMigration(
        this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IFieldMigration>(new CustomDropdownFieldMigration());
        return serviceCollection;
    }
}

// =============================================================================
// PATTERN: ServiceCollectionExtensions — central registration for all field migrations
// Every migration class has its own extension method. This class calls them all.
// =============================================================================
public static class ServiceCollectionExtensions
{
    public static IServiceCollection UseCustomizations(this IServiceCollection services)
    {
        // IFieldMigration registrations
        services.AddCommunityTextEditorMigration();
        services.AddDateTextMigration();
        services.AddHtmlCleanupMigration();
        services.AddUrlPathMigration();
        services.AddCustomDropdownMigration();
        // ... add all other field migrations here ...

        // Note: fields handled by the above IFieldMigration code
        // do NOT need entries in appsettings.json FieldMigrations config.
        // Only add config entries for simple form control swaps
        // that don't require custom value transformation logic.

        return services;
    }
}
