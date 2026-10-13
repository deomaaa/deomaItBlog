# Architecture

This document explains the shape of the codebase and the reasoning behind each boundary.
If you only read one section, read [The dependency rule](#the-dependency-rule).

## The dependency rule

```
Domain  ◀──  Application  ◀──  Infrastructure  ◀──  Web
   ▲                ▲
   └────────────────┴──── tests reference inward only
```

- A project may reference projects to its **left**, never to its right.
- `DeomaBlog.Domain` has **no** `PackageReference` entries. Not EF Core, not ASP.NET, not
  even a logging library. Its only dependency is the base class library.
- `DeomaBlog.Application` knows the domain and defines the ports (`IPostRepository`,
  `ITagRepository`, `IUnitOfWork`, `IClock`, `IMarkdownRenderer`) that the outside world must
  implement.
- `DeomaBlog.Infrastructure` implements those ports with EF Core, SQLite, Markdig and the
  system clock. It also holds migrations and seeding.
- `DeomaBlog.Web` is the composition root: it wires everything, maps HTTP to use cases, and
  owns view models, controllers, filters and static assets.

**The test that keeps it honest:** if a change to the database schema forces an edit inside
`Domain`, the boundary is broken.

## Why an onion and not layers-by-type

A "Controllers / Services / Repositories" layout puts the reason for a change in three folders
at once. The onion groups code by the *reason it changes*:

| Layer | Changes when |
|---|---|
| Domain | the business rules of a blog change (a post gains a status, a slug rule changes) |
| Application | a use case changes (posts can be filtered by tag, a draft can be scheduled) |
| Infrastructure | a technical choice changes (SQLite → PostgreSQL, Markdig → another renderer) |
| Web | the presentation or the contract changes (a page, an endpoint, a view model) |

## Read path vs write path

The two sides of the blog are deliberately different:

- **Reads** (`BlogQueryService`) return **DTOs**. They never materialise an aggregate for the
  caller to mutate, and they can be shaped for the database (paging, projection) without
  leaking persistence concerns upward.
- **Writes** (`BlogAuthoringService`) work with the **aggregate**. Every change goes through
  methods that enforce invariants, and the unit of work is saved explicitly.

This is the amount of CQRS that pays off at this size. Separate read models, event buses and
projections would be architecture theatre for a blog with six posts — the README says so, and
that is intentional: choosing *not* to add a pattern is also a decision worth explaining.

## Domain model notes

### Value objects

`Slug` and `ReadingTime` are immutable, validate in their factories, and compare by value.
`Slug` is persisted through an EF Core value converter:

```csharp
builder.Property(post => post.Slug)
    .HasConversion(slug => slug.Value, value => Slug.Create(value));
```

The database column is a plain `TEXT`; the code reads and writes a `Slug`. Because EF Core
materialises entities through `Slug.Create`, a row with an invalid slug throws instead of
silently producing a broken object.

> Implementation detail worth knowing: in-memory comparisons use `Equals`, while LINQ queries
> use `==`. A user-defined `operator ==` on a value object cannot be translated by EF Core, so
> the domain code (`Post.RemoveTag`, `Post.AssignTag`) calls `Equals` explicitly.

### Aggregate boundaries

`Post` is the only aggregate root that the application layer mutates. `Tag` is created and
resolved through `ITagRepository`, and the relationship is an explicit `PostTag` entity so the
join table can grow columns later without a breaking change.

### Time

`IClock` is a port, and `SystemClock` is its only production implementation. `Post.Publish`
requires an explicit timestamp and rejects `default` — a test cannot accidentally publish a
post in the year 1.

## Persistence

- **Provider:** `Microsoft.EntityFrameworkCore.Sqlite`. Swapping to PostgreSQL means a package
  reference, a different `Use*` call and its own migration set; nothing in `Application` or
  `Domain` changes.
- **Query hygiene:** list queries go through `Query(noTracking: true)`. Tracking is opt-in and
  reserved for the write path.
- **Indexing:** unique index on `slug`, composite index on `(status, published_at)` for the
  feed query.
- **Migrations:** generated into `Persistence/Migrations` and applied on start-up, so a deploy
  cannot ship code that expects a schema the database does not have yet.

## Web layer

- `Program.cs` is the composition root: Serilog, layers, health checks, forwarded headers,
  OpenAPI, the API filter, static-file caching, and a start-up block that migrates and seeds.
- The `ApiExceptionFilter` turns `NotFoundException` into `404` and `DomainException` into
  `400` — **only** for `/api/*` requests. HTML pages keep the `/error` re-execution pipeline,
  so a browser never receives a JSON body for a broken view.
- `RequireApiKeyAttribute` guards the write endpoints with a constant-time comparison and
  returns `503` when no key is configured (fail closed, not open).
- `UseForwardedHeaders()` runs first because nginx terminates TLS; without it, generated links
  and redirects would say `http://`.

## Testing strategy

| Level | What it covers | Why it exists |
|---|---|---|
| Unit (`DeomaBlog.UnitTests`) | Domain invariants, use cases, paging, slug uniqueness | Fast feedback; hand-written fakes keep the ports honest |
| Integration (`DeomaBlog.IntegrationTests`) | The real `Program.cs` over a throwaway SQLite file | Catches wiring, routing, model-binding, DI and EF translation bugs that unit tests cannot see |

The fakes are written by hand rather than generated by a mocking framework on purpose: if a
port is awkward to fake, that is a design smell worth noticing early.

## Known trade-offs

- **No authentication for humans.** The write API uses a shared key; a real editorial workflow
  needs ASP.NET Core Identity (roadmap item).
- **View counter writes on every page view.** Fine at this traffic level; a queue or a
  batched update would be the next step.
- **Search is `LIKE`-based.** Correct and portable, but not indexed full-text search — SQLite
  FTS5 or PostgreSQL `tsvector` is the planned upgrade.
- **No output caching.** Response compression and static-file caching cover the current load;
  `OutputCache` would be the next lever.
