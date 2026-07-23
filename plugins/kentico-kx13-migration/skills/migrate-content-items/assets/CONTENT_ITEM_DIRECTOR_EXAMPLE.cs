// =============================================================================
// CONTENT_ITEM_DIRECTOR_EXAMPLE.cs
//
// Complete annotated example showing all ContentItemDirectorBase patterns for a
// realistic migration scenario (MedioClinic KX13 → XbyK). Use this as a
// reference when generating content item director code.
//
// This file is NOT meant to be compiled directly — it demonstrates patterns.
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using Migration.Tool.Source.Mappers.ContentItemMapperDirectives;
using Newtonsoft.Json.Linq;

namespace Migration.Tool.Extensions.ContentItemDirectors;

// =============================================================================
// PATTERN 1: Linked page handling (DirectLinkedNode)
// When to use: KX13 site has linked pages (linked documents) that need custom
//              handling — materialize as independent copies, drop, or store as
//              content item references on an ancestor page.
// MedioClinic scenario:
//   - CompanyService linked nodes under /Home → Materialize as independent
//     MedicalService Content Hub items
//   - Company child under /Contact-us → StoreReferenceInAncestor on ContactPage
//   - MapLocation children under /Contact-us/Office-locations → StoreReference
//     on ContactPage (level -2 because parent is CMS.Folder, not ContactPage)
// =============================================================================
public class LinkedPageDirector : ContentItemDirectorBase
{
    // Source class IDs (KX13) — for ICmsTree.NodeClassID matching
    private const int ClassID_CompanyService = 1001;
    private const int ClassID_Company = 1002;
    private const int ClassID_MapLocation = 1003;

    // Target field names (XbyK)
    private const string Target_Field_OfficeLocations = "ContactPageOfficeLocations";

    public override void DirectLinkedNode(LinkedPageSource source, ILinkedPageActionProvider options)
    {
        // ICmsTree does not have SourceClassName — use NodeClassID for routing
        switch (source.LinkedNode.NodeClassID)
        {
            case ClassID_CompanyService:
                // Linked CompanyService nodes → create independent Content Hub copies.
                // With ConvertClassesToContentHub, each yields a separate MedicalService item.
                // Editors can clean up duplicates post-migration.
                options.Materialize();
                break;

            case ClassID_Company:
                // Company child under /Contact-us/Medio-Clinic → store reference on parent.
                // Parent (level -1) is the ContactPage.
                options.StoreReferenceInAncestor(-1, Target_Field_OfficeLocations);
                break;

            case ClassID_MapLocation:
                // MapLocation children under /Contact-us/Office-locations/*.
                // Level -1 is CMS.Folder (not migrated); level -2 is the ContactPage.
                options.StoreReferenceInAncestor(-2, Target_Field_OfficeLocations);
                break;

            default:
                // DirectLinkedNode() is virtual — base call is OK
                base.DirectLinkedNode(source, options);
                break;
        }
    }
}

// =============================================================================
// PATTERN 2: Child page linking (LinkChildren)
// When to use: Parent pages should hold content item references to their child
//              pages. The migration tool auto-creates the reference field on
//              the target content type.
// MedioClinic scenario:
//   - SiteSection at /Doctors → link Doctor children as "FeaturedDoctors"
// =============================================================================
public class ChildPageLinkingDirector : ContentItemDirectorBase
{
    // Source class names (KX13) — for ContentItemSource.SourceClassName matching
    private const string Source_SiteSection = "MedioClinic.SiteSection";
    private const string Source_Doctor = "MedioClinic.Doctor";

    // Source class IDs (KX13) — for ICmsTree.NodeClassID matching (child/linked node filtering)
    private const int ClassID_Doctor = 2001;

    // Target field names
    private const string Target_Field_FeaturedDoctors = "FeaturedDoctors";

    public override void Direct(ContentItemSource source, IContentItemActionProvider options)
    {
        if (source.SourceClassName == Source_SiteSection
            && source.SourceNode!.NodeAliasPath == "/Doctors")
        {
            // Link only Doctor children into the FeaturedDoctors reference field.
            // Always apply a Where filter to select the appropriate child types.
            // Note: ICmsTree does not have SourceClassName — use NodeClassID for filtering.
            options.LinkChildren(
                Target_Field_FeaturedDoctors,
                source.ChildNodes!.Where(c => c.NodeClassID == ClassID_Doctor));
        }
        else
        {
            // Direct() is abstract — do NOT call base.Direct(). No action = default behavior.
        }
    }
}

// =============================================================================
// PATTERN 3: Page-to-widget conversion (AsWidget)
// When to use: KX13 child pages should become Page Builder widget instances on
//              an ancestor page in XbyK. The source page is also converted to a
//              Content Hub reusable item (via ConvertClassesToContentHub) and
//              linked from the widget.
// Hypothetical scenario (DancingGoat-style):
//   - AboutUsSection pages under /About-Us → widgets on the /About-Us page
//   - /About-Us page gets a template override for Page Builder
// =============================================================================
public class PageToWidgetDirector : ContentItemDirectorBase
{
    // Source
    private const string Source_AboutUsSection = "DancingGoatCore.AboutUsSection";

    // Target
    private const string Target_Template = "DancingGoat.LandingPageSingleColumn";
    private const string Target_WidgetIdentifier = "DancingGoat.Widgets.AboutUsSection";
    private const string Target_EditableArea = "top";
    private const string Target_SectionIdentifier = "DancingGoat.SingleColumnSection";

