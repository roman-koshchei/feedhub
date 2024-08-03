using Lib;
using Microsoft.EntityFrameworkCore;
using Web.Data;
using Web.Lib;
using Web.Services;

namespace Web.Handlers;

public static class HomeHandlers
{
    public static readonly WaveRoute HomeRoute = new("/");

    public static void Map(IEndpointRouteBuilder builder)
    {
        builder.MapGet(HomeRoute.Pattern, async (HttpResponse res, Db db) =>
        {
            var wave = Wave.Html(res, StatusCodes.Status200OK);
            var layout = UI.Layout("Feedhub - Customers feedback inside of your GitHub");

            await wave.Add(layout.Start);
            await wave.Add(UI.Heading("Feedhub", "Store your customers' feedback inside of your GitHub. You own data and you can easily start working on it."));
            await wave.Add($"<a href='{DashboardHandlers.DashboardRoute.Url()}' role='button'>Go to dashboard</a>");
            await wave.Add(Tags.A.Href("https://github.com/roman-koshchei/feedhub").Blank().Attr("style", "margin-left: 1rem;").Wrap("GitHub"));
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
    }
}