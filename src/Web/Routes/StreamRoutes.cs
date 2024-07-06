using System.IO.Pipelines;
using System.Text;
using Web.Controllers;
using Web.Data;
using Web.Services;

namespace Web.Routes;

public static class StreamRoutes
{
    public static (string, string) PageView(string slug)
    {
        return ($@"
            <!DOCTYPE html>
            <html lang=""en"">
            <head>
                <meta charset=""utf-8"" />
                <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
                <script defer src=""/lib/htmx.min.js"" asp-append-version=""true""></script>
                <title>{slug}</title>
            </head>
            <body style='overflow: auto scroll;'>
                <main role=""main"" style=""max-width:720px; margin-left:auto; margin-right:auto;"">
                    <div class=""text-center"">
                        <h1 class=""display-4"">Feedhub</h1>
                        <p>Leave your Feedback right here, so we know what the heck in your mind:</p>
                    </div>

                    <form hx-post=""/{slug}"" hx-target=""#list"" hx-swap=""afterbegin"" style=""max-width:360px; width:100%;"">
                        <input type=""text"" style=""width:100%;"" name=""{nameof(FeedbackController.Input.User)}"" value="""" placeholder=""Name"" />
                        <textarea style=""width:100%; margin-top:0.5rem;"" rows=""10"" name=""{nameof(FeedbackController.Input.Content)}"" placeholder=""Your feedback""></textarea>

                        <div style=""display:flex; gap:0.5rem; margin-top:0.5rem;"">
                            <button type=""submit"" value=""{FeedbackController.ReportBugAction}"" name=""action"">Report Bug</button>
                            <button type=""submit"" value=""{FeedbackController.LeaveFeedbackAction}"" name=""action"">Leave Feedback</button>
                        </div>
                    </form>
                    <div id=""list"">", @"</div>
                </main>
            </body>
            </html>");
    }

    public static string FeedbackView(string title, string content)
    {
        return $"<div><h3>{title}</h3><p>{content}</p></div>";
    }

    public static void Map(IEndpointRouteBuilder builder)
    {
        builder.MapGet("/stream/{slug}",
        async (
            HttpResponse res, Db db, string slug
        ) =>
        {
            var app = db.Apps.FirstOrDefault(x => x.Slug == slug);
            if (app != null)
            {
                GitHub gitHub = new(app.RepositoryOwner, app.RepositoryName);
                var issuesTask = gitHub.GetFeedHubIssues();

                var (pageStart, pageEnd) = PageView(app.Slug);

                await res.BodyWriter.WriteString(pageStart);
                await res.BodyWriter.FlushAsync();

                var issues = await issuesTask;
                if (issues != null)
                {
                    foreach (var issue in issues)
                    {
                        await res.BodyWriter.WriteString(FeedbackView(issue.Title, issue.Body));
                    }
                    // flus after all issues, because we don't stream issues for now
                    //await res.BodyWriter.FlushAsync();
                }

                await res.BodyWriter.WriteString(pageEnd);
                await res.BodyWriter.CompleteAsync();
            }
        });
    }
}

public static class BodyWriterExtensions
{
    public static async Task WriteString(this PipeWriter writer, string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        await writer.WriteAsync(bytes);
    }
}