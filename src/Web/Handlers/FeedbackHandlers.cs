using Lib;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.EntityFrameworkCore;
using Octokit;
using System.ComponentModel.DataAnnotations;
using System.Web;
using Web.Config;
using Web.Data;
using Web.Lib;
using Web.Services;

namespace Web.Handlers;

public static class FeedbackUI
{
    public static string Comment(IssueComment comment)
    {
        return Tags.Blockquote.Wrap(
            Tags.Text(comment.Body),
            comment.UpdatedAt.HasValue
            ? Tags.Footer.Wrap($"<cite>— {comment.UpdatedAt.Value:f}</cite>")
            : "");
    }

    public static string Issue(string slug, Issue issue)
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

        return @$"<hr>{UI.ListItem(issue.Title, content, $"#{issue.Number}").Wrap(
            Tags.A
                .Href(FeedbackHandlers.UpvoteRoute.Url(slug, issue.Number))
                .Style("margin-right: 1rem")
                .Role("button")
                .Wrap($"Upvote {upvotes}"),
            Tags.A
                .Href(FeedbackHandlers.CommentsRoute.Url(slug, issue.Number))
                .Wrap("Comments")
        )}";
    }
}

public static class FeedbackHandlers
{
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

    public static SplitElement FeedbackPageView(
        string title, string slug,
        string? error, string description
    )
    {
        var layout = UI.Layout(title, description, "<script src='/scripts/feedback.js' defer></script>");
        return new($@"
        {layout.Start}
            {UI.Heading(title, description)}
            {(error == null ? "" : Tags.P.Wrap(error))}

            {Tags.Label.Attr("for", "hide-create-form").Role("button").Class("outline").Wrap("Leave feedback")}
            {Tags.P}
            {Tags.Input.Type("checkbox").Id("hide-create-form").Style("display:none").Class("hide-next-if-checked").Flag("checked")}
            {Tags.Form.Id("feedback-form").Attr("method", "post").Attr("action", FeedbackRoute.Url(slug)).Wrap(
                Tags.Label.Wrap("Title",
                    Tags.Input
                        .Attr("name", nameof(PostFeedbackBody.Title))
                        .Attr("placeholder", "Feedback title").Flag("required").Wrap()
                ),
                Tags.Label.Wrap("Content",
                    Tags.Textarea
                        .Attr("name", nameof(PostFeedbackBody.Content))
                        .Attr("rows", "3")
                        .Attr("placeholder", "Leave your feedback here")
                        .Flag("required").Wrap()
                ),
                Tags.Div.Style("margin-top:0.5rem;").Role("group").Wrap(
                    Tags.Button
                        .Class("secondary")
                        .Attr("type", "submit")
                        .Attr("value", ReportBugAction)
                        .Attr("name", nameof(PostFeedbackBody.Action))
                        .Wrap("Report Bug"),
                    Tags.Button
                        .Attr("type", "submit")
                        .Attr("value", LeaveFeedbackAction)
                        .Attr("name", nameof(PostFeedbackBody.Action))
                        .Wrap("Leave feedback")
                )
            )}
            <div id=""feedback-list"">", $@"</div>
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

        public string Action { get; set; } = string.Empty;
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

        var page = FeedbackPageView(name, slug, error, description);
        await wave.Send(page.Start);

        var issues = await issuesTask;
        if (issues != null)
        {
            foreach (var issue in issues)
            {
                await wave.Add(FeedbackUI.Issue(slug, issue));
            }
        }

        await wave.Add("<hr>");
        await wave.Add(Tags.Footer.Wrap(
            Tags.A
                .Class("secondary")
                .Href(HomeHandlers.HomeRoute.Url())
                .Wrap("Powered by Feedhub")
        ));
        await wave.Add(page.End);
    }

    public static readonly WaveRoute<string> FeedbackRoute = new WaveRoute("/").Param<string>("slug");

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

        var issue = await github.CreateIssue(body.Title, body.Content, body.Action switch
        {
            ReportBugAction => GitHub.IssueType.Bug,
            _ => GitHub.IssueType.Feedback
        });
        if (issue.IsErr)
        {
            var wave = Wave.Html(ctx.Response, StatusCodes.Status500InternalServerError);
            await WritePage(wave, github, app.Description, app.Name, app.Slug, "Can't add your feedback right now");
            return;
        }

        if (Wave.IsJavascript(ctx))
        {
            await Wave
                .Html(ctx, StatusCodes.Status200OK)
                .Add(FeedbackUI.Issue(slug, issue.Val));
        }
        else
        {
            Wave.HttpRedirect(ctx, FeedbackRoute.Url(slug));
        }
    }

    public static readonly WaveRoute<string, int> UpvoteRoute = FeedbackRoute.Add("upvote").Param<int>("issue");

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

    public record CommentsQueryParams(string? Name);

    public static readonly WaveRoute<string, int> CommentsRoute
        = FeedbackRoute.Param<int>("issue");

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

        var wave = Wave.Html(res, StatusCodes.Status200OK);
        await using (await UI.Layout(
            "Comments", "Comments for it",
           "<script src='/scripts/comments.js' defer></script>"
        ).Disposable(wave))
        {
            await wave.Send();

            GitHub github = new(app.GitHubApiToken, app.RepositoryOwner, app.RepositoryName);
            var issueTask = github.GetIssue(issue).ConfigureAwait(false);
            var commentsTask = github.GetIssueComments(issue).ConfigureAwait(false);

            var githubIssue = await issueTask;

            await wave.Add(Tags.Many(
                UI.Heading(Tags.Text(githubIssue?.Title ?? "Comments"), "Comments about this feedback report"),
                Tags.P.Wrap(Tags.A.Href(FeedbackRoute.Url(slug)).Wrap("Back")),
                Tags.Form
                .Attr("method", "post")
                .Attr("action", CommentsRoute.Url(slug, issue))
                .Role("group")
                .Id("comment-form")
                .Wrap(
                    Tags.Input.Name(nameof(CommentForm.Comment)).Placeholder("Leave your comment").Flag("required").Wrap(),
                    Tags.Button.Type("submit").Wrap("Comment")
                )
            ));
            await wave.Send();

            var comments = await commentsTask;
            if (comments == null)
            {
                await wave.Add(Tags.P.Wrap(
                    "We can't load comments for this feedback report. Sorry. Try later."
                ));
            }
            else
            {
                await wave.Add(Tags.Div.Id("comment-list").Wrap(
                    Tags.Many(comments.Select(FeedbackUI.Comment)),
                    Tags.P
                        .Class("hide-self-if-has-previous")
                        .Wrap("Noone has left comment yet")
                ));
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
            Wave.Redirect(ctx, CommentsRoute.Url(slug, issue));
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
            Wave.Redirect(ctx, CommentsRoute.Url(slug, issue));
        }
    }
}