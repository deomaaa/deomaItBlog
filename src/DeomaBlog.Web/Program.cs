using DeomaBlog.Application;
using DeomaBlog.Infrastructure;
using DeomaBlog.Infrastructure.Persistence;
using DeomaBlog.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Logging: structured console output — journald collects it on the VPS.
// ---------------------------------------------------------------------------
builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// ---------------------------------------------------------------------------
// Layers of the onion, wired from the outside in.
// ---------------------------------------------------------------------------
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration.GetConnectionString("Default"));

builder.Services.AddControllersWithViews(options =>
{
    // API-only: turns application exceptions into problem+json, never touches HTML pages.
    options.Filters.Add<DeomaBlog.Web.Filters.ApiExceptionFilter>();
});
builder.Services.AddResponseCompression(options => options.EnableForHttps = true);

// Built-in OpenAPI document (no third-party generator): served at /openapi/v1.json,
// human-readable docs at /api/docs.
builder.Services.AddOpenApi();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<BlogDbContext>("sqlite");

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

builder.Services.AddProblemDetails();

var app = builder.Build();

// ---------------------------------------------------------------------------
// Pipeline
// ---------------------------------------------------------------------------
app.UseForwardedHeaders();   // must be first: nginx terminates TLS and sets X-Forwarded-Proto

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseSerilogRequestLogging();
app.UseResponseCompression();
app.UseStatusCodePagesWithReExecute("/error/{0}");
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        var maxAge = app.Environment.IsDevelopment() ? TimeSpan.FromSeconds(30) : TimeSpan.FromDays(7);
        context.Context.Response.Headers.CacheControl = $"public,max-age={maxAge.TotalSeconds:0}";
    },
});

app.UseRouting();
app.UseAuthorization();

app.MapOpenApi();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Blog}/{action=Index}/{id?}");
app.MapControllers();
app.MapHealthChecks("/health");

// ---------------------------------------------------------------------------
// Migrate + seed on start-up (no-op when the database already has data).
// ---------------------------------------------------------------------------
await using (var scope = app.Services.CreateAsyncScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.InitializeAsync();
}

Log.Information(
    "DeomaBlog ready — {Environment} environment, content root {ContentRoot}.",
    app.Environment.EnvironmentName,
    app.Environment.ContentRootPath);

app.Run();

/// <summary>Exposed so the integration tests can boot the real pipeline with WebApplicationFactory.</summary>
public partial class Program;
