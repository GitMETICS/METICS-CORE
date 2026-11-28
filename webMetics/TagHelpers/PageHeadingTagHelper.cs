using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace webMetics.TagHelpers;

/// <summary>Suppresses the original heading only when it is rendered in the shared header.</summary>
[HtmlTargetElement(Attributes = "metics-page-heading")]
public sealed class PageHeadingTagHelper : TagHelper
{
    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = default!;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.Attributes.RemoveAll("metics-page-heading");
        if (ViewContext.ViewData["UseSharedPageHeader"] is true)
            output.SuppressOutput();
    }
}
