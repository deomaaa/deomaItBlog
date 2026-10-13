namespace DeomaBlog.Domain.Common;

/// <summary>
/// Raised when an invariant of the domain model is violated.
/// The Infrastructure/Web layers translate it into an HTTP 400/409 response.
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }

    public DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
