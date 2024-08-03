using System.Data;
using System.Numerics;
using System.Text;
using System.Web;

namespace Web.Lib;

public class Tags
{
    public static Tag Label => new("label");
    public static Tag Button => new("button");
    public static Tag Form => new("form");
    public static ATag A => new("a");
    public static Tag Div => new("div");
    public static Tag Textarea => new("textarea");
    public static Tag P => new("p");
    public static Tag Input => new("input");
    public static Tag Footer => new("footer");
    public static Tag Blockquote => new("blockquote");

    public static string Text(string text) => HttpUtility.HtmlEncode(text);

    public static string Many(params string[] tags)
    {
        return string.Join(null, tags);
    }

    public static string Many(IEnumerable<string> tags)
    {
        return string.Join(null, tags);
    }
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

    public T Role(string role)
    {
        attributes.Add(("role", role));
        return (T)this;
    }

    public T Id(string id)
    {
        attributes.Add(("id", id));
        return (T)this;
    }

    public T Style(string style)
    {
        attributes.Add(("style", style));
        return (T)this;
    }

    public T Class(string className)
    {
        attributes.Add(("class", className));
        return (T)this;
    }

    public T Type(string value)
    {
        attributes.Add(("type", value));
        return (T)this;
    }

    public T Name(string value)
    {
        attributes.Add(("name", value));
        return (T)this;
    }

    public T Placeholder(string value)
    {
        attributes.Add(("placeholder", value));
        return (T)this;
    }

    public string Wrap(string content)
    {
        return $"<{tag} {string.Join(" ", attributes.Select((x) => x.Item2 is null ? $"{x.Item1}" : $"{x.Item1}='{x.Item2}'"))}>{content}</{tag}>";
    }

    public override string ToString()
    {
        return Wrap("");
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

    public ATag Target(string target)
    {
        attributes.Add(("target", target));
        return this;
    }

    public ATag Blank()
    {
        attributes.Add(("target", "_blank"));
        return this;
    }
}