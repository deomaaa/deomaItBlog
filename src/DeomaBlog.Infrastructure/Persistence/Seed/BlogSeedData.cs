namespace DeomaBlog.Infrastructure.Persistence.Seed;

/// <summary>
/// Content shipped with the project so a fresh clone is not an empty page.
/// Kept as plain C# records: no JSON file to parse, refactor-safe, and easy to diff.
/// </summary>
public static class BlogSeedData
{
    public sealed record SeedPost(
        string Title,
        string Summary,
        string ContentMarkdown,
        IReadOnlyList<string> Tags,
        int PublishedDaysAgo,
        int ViewCount);

    public static IReadOnlyList<SeedPost> Posts { get; } =
    [
        new(
            "Clean Architecture without the ceremony: four projects, one direction",
            "How DeomaBlog is sliced into Domain, Application, Infrastructure and Web — and the single rule that keeps the dependency arrows pointing one way.",
            """
            Most "clean architecture" repositories I have read are 40 projects deep and impossible to
            navigate in a code review. This blog does it with four projects and one rule: **dependencies
            point inward, and only inward.**

            ```
            Web ─────────► Application ─────────► Domain
             │                  ▲
             └──► Infrastructure┘
            ```

            ## What lives where

            | Project | Owns | Knows about |
            |---|---|---|
            | `DeomaBlog.Domain` | `Post`, `Tag`, `Slug`, `ReadingTime` | nothing |
            | `DeomaBlog.Application` | use cases, DTOs, ports (`IPostRepository`, `IClock`) | Domain |
            | `DeomaBlog.Infrastructure` | EF Core, SQLite, Markdig | Application |
            | `DeomaBlog.Web` | MVC controllers, Razor views, REST API | all three |

            The important part is not the four folders — it is that `Domain` has zero package
            references. No EF attributes, no JSON serializers, no ASP.NET types. If I ever move the
            storage to PostgreSQL or expose the same use cases through gRPC, the inner two projects
            do not change a single line.

            ## Where the boundaries actually earn their keep

            Two concrete examples from this codebase:

            1. **`ReadingTime` is a value object, not a database column.** It is derived from the
               markdown body, so it can never go stale after an edit. The domain computes it, the UI
               only displays it.
            2. **`IPostRepository` returns aggregates; `IBlogQueryService` returns DTOs.** Write paths
               work with entities and invariants, read paths skip the aggregate entirely and let SQL
               do the shaping. That is the whole of CQRS I need here — no mediator, no buses.

            ## The rule I would defend in a review

            If a change to the database schema forces an edit inside `DeomaBlog.Domain`, the boundary
            is broken. That single test catches 90% of architecture drift, and it is cheap to apply.
            """,
            ["architecture", "dotnet", "csharp"],
            24,
            412),

        new(
            "IEnumerable vs IQueryable: the one-word difference that hits your database",
            "Deferred execution is a feature until the wrong interface lets LINQ pull 50 000 rows into memory.",
            """
            Both interfaces defer execution. Only one of them defers it **to the server**.

            ```csharp
            // IQueryable: EF Core translates this into SQL
            IQueryable<Post> query = context.Posts.Where(p => p.Status == PostStatus.Published);

            // IEnumerable: this one runs on the client, after the rows are already in memory
            IEnumerable<Post> loaded = context.Posts;
            var published = loaded.Where(p => p.Status == PostStatus.Published);   // 😐
            ```

            ## Why it bites quietly

            `IEnumerable.Where` is a plain .NET delegate call, so every row has to be materialised
            first. Tests pass — they always pass on 200 rows — and production starts swapping. The
            bug is invisible in a code review because both lines look identical.

            ## The rule I use now

            - Keep `IQueryable<T>` inside the data-access layer, and never return it across a boundary.
              An `IQueryable` escaping a repository is a query builder leaking into your UI.
            - Return `IReadOnlyList<T>` or a paged result from repositories — in this project
              `GetPagedAsync` returns `PagedResult<Post>` with the total count, so the caller cannot
              accidentally enumerate twice.
            - Use `IEnumerable<T>` only when the data is already local (in-memory collections,
              projections you made on purpose).

            ## Bonus: `AsNoTracking` is the other half of the story

            Read-only queries in this project go through a `Query(noTracking: true)` helper. Tracking
            is not free: EF Core keeps a snapshot of every entity it materialises. For lists rendered
            on a blog page, tracking is pure overhead.
            """,
            ["csharp", "ef-core", "performance"],
            18,
            388),

        new(
            "Records vs classes in C#: choosing on purpose, not by habit",
            "Positional records are not \"better classes\". Here is the checklist I run before adding the word record.",
            """
            `record` gives you value equality, a compiler-generated `ToString`, `with` expressions and
            deconstruction. Those are excellent — for the right kind of type.

            ## Use a record when…

            - the type is a **value**: `Money`, `DateRange`, `Slug`, `PostSummaryDto`;
            - it is immutable by design and "changed" by copying;
            - two instances with the same fields genuinely *are* the same thing.

            ```csharp
            // Value object with a guard: invalid instances cannot be constructed
            public sealed partial record ReadingTime
            {
                public static ReadingTime FromMinutes(int minutes) =>
                    new(Math.Clamp(minutes, 1, 600));
            }
            ```

            ## Use a class when…

            - the type has **identity** that outlives its fields: `Post`, `User`, `Order`;
            - it owns invariants that change over time (`post.Publish(...)`, `post.Archive(...)`);
            - you rely on reference semantics or on EF Core change tracking.

            In this codebase `Post` is a class with private setters and a private constructor: the
            only way to get a published post is to call `Publish`, and that method will not accept a
            default timestamp. `PostSummaryDto`, `PostFilter` and `Slug` are records — nobody should
            be able to mutate a DTO mid-flight.

            ## The equality trap

            `record` equality walks every member. On a mutable entity full of navigation properties
            that is a slow, surprising `Equals`. If you catch yourself overriding `Equals` on a record,
            the type probably wanted to be a class from the beginning.

            ## Practical checklist

            1. Does it have an identity? → class.
            2. Do I want `with` expressions? → record.
            3. Is it a DTO, a filter, an event, a value? → record.
            4. Does it mutate through methods that enforce rules? → class.
            """,
            ["csharp", "dotnet"],
            12,
            301),

        new(
            "async/await: five mistakes I kept repeating until I wrote them down",
            "Blocking on async code, forgetting CancellationToken, and the subtle cost of async void in request pipelines.",
            """
            Every one of these cost me real debugging time.

            ## 1. `.Result` and `.Wait()` inside a request

            ```csharp
            var html = renderer.ToHtmlAsync(markdown).Result;   // 🚫 deadlock and/or thread starvation
            ```

            If the API is async, the call chain is async. Full stop. The only synchronous boundary in
            an ASP.NET Core app is the entry point.

            ## 2. Forgetting `CancellationToken`

            A cancelled request should stop doing work. Threading the token through repositories,
            EF Core calls and HTTP clients is a one-line change per method:

            ```csharp
            public Task<Post?> GetBySlugAsync(Slug slug, bool includeUnpublished = false,
                CancellationToken cancellationToken = default)
                => query.FirstOrDefaultAsync(post => post.Slug == slug, cancellationToken);
            ```

            Every EF Core query in this project takes a token. It is boring, and boring is the point.

            ## 3. `async void`

            `async void` exists for event handlers, not for services. Exceptions thrown in it escape
            the `Task` and land in `UnobservedTaskException` — or crash the process. Return `Task`.

            ## 4. Not awaiting inside `using`

            ```csharp
            using var client = new HttpClient();
            await client.GetStringAsync(url);   // ✅ sequential by design
            ```

            Note that for a *blog* this is fine; for parallel fan-out, `Task.WhenAll` plus independent
            `DbContext` instances (or `IDbContextFactory`) is the way — a single scoped `DbContext`
            is not thread-safe.

            ## 5. Believing async makes code faster

            Async makes a server *scalable*, not a single request faster. On a blocking, CPU-bound
            workload it only adds state machines. Use it for I/O, which is where a web app spends
            99% of its wall time.
            """,
            ["csharp", "async", "dotnet"],
            8,
            256),

        new(
            "Shipping a .NET app to a bare VPS: systemd, nginx, Let's Encrypt",
            "No Docker, no PaaS, no magic. A Kestrel service on 127.0.0.1, an nginx reverse proxy and a certificate that renews itself.",
            """
            This blog runs on a 1-CPU VPS. The whole deployment is four files.

            ## 1. Publish

            ```bash
            dotnet publish src/DeomaBlog.Web -c Release -o /var/www/deomablog
            ```

            ## 2. Run it as a systemd service

            ```ini
            [Unit]
            Description=DeomaBlog web app
            After=network.target

            [Service]
            WorkingDirectory=/var/www/deomablog
            ExecStart=/usr/bin/dotnet /var/www/deomablog/DeomaBlog.Web.dll
            Environment=ASPNETCORE_URLS=http://127.0.0.1:5080
            Environment=ASPNETCORE_ENVIRONMENT=Production
            Restart=always
            User=www-data

            [Install]
            WantedBy=multi-user.target
            ```

            Kestrel binds `127.0.0.1` on purpose: nothing but nginx should reach it directly.

            ## 3. Reverse proxy with nginx

            ```nginx
            server {
                listen 80;
                server_name blog.example.com;

                location / {
                    proxy_pass http://127.0.0.1:5080;
                    proxy_set_header Host $host;
                    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
                    proxy_set_header X-Forwarded-Proto $scheme;
                }
            }
            ```

            `UseForwardedHeaders()` in `Program.cs` is what makes `X-Forwarded-Proto` produce correct
            `https://` links and 301 redirects behind the proxy.

            ## 4. Certificates

            ```bash
            certbot --nginx -d blog.example.com --redirect
            ```

            Certbot edits the vhost, adds the 443 block and installs a renewal timer. `sudo certbot
            renew --dry-run` once, and forget about it.

            ## What I deliberately skipped

            Docker on a single-box deployment adds an image registry, a build pipeline and a volume
            story for zero operational gain — a `git pull && dotnet publish && systemctl restart` is
            one command away. The `Dockerfile` in this repo exists so that the same app can move to a
            container platform later without touching the source.
            """,
            ["dotnet", "devops", "linux"],
            4,
            197),

        new(
            "Reading code like prose: what 300 lines of someone else's pull request taught me",
            "Rubber-duck the diff, follow the data, and ask why before how — a checklist for reviewing unfamiliar C#.",
            """
            I review code slower than I write it, and that turned out to be the useful half of the
            skill.

            ## Start from the data, not the files

            In an unfamiliar .NET solution I look for the model first. Entities and DTOs tell me what
            the system believes about the world; controllers and services are just the plumbing
            around those beliefs. In this project that is a five-minute read: `Post`, `Tag`, `Slug`,
            `ReadingTime`.

            ## Read the diff bottom-up

            Commit messages lie, comments rot, but the call graph does not. If `BlogAuthoringService`
            calls `IPostRepository.SlugExistsAsync` before creating a post, that is where the
            interesting decision lives — not in the controller.

            ## Ask "why here?"

            Every line of code had a reason. `DisableHtml()` in the markdown pipeline is not a random
            flag: it is the XSS boundary. `AsNoTracking()` on list queries is not noise: it is why a
            page render does not keep snapshots of twenty entities. When I cannot explain a line's
            reason, that is my review comment — not a style nit.

            ## Three questions I always ask in a review

            1. **What fails here, and how loudly?** (a caught exception with no log is a bug)
            2. **What happens on the second request?** (idempotency, caching, slug uniqueness)
            3. **Who deletes this data, and what breaks?** (cascade rules, orphan rows)

            ## Why this matters more than syntax

            Naming and formatting can be fixed by a tool in one commit. Understanding is the part
            that does not automate, and it is the part a team is actually paying a reviewer for.
            """,
            ["career", "csharp"],
            1,
            88),
    ];
}
