using System.Text;
using System.Text.RegularExpressions;
using DeomaBlog.Domain.Common;

namespace DeomaBlog.Domain.Posts;

/// <summary>
/// Url friendly identifier of a post ("how-records-beat-classes").
/// A value object: created through the factories, always valid, compares by value.
/// </summary>
public sealed partial class Slug : IEquatable<Slug>
{
    public const int MaxLength = 128;

    private Slug(string value) => Value = value;

    public string Value { get; }

    /// <summary>
    /// Builds a slug out of human text: lower-cases it, keeps letters and digits, and collapses
    /// everything else into single dashes. The result is truncated to <see cref="MaxLength"/>.
    /// </summary>
    public static Slug FromTitle(string title)
    {
        Guard.NotNullOrWhiteSpace(title, nameof(title));

        var builder = new StringBuilder(title.Length);
        var previousWasSeparator = true;

        foreach (var character in title.Trim().ToLowerInvariant())
        {
            if (builder.Length >= MaxLength)
            {
                break;
            }

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                previousWasSeparator = false;
            }
            else if (character is ' ' or '-' or '_' or '/' or '.' or '&' or ':')
            {
                if (!previousWasSeparator)
                {
                    builder.Append('-');
                    previousWasSeparator = true;
                }
            }
        }

        var slug = builder.ToString().Trim('-');

        return slug.Length == 0
            ? throw new DomainException("Title does not contain any character usable in a slug.")
            : Create(slug);
    }

    /// <summary>
    /// Validates a slug that came from a human or from the database. Input is taken literally:
    /// "UPPER" or "has space" are rejected instead of being silently repaired, so a bad value is
    /// always visible at the boundary.
    /// </summary>
    public static Slug Create(string value)
    {
        var candidate = Guard.NotNullOrWhiteSpace(value, nameof(value), MaxLength);

        if (!SlugPattern().IsMatch(candidate))
        {
            throw new DomainException(
                $"'{candidate}' is not a valid slug: use lower-case latin letters, digits and single dashes.");
        }

        return new Slug(candidate);
    }

    /// <summary>Non-throwing check used by the query side, where an invalid slug means "not found".</summary>
    public static bool IsValid(string? value)
        => !string.IsNullOrWhiteSpace(value)
           && value.Length <= MaxLength
           && SlugPattern().IsMatch(value);

    public bool Equals(Slug? other)
        => other is not null && string.Equals(Value, other.Value, StringComparison.Ordinal);

    public override bool Equals(object? obj) => Equals(obj as Slug);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

    public override string ToString() => Value;

    /// <summary>Implicit conversion keeps the EF Core value-converter mapping trivial.</summary>
    public static implicit operator string(Slug slug) => slug.Value;

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    private static partial Regex SlugPattern();
}
