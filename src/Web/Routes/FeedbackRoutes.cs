using Lib;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Octokit;
using System.ComponentModel.DataAnnotations;
using System.Net.WebSockets;
using Web.Config;
using Web.Data;
using Web.Lib;
using Web.Services;

namespace Web.Routes;

public static class FeedbackUI
{
    public static string Comment(IssueComment comment)
    {
        return @$"<blockquote>{comment.Body}
            {(comment.UpdatedAt.HasValue
                ? $"<footer><cite>— {comment.UpdatedAt.Value:f}</cite></footer>"
            : "")}</blockquote>";
    }
}

public static class FeedbackRoutes
{
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

                <div style=""margin-top:0.5rem;"" role='group'>
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
       WaveHtml wave, GitHub gitHub, string description,
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
                int upvotes = 0;
                var lastLineIndex = issue.Body.LastIndexOf('\n');
                var lastLine = content[(lastLineIndex + 1)..].Trim();
                if (lastLineIndex > 0 && lastLine.StartsWith(GitHub.UPVOTES_FIELD))
                {
                    content = content[..lastLineIndex];
                    if (int.TryParse(lastLine[GitHub.UPVOTES_FIELD.Length..], out var num))
                    {
                        upvotes = num;
                    }
                }
                if (content.Length > 200)
                {
                    content = content[..200];
                }

