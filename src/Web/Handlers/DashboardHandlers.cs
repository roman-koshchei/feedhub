using Lib;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Web.Data;
using Web.Lib;
using Web.Services;
using static Web.Handlers.FeedbackHandlers;
using static Web.Lib.Tags;

namespace Web.Handlers;

public static class DashboardHandlers
{
    public static void Map(IEndpointRouteBuilder builder)
    {
        builder.MapGet(AppsRoute.Pattern, AppsHandler).RequireAuthorization();
        builder.MapGet(DeleteAppRoute.Pattern, DeleteAppHandler).RequireAuthorization();

        builder.MapPost(AppsRoute.Pattern, CreateAppHandler)
            .RequireAuthorization()
            .DisableAntiforgery();

        builder.MapGet(EditAppRoute.Pattern, EditAppGetHandler)
            .RequireAuthorization();
        builder.MapPost(EditAppRoute.Pattern, EditAppPostHandler)
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
            await html.Add(Tags.A.Href(AuthHandlers.LogoutRoute.Url()).Wrap("Logout"));
            await html.Add("<p></p>");
            await html.Add(AppFormComponent);
            await html.Send();

            var apps = await db.Apps
                .AsNoTracking()
                .Select(x => new { x.Id, x.Name, x.Description, x.Slug, x.RepositoryName, x.RepositoryOwner })
                .ToListAsync();

            foreach (var app in apps)
            {
                await html.Add("<hr>");
                await html.Add(UI.ListItem(app.Name, app.Description).Wrap(
                    Div.Role("group")[
                         A.Role("button").Href(EditAppRoute.Url(app.Id))["Edit"],
                        A.Role("button").Href(FeedbackHandlers.FeedbackRoute.Url(app.Slug))
                        [
                            "Visit"
                        ],
                        A
                        .Role("button")
                        .Class("contrast")
                        .Blank()
                        .Href($"https://github.com/{app.RepositoryOwner}/{app.RepositoryName}")
                        ["GitHub"]
                    ],
                     A
                         .Href(DeleteAppRoute.Url(app.Id))
                         .Wrap("Delete Without Confirmation")
                ));
            }
        }
    }

    public class EditAppForm
    {
        [Required, MinLength(1)]
        public string GitHubApiToken { get; set; } = string.Empty;
    }

    public static readonly WaveRoute<string> EditAppRoute = DashboardRoute.Param("id");

    private static async Task EditAppGetHandler(HttpContext ctx, [FromServices] Db db, [FromRoute] string id)
    {
        var app = await db.Apps.Where(x => x.Id == id).FirstOrDefaultAsync();
        if (app == null)
        {
            Wave.Redirect(ctx, AppsRoute.Url());
            return;
        }

        WaveHtml wave = new(ctx.Response, 200);

        await wave.Add(UI.Layout($"Edit App: {app.Name}").Wrap(
            UI.Heading($"Edit: {app.Name}", "Edit properties of application"),
            Tags.A.Href(AppsRoute.Url())["Back"],
            "<hr>",
            Tags.Form.Attr("method", "post").Attr("action", EditAppRoute.Url(id))[
                 Tags.Label.Wrap("GitHub api token",
                    Tags.Input
                        .Attr("type", "password")
                        .Attr("name", nameof(EditAppForm.GitHubApiToken))
                        .Attr("value", app.GitHubApiToken)
                        .Flag("required"),
                     Tags.Button
                        .Attr("type", "submit")
                        ["Change"]
                )
            ]
        ));
    }

    private static async Task EditAppPostHandler(HttpContext ctx, [FromServices] Db db, [FromRoute] string id, [FromForm] EditAppForm form)
    {
        if (!form.IsValid())
        {
            Wave.Redirect(ctx, AppsRoute.Url());
            return;
        }

        var app = await db.Apps.Where(x => x.Id == id).FirstOrDefaultAsync();
        if (app == null)
        {
            Wave.Redirect(ctx, AppsRoute.Url());
            return;
        }

        try
        {
            app.GitHubApiToken = form.GitHubApiToken;
            await db.SaveChangesAsync();
        }
        catch
        {
        }

        Wave.Redirect(ctx, AppsRoute.Url());
        return;
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

    public static readonly WaveRoute<string> DeleteAppRoute
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