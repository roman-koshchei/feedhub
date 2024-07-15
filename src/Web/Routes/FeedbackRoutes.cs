using Lib;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Net.WebSockets;
using Web.Config;
using Web.Data;
using Web.Lib;
using Web.Routes;
using Web.Services;

namespace Web.Routes;

public static class FeedbackRoutes
{
    private const string NotFoundView = @"
        <!DOCTYPE html>
        <html lang=""en"">
        <head>
            <meta charset=""utf-8"" />
            <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
            <title>Not Found</title>
        </head>
        <body style='overflow: auto scroll;'>
            <main role=""main"" style=""max-width:720px; margin-left:auto; margin-right:auto;"">
                <h1>Not found</h1>
            </main>
        </body>
        </html>";

    public static SplitElement PageView(
        string title, string slug,
        string? error, string description
    )
    {
        var layout = UI.Layout(title, description);
        return new($@"
        {layout.Start}
            {UI.Heading(title, description)}
            {(error == null ? "" : $"<p>{error}</p>")}

            <label for='hide-create-form' role='button' class='outline'>Leave feedback</label>
            <p></p>
            <input type='checkbox' id='hide-create-form' style='display:none' class='hide-next-if-checked' checked />
            <form method='post' action='/{slug}'>
                <label>
                    Title
                    <input name=""{nameof(PostFeedbackBody.Title)}"" placeholder='Feedback title' required />
                </label>
                <label>
                    Content
                    <textarea
                        required rows='3' name='{nameof(PostFeedbackBody.Content)}'
                        placeholder='Leave your feedback here'
                    ></textarea>
                </label>

                <div style=""display:flex; gap:0.5rem; margin-top:0.5rem;"">
                    <button type='submit' value='{ReportBugAction}' class='secondary' name='action'>Report Bug</button>
                    <button type='submit' value='{LeaveFeedbackAction}' name='action'>Leave feedback</button>
                </div>
            </form>
            <div id=""list"">", $@"</div>
        {layout.End}");
    }

    private const string ReportBugAction = "report-bug";
    private const string LeaveFeedbackAction = "leave-feedback";

    public class PostFeedbackBody
    {
        //public string? Name { get; set; }

        [Required, MinLength(1)]
        public required string Title { get; set; }

        [Required, MinLength(1)]
        public required string Content { get; set; }

        [Required, MinLength(1)]
        public required string Action { get; set; }
    }

    private record AppViewModel(
        string Slug, string Name, string GitHubApiToken,
        string RepositoryOwner, string RepositoryName
    );

    private static async Task WritePage(
       HtmlWave wave, GitHub gitHub, string description,
       string name, string slug, string? error
   )
    {
        var issuesTask = gitHub.GetFeedHubIssues();

        var page = PageView(name, slug, error, description);
        await wave.Send(page.Start);

        var issues = await issuesTask;
        if (issues != null)
        {
            foreach (var issue in issues)
            {
                var content = issue.Body;
                var lastLineIndex = issue.Body.LastIndexOf('\n');
                if (lastLineIndex > 0 && content[(lastLineIndex + 1)..].Trim().StartsWith(GitHub.UPVOTES_FIELD))
                {
                    content = content[..lastLineIndex];
                }
                if (content.Length > 200)
                {
                    content = content[..200];
                }

                await wave.Write(@$"<hr>{UI.ListItem(issue.Title, content, $"#{issue.Number}")
                    .Wrap($"<a href='{UpvoteRoute.Url(slug, issue.Number.ToString())}' role='button'>Upvote</a>")}");
            }
        }

        await wave.Write("<hr><footer><a class='secondary' href='/'>Powered by Feedhub</a></footer>");
        await wave.Complete(page.End);
    }

    public static void Map(IEndpointRouteBuilder builder)
    {
        builder.MapGet(FeedbackRoute.Pattern, Feedback);

        builder
            .MapPost(FeedbackRoute.Pattern, SubmitFeedback)
            .RequireRateLimiting(Globals.FixedRateLimiter)
            .DisableAntiforgery();

        builder
            .MapGet(UpvoteRoute.Pattern, UpvoteFeedback)
            .RequireRateLimiting(Globals.FixedRateLimiter);
    }

    public static readonly URoute1Param FeedbackRoute = new URoute("/").Param("slug");

    private static async Task<IResult> Feedback(string slug, HttpResponse res, Db db)
    {
        var wave = new HtmlWave(res);
        var app = await db.Apps.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == slug);
        if (app == null)
        {
            await wave.Complete(NotFoundView);
            return Results.NotFound();
        }

        var github = new GitHub(app.GitHubApiToken, app.RepositoryOwner, app.RepositoryName);
        await WritePage(wave, github, app.Description, app.Name, app.Slug, null);
        return Results.Ok();
    }

    private static async Task<IResult> SubmitFeedback(
        HttpContext ctx, [FromRoute] string slug,
        [FromServices] Db db, [FromForm] PostFeedbackBody body
    )
    {
        var wave = new HtmlWave(ctx.Response);
        var app = await db.Apps
            .Where(x => x.Slug == slug)
            .Select(x => new { x.Slug, x.Name, x.Description, x.GitHubApiToken, x.RepositoryName, x.RepositoryOwner })
            .FirstOrDefaultAsync();
        if (app == null)
        {
            await wave.Complete(NotFoundView);
            return Results.NotFound();
        }

        var github = new GitHub(app.GitHubApiToken, app.RepositoryOwner, app.RepositoryName);
        if (!body.IsValid())
        {
            await WritePage(wave, github, app.Description, app.Name, app.Slug, "Form isn't valid");
            return Results.BadRequest();
        }

        var err = await github.CreateIssue(body.Title, body.Content, body.Action switch
        {
            ReportBugAction => GitHub.IssueType.Bug,
            _ => GitHub.IssueType.Feedback
        });
        if (err != null)
        {
            await WritePage(wave, github, app.Description, app.Name, app.Slug, "Can't add your feedback right now");
            return Results.Problem();
        }

        return Results.Redirect(FeedbackRoute.Url(slug));
    }

    public static readonly URoute2Param UpvoteRoute = FeedbackRoute.Add("upvote").Param("issue");

    private static async Task<IResult> UpvoteFeedback(
        HttpContext ctx, [FromRoute] string slug, [FromRoute] int issue,
        [FromServices] Db db
    )
    {
        var wave = new HtmlWave(ctx.Response);
        var app = await db.Apps
            .Where(x => x.Slug == slug)
            .Select(x => new { x.Slug, x.GitHubApiToken, x.RepositoryName, x.RepositoryOwner })
            .FirstOrDefaultAsync();
        if (app == null)
        {
            await wave.Complete(NotFoundView);
            return Results.NotFound();
        }

        var github = new GitHub(app.GitHubApiToken, app.RepositoryOwner, app.RepositoryName);
        _ = await github.UpvoteIssue(issue);

        return Results.Redirect(FeedbackRoute.Url(slug));
    }
}