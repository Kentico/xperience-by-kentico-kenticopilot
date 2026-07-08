# Example section

A view-component-based section with a single configurable property and one widget zone. Files live under `~/Components/Sections/SingleColumn/`.

## Registration + view component — `SingleColumnSectionViewComponent.cs`

```csharp
using Kentico.PageBuilder.Web.Mvc;

using Microsoft.AspNetCore.Mvc;

using CompanyName.Components.Sections.SingleColumn;

[assembly: RegisterSection(
    identifier: SingleColumnSectionViewComponent.IDENTIFIER,
    viewComponentType: typeof(SingleColumnSectionViewComponent),
    name: "{$companyname.section.singlecolumn.name$}",
    propertiesType: typeof(SingleColumnSectionProperties),
    Description = "{$companyname.section.singlecolumn.description$}",
    IconClass = "icon-square")]

namespace CompanyName.Components.Sections.SingleColumn;

public class SingleColumnSectionViewComponent : ViewComponent
{
    public const string IDENTIFIER = "CompanyName.Sections.SingleColumn";

    public IViewComponentResult Invoke(ComponentViewModel<SingleColumnSectionProperties> sectionProperties)
    {
        var model = new SingleColumnSectionViewModel
        {
            CssClass = sectionProperties.Properties.CssClass
        };

        return View("~/Components/Sections/SingleColumn/_SingleColumnSection.cshtml", model);
    }
}
```

## Properties — `SingleColumnSectionProperties.cs`

```csharp
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace CompanyName.Components.Sections.SingleColumn;

public class SingleColumnSectionProperties : ISectionProperties
{
    [TextInputComponent(
        Label = "{$companyname.section.singlecolumn.cssclass.label$}",
        ExplanationText = "{$companyname.section.singlecolumn.cssclass.tooltip$}",
        Order = 10)]
    public string CssClass { get; set; } = string.Empty;
}
```

## View model — `SingleColumnSectionViewModel.cs`

```csharp
namespace CompanyName.Components.Sections.SingleColumn;

public class SingleColumnSectionViewModel
{
    public string CssClass { get; set; } = string.Empty;
}
```

## Partial view — `_SingleColumnSection.cshtml`

The view must contain at least one widget zone. Use a named zone so widgets transfer correctly when an editor switches the section type.

```cshtml
@using Kentico.PageBuilder.Web.Mvc
@using Kentico.Web.Mvc

@model SingleColumnSectionViewModel

<div class="section-single-column @Model.CssClass">
    @await Html.Kentico().WidgetZoneAsync("main")
</div>
```

## Notes

- For a **basic** section (no logic), skip the view component: create only the partial view (model `ComponentViewModel<TProperties>`) and register with `[assembly: RegisterSection("CompanyName.Sections.SingleColumn", "Name", typeof(SingleColumnSectionProperties), "~/Components/Sections/SingleColumn/_SingleColumnSection.cshtml")]`.
- To restrict which widgets a zone accepts, pass `allowedWidgets`: `@await Html.Kentico().WidgetZoneAsync("main", allowedWidgets: new[] { "CompanyName.Widgets.CardWidget" })`.
