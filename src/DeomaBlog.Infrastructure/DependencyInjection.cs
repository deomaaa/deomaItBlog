using DeomaBlog.Application.Abstractions;
using DeomaBlog.Application.Abstractions.Persistence;
using DeomaBlog.Infrastructure.Markdown;
using DeomaBlog.Infrastructure.Persistence;
using DeomaBlog.Infrastructure.Persistence.Repositories;
using DeomaBlog.Infrastructure.Persistence.Seed;
using DeomaBlog.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DeomaBlog.Infrastructure;

/// <summary>Composition root of the Infrastructure layer.</summary>
public static class DependencyInjection
{
    public const string DefaultConnectionString = "Data Source=blog.db";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string? connectionString = null)
    {
        var connection = string.IsNullOrWhiteSpace(connectionString) ? DefaultConnectionString : connectionString;

        services.AddDbContext<BlogDbContext>(options => options.UseSqlite(connection));

        services.AddScoped<IPostRepository, PostRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<DatabaseSeeder>();

        services.AddSingleton<IMarkdownRenderer, MarkdigRenderer>();
        services.AddSingleton<IClock, SystemClock>();

        return services;
    }
}
