# Example page template

A default page template with one property and a Page Builder editable area. Files live under `~/PageTemplates/LandingPage/`.

## Registration — `PageTemplateRegister.cs` (`~/PageTemplates/`)

When a template needs no extra services or view-component logic, put its `RegisterPageTemplate` attribute in a dedicated registration file (e.g. `PageTemplateRegister.cs`) instead of on a component class. This keeps registrations organized.

```csharp
using Kentico.PageBuilder.Web.Mvc.PageTemplates;

using CompanyName.PageTemplates.LandingPage;

[assembly: RegisterPageTemplate(
    identifier: "CompanyName.LandingPageTemplate",
    name: "{$companyname.template.landingpage.name$}",
    propertiesType: typeof(LandingPageTemplateProperties),
    customViewName: "~/PageTemplates/LandingPage/_LandingPageTemplate.cshtml",
    ContentTypeNames = new[] { LandingPage.CONTENT_TYPE_NAME },
    Description = "{$companyname.template.landingpage.description$}")]
```

## Properties — `LandingPageTemplateProperties.cs`

```csharp
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace CompanyName.PageTemplates.LandingPage;

public class LandingPageTemplateProperties : IPageTemplateProperties
{
    [DropDownComponent(
        Label = "{$companyname.template.landingpage.theme.label$}",
        Options = "light;Light\r\ndark;Dark",
        Order = 10)]
    public string ColorScheme { get; set; } = "light";
}
```

## View — `_LandingPageTemplate.cshtml`

The template view renders a full HTML page and must include the Page Builder styles and scripts.

```cshtml
@using Kentico.PageBuilder.Web.Mvc
@using Kentico.Content.Web.Mvc.PageBuilder
@using Kentico.Web.Mvc

@model TemplateViewModel<LandingPageTemplateProperties>

@{
    string colorScheme = Model.Properties.ColorScheme;
    Layout = null;
}
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <link rel="stylesheet" href="~/css/site.css" />
    <page-builder-styles />
</head>
<body class="theme-@colorScheme">
    <main>
        @* Use a stable editable-area identifier so content transfers between templates of this content type. *@
        @await Html.Kentico().EditableAreaAsync("main")
    </main>

    <page-builder-scripts />
</body>
</html>
```

## Routing

The content type's route action returns a `TemplateResult` — the router resolves which page (and therefore which template) to render. No per-page view is needed.

```csharp
public ActionResult Index()
{
    // Optional custom model: return new TemplateResult(myModel);
    return new TemplateResult();
}
```

## Notes

- This example inlines the whole document with `Layout = null`. Equally valid — and often preferred when templates share chrome — is to move `<html>/<head>/<body>` and the `<page-builder-styles />`/`<page-builder-scripts />` tags into a shared layout and set `Layout = "~/Views/Shared/_MyTemplateLayout.cshtml"` here. Follow whatever the project's existing templates do; just make sure the Page Builder resource tags render exactly once.
- The content type must have *Include in routing* and Page Builder enabled.
- Page-template icons use the `xp-` prefix (`Kentico.Xperience.Admin.Base.Icons`), unlike widget/section icons which use `icon-`.
