using Lib;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Octokit;
using System.Diagnostics;
using System.Text;
using Web.Controllers;
using Web.Data;
using Web.Routes;
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

StreamRoutes.Map(app);

app.Run();

//var c = Component.Div.Style("overflow:hidden;")[
//    Component.P["very strange syntax"],
//    Component.Div[
//        Component.Slot
//    ],
//    Component.P["after slot"]
//];

//var Component = El.Div.Style("overflow.hidden")[
//    El.P("aaaa"),
//    El.P("aaaa").Style("color:red;"),
//    El.Slot(),
//    El.A(),
//];

//Component(
//    IntoSlot(

//    )
//);

//Component.Start(res);

//Component.End(res);

//var component = Component.Div<T>[
//    Component.P[(props) => props.Slug],
//    Component.Slot[
//        () =>
//        {
//            var issues = await issuesTask;

//            if (issues != null)
//            {
//                foreach (var issue in issues)
//                {
//                    await WriteToRes(res, Component.P[issue.Title, issue.Body]);
//                    await res.BodyWriter.FlushAsync();
//                }
//            }
//        }
//]
//];
internal class Component
{
    public readonly Dictionary<string, string> Attributes = [];

    public Component Style(string value)
    {
        Attributes.Add("style", value);
        return this;
    }

    public Component this[params Component[] component] => this;
    public Component this[string text] => new();

    public static Component Slot => new();

    public static Component P => new();

    public static Component Div => new();
}