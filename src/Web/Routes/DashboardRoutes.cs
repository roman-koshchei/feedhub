using Lib;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Octokit;
using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;
using Web.Data;
using Web.Lib;
using Web.Services;

namespace Web.Routes;

public static class DashboardRoutes
{
    public static void Map(IEndpointRouteBuilder builder)
    {
        builder.MapGet(AppsRoute.Pattern, AppsHandler).RequireAuthorization();
        builder.MapGet(DeleteAppRoute.Pattern, DeleteAppHandler).RequireAuthorization();

        builder.MapPost(AppsRoute.Pattern, CreateAppHandler)
            .RequireAuthorization()
            .DisableAntiforgery();
    }

    public static readonly WaveRoute DashboardRoute = new("/dashboard");

    public static readonly WaveRoute AppsRoute = DashboardRoute;

    private static async Task AppsHandler(HttpResponse res, [FromServices] Db db)
    {
        var html = Wave.Html(res, StatusCodes.Status200OK);
        await using (await UI.Layout("Dashboard").Disposable(html))
        {
            await html.Add(UI.Heading("Dashboard", "Apps that you have connected to Feedhub"));
            await html.Add(Tags.A.Href(AuthRoutes.LogoutRoute.Url()).Attr("role", "button").Wrap("Logout"));
            await html.Add("<p></p>");
            await html.Add(AppFormComponent);
            await html.Send();

            var apps = await db.Apps
                .AsNoTracking()
                .Select(x => new { x.Id, x.Name, x.Description, x.Slug })
                .ToListAsync();

            foreach (var app in apps)
            {
                await html.Add("<hr>");
                await html.Add(UI.ListItem(app.Name, app.Description).Wrap(
                    Tags.Div.Attr("role", "group").Wrap(
                        Tags.A.Role("button").Href(DeleteAppRoute.Url(app.Id)).Wrap("Delete"),
                        Tags.A.Role("button").Href(FeedbackRoutes.FeedbackRoute.Url(app.Slug)).Wrap("Visit")
                    )
                ));
            }
        }
    }

    public class CreateAppInput
    {
        [Required, MinLength(1)]
        public required string Slug { get; set; }

        public required string Name { get; set; }
        public required string Description { get; set; }

        [Required, MinLength(1)]
        public required string RepositoryOwner { get; set; }

        [Required, MinLength(1)]
        public required string RepositoryName { get; set; }

        [Required, MinLength(3)]
        public required string GitHubApiKey { get; set; }
    }

    private static string AppFormComponent => $@"
        <label for='hide-create-form' role='button' class='outline'>Add new product</label>
        <p></p>
        <input type='checkbox' id='hide-create-form' style='display:none' class='hide-next-if-checked' checked />
        <form method='post' action='{AppsRoute.Pattern}' style='display:flex; flex-direction:column;'>
            <input name='{nameof(CreateAppInput.Slug)}' placeholder='Slug'></input>
            <input name='{nameof(CreateAppInput.Name)}' placeholder='Name'></input>
            <input name='{nameof(CreateAppInput.Description)}' placeholder='Description'></input>
            <input name='{nameof(CreateAppInput.RepositoryOwner)}' placeholder='Repository Owner'></input>
            <input name='{nameof(CreateAppInput.RepositoryName)}' placeholder='Repository Name'></input>
            <input name='{nameof(CreateAppInput.GitHubApiKey)}' type='password' placeholder='GitHub Api Key'></input>
            <button type='submit'>Confirm</button>
        </form>
    ";

    private static async Task CreateAppHandler(
        HttpContext ctx, Db db, [FromForm] CreateAppInput form
    )
    {
        try
        {
            var userId = ctx.User.Id();
            if (!form.IsValid()) throw new InvalidDataException();

            var appsCount = await db.Apps.CountAsync();
            if (appsCount > 3)
            {
                throw new Exception();
            }

            var slugTaken = await db.Apps.AnyAsync(x => x.Slug == form.Slug);
            if (slugTaken) throw new Exception();

            await db.Apps.AddAsync(new App
            {
                UserId = userId,
                Slug = form.Slug,
                GitHubApiToken = form.GitHubApiKey,
                Name = form.Name,
                Description = form.Description,
                RepositoryName = form.RepositoryName,
                RepositoryOwner = form.RepositoryOwner
            });
            await db.SaveChangesAsync();
        }
        catch { }

        ctx.Response.Redirect(DashboardRoute.Url());
    }

    public static readonly WaveRoute1Param DeleteAppRoute
        = DashboardRoute.Add("delete").Param("id");

    public static async Task DeleteAppHandler(
        HttpContext ctx, [FromServices] Db db, [FromRoute] string id
    )
    {
        var userId = ctx.User.Id();

        var app = await db.Apps.FirstOrDefaultAsync(x => x.UserId == userId && x.Id == id);
        if (app == null)
        {
            ctx.Response.Redirect(DashboardRoute.Url());
            return;
        }

        try
        {
            db.Apps.Remove(app);
            await db.SaveChangesAsync();
        }
        catch { }

        ctx.Response.Redirect(DashboardRoute.Url());
    }
}