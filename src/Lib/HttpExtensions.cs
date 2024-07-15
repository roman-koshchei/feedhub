using System.IO.Pipelines;
using System.Text;

namespace Lib;

public static class HttpExtensions
{
    public static async Task WriteString(this PipeWriter writer, string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        await writer.WriteAsync(bytes);
    }
}