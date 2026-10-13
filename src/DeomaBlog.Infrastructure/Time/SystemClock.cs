using DeomaBlog.Application.Abstractions;

namespace DeomaBlog.Infrastructure.Time;

/// <inheritdoc cref="IClock" />
public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
