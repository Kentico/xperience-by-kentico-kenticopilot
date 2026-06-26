// =============================================================================
// WIDGET_MIGRATION_EXAMPLE.cs
//
// Complete annotated example showing all IWidgetMigration and
// IWidgetPropertyMigration patterns for a realistic migration scenario
// (MedioClinic KX13 → XbyK). Use this as a reference when generating
// widget migration code.
//
// This file is NOT meant to be compiled directly — it demonstrates patterns.
//
// IMPORTANT BUILD NOTES:
// 1. Extension methods (Add{Name}Migration) MUST be in separate static classes,
//    NOT inside the migration class. Migration classes use primary constructors
//    for DI (ILogger, ModelFacade, etc.), making them non-static. C# requires
//    extension methods to be in non-generic static classes.
//    Pattern: class FooMigration : IWidgetMigration { ... }
//             static class FooMigrationExtensions { public static IServiceCollection AddFooMigration(...) }
//
// 2. WidgetPropertyMigrationContext only has SiteId and EditingFormControlModel.
//    There is NO ComponentIdentifier property. Do not reference it.
//
// 3. Standalone IWidgetPropertyMigration classes are ONLY invoked when API
//    Discovery provides EditingFormControlModel for the property. If a KX13
//    widget property lacks [EditingComponent(...)] attribute, the property
//    migration is silently skipped. Always pair with an IWidgetMigration that
//    delegates via propertyMigrations dictionary to guarantee execution.
// =============================================================================

using CMS.ContentEngine;
using CMS.Core;
using CMS.DataEngine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Migration.Tool.Common.Enumerations;
using Migration.Tool.Common.Services;
using Migration.Tool.Extensions.DefaultMigrations;
using Migration.Tool.KXP.Api.Services.CmsClass;
using Migration.Tool.Source.Services.Model;
using Newtonsoft.Json.Linq;

namespace Migration.Tool.Extensions.WidgetMigrations;

// =============================================================================
// PATTERN 1: Section type rename (no property changes)
// When to use: A KX13 section type identifier changes in XbyK but the section
//              has no properties or uses target defaults. Just rename the type.
// MedioClinic scenario:
//   MedioClinic.Section.SingleColumn → SingleColumnSection
//   Target has BackgroundStyle, Padding, etc. — will use XbyK defaults.
// =============================================================================
public class SingleColumnSectionMigration : IWidgetMigration
{
    // Source section type identifier in KX13
    private const string Source_TypeIdentifier = "MedioClinic.Section.SingleColumn";

    // Target section type identifier in XbyK
    private const string Target_TypeIdentifier = "SingleColumnSection";

    // PATTERN: Rank < 100,000 for custom migrations (lower = higher priority)
    // Built-in defaults use 100,000+. Leave gaps for future insertions.
    public int Rank => 1;

    // PATTERN: ShallMigrate matches on widget/section type identifier — case-insensitive
    public bool ShallMigrate(WidgetMigrationContext context, WidgetIdentifier identifier)
        => string.Equals(Source_TypeIdentifier, identifier.TypeIdentifier,
            StringComparison.InvariantCultureIgnoreCase);

    // PATTERN: Simple rename — change type, no property changes
    public Task<WidgetMigrationResult> MigrateWidget(
        WidgetIdentifier identifier, JToken? value, WidgetMigrationContext context)
    {
        // Rename the section type identifier
        value!["type"] = Target_TypeIdentifier;

        // No property changes needed — return null for propertyMigrations
        return Task.FromResult(new WidgetMigrationResult(value, null));
    }
}

// PATTERN: Per-migration extension method for DI registration (AddTransient, NOT AddSingleton)
// Extension method MUST be in a separate static class — migration classes use primary
// constructors for DI, making them non-static. C# requires extension methods in static classes.
public static class SingleColumnSectionMigrationExtensions
{
    public static IServiceCollection AddSingleColumnSectionMigration(
        this IServiceCollection services)
    {
        services.AddTransient<IWidgetMigration, SingleColumnSectionMigration>();
        return services;
    }
}

