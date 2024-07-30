using System.IO.Pipelines;
using System.Text;

namespace Web.Lib;

public class SplitElement(string start, string end)
{
    public string Start { get; } = start;
    public string End { get; } = end;

    public string Wrap(string content) => $"{Start}{content}{End}";

    public string Wrap(params string[] content)
    {
        var sb = new StringBuilder(Start);
        foreach (var item in content)
        {
            sb.Append(item);
        }
        sb.Append(End);
        return sb.ToString();
    }

    public void Deconstruct(out string start, out string end)
    {
        start = Start; end = End;
    }

    public async Task<DisposableSplitElement> Disposable(WaveHtml html)
    {
        return await html.Wrap(this);
    }
}

public class DisposableSplitElement(WaveHtml html, SplitElement element) : IAsyncDisposable
{
    private readonly WaveHtml html = html;

    public async ValueTask DisposeAsync()
    {
        await html.Add(element.End);
    }
}

public class WaveHtml
{
    private readonly HttpResponse res;

    //public WaveHtml(HttpResponse res, int status)
    //{
    //    this.res = res;
    //    res.StatusCode = status;
    //}

    public WaveHtml(HttpResponse res, int status)
    {
        this.res = res;
        res.StatusCode = status;
        res.ContentType = "text/html";
    }

    public async Task Add(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        await res.BodyWriter.WriteAsync(bytes);
    }

    public async Task Send()
    {
        await res.BodyWriter.FlushAsync();
    }

    public async Task Send(string content)
    {
        await Add(content);
        await res.BodyWriter.FlushAsync();
    }

    public async Task<DisposableSplitElement> Wrap(SplitElement element)
    {
        await Add(element.Start);
        return new DisposableSplitElement(this, element);
    }
}