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

    public static readonly URoute DashboardRoute = new("/dashboard");

    public static readonly URoute AppsRoute = DashboardRoute;

    private static async Task AppsHandler(HttpResponse res, [FromServices] Db db)
    {
        var html = new HtmlWave(res);

        await using (await UI.Layout("Apps").Disposable(html))
        {
            await html.Write(UI.Heading("Your apps:", "Apps that you have connected to Feedhub"));
            await html.Write(AppFormComponent);
            await html.Send();

            var apps = await db.Apps.AsNoTracking().Select(x => new { x.Id, x.Name, x.Description }).ToListAsync();
            foreach (var app in apps)
            {
                await html.Write("<hr>");
                await html.Write(UI.ListItem(app.Name, app.Description).Wrap(
                    $"<a role='button' href='{DeleteAppRoute.Url(app.Id)}'>Delete</a>"
                ));
            }
        }
        await html.Complete();
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

    private static async Task<IResult> CreateAppHandler(
        HttpContext ctx, Db db, [FromForm] CreateAppInput form
    )
    {
        try
        {
            var userId = ctx.User.Id();
            if (!form.IsValid()) throw new InvalidDataException();

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

        return Results.Redirect("/dashboard");
    }

    public static readonly URoute1Param DeleteAppRoute
        = DashboardRoute.Add("delete").Param("id");

    public static async Task<IResult> DeleteAppHandler(
        HttpContext ctx, [FromServices] Db db, [FromRoute] string id
    )
    {
        var userId = ctx.User.Id();

        var app = await db.Apps.FirstOrDefaultAsync(x => x.UserId == userId && x.Id == id);
        if (app == null) return Results.Redirect(DashboardRoute.Url());

        try
        {
            db.Apps.Remove(app);
            await db.SaveChangesAsync();
        }
        catch { }

        return Results.Redirect(DashboardRoute.Url());
    }
}