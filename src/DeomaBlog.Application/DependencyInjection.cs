using DeomaBlog.Application.Posts;
using Microsoft.Extensions.DependencyInjection;

namespace DeomaBlog.Application;

/// <summary>Composition root of the Application layer — one line per use case.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IBlogQueryService, BlogQueryService>();
        services.AddScoped<IBlogAuthoringService, BlogAuthoringService>();

        return services;
    }
}
