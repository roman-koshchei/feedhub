using Lib;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Octokit;
using System.Diagnostics;
using System.Text;
using Web.Controllers;
using Web.Data;
using Web.Services;

Env.LoadFile("./.env");
var errors = Env.Ensure();
if (errors.Count > 0)
{
    Env.Describe(errors);
    return;
}

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddSingleton<Db>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapGet("/stream", async (HttpResponse res) =>
{
    res.ContentType = "text/html; charset=utf-8";
    res.Headers.CacheControl = "no-cache";
    res.Headers.Connection = "keep-alive";

    await using StreamWriter writer = new(stream: res.Body, Encoding.UTF8, leaveOpen: true);

    await writer.WriteAsync("<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"UTF-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\"><title>Non-linear Streaming</title></head><body><h1>Data Streaming Example</h1><div id=\"content\">");
    await writer.FlushAsync();
    //res.DeclareTrailer("trailername");

    for (int i = 0; i < 10; i++)
    {
        await Task.Delay(1000); // Simulate a delay
        string message = $"<p>Message {DateTime.Now:O}</p>";
        Console.WriteLine("Sending: " + message);
        await writer.WriteAsync(message);
        await writer.FlushAsync();
    }

    // Close the div and body tags
    await writer.WriteAsync("</div></body></html>");
    await writer.FlushAsync();
});

app.MapGet("/stream2", async (HttpResponse res) =>
{
    res.ContentType = "text/html; charset=utf-8";
    res.Headers.CacheControl = "no-cache";
    res.Headers.Connection = "keep-alive";

    var bytes = Encoding.UTF8.GetBytes("<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"UTF-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\"><title>Non-linear Streaming</title></head><body><h1>Data Streaming Example</h1><div id=\"content\">");
    await res.BodyWriter.WriteAsync(bytes);
    await res.BodyWriter.FlushAsync();
    //res.DeclareTrailer("trailername");

    for (int i = 0; i < 10; i++)
    {
        await Task.Delay(1000); // Simulate a delay
        string message = $"<p>Message {DateTime.Now:O}</p>";
        Console.WriteLine("Sending: " + message);
        bytes = Encoding.UTF8.GetBytes(message);
        await res.BodyWriter.WriteAsync(bytes);
        await res.BodyWriter.FlushAsync();
    }

    // Close the div and body tags
    bytes = Encoding.UTF8.GetBytes("</div></body></html>");
    await res.BodyWriter.WriteAsync(bytes);
    await res.BodyWriter.FlushAsync();
});

app.MapGet("/stream/{slug}",
async (
    HttpResponse res,
   [FromServices] Db db,
   [FromRoute] string slug
) =>
{
    var app = db.Apps.FirstOrDefault(x => x.Slug == slug);
    if (app != null)
    {
        GitHub gitHub = new(app.RepositoryOwner, app.RepositoryName);
        var issuesTask = gitHub.GetFeedHubIssues();

        await WriteToRes(res, $@"
            <!DOCTYPE html>
            <html lang=""en"">
            <head>
                <meta charset=""utf-8"" />
                <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
                <script defer src=""~/lib/htmx.min.js"" asp-append-version=""true""></script>
                <title>{app.Slug}</title>
            </head>
            <body style='overflow: scroll;'>
                <main role=""main"" style=""max-width:720px; margin-left:auto; margin-right:auto;"">
                    <div class=""text-center"">
                        <h1 class=""display-4"">Feedhub</h1>
                        <p>Leave your Feedback right here, so we know what the heck in your mind:</p>
                    </div>

                    <form hx-post=""/{app.Slug}"" hx-target=""#list"" hx-swap=""afterbegin"" style=""max-width:360px; width:100%;"">
                        <input type=""text"" style=""width:100%;"" name=""{nameof(FeedbackController.Input.User)}"" value="""" placeholder=""Name"" />
                        <textarea style=""width:100%; margin-top:0.5rem;"" rows=""10"" name=""{nameof(FeedbackController.Input.Content)}"" placeholder=""Your feedback""></textarea>

                        <div style=""display:flex; gap:0.5rem; margin-top:0.5rem;"">
                            <button type=""submit"" value=""{FeedbackController.ReportBugAction}"" name=""action"">Report Bug</button>
                            <button type=""submit"" value=""{FeedbackController.LeaveFeedbackAction}"" name=""action"">Leave Feedback</button>
                        </div>
                    </form>
                    <div id=""list"">
        ");
        await res.BodyWriter.FlushAsync();

        var issues = await issuesTask;

        if (issues != null)
        {
            foreach (var issue in issues)
            {
                await WriteToRes(res, $@"
                    <div>
                        <h3>{issue.Title}</h3>
                        <p>{issue.Body}</p>
                    </div>
                ");
                await res.BodyWriter.FlushAsync();
            }
        }

        await WriteToRes(res, $@"</div></main></body></html>");
    }
});

app.Run();

static async Task WriteToRes(HttpResponse res, string content)
{
    var bytes = Encoding.UTF8.GetBytes(content);
    await res.BodyWriter.WriteAsync(bytes);
}