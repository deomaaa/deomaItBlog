namespace DeomaBlog.Web.Security;

/// <summary>Names shared by the API-key filter and the OpenAPI security definition.</summary>
public static class ApiKeyAuthenticationDefaults
{
    public const string Scheme = "AdminApiKey";
    public const string HeaderName = "X-Api-Key";
    public const string ConfigurationKey = "Admin:ApiKey";
}
