using System.Text;

namespace Web.Lib;

public class Tags
{
    public static Tag Label => new("label");
    public static Tag Button => new("button");
    public static ATag A => new("a");

    public static Tag Div => new("div");
}

public class Tag(string tag) : Tag<Tag>(tag)
{
}

public class Tag<T>(string tag) where T : Tag<T>
{
    protected readonly List<(string, string?)> attributes = [];

    public T Attr(string name, string value)
    {
        attributes.Add((name, value));
        return (T)this;
    }

    public T Flag(string name)
    {
        attributes.Add((name, null));
        return (T)this;
    }

    public string Wrap(string content)
    {
        return $"<{tag} {string.Join(" ", attributes.Select((x) => x.Item2 is null ? $"{x.Item1}" : $"{x.Item1}='{x.Item2}'"))}>{content}</{tag}>";
    }

    public string Wrap(params string[] content)
    {
        var sb = new StringBuilder();
        sb.Append('<');
        sb.Append(tag);
        sb.Append(' ');
        foreach (var attr in attributes)
        {
            if (attr.Item2 is null)
            {
                sb.Append(attr.Item1);
            }
            else
            {
                sb.Append($"{attr.Item1}='{attr.Item2}'");
            }
        }
        sb.Append('>');

        foreach (var item in content)
        {
            sb.Append(item);
        }
        sb.Append($"</{tag}>");
        return sb.ToString();
    }
}

public class ATag(string tag) : Tag<ATag>(tag)
{
    public ATag Href(string url)
    {
        attributes.Add(("href", url));
        return this;
    }

    public ATag Role(string role)
    {
        attributes.Add(("role", role));
        return this;
    }
}