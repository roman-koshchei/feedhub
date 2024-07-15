using System.Text;

namespace Web.Lib;

public class URoute(string pattern)
{
    private readonly string pattern = pattern.TrimEnd('/');

    public URoute Add(string path)
    {
        return new($"{pattern}/{path.TrimStart('/')}");
    }

    public URoute1Param Param(string name) => new(pattern, name, "");

    public string Pattern => pattern;

    public string Url() => pattern;
}

public class URoute1Param(string start, string paramName, string end)
{
    private readonly string start = start.TrimEnd('/');
    private readonly string param = paramName.Trim('/');
    private readonly string end = end.Trim('/');

    public URoute1Param Add(string path)
    {
        return new(start, param, $"{end.TrimEnd('/')}/{path.TrimStart('/')}");
    }

    public string Pattern => $"{start}/{{{param}}}/{end}";

    public string Url(string param) => $"{start}/{param}/{end}";

    public URoute2Param Param(string name) => new(start, param, end, name, "");
}

public class URoute2Param(string start, string param1, string middle, string param2, string end)
{
    private readonly string start = start.TrimEnd('/');
    private readonly string param1 = param1.Trim('/');
    private readonly string middle = middle.Trim('/');
    private readonly string param2 = param2.Trim('/');
    private readonly string end = end.Trim('/');

    public string Pattern
    {
        get
        {
            StringBuilder sb = new($"{start}/{{{param1}}}");
            if (!string.IsNullOrWhiteSpace(middle)) sb.Append($"/{middle}");
            sb.Append($"/{{{param2}}}");
            if (!string.IsNullOrWhiteSpace(end)) sb.Append($"/{end}");
            return sb.ToString();
        }
    }

    public string Url(string param1, string param2)
    {
        StringBuilder sb = new($"{start}/{param1}");
        if (!string.IsNullOrWhiteSpace(middle)) sb.Append($"/{middle}");
        sb.Append($"/{param2}");
        if (!string.IsNullOrWhiteSpace(end)) sb.Append($"/{end}");
        return sb.ToString();
    }
}