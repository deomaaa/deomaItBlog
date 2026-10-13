namespace DeomaBlog.Application.Exceptions;

/// <summary>Requested resource does not exist — mapped to HTTP 404 by the API layer.</summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string resource, object key)
        : base($"{resource} '{key}' was not found.")
    {
        Resource = resource;
        Key = key;
    }

    public string Resource { get; }

    public object Key { get; }
}
