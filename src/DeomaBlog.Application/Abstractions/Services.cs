namespace DeomaBlog.Application.Abstractions;

/// <summary>Markdown → HTML port. Kept in Application so the renderer can be swapped (Markdig, CommonMark.NET, plain text).</summary>
public interface IMarkdownRenderer
{
    string ToHtml(string markdown);

    /// <summary>Plain text teaser used for meta descriptions.</summary>
    string ToPlainText(string markdown, int maxLength = 160);
}

/// <summary>Ambient time port — makes time-dependent logic testable.</summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