// =============================================================================
// PATTERN 2: Section type rename with inline property conversion
// When to use: Section type changes AND properties need value transformation.
//              When the conversion is simple, do it inline in MigrateWidget
//              rather than creating a separate IWidgetPropertyMigration.
// MedioClinic scenario:
//   MedioClinic.Section.TwoColumn → TwoColumnSection
//   leftColumnWidth (int, e.g. 60) → ColumnRatio (string, e.g. "60/40")
// =============================================================================
public class TwoColumnSectionMigration : IWidgetMigration
{
    private const string Source_TypeIdentifier = "MedioClinic.Section.TwoColumn";
    private const string Target_TypeIdentifier = "TwoColumnSection";

    public int Rank => 2;

    public bool ShallMigrate(WidgetMigrationContext context, WidgetIdentifier identifier)
        => string.Equals(Source_TypeIdentifier, identifier.TypeIdentifier,
            StringComparison.InvariantCultureIgnoreCase);

    public Task<WidgetMigrationResult> MigrateWidget(
        WidgetIdentifier identifier, JToken? value, WidgetMigrationContext context)
    {
        value!["type"] = Target_TypeIdentifier;

        // Access the first variant's properties
        var variants = (JArray)value!["variants"]!;
        var singleVariant = variants[0];

        // PATTERN: Inline value conversion — int width → string ratio
        var leftWidth = singleVariant["properties"]!["leftColumnWidth"]?.Value<int>() ?? 50;
        var ratio = $"{leftWidth}/{100 - leftWidth}";

        // Rebuild properties with the converted value
        singleVariant["properties"] = new JObject
        {
            ["ColumnRatio"] = ratio,
        };

        return Task.FromResult(new WidgetMigrationResult(value, null));
    }
}

public static class TwoColumnSectionMigrationExtensions
{
    public static IServiceCollection AddTwoColumnSectionMigration(
        this IServiceCollection services)
    {
        services.AddTransient<IWidgetMigration, TwoColumnSectionMigration>();
        return services;
    }
}

// =============================================================================
// PATTERN 3: Simple widget rename with property rename
// When to use: Widget type changes AND properties are renamed, but values stay
//              in the same format (no conversion needed). Rebuild the JObject
//              with new property names mapping to source values.
// MedioClinic scenario:
//   MedioClinic.Widget.Text → RichTextWidget
//   text → RichTextWidgetContent
//   NOTE: Do NOT consolidate with Kentico.Widget.RichText — that built-in
//         widget migrates automatically to the XbyK Rich text widget.
// =============================================================================
public class TextWidgetMigration : IWidgetMigration
{
    private const string Source_TypeIdentifier = "MedioClinic.Widget.Text";
    private const string Target_TypeIdentifier = "RichTextWidget";

    public int Rank => 3;

    public bool ShallMigrate(WidgetMigrationContext context, WidgetIdentifier identifier)
        => string.Equals(Source_TypeIdentifier, identifier.TypeIdentifier,
            StringComparison.InvariantCultureIgnoreCase);

    public Task<WidgetMigrationResult> MigrateWidget(
        WidgetIdentifier identifier, JToken? value, WidgetMigrationContext context)
    {
        value!["type"] = Target_TypeIdentifier;

        var variants = (JArray)value!["variants"]!;
        var singleVariant = variants[0];

        // PATTERN: Property rename — map source property to target property name
        singleVariant["properties"] = new JObject
        {
            ["RichTextWidgetContent"] = singleVariant["properties"]!["text"],
        };

        return Task.FromResult(new WidgetMigrationResult(value, null));
    }
}

public static class TextWidgetMigrationExtensions
{
    public static IServiceCollection AddTextWidgetMigration(
        this IServiceCollection services)
    {
        services.AddTransient<IWidgetMigration, TextWidgetMigration>();
        return services;
    }
}

