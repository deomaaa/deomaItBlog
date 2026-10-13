using System.Globalization;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DeomaBlog.Infrastructure.Persistence.Conversions;

/// <summary>
/// SQLite has no native date type and refuses to translate <c>ORDER BY</c> over
/// <see cref="DateTimeOffset"/>. Storing timestamps as ISO-8601 UTC strings fixes ordering
/// (the format sorts lexicographically) and keeps the database file readable in a SQL prompt.
/// </summary>
internal static class DateTimeOffsetConverters
{
    private const string Format = "yyyy-MM-ddTHH:mm:ss.fffffffZ";

    public static readonly ValueConverter<DateTimeOffset, string> ToIsoUtc = new(
        value => value.ToUniversalTime().ToString(Format, CultureInfo.InvariantCulture),
        value => DateTimeOffset.Parse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal));
}
