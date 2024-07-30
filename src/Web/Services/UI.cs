using System.Web;
using Web.Lib;

namespace Web.Services;

public static class UI
{
    public static SplitElement Layout(string title, string description = "", string head = "") => new(@$"
        <!DOCTYPE html><html lang='en' data-theme='light'>
        <head>
            <meta charset='utf-8' />
            <meta name='viewport' content='width=device-width, initial-scale=1.0' />
            <link rel='stylesheet' href='/pico.blue.min.css' />
            <link rel='stylesheet' href='/styles.css' />

            <title>{title}</title>
            <meta name='description' content='{description ?? "Leave your Feedback right here, so we know what the heck in your mind"}' />

            <script src='/scripts/wave.js' defer></script>

            {head}
        </head>
        <body style='overflow: auto scroll;' class='container'>
            <header></header>
            <main style='max-width:720px; margin-left:auto; margin-right:auto;'>",
    @"</main></body></html>");

    public static string Text(string text) => HttpUtility.HtmlEncode(text);

    public static string Heading(string title, string description)
        => $"<hgroup><h1>{Text(title)}</h1><h2>{Text(description)}</h2></hgroup>";

    public static string Input(
        string name, string type, string label, string placeholder, bool isRequired, string? error, string? value = null
    ) => $@"<label>{label}
        <input
            type='{type}' placeholder='{placeholder}' {(isRequired ? $"required" : "")}
            name='{name}'
            aria-describedby='{name}-helper'
            {(value != null ? $"value='{value}'" : "")}
        />
        {(error != null ? $"<small id='{name}-helper'>{error}</small>" : "")}
    </label>";

    public static SplitElement ListItem(string title, string content, string label = "") => new($@"<div>
        <hgroup><p><b>{Text(title)}</b> {Text(label)}</p><p>{Text(content)}</p></hgroup>",
    "</div>");
}