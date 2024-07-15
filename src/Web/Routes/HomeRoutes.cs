using Lib;
using Microsoft.EntityFrameworkCore;
using Web.Data;
using Web.Lib;
using Web.Services;

namespace Web.Routes;

public static class HomeRoutes
{
    public static readonly URoute HomeRoute = new("/");

    public static void Map(IEndpointRouteBuilder builder)
    {
        builder.MapGet(HomeRoute.Pattern, async (HttpResponse res, Db db) =>
        {
            var wave = new HtmlWave(res);
            var layout = UI.Layout("Feedhub - Customers feedback inside of your GitHub");

            await wave.Write(layout.Start);
            await wave.Write(UI.Heading("Feedhub", "Store your customers' feedback inside of your GitHub. You own data and you can easily start working on it."));
            await wave.Write($"<a href='{DashboardRoutes.DashboardRoute.Url()}' role='button'>Go to dashboard</a>");
            await wave.Send("<hr>");

            var apps = await db.Apps.ToListAsync();
            await wave.Write("<section><hgroup><h3>Who uses ?</h3><p>Here are products that use Feedhub</p></hgroup><ul>");
            foreach (var app in apps)
            {
                await wave.Write($"<li><a href='/{UI.Text(app.Slug)}'>{UI.Text(app.Name)}</a></li>");
            }
            await wave.Write("</ul></section>");

            await wave.Complete(layout.End);
        });
    }
}