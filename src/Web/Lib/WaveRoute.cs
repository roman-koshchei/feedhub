using System.Text;

namespace Web.Lib;

public class WaveRoute(string pattern)
{
    private readonly string pattern = pattern;

    public WaveRoute Add(string path)
    {
        return new($"{pattern.TrimEnd('/')}/{path.TrimStart('/')}");
    }

    public WaveRoute1Param Param(string name) => new(pattern, name, "");

    public WaveRoute<T> Param<T>(string name) => new(pattern, name, "");

    public string Pattern => pattern;

    public string Url() => $"/{pattern.Trim('/')}";

    public static WaveRoute New(string pattern) => new(pattern);
}

public class WaveRoute<TParam1>(string start, string paramName, string end)
{
    private readonly string start = start.TrimEnd('/');
    private readonly string param = paramName.Trim('/');
    private readonly string end = end.Trim('/');

    public WaveRoute<TParam1> Add(string path)
    {
        return new(start, param, $"{end.TrimEnd('/')}/{path.TrimStart('/')}");
    }

    public string Pattern => $"{start}/{{{param}}}/{end}";

    public string Url(TParam1 param) => $"{start}/{param}/{end}";

    public WaveRoute<TParam1, TParam2> Param<TParam2>(string name) => new(start, param, end, name, "");
}

public class WaveRoute1Param(string start, string paramName, string end)
{
    private readonly string start = start.TrimEnd('/');
    private readonly string param = paramName.Trim('/');
    private readonly string end = end.Trim('/');

    public WaveRoute1Param Add(string path)
    {
        return new(start, param, $"{end.TrimEnd('/')}/{path.TrimStart('/')}");
    }

    public string Pattern => $"{start}/{{{param}}}/{end}";

    public string Url(string param) => $"{start}/{param}/{end}";

    public WaveRoute2Param Param(string name) => new(start, param, end, name, "");
}

public class WaveRoute<TParam1, TParam2>(
    string start, string param1, string middle, string param2, string end
)
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

    public string Url(TParam1 param1, TParam2 param2)
    {
        StringBuilder sb = new($"{start}/{param1}");
        if (!string.IsNullOrWhiteSpace(middle)) sb.Append($"/{middle}");
        sb.Append($"/{param2}");
        if (!string.IsNullOrWhiteSpace(end)) sb.Append($"/{end}");
        return sb.ToString();
    }

    public WaveRoute<TParam1, TParam2, TQuery> Query<TQuery>()
    {
        return new(start, param1, middle, param2, end);
    }
}

public class WaveRoute<TParam1, TParam2, TQuery>(
    string start, string param1, string middle, string param2, string end
)
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

    public string Url(TParam1 param1, TParam2 param2, TQuery query)
    {
        StringBuilder sb = new($"{start}/{param1}");
        if (!string.IsNullOrWhiteSpace(middle)) sb.Append($"/{middle}");
        sb.Append($"/{param2}");
        if (!string.IsNullOrWhiteSpace(end)) sb.Append($"/{end}");

        return sb.ToString();
    }
}

public class WaveRoute2Param(string start, string param1, string middle, string param2, string end)
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