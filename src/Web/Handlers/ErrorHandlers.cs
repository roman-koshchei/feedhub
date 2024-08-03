using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.RateLimiting;
using Web.Lib;
using Web.Services;

namespace Web.Handlers;

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

        await html.Add(UI.Layout(
            title: "Limit Reached - Feedhub",
            description: "You reached the rate limit of form submissions"
        ).Wrap(
            UI.Heading("Rate limit reached", "You send too much of form submissions. We think you are bot."),
            Tags.P.Wrap(Tags.A
                .Href(FeedbackHandlers.FeedbackRoute.Url("feedhub"))
                .Wrap("You are not a bot? Just send a lot of feedbacks? Report bug here.")
            ),
            Tags.P.Wrap(Tags.A
                .Href(HomeHandlers.HomeRoute.Url())
                .Wrap("Checkout Feedhub if you want to collect your users' feedback.")
            )
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
        var wave = Wave.Html(ctx, StatusCodes.Status404NotFound);
        var html = UI.Layout("Not found - Feedhub").Wrap(
            UI.Heading("Not found", "We can't found a resource you are requesting"),
            Tags.A.Href(HomeHandlers.HomeRoute.Url()).Wrap("Come back home")
        );
        await wave.Add(html);
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