// =============================================================================
// PATTERN 4: Widget with media GUID → content item reference (WidgetFileMigration)
// When to use: Widget has a media file property (GUID referencing a media
//              library file) that needs conversion to a content item asset
//              reference. Use the built-in WidgetFileMigration by referencing
//              it in the propertyMigrations dictionary.
// MedioClinic scenario:
//   MedioClinic.Widget.Image → ImageDisplayWidget
//   imageGuid (media GUID) → ImageDisplayWidgetImage (content item ref)
//   Uses WidgetFileMigration for the actual GUID conversion.
// =============================================================================
public class ImageWidgetMigration : IWidgetMigration
{
    private const string Source_TypeIdentifier = "MedioClinic.Widget.Image";
    private const string Target_TypeIdentifier = "ImageDisplayWidget";

    public int Rank => 4;

    public bool ShallMigrate(WidgetMigrationContext context, WidgetIdentifier identifier)
        => string.Equals(Source_TypeIdentifier, identifier.TypeIdentifier,
            StringComparison.InvariantCultureIgnoreCase);

    public Task<WidgetMigrationResult> MigrateWidget(
        WidgetIdentifier identifier, JToken? value, WidgetMigrationContext context)
    {
        value!["type"] = Target_TypeIdentifier;

        var variants = (JArray)value!["variants"]!;
        var singleVariant = variants[0];

        singleVariant["properties"] = new JObject
        {
            // Map media GUID property — WidgetFileMigration will convert the value
            ["ImageDisplayWidgetImage"] = singleVariant["properties"]!["imageGuid"],
            ["ImageDisplayWidgetAltText"] = singleVariant["properties"]!["altText"],
        };

        // PATTERN: propertyMigrations dictionary — delegate media file conversion
        // to the built-in WidgetFileMigration rather than implementing manually
        var propertyMigrations = new Dictionary<string, Type>
        {
            ["ImageDisplayWidgetImage"] = typeof(WidgetFileMigration)
        };

        return Task.FromResult(new WidgetMigrationResult(value, propertyMigrations));
    }
}

public static class ImageWidgetMigrationExtensions
{
    public static IServiceCollection AddImageWidgetMigration(
        this IServiceCollection services)
    {
        services.AddTransient<IWidgetMigration, ImageWidgetMigration>();
        return services;
    }
}

// =============================================================================
// PATTERN 5: Widget with array property + multiple direct renames
// When to use: Widget has an array property (e.g., multiple media GUIDs) plus
//              several display properties that rename directly. The array
//              property needs a custom IWidgetPropertyMigration for batch
//              conversion.
// MedioClinic scenario:
//   MedioClinic.Widget.Slideshow → ImageCarouselWidget
//   imageGuids (array of media GUIDs) → ImageCarouselWidgetImages (content refs)
//   transitionDelay → ImageCarouselWidgetTransitionDelay (direct rename)
//   transitionSpeed → ImageCarouselWidgetTransitionSpeed (direct rename)
//   displayArrowSigns → ImageCarouselWidgetShowArrows (direct rename)
//   enforceDimensions → ImageCarouselWidgetEnforceDimensions (direct rename)
//   width → ImageCarouselWidgetWidth (direct rename)
//   height → ImageCarouselWidgetHeight (direct rename)
// =============================================================================
public class SlideshowWidgetMigration : IWidgetMigration
{
    private const string Source_TypeIdentifier = "MedioClinic.Widget.Slideshow";
    private const string Target_TypeIdentifier = "ImageCarouselWidget";

    public int Rank => 5;

    public bool ShallMigrate(WidgetMigrationContext context, WidgetIdentifier identifier)
        => string.Equals(Source_TypeIdentifier, identifier.TypeIdentifier,
            StringComparison.InvariantCultureIgnoreCase);