                await wave.Add(@$"<hr>{UI.ListItem(issue.Title, content, $"#{issue.Number}").Wrap(@$"
                    <a href='{UpvoteRoute.Url(slug, issue.Number.ToString())}' role='button' class='outline'>Upvote {upvotes}</a>
                    <a href='{CommentsRoute.Url(slug, issue.Number.ToString())}' role='button' class='outline contrast'>Comments</a>")}");
            }
        }

        await wave.Add("<hr><footer><a class='secondary' href='/'>Powered by Feedhub</a></footer>");
        await wave.Add(page.End);
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

        builder
            .MapGet(CommentsRoute.Pattern, Comments);

        builder
            .MapPost(CommentsRoute.Pattern, SubmitComment)
            .DisableAntiforgery()
            .RequireRateLimiting(Globals.FixedRateLimiter);
    }

    public static readonly WaveRoute1Param FeedbackRoute = new WaveRoute("/").Param("slug");

    private static async Task Feedback(string slug, HttpResponse res, Db db)
    {
        var app = await db.Apps.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == slug);
        if (app == null)
        {
            Wave.Status(res, StatusCodes.Status404NotFound);
            return;
        }

        var wave = Wave.Html(res, StatusCodes.Status200OK);
        var github = new GitHub(app.GitHubApiToken, app.RepositoryOwner, app.RepositoryName);
        await WritePage(wave, github, app.Description, app.Name, app.Slug, null);
        return;
    }

    private static async Task SubmitFeedback(
        HttpContext ctx, [FromRoute] string slug,
        [FromServices] Db db, [FromForm] PostFeedbackBody body
    )
    {
        var app = await db.Apps
            .Where(x => x.Slug == slug)
            .Select(x => new { x.Slug, x.Name, x.Description, x.GitHubApiToken, x.RepositoryName, x.RepositoryOwner })
            .FirstOrDefaultAsync();
        if (app == null)
        {
            Wave.Status(ctx.Response, StatusCodes.Status404NotFound);
            return;
        }

        var github = new GitHub(app.GitHubApiToken, app.RepositoryOwner, app.RepositoryName);
        if (!body.IsValid())
        {
            var wave = Wave.Html(ctx.Response, StatusCodes.Status400BadRequest);
            await WritePage(wave, github, app.Description, app.Name, app.Slug, "Form isn't valid");
            return;
        }

        var err = await github.CreateIssue(body.Title, body.Content, body.Action switch
        {
            ReportBugAction => GitHub.IssueType.Bug,
            _ => GitHub.IssueType.Feedback
        });
        if (err != null)
        {
            var wave = Wave.Html(ctx.Response, StatusCodes.Status500InternalServerError);
            await WritePage(wave, github, app.Description, app.Name, app.Slug, "Can't add your feedback right now");
            return;
        }

        ctx.Response.Redirect(FeedbackRoute.Url(slug));
    }

    public static readonly WaveRoute2Param UpvoteRoute = FeedbackRoute.Add("upvote").Param("issue");

    private static async Task UpvoteFeedback(
        HttpContext ctx, [FromRoute] string slug, [FromRoute] int issue,
        [FromServices] Db db
    )
    {
        var app = await db.Apps
            .Where(x => x.Slug == slug)
            .Select(x => new { x.Slug, x.GitHubApiToken, x.RepositoryName, x.RepositoryOwner })
            .FirstOrDefaultAsync();
        if (app == null)
        {
            Wave.Status(ctx, StatusCodes.Status404NotFound);
            return;
        }

        var github = new GitHub(app.GitHubApiToken, app.RepositoryOwner, app.RepositoryName);
        _ = await github.UpvoteIssue(issue);

        ctx.Response.Redirect(FeedbackRoute.Url(slug));
    }

    public static readonly WaveRoute2Param CommentsRoute = FeedbackRoute.Param("issue");

    public static async Task Comments(
        HttpResponse res, [FromServices] Db db,
        [FromRoute] string slug, [FromRoute] int issue
    )
    {
        var app = await db.Apps
            .Where(x => x.Slug == slug)
            .Select(x => new { x.Slug, x.GitHubApiToken, x.RepositoryName, x.RepositoryOwner })
            .FirstOrDefaultAsync();
        if (app == null)
        {
            Wave.Status(res, StatusCodes.Status404NotFound);
            return;
        }

        GitHub github = new(app.GitHubApiToken, app.RepositoryOwner, app.RepositoryName);
        var commentsTask = github.GetIssueComments(issue).ConfigureAwait(false);

        var html = Wave.Html(res, StatusCodes.Status200OK);
        await using (await UI.Layout(
            "Comments", "Comments for it",
           "<script src='/scripts/comments.js' defer></script>"
        ).Disposable(html))
        {
            await html.Add(UI.Heading("Comments", "Comments about this feedback report"));
            await html.Add($"<p><a href='{FeedbackRoute.Url(slug)}'>Back</a><p>");
            await html.Add($@"
                <form method='post' action='{CommentsRoute.Url(slug, issue.ToString())}' role='group' id='comment-form'>
                    <input name='{nameof(CommentForm.Comment)}' placeholder='Leave your comment' required />
                    <button type='submit'>Comment</button>
                </form>
            ");
            await html.Send();

            var comments = await commentsTask;
            if (comments == null)
            {
                await html.Add("<p>We can't load comments for this feedback report. Sorry. Try later.</p>");
            }
            else
            {
                await html.Add("<div id='comment-list'>");
                foreach (var comment in comments)
                {
                    await html.Add(FeedbackUI.Comment(comment));
                }
                await html.Add("</div>");
            }
        }
    }

    public class CommentForm
    {
        [Required, MinLength(1)]
        public required string Comment { get; set; }
    }

    public static async Task SubmitComment(
        HttpContext ctx, [FromServices] Db db,
        [FromRoute] string slug, [FromRoute] int issue,
        [FromForm] CommentForm form
    )
    {
        var app = await db.Apps
            .Where(x => x.Slug == slug)
            .Select(x => new { x.Slug, x.GitHubApiToken, x.RepositoryName, x.RepositoryOwner })
            .FirstOrDefaultAsync();
        if (app == null)
        {
            Wave.Status(ctx, StatusCodes.Status404NotFound);
            return;
        }

        if (!form.IsValid())
        {
            Wave.Redirect(ctx, CommentsRoute.Url(slug, issue.ToString()));
            return;
        }

        var github = new GitHub(app.GitHubApiToken, app.RepositoryOwner, app.RepositoryName);
        var comment = await github.CreateComment(issue, form.Comment);

        if (comment != null && Wave.IsJavascript(ctx.Request))
        {
            await Wave
                .Html(ctx, StatusCodes.Status200OK)
                .Add(FeedbackUI.Comment(comment));
        }
        else
        {
            Wave.Redirect(ctx, CommentsRoute.Url(slug, issue.ToString()));
        }
    }
}