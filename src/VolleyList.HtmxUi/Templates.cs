using HandlebarsDotNet;

namespace HtmxUi;

public static class Templates
{
    public static void Init()
    {
        Handlebars.RegisterTemplate("create-listing-form", File.OpenText("Templates/create-listing-form.mustache").ReadToEnd());

        Handlebars.RegisterTemplate("display-computed-list", File.OpenText("Templates/display-computed-listing.mustache").ReadToEnd());

        Handlebars.RegisterTemplate("add-player", File.OpenText("Templates/add-player.mustache").ReadToEnd());
    }

    public static readonly HandlebarsTemplate<object, object> ComputedListPageTemplate =
        Handlebars.Compile(File.OpenText("Templates/computed-listing-page.mustache").ReadToEnd());

    public static readonly HandlebarsTemplate<object, object> CreateListingTemplating = Handlebars.Compile("{{> create-listing-form . }}");

    public static readonly HandlebarsTemplate<object, object> DisplayListingFormTemplate = Handlebars.Compile("{{> display-computed-list }}");


    public static readonly HandlebarsTemplate<object, object> IndexTemplate = Handlebars.Compile(File.OpenText("Templates/index.mustache").ReadToEnd());
}