    public Task<WidgetMigrationResult> MigrateWidget(
        WidgetIdentifier identifier, JToken? value, WidgetMigrationContext context)
    {
        value!["type"] = Target_TypeIdentifier;

        var variants = (JArray)value!["variants"]!;
        var singleVariant = variants[0];
        var sourceProps = singleVariant["properties"]!;

        // PATTERN: Rebuild properties — array property + multiple direct renames
        singleVariant["properties"] = new JObject
        {
            // Array property — will be handled by a custom IWidgetPropertyMigration
            // for batch media GUID → content item ref conversion
            ["ImageCarouselWidgetImages"] = sourceProps["imageGuids"],

            // Direct property renames — values stay in the same format
            ["ImageCarouselWidgetTransitionDelay"] = sourceProps["transitionDelay"],
            ["ImageCarouselWidgetTransitionSpeed"] = sourceProps["transitionSpeed"],
            ["ImageCarouselWidgetShowArrows"] = sourceProps["displayArrowSigns"],
            ["ImageCarouselWidgetEnforceDimensions"] = sourceProps["enforceDimensions"],
            ["ImageCarouselWidgetWidth"] = sourceProps["width"],
            ["ImageCarouselWidgetHeight"] = sourceProps["height"],
        };

        // TODO: Register a custom IWidgetPropertyMigration for batch media GUID
        // array conversion, or use WidgetFileMigration if it handles arrays.
        var propertyMigrations = new Dictionary<string, Type>
        {
            ["ImageCarouselWidgetImages"] = typeof(WidgetFileMigration)
        };

        return Task.FromResult(new WidgetMigrationResult(value, propertyMigrations));
    }
}

public static class SlideshowWidgetMigrationExtensions
{
    public static IServiceCollection AddSlideshowWidgetMigration(
        this IServiceCollection services)
    {
        services.AddTransient<IWidgetMigration, SlideshowWidgetMigration>();
        return services;
    }
}

// =============================================================================
// PATTERN 6: Complex widget — color mapping, resource key, URL mapping
// When to use: Widget properties need inline value transformations that go
//              beyond simple renames: dictionary-based value mapping, resource
//              key resolution, URL restructuring.
// MedioClinic scenario:
//   MedioClinic.Widget.Button → CTAButtonWidget
//   buttonColor (string) → CTAButtonWidgetStyleOverride (mapped via dictionary)
//   linkTextResourceKey → CTAButtonWidgetLabelOverride (resolve resource key)
//   url → CTAButtonWidgetUrl (direct)
// =============================================================================
public class ButtonWidgetMigration : IWidgetMigration
{
    private const string Source_TypeIdentifier = "MedioClinic.Widget.Button";
    private const string Target_TypeIdentifier = "CTAButtonWidget";

    // PATTERN: Color-to-style mapping dictionary
    private static readonly Dictionary<string, string> ColorToStyleMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["red"] = "Primary",
            ["blue"] = "Secondary",
            ["green"] = "Accent",
            ["gray"] = "Muted",
        };

    public int Rank => 6;

    public bool ShallMigrate(WidgetMigrationContext context, WidgetIdentifier identifier)
        => string.Equals(Source_TypeIdentifier, identifier.TypeIdentifier,
            StringComparison.InvariantCultureIgnoreCase);

    public Task<WidgetMigrationResult> MigrateWidget(
        WidgetIdentifier identifier, JToken? value, WidgetMigrationContext context)
    {
        value!["type"] = Target_TypeIdentifier;

        var variants = (JArray)value!["variants"]!;
        var singleVariant = variants[0];
        var sourceProps = singleVariant["properties"]!;

        // Map buttonColor string → style override using dictionary
        var colorValue = sourceProps["buttonColor"]?.Value<string>() ?? "";
        var style = ColorToStyleMap.GetValueOrDefault(colorValue, "Primary");

        // TODO: Resolve linkTextResourceKey → localized text.
        // Look up the value from KX13's CMS_ResourceString / CMS_ResourceTranslation
        // tables and insert the resolved text. XbyK does not use resource keys.
        var resourceKey = sourceProps["linkTextResourceKey"]?.Value<string>();

        singleVariant["properties"] = new JObject
        {
            ["CTAButtonWidgetUrl"] = sourceProps["url"],
            ["CTAButtonWidgetStyleOverride"] = style,
            ["CTAButtonWidgetLabelOverride"] = resourceKey ?? "",
        };

        return Task.FromResult(new WidgetMigrationResult(value, null));
    }
}

