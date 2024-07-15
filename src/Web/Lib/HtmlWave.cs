using System.IO.Pipelines;
using System.Text;

namespace Web.Lib;

public class SplitElement(string start, string end)
{
    public string Start { get; } = start;
    public string End { get; } = end;

    public string Wrap(string content) => $"{Start}{content}{End}";

    public void Deconstruct(out string start, out string end)
    {
        start = Start; end = End;
    }

    public async Task<DisposableSplitElement> Disposable(HtmlWave html)
    {
        return await html.Wrap(this);
    }
}

public class DisposableSplitElement(HtmlWave html, SplitElement element) : IAsyncDisposable
{
    private readonly HtmlWave html = html;

    public async ValueTask DisposeAsync() => await html.Write(element.End);
}

public class HtmlWave
{
    private readonly HttpResponse res;

    public HtmlWave(HttpResponse res)
    {
        this.res = res;
        res.Headers.ContentType = "text/html";
    }

    public async Task Write(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        await res.BodyWriter.WriteAsync(bytes);
    }

    public async Task Complete()
    {
        await res.BodyWriter.CompleteAsync();
    }

    public async Task Complete(string content)
    {
        await Write(content);
        await res.BodyWriter.CompleteAsync();
    }

    public async Task Send()
    {
        await res.BodyWriter.FlushAsync();
    }

    public async Task Send(string content)
    {
        await Write(content);
        await res.BodyWriter.FlushAsync();
    }

    public async Task<DisposableSplitElement> Wrap(SplitElement element)
    {
        await Write(element.Start);
        return new DisposableSplitElement(this, element);
    }
}