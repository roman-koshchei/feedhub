namespace Web.Lib;

/// <summary>
/// Wave is a set of helpers to work with http on more low-level, but using high-level functions.
/// It exists, because I use custom route, html templating/streaming, etc.
/// </summary>
public class Wave
{
    /// <summary>
    /// Creates wave html, setting <paramref name="status"/> before writing body.
    /// Because you can't set any header after even a bit of body was written.
    /// So to create html wave I should make explicit setting of status.
    /// </summary>
    public static WaveHtml Html(HttpResponse res, int status) => new(res, status);

    public static WaveHtml Html(HttpContext ctx, int status) => new(ctx.Response, status);

    public static void Status(HttpResponse res, int status)
    {
        res.StatusCode = status;
    }

    public static void Status(HttpContext res, int status)
    {
        res.Response.StatusCode = status;
    }

    public static bool IsJavascript(HttpRequest req)
    {
        return req.Headers.ContainsKey("WaveJavascript");
    }

    public static bool IsJavascript(HttpContext ctx) => IsJavascript(ctx.Request);

    /// <summary>
    /// Performs client-side redirection by wave.js
    /// </summary>
    public static void WaveRedirect(HttpContext ctx, string url)
    {
        ctx.Response.Headers.Append("WaveRedirect", url);
    }

    /// <summary>
    /// Performs server-side redirection
    /// </summary>
    public static void HttpRedirect(HttpContext ctx, string url, bool permanent = false)
    {
        ctx.Response.Redirect(url, permanent);
    }

    /// <summary>
    /// Automatically determines weather perform client-side redirect or server-side
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="url"></param>
    /// <param name="permanent"></param>
    public static void Redirect(HttpContext ctx, string url, bool permanent = false)
    {
        if (IsJavascript(ctx.Request))
        {
            WaveRedirect(ctx, url);
        }
        else
        {
            ctx.Response.Redirect(url, permanent);
        }
    }
}