public static class ButtonWidgetMigrationExtensions
{
    public static IServiceCollection AddButtonWidgetMigration(
        this IServiceCollection services)
    {
        services.AddTransient<IWidgetMigration, ButtonWidgetMigration>();
        return services;
    }
}

// =============================================================================
// PATTERN 7: Advanced — Content item creation during migration
// When to use: Widget text/data should be extracted into a reusable content
//              item during migration, with the widget property updated to
//              reference the new item. Uses IContentItemManagerFactory to create
//              content items inline.
// Based on: HeroImageWidgetMigration from Kentico Docs
//   (https://docs.kentico.com/guides/upgrade-to-xbyk/upgrade-deep-dives/
//    migrate-widget-data-to-content-hub)
// DancingGoat scenario:
//   DancingGoat.LandingPage.HeroImage → same widget type
//   Extracts text, buttonTarget, buttonText → DancingGoatCore.Hero content item
//   Widget property "hero" references the new content item
// =============================================================================
public class HeroContentWidgetMigration(
    ILogger<HeroContentWidgetMigration> logger) : IWidgetMigration
{
    // Source widget
    private const string Source_TypeIdentifier = "DancingGoat.LandingPage.HeroImage";
    private const int Source_SiteId = 1;

    // Target content type for extracted data
    private const string Target_ContentTypeName = "DancingGoatCore.Hero";

    public int Rank => 100;

    public bool ShallMigrate(WidgetMigrationContext context, WidgetIdentifier identifier)
        => string.Equals(Source_TypeIdentifier, identifier.TypeIdentifier,
            StringComparison.InvariantCultureIgnoreCase)
        && Source_SiteId == context.SiteId;

    public async Task<WidgetMigrationResult> MigrateWidget(
        WidgetIdentifier identifier, JToken? value, WidgetMigrationContext context)
    {
        var variants = (JArray)value!["variants"]!;
        var singleVariant = variants[0];

        // PATTERN: Extract properties → create reusable content item → reference it
        var heroItemReference = await CreateHeroContentItem(singleVariant["properties"]!);

        // Rebuild widget properties with content item reference
        singleVariant["properties"] = new JObject
        {
            ["hero"] = heroItemReference,
            ["image"] = singleVariant["properties"]!["image"],
            ["theme"] = singleVariant["properties"]!["theme"],
            ["openInNewTab"] = JToken.FromObject(false),
        };

        // No additional property migrations needed
        return new WidgetMigrationResult(value, new Dictionary<string, Type>());
    }

    private async Task<JToken?> CreateHeroContentItem(JToken properties)
    {
        // IMPORTANT: Verify these values against your target XbyK database
        const string workspaceName = "KenticoDefault";
        const int adminUserId = 53; // Global Administrator user ID
        const string language = "en-US";

        var heading = properties["text"]?.ToString() ?? "";
        var target = properties["buttonTarget"]?.ToString() ?? "";
        var callToAction = properties["buttonText"]?.ToString() ?? "";

        // PATTERN: Use IContentItemManagerFactory to create content items
        var ciManager = Service.Resolve<IContentItemManagerFactory>().Create(adminUserId);

        var createParams = new CreateContentItemParameters(
            contentTypeName: Target_ContentTypeName,
            name: $"MigratedHeroItem{Guid.NewGuid():N}",
            displayName: $"Hero item - {heading}",
            languageName: language,
            workspaceName: workspaceName);

        var contentItemData = new ContentItemData();
        contentItemData.SetValue("HeroHeading", heading);
        contentItemData.SetValue("HeroTarget", target);
        contentItemData.SetValue("HeroCallToAction", callToAction);

        int itemId = await ciManager.Create(createParams, contentItemData);
        if (itemId <= 0)
        {
            logger.LogError("Failed to create Hero content item for heading: {Heading}", heading);
            throw new Exception("Unable to create Hero content item");
        }

        if (!await ciManager.TryPublish(itemId, language))
        {
            logger.LogWarning("Created Hero item {ItemId} but failed to publish", itemId);
        }

        var itemGuid = CMS.ContentEngine.Internal.ContentItemInfo.Provider.Get(itemId).ContentItemGUID;
        return JToken.FromObject(new[] { new ContentItemReference { Identifier = itemGuid } });
    }
}

