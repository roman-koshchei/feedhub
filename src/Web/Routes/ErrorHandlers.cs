using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.RateLimiting;
using Web.Lib;
using Web.Services;

namespace Web.Routes;

public static class ErrorHandlers
{
    public static void Map(IEndpointRouteBuilder builder)
    {
        builder.MapGet(TooManyRequestsRoute.Pattern, TooManyRequestsPage);
        builder.MapGet(NotFoundRoute.Pattern, NotFoundPage);
    }

    public static readonly WaveRoute ErrorsRoute = new("errors");

    public static readonly WaveRoute TooManyRequestsRoute = ErrorsRoute.Add("too-many-requests");
    public static readonly WaveRoute NotFoundRoute = ErrorsRoute.Add("not-found");

    public static async Task TooManyRequestsPage(HttpContext ctx)
    {
        var html = Wave.Html(ctx.Response, StatusCodes.Status429TooManyRequests);
        await html.Add(UI.Layout("Limit Reached - Feedhub", "You reached the rate limit of form submissions").Wrap(@$"
            {UI.Heading("Rate limit reached", "You send too much of form submissions. We think you are bot.")}
            <p><a href='{FeedbackRoutes.FeedbackRoute.Url("feedhub")}'>
                You are not a bot? Just send a lot of feedbacks? Report bug here.
            </a></p>
            <p><a href='{HomeRoutes.HomeRoute.Url()}'>
                Checkout Feedhub if you want to collect your users' feedback.
            </a></p>"
        ));
    }

    public static async ValueTask RateLimiterOnReject(OnRejectedContext ctx, CancellationToken token)
    {
        if (Wave.IsJavascript(ctx.HttpContext.Request))
        {
            Wave.WaveRedirect(ctx.HttpContext, TooManyRequestsRoute.Url());
        }
        else
        {
            await TooManyRequestsPage(ctx.HttpContext);
        }
    }

    public static async Task NotFoundPage(HttpContext ctx)
    {
        var html = Wave.Html(ctx, StatusCodes.Status404NotFound);
        await using (await UI.Layout("Not found - Feedhub").Disposable(html))
        {
            await html.Add(UI.Heading("Not found", "We can't found a resource you are requesting"));
            await html.Add($"<a href='{HomeRoutes.HomeRoute.Url()}'>Come back home</a>");
        }
    }

    public static async Task StatusCodePages(StatusCodeContext ctx)
    {
        if (ctx.HttpContext.Response.StatusCode == StatusCodes.Status404NotFound)
        {
            if (Wave.IsJavascript(ctx.HttpContext))
            {
                Wave.WaveRedirect(ctx.HttpContext, NotFoundRoute.Url());
            }
            else
            {
                await NotFoundPage(ctx.HttpContext);
            }
        }
    }
}