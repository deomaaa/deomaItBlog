using System.Text.RegularExpressions;
using DeomaBlog.Domain.Common;

namespace DeomaBlog.Domain.Posts;

/// <summary>
/// Estimated reading time of a post, derived from its markdown body (~180 words per minute).
/// </summary>
public sealed partial record ReadingTime
{
    private const int WordsPerMinute = 180;

    private ReadingTime(int minutes) => Minutes = minutes;

    public int Minutes { get; }

    public string Display => Minutes <= 1 ? "1 min read" : $"{Minutes} min read";

    public static ReadingTime FromMarkdown(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return new ReadingTime(1);
        }

        // Strip code fences and inline code: developers skim code far faster than prose.
        var prose = CodeBlock().Replace(markdown, " ");
        var words = Word().Matches(prose).Count;

        var minutes = (int)Math.Ceiling(words / (double)WordsPerMinute);
        return new ReadingTime(Math.Max(1, minutes));
    }

    public static ReadingTime FromMinutes(int minutes) => new(Guard.InRange(minutes, 1, 600, nameof(minutes)));

    public override string ToString() => Display;

    [GeneratedRegex(@"```[\s\S]*?```|`[^`]*`")]
    private static partial Regex CodeBlock();

    [GeneratedRegex(@"[\p{L}\p{N}][\p{L}\p{N}'’\-]*")]
    private static partial Regex Word();
}