public static class HeroContentWidgetMigrationExtensions
{
    public static IServiceCollection AddHeroContentWidgetMigration(
        this IServiceCollection services)
    {
        services.AddTransient<IWidgetMigration, HeroContentWidgetMigration>();
        return services;
    }
}

// =============================================================================
// PATTERN 8: IWidgetPropertyMigration — property value conversion
// When to use: A property value transformation is reusable across widgets or
//              needs to be applied independently from the widget migration.
//              Register via the propertyMigrations dictionary or as a standalone
//              migration matching by property name or form component.
// MedioClinic scenario:
//   leftColumnWidth (int, e.g. 60) → ColumnRatio (string, e.g. "60/40")
//   Used by TwoColumnSection but could apply to any section with column ratios.
// =============================================================================
public class ColumnRatioPropertyMigration : IWidgetPropertyMigration
{
    public int Rank => 1;

    // PATTERN: ShallMigrate matches by property name
    public bool ShallMigrate(WidgetPropertyMigrationContext context, string propertyName)
        => propertyName.Equals("leftColumnWidth", StringComparison.InvariantCultureIgnoreCase);

    // PATTERN: MigrateWidgetProperty converts the value and returns a result
    public Task<WidgetPropertyMigrationResult> MigrateWidgetProperty(
        string key, JToken? value, WidgetPropertyMigrationContext context)
    {
        // Convert integer width to string ratio: 60 → "60/40"
        var leftWidth = value?.Value<int>() ?? 50;
        var ratio = $"{leftWidth}/{100 - leftWidth}";

        return Task.FromResult(
            new WidgetPropertyMigrationResult(JToken.FromObject(ratio)));
    }
}

public static class ColumnRatioPropertyMigrationExtensions
{
    public static IServiceCollection AddColumnRatioPropertyMigration(
        this IServiceCollection services)
    {
        services.AddTransient<IWidgetPropertyMigration, ColumnRatioPropertyMigration>();
        return services;
    }
}

