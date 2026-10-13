namespace DeomaBlog.Domain.Common;

/// <summary>
/// Small argument-validation helper that keeps the domain objects free of
/// scattered if-throw blocks and always reports failures as <see cref="DomainException"/>.
/// </summary>
public static class Guard
{
    public static string NotNullOrWhiteSpace(string? value, string parameterName, int maxLength = 512)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"'{parameterName}' must not be empty.");
        }

        var trimmed = value.Trim();

        if (trimmed.Length > maxLength)
        {
            throw new DomainException($"'{parameterName}' must be at most {maxLength} characters long.");
        }

        return trimmed;
    }

    public static T NotNull<T>(T? value, string parameterName) where T : class
        => value ?? throw new DomainException($"'{parameterName}' must not be null.");

    public static int InRange(int value, int min, int max, string parameterName)
    {
        if (value < min || value > max)
        {
            throw new DomainException($"'{parameterName}' must be between {min} and {max}.");
        }

        return value;
    }

    public static DateTimeOffset NotDefault(DateTimeOffset value, string parameterName)
        => value == default
            ? throw new DomainException($"'{parameterName}' must be a valid timestamp.")
            : value;

    public static string? OptionalUrl(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new DomainException($"'{parameterName}' must be an absolute http(s) url.");
        }

        return trimmed;
    }
}
