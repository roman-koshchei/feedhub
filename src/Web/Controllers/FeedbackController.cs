using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Web.Data;
using Web.Lib;
using Web.Models;
using Web.Services;

namespace Web.Controllers;

public class FeedbackController(Db db) : Controller
{
    public static readonly View<AppViewModel> AppView = new("/Views/Feedback/App.cshtml");
    public static readonly View<FeedbackViewModel> _FeedbackView = new("/Views/Feedback/_Feedback.cshtml");

    private readonly Db db = db;

    [HttpGet("/{slug}")]
    public async Task<IActionResult> GetApp([FromRoute] string slug)
    {
        var app = db.Apps.FirstOrDefault(x => x.Slug == slug);
        if (app == null) return NotFound();

        GitHub gitHub = new(app.RepositoryOwner, app.RepositoryName);

        var watch = new Stopwatch();
        watch.Start();
        var issues = await gitHub.GetFeedHubIssues();
        watch.Stop();
        Console.WriteLine($"!!!!!!!!! {watch.ElapsedMilliseconds}");

        if (issues == null) return Problem();

        return AppView.ViewResult(this, new(slug, issues));
    }

    public const string ReportBugAction = "report-bug";
    public const string LeaveFeedbackAction = "leave-feedback";

    public record Input(string? User, string Content, string Action);

    [HttpPost("/{slug}")]
    public async Task<IActionResult> Post(
        [FromRoute] string slug,
        [FromForm] Input input
    )
    {
        var app = db.Apps.FirstOrDefault(x => x.Slug == slug);
        if (app == null) return NotFound();

        GitHub gitHub = new(app.RepositoryOwner, app.RepositoryName);

        var feedback = new Feedback(input.User, input.Content);
        await gitHub.CreateIssue(feedback.User, "Test", feedback.Content);

        return _FeedbackView.PartialResult(this, new(feedback.User ?? "", feedback.Content));
    }
}