// =============================================================================
// PATTERN 9: Override built-in property migration (PageSelector → ContentItemReference)
// When to use: The built-in WidgetPageSelectorMigration (Rank 100,002) converts
//              page selectors to WebPageRelatedItem. Override it when you need
//              ContentItemReference instead (e.g., for combined content selectors).
//              Use a lower Rank to take priority over the built-in.
// Key concepts:
//   - ShallMigrate matches by FormComponentIdentifier (same as the built-in)
//   - Lower Rank wins — custom Rank 100 beats built-in Rank 100,002
//   - ISpoiledGuidContext resolves KX13 NodeGuid → migrated XbyK content item GUID
//   - Requires API Discovery (QuerySourceInstanceApi) for FormComponentIdentifier
// Based on: https://docs.kentico.com/guides/upgrade-to-xbyk/upgrade-deep-dives/transform-widget-properties
// =============================================================================
public class PageSelectorToCombinedSelectorPropertyMigration(
    ISpoiledGuidContext spoiledGuidContext,
    ILogger<PageSelectorToCombinedSelectorPropertyMigration> logger) : IWidgetPropertyMigration
{
    // Use the same Kx13FormComponents constant as the built-in
    private const string MigratedComponent = Kx13FormComponents.Kentico_PageSelector;

    // PATTERN: Rank < 100,000 overrides built-in WidgetPageSelectorMigration (Rank 100,002)
    public int Rank => 100;

    // PATTERN: Match by form component identifier — targets ALL page selector properties
    // across ALL widgets, regardless of property name or widget type.
    // Requires QuerySourceInstanceApi enabled in appsettings.json.
    public bool ShallMigrate(WidgetPropertyMigrationContext context, string propertyName)
        => MigratedComponent.Equals(
            context.EditingFormControlModel?.FormComponentIdentifier,
            StringComparison.InvariantCultureIgnoreCase);

    // PATTERN: Convert PageSelectorItem[] → ContentItemReference[] (instead of WebPageRelatedItem[])
    public Task<WidgetPropertyMigrationResult> MigrateWidgetProperty(
        string key, JToken? value, WidgetPropertyMigrationContext context)
    {
        (int siteId, _) = context;

        if (value?.ToObject<List<PageSelectorItem>>() is { Count: > 0 } items)
        {
            // Convert each page selector item to a ContentItemReference
            // ISpoiledGuidContext.EnsureNodeGuid resolves KX13 NodeGuid → XbyK content item GUID
            var result = items.Select(x => new ContentItemReference
            {
                Identifier = spoiledGuidContext.EnsureNodeGuid(x.NodeGuid, siteId)
            }).ToList();

            return Task.FromResult(
                new WidgetPropertyMigrationResult(JToken.FromObject(result)));
        }

        logger.LogError(
            "Failed to parse '{ComponentName}' json {Json}",
            MigratedComponent,
            value?.ToString() ?? "<null>");

        // Leave value as-is when parsing fails
        return Task.FromResult(new WidgetPropertyMigrationResult(value));
    }
}

public static class PageSelectorToCombinedSelectorPropertyMigrationExtensions
{
    public static IServiceCollection AddPageSelectorToCombinedSelectorPropertyMigration(
        this IServiceCollection services)
    {
        services.AddTransient<IWidgetPropertyMigration, PageSelectorToCombinedSelectorPropertyMigration>();
        return services;
    }
}

// =============================================================================
// SERVICE REGISTRATION
// Central registration class calling all widget migration extension methods.
// Register as transient (NOT singleton). Widget migrations run during the
// --pages CLI phase.
//
// Prerequisites:
//   - appsettings.json: OptInFeatures.QuerySourceInstanceApi must be enabled
//     for automatic built-in widget property discovery.
//   - Deploy ToolApiController.cs to KX13 instance before running --pages.
//
// Built-in widgets that migrate automatically (no custom code needed):
//   - Kentico.Widget.RichText → XbyK Rich text widget
//   - Kentico.FormWidget → XbyK Form widget
// =============================================================================
public static class WidgetMigrationServiceExtensions
{
    public static IServiceCollection AddAllWidgetMigrations(
        this IServiceCollection services)
    {
        // IWidgetMigration registrations — section types
        services.AddSingleColumnSectionMigration();
        services.AddTwoColumnSectionMigration();

        // IWidgetMigration registrations — custom widgets
        services.AddTextWidgetMigration();
        services.AddImageWidgetMigration();
        services.AddSlideshowWidgetMigration();
        services.AddButtonWidgetMigration();

        // IWidgetMigration registrations — advanced (content item creation)
        // Uncomment if using the DancingGoat hero pattern:
        // services.AddHeroContentWidgetMigration();

        // IWidgetPropertyMigration registrations
        services.AddColumnRatioPropertyMigration();

        // IWidgetPropertyMigration registrations — built-in overrides
        // Uncomment to override built-in PageSelector → WebPageRelatedItem
        // with custom PageSelector → ContentItemReference conversion:
        // services.AddPageSelectorToCombinedSelectorPropertyMigration();

        return services;
    }
}