    public override void Direct(ContentItemSource source, IContentItemActionProvider options)
    {
        // Scope: only process pages under /About-Us
        if (source.SourceNode!.NodeAliasPath.StartsWith("/About-Us"))
        {
            if (source.SourceNode.NodeAliasPath == "/About-Us")
            {
                // Host page: override template to enable Page Builder
                options.OverridePageTemplate(Target_Template);
            }
            else if (source.SourceClassName == Source_AboutUsSection)
            {
                // Child pages: convert to widgets on the parent page
                options.AsWidget(Target_WidgetIdentifier, null, null, opts =>
                {
                    // Location: full chain is required
                    opts.Location
                        .OnAncestorPage(-1)                    // -1 = parent (/About-Us)
                        .InEditableArea(Target_EditableArea)    // editable area in the template view
                        .InSection(Target_SectionIdentifier)    // section type from XbyK project
                        .InFirstZone();                         // first zone of the section

                    // Properties: fill widget properties from the converted reusable item
                    opts.Properties.Fill(true, (itemProps, reusableItemGuid, childGuids) =>
                    {
                        var widgetProps = new JObject();

                        // Link to the converted Content Hub item (single reference)
                        widgetProps["aboutUsSectionItem"] = LinkedItemPropertyValue(reusableItemGuid!.Value);

                        // Static property example
                        widgetProps["alignment"] = "ImageLeft";

                        return widgetProps;
                    });
                });
            }
            else
            {
                // Drop all other child pages under /About-Us
                options.Drop();
            }
        }
        // else: Direct() is abstract — do NOT call base.Direct(). No action = default behavior.
    }
}

// =============================================================================
// PATTERN 4: Combined director (Direct + DirectLinkedNode)
// When to use: A single director handling multiple concerns via conditional
//              logic. This is the standard real-world pattern — one director per
//              logical concern, using switch/if on SourceClassName (on ContentItemSource)
//              or NodeClassID / NodeAliasPath (on ICmsTree child/linked nodes)
//              to handle different page types.
// MedioClinic scenario:
//   - Direct(): template override, child linking, drop unwanted pages
//   - DirectLinkedNode(): materialize services, store ancestor references
// =============================================================================
public class MedioClinicContentItemDirector : ContentItemDirectorBase
{
    // Source class names (KX13) — for ContentItemSource.SourceClassName matching
    private const string Source_CompanyService = "MedioClinic.CompanyService";
    private const string Source_Company = "MedioClinic.Company";
    private const string Source_MapLocation = "MedioClinic.MapLocation";
    private const string Source_SiteSection = "MedioClinic.SiteSection";
    private const string Source_Doctor = "MedioClinic.Doctor";

    // Source class IDs (KX13) — for ICmsTree.NodeClassID matching
    private const int ClassID_CompanyService = 3001;
    private const int ClassID_Company = 3002;
    private const int ClassID_MapLocation = 3003;
    private const int ClassID_Doctor = 3004;

    // Target field names
    private const string Target_Field_OfficeLocations = "ContactPageOfficeLocations";
    private const string Target_Field_FeaturedDoctors = "FeaturedDoctors";

    public override void Direct(ContentItemSource source, IContentItemActionProvider options)
    {
        switch (source.SourceClassName)
        {
            case Source_SiteSection when source.SourceNode!.NodeAliasPath == "/Doctors":
                // Link Doctor children as content item references
                // Note: ICmsTree does not have SourceClassName — use NodeClassID
                options.LinkChildren(
                    Target_Field_FeaturedDoctors,
                    source.ChildNodes!.Where(c => c.NodeClassID == ClassID_Doctor));
                break;

            // Template override example (path-based):
            // case "MedioClinic.LandingPage" when source.SourceNode!.NodeAliasPath == "/Home":
            //     options.OverridePageTemplate("MedioClinic.HomePageTemplate");
            //     break;

            default:
                // Direct() is abstract — do NOT call base.Direct(). No action = default behavior.
                break;
        }
    }

    public override void DirectLinkedNode(LinkedPageSource source, ILinkedPageActionProvider options)
    {
        // ICmsTree does not have SourceClassName — use NodeClassID for routing
        switch (source.LinkedNode.NodeClassID)
        {
            case ClassID_CompanyService:
                // Linked CompanyService → materialize as independent MedicalService items
                options.Materialize();
                break;

            case ClassID_Company:
                // Company under /Contact-us → reference on parent ContactPage (level -1)
                options.StoreReferenceInAncestor(-1, Target_Field_OfficeLocations);
                break;

            case ClassID_MapLocation:
                // MapLocation under /Contact-us/Office-locations →
                // reference on grandparent ContactPage (level -2, skipping CMS.Folder)
                options.StoreReferenceInAncestor(-2, Target_Field_OfficeLocations);
                break;

            default:
                // DirectLinkedNode() is virtual — base call is OK
                base.DirectLinkedNode(source, options);
                break;
        }
    }
}

// =============================================================================
// SERVICE REGISTRATION
// Register each director as transient (NOT singleton). Directors run during
// the --pages CLI phase, before widget and field migrations.
// =============================================================================
public static class ContentItemDirectorServiceExtensions
{
    public static IServiceCollection AddLinkedPageDirector(this IServiceCollection services)
    {
        services.AddTransient<ContentItemDirectorBase, LinkedPageDirector>();
        return services;
    }

    public static IServiceCollection AddChildPageLinkingDirector(this IServiceCollection services)
    {
        services.AddTransient<ContentItemDirectorBase, ChildPageLinkingDirector>();
        return services;
    }

    public static IServiceCollection AddPageToWidgetDirector(this IServiceCollection services)
    {
        services.AddTransient<ContentItemDirectorBase, PageToWidgetDirector>();
        return services;
    }

    public static IServiceCollection AddMedioClinicContentItemDirector(this IServiceCollection services)
    {
        // Use this INSTEAD of the individual directors above when combining
        // all concerns into a single director class.
        services.AddTransient<ContentItemDirectorBase, MedioClinicContentItemDirector>();
        return services;
    }
}
