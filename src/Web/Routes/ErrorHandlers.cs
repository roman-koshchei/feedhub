using Microsoft.AspNetCore.RateLimiting;
using Web.Lib;
using Web.Services;

namespace Web.Routes;

public static class ErrorHandlers
{
    public static async ValueTask RateLimitReached(OnRejectedContext ctx, CancellationToken token)
    {
        var html = new HtmlWave(ctx.HttpContext.Response);
        await html.Complete(UI.Layout("Limit Reached - Feedhub", "You reached the rate limit of form submissions").Wrap(@$"
            {UI.Heading("Rate limit reached", "You send too much of form submissions. We think you are bot.")}
            <p><a href='{FeedbackRoutes.FeedbackRoute.Url("feedhub")}'>
                You are not a bot? Just send a lot of feedbacks? Report bug here.
            </a></p>
            <p><a href='{HomeRoutes.HomeRoute.Url()}'>
                Checkout Feedhub if you want to collect your users' feedback.
            </a></p>"
        ));
    }
}