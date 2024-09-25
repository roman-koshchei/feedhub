using Microsoft.EntityFrameworkCore;
using Web.Data;
using Web.Lib;
using Web.Services;
using static Web.Lib.Tags;

namespace Web.Handlers;

public static class HomeHandlers
{
    public static readonly WaveRoute HomeRoute = new("/");

    public static void Map(IEndpointRouteBuilder builder)
    {
        _ = builder.MapGet(HomeRoute.Pattern, async (HttpResponse res, Db db) =>
        {
            var wave = Wave.Html(res, StatusCodes.Status200OK);
            var layout = UI.Layout("Feedhub - Customers feedback inside of your GitHub");

            await wave.Add(layout.Start);
            await wave.Add(UI.Heading("Feedhub", "Store your customers' feedback inside of your GitHub. You own data and you can easily start working on it!"));
            await wave.Add($"<a href='{DashboardHandlers.DashboardRoute.Url()}' role='button'>Go to dashboard</a>");
            await wave.Add(A.Href("https://github.com/roman-koshchei/feedhub").Blank().Attr("style", "margin-left: 1rem;").Wrap("GitHub"));
            await wave.Send("<hr>");

            var apps = await db.Apps.ToListAsync();
            await wave.Add("<section><hgroup><h3>Who uses ?</h3><p>Here are products that use Feedhub</p></hgroup><ul>");
            foreach (var app in apps)
            {
                await wave.Add($"<li><a href='/{UI.Text(app.Slug)}'>{UI.Text(app.Name)}</a></li>");
            }
            await wave.Add("</ul></section>");

            await wave.Add(layout.End);
        });

        _ = builder.MapGet(WaveRoute.New("email").Pattern, static async (HttpContext ctx) =>
        {
            await Wave.Html(ctx, 200).Add(UI.BaseLayout("Email", "Creation of email page").Wrap(Form.Class("grid").Wrap(
                Div.Wrap(
                    Label[
                        "Email preset",
                        Select.Wrap(
                            Option.Flag("selected").Attr("value", "")["Use new email sender"],
                            Option["Roman Koshchei - roman@flurium.com"],
                            Option["Flurium - roman@flurium.com"]
                        ),
                        Small["If you select preset then it's prioritized over inputs"]
                    ],
                    Div.Class("grid")[
                        Label["Your name", Input.Placeholder("Roman Koshchei")],
                        Label["Your email", Input.Placeholder("email@example.com")]
                    ],
                    P.Wrap(Label.Wrap(Input.Attr("type", "checkbox"), "Save sender?")),
                    Label.Wrap("Subject", Input.Placeholder("Story about ...")),
                    Button["Send"]
                ),
                Div.Wrap(
                    Label.Wrap("Markdown content", Textarea.Attr("rows", "15").Placeholder(
                        "# Heading 1 \n\n Some content of paragraph \n\n - list item 1"
                    ))
                )
            )));
        });
    }
}