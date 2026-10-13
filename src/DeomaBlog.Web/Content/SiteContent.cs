using DeomaBlog.Web.Models;

namespace DeomaBlog.Web.Content;

/// <summary>
/// Hand-written page content (about, projects, experience). Keeping it in C# instead of a CMS
/// is a deliberate scope decision for a one-author portfolio: it is versioned with the code,
/// needs no admin UI, and is trivial to edit in a pull request.
/// </summary>
public static class SiteContent
{
    public const string AuthorName = "Demid Andreev";
    public const string AuthorRole = ".NET developer";
    public const string Email = "dmdndrv@gmail.com";
    public const string Telegram = "https://t.me/deoooma";
    public const string GitHub = "https://github.com/deomaaa";
    public const string Repository = "https://github.com/deomaaa/deomaItBlog";
    public const string University = "https://etu.ru/";

    public static IReadOnlyList<string> Stack { get; } =
    [
        "C# / .NET 10",
        "ASP.NET Core MVC + REST",
        "Entity Framework Core",
        "SQLite / SQL Server",
        "Clean Architecture, SOLID",
        "LINQ, async/await",
        "HTML, CSS, JavaScript",
        "Git, GitHub Actions",
        "nginx, systemd, Linux",
        "xUnit, integration testing",
    ];

    public static IReadOnlyList<ProjectCardViewModel> Projects { get; } =
    [
        new(
            "DeomaBlog",
            "This blog. A clean-architecture ASP.NET Core application with a markdown backed " +
            "post store, read API, key-protected write API and a hand-written dark theme.",
            ".NET 10 · EF Core · SQLite · xUnit · nginx",
            "https://github.com/deomaaa/deomaItBlog"),
        new(
            "events_microservices-api",
            "REST API for an event platform: participants, venues and schedules split across " +
            "services with their own data stores.",
            "C# · ASP.NET Core · microservices",
            "https://github.com/deomaaa/events_microservices-api"),
        new(
            "API_excel",
            "Service that imports and exports spreadsheet data, with validation and reporting " +
            "over the imported rows.",
            "C# · .NET · Excel interop",
            "https://github.com/deomaaa/API_excel"),
        new(
            "StarCraft-2-Vue.js",
            "A small SPA that renders StarCraft II unit statistics as interactive cards and " +
            "comparison tables.",
            "Vue.js · JavaScript · CSS",
            "https://github.com/deomaaa/StarCraft-2-Vue.js"),
        new(
            "SellCarWebSite",
            "Responsive multi-page marketing layout: flexbox grids, sticky header and a " +
            "mobile-first breakpoint set.",
            "HTML · CSS",
            "https://github.com/deomaaa/SellCarWebSite"),
        new(
            "GradientGeneratorCss",
            "Tiny tool that generates CSS gradient snippets and copies them to the clipboard.",
            "JavaScript · CSS",
            "https://github.com/deomaaa/GradientGeneratorCss"),
    ];

    public static IReadOnlyList<TimelineEntryViewModel> Experience { get; } =
    [
        new(
            "2023 — today",
            "Backend developer, self-directed projects",
            "Pet projects published on GitHub",
            [
                "Design and ship .NET services end to end: domain model, REST API, persistence, tests, deployment.",
                "Maintain this blog: architecture, content pipeline and the VPS it runs on.",
                "Rebuilt a Razor Pages prototype into a layered ASP.NET Core application to practise clean boundaries.",
            ]),
        new(
            "2022 — today",
            "Student, ETU \"LETI\"",
            "Department of Electrical Engineering and Automation",
            [
                "Algorithms, data structures and the mathematics behind them.",
                "Coursework in C#, databases and computer networks.",
                "Team projects: git workflow, code review, task breakdown.",
            ]),
        new(
            "2022 — today",
            "Learning in public",
            "GitHub · Telegram",
            [
                "Write posts explaining .NET concepts with runnable examples.",
                "Read other people's code and review pull requests to sharpen design judgement.",
            ]),
    ];
}
