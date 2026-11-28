using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace webMetics.Helpers;

/// <summary>Opt-in presentation metadata for the shared page heading.</summary>
public static class PageHeadingBuilder
{
    public static void Configure(ViewContext context, IUrlHelper url, string? title = null, string? subtitle = null)
    {
        context.ViewData["UseSharedPageHeader"] = BreadcrumbBuilder.Build(context, url).Count > 0;
        context.ViewData["PageHeading"] = title ?? Convert.ToString(context.ViewData["Title"]);
        context.ViewData["PageSubheading"] = subtitle;
    }
}
