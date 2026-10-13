using System.Text.RegularExpressions;
using DeomaBlog.Application.Abstractions;
using Markdig;

namespace DeomaBlog.Infrastructure.Markdown;

/// <summary>Markdig based markdown renderer with the extensions a developer blog needs.</summary>
public sealed partial class MarkdigRenderer : IMarkdownRenderer
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAutoIdentifiers()
        .UseEmphasisExtras()
        .UsePipeTables()
        .UseTaskLists()
        .UseAutoLinks()
        .UseFootnotes()
        .DisableHtml()          // user supplied HTML is escaped: no XSS through post bodies
        .Build();

    public string ToHtml(string markdown)
        => string.IsNullOrWhiteSpace(markdown) ? string.Empty : Markdig.Markdown.ToHtml(markdown, Pipeline);

    public string ToPlainText(string markdown, int maxLength = 160)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return string.Empty;
        }

        var html = ToHtml(markdown);
        var text = HtmlTag().Replace(html, " ");
        text = Regex.Replace(text, @"\s+", " ").Trim();

        return text.Length <= maxLength ? text : $"{text[..maxLength].TrimEnd()}…";
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlTag();
}
