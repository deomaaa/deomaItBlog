# deomaBlog

> A developer blog that is also the résumé: **ASP.NET Core 10**, clean architecture,
> EF Core, REST API with a protected write side, tests, CI, and a dark terminal theme
> hand-written in CSS. Running in production on a bare Linux VPS behind nginx.

[![CI](https://github.com/deomaaa/deomaItBlog/actions/workflows/ci.yml/badge.svg)](https://github.com/deomaaa/deomaItBlog/actions/workflows/ci.yml)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)
![Tests](https://img.shields.io/badge/tests-62%20passing-brightgreen)
![License](https://img.shields.io/badge/license-MIT-blue)

**Live:** <https://deomaaa.87-199-201-108.sslip.io> · **API docs:** [/api/docs](https://deomaaa.87-199-201-108.sslip.io/api/docs)

---

## 👋 Hello, and thank you for stopping by

If you are a recruiter, a hiring manager or a developer reviewing my work — welcome. This
repository is the most honest thing I can hand you: not a tutorial clone, but an application I
designed, wrote, deployed and keep running, with the reasoning left in the code and in this
README. It grew out of a 2023 Razor Pages prototype (still visible in the git history) that I
rebuilt from scratch once I understood why the boundaries matter.

I am **Demid Andreev**, a .NET developer and a student at ETU "LETI". I like code that can be
read out loud: names that explain themselves, boundaries that mean something, and tests that
document behaviour instead of restating implementation. Everything below is written with that
in mind — including the parts I would do differently next time, which I list at the bottom
instead of hiding.

**The 60-second version:**

| I wanted to show | Where to look |
|---|---|
| Clean architecture that isn't 40 projects | [`src/`](src) — four projects, dependencies point inward, `Domain` has zero package references |
| Domain modelling with real invariants | [`Post.cs`](src/DeomaBlog.Domain/Posts/Post.cs), [`Slug.cs`](src/DeomaBlog.Domain/Posts/Slug.cs) |
| EF Core done deliberately | [`PostRepository.cs`](src/DeomaBlog.Infrastructure/Persistence/Repositories/PostRepository.cs), value converters, explicit join entity, migrations |
| A REST API with a read path *and* a protected write path | [`PostsApiController.cs`](src/DeomaBlog.Web/Controllers/Api/PostsApiController.cs) |
| Tests that catch regressions | [`tests/`](tests) — unit tests against hand-written fakes + integration tests over the real pipeline |
| Deployment skills, not just coding skills | [`deploy/`](deploy) — systemd unit, nginx vhost, certbot, health checks |
| That I can design a UI | [`style.css`](src/DeomaBlog.Web/wwwroot/css/style.css) and the Razor views |

---

## What the application does

- **Blog** — posts stored as markdown, rendered to HTML server-side (Markdig with HTML
  escaping disabled input, so a post body can never inject a script).
- **Archive with search, tags, sorting and pagination** — `/posts`, `/posts/{slug}`,
  `/posts?tag=csharp&q=async&sort=popular`.
- **Reading time and view counters** derived from the domain model, not typed by hand.
- **Page views + JSON API over the same use cases** — one application layer, two front doors.
- **Key-protected write API** — create, edit, publish, archive, delete posts (`X-Api-Key`).
- **OpenAPI document and interactive docs** — `/openapi/v1.json`, `/api/docs`.
- **Health endpoint** — `/health`, used by the deployment script and by systemd restarts.
- **Seeded content** — a fresh clone shows a real blog, not an empty page.

## Architecture

Four projects, one rule: **dependencies only ever point inward.**

```
              ┌──────────────────────────── DeomaBlog.Domain ────────────────────────────┐
              │  Entities & value objects: Post, Tag, PostTag, Slug, ReadingTime          │
              │  Invariants live here. No NuGet packages, no EF, no ASP.NET types.        │
              └────────────────────────────────▲──────────────────────────────────────────┘
                                               │
              ┌──────────────────────── DeomaBlog.Application ──────────────────────────┐
              │  Use cases (BlogQueryService, BlogAuthoringService), DTOs, filters,     │
              │  and the ports: IPostRepository, ITagRepository, IUnitOfWork, IClock,   │
              │  IMarkdownRenderer. Depends on Domain only.                             │
              └────────────────────────────────▲──────────────────────────────────────────┘
                                               │
   ┌──────────── DeomaBlog.Infrastructure ─────┴──────────────┐
   │  EF Core + SQLite, repositories, migrations, seeding,     │
   │  Markdig renderer, system clock. Implements the ports.    │
   └──────────────────────────▲────────────────────────────────┘
                              │
   ┌──────────────── DeomaBlog.Web ────────────────────────────┐
   │  Program.cs (composition root), MVC controllers + Razor    │
   │  views, JSON API, API-key filter, Serilog, OpenAPI.        │
   └───────────────────────────────────────────────────────────┘
```

| Project | Responsibility | Depends on |
|---|---|---|
| `DeomaBlog.Domain` | Entities, value objects, invariants | — |
| `DeomaBlog.Application` | Use cases, DTOs, ports | Domain |
| `DeomaBlog.Infrastructure` | EF Core, SQLite, markdown, clock | Application, Domain |
| `DeomaBlog.Web` | HTTP surface: pages + API | all of them |
| `DeomaBlog.UnitTests` | Domain & application behaviour | Domain, Application |
| `DeomaBlog.IntegrationTests` | The real pipeline over SQLite | Web |

**The rule I test in a review:** if a database change forces an edit inside `Domain`, a boundary
is broken. It catches most architecture drift in one question.

More detail, including trade-offs: [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md).

## Tech stack

| Area | Choice | Why |
|---|---|---|
| Framework | ASP.NET Core 10 (MVC + controllers) | Server-rendered pages are the right tool for a blog: fast first paint, real URLs, no JS build |
| Language | C# 14, nullable enabled, warnings as errors | The compiler is the cheapest reviewer |
| Persistence | EF Core 10 + SQLite | Single-file database, zero external services on a 1-CPU VPS; the repository boundary keeps PostgreSQL one registration away |
| Markdown | Markdig | Extensions for tables, task lists, footnotes; raw HTML disabled |
| Logging | Serilog | Structured console logs, collected by journald |
| API docs | Built-in OpenAPI + Swagger UI from CDN | No third-party generator in the dependency tree |
| Tests | xUnit + `WebApplicationFactory` | Unit tests for behaviour, integration tests for wiring |
| Front end | Hand-written CSS, ~20 lines of progressive-enhancement JS | The whole UI is readable in one sitting |

## The domain model in one screen

```
Post (aggregate root)                 Tag
 ├─ Title, Summary, ContentMarkdown    ├─ Name
 ├─ Slug        (value object)         └─ Slug (value object)
 ├─ Status      Draft|Published|Archived
 ├─ PublishedAt, UpdatedAt, ViewCount      PostTag (explicit join entity)
 ├─ ReadingTime (computed value object)     ├─ PostId
 └─ PostTag[] ─────────────────────────────►├─ TagId
                                            └─ future: isPrimary, order
```

- `Post.Publish(at)` refuses a default timestamp, `Archive` refuses to archive a draft, and
  `AssignTag` ignores duplicates — invalid posts cannot exist in memory.
- `Slug` slugs, validates and compares by value; it is persisted through an EF Core value
  converter, so the database stores a string and the code still writes `post.Slug`.
- `ReadingTime` is derived from the markdown on read, so it can never go stale after an edit.
- `PostTag` is a real entity instead of a skip navigation, so the join table can grow columns
  (ordering, "primary tag") without a breaking change.

## REST API

| Method | Route | Auth | Purpose |
|---|---|---|---|
| `GET` | `/api/posts?page&pageSize&tag&q&sort` | — | Paged, filtered archive |
| `GET` | `/api/posts/{slug}` | — | One post with rendered HTML |
| `GET` | `/api/posts/{slug}/related` | — | Posts sharing tags |
| `GET` | `/api/tags` | — | Tags with published post counts |
| `POST` | `/api/posts` | `X-Api-Key` | Create a draft (or publish immediately) |
| `PUT` | `/api/posts/{slug}` | `X-Api-Key` | Edit title, summary, body, tags |
| `POST` | `/api/posts/{slug}/publish` | `X-Api-Key` | Publish a draft |
| `POST` | `/api/posts/{slug}/archive` | `X-Api-Key` | Hide without deleting |
| `DELETE` | `/api/posts/{slug}` | `X-Api-Key` | Delete |
| `GET` | `/health` | — | Liveness + database probe |
| `GET` | `/openapi/v1.json` | — | OpenAPI contract |

Errors are RFC 7807 `application/problem+json`; the key comparison is constant-time, and the
write API answers `503` when no key is configured rather than silently allowing writes.

```bash
curl -s https://deomaaa.87-199-201-108.sslip.io/api/posts?pageSize=2 | jq '.items[].title'
```

## Run it locally

```bash
git clone https://github.com/deomaaa/deomaItBlog.git
cd deomaItBlog

dotnet restore
dotnet run --project src/DeomaBlog.Web        # http://localhost:5080
```

The SQLite file (`blog.dev.db`) and the demo posts are created on first start. Migrations live
in `src/DeomaBlog.Infrastructure/Persistence/Migrations`.

```bash
dotnet test                                   # unit + integration tests

# add a migration after changing the model
dotnet ef migrations add <Name> --project src/DeomaBlog.Infrastructure \
                                --output-dir Persistence/Migrations
```

### Write API locally

```bash
curl -X POST http://localhost:5080/api/posts \
  -H 'Content-Type: application/json' \
  -H 'X-Api-Key: local-development-key' \
  -d '{"title":"New post","summary":"Why this exists","contentMarkdown":"# Body","tags":["csharp"],"publishNow":true}'
```

## Deployment

The production instance runs on a single Linux VPS: Kestrel bound to `127.0.0.1`, nginx in
front for TLS, systemd for the process lifecycle. Everything needed is in [`deploy/`](deploy).

```
internet ──▶ nginx :443 (TLS, gzip, static caching) ──▶ Kestrel 127.0.0.1:5080 ──▶ SQLite
                                                              ▲
                                              systemd (Restart=always, hardened unit)
```

```bash
# 1. publish and restart (the script waits for /health)
sudo bash deploy/deploy.sh

# 2. first-time setup
sudo install -d -o www-data -g www-data /var/www/deomablog
sudo install -d -o www-data -g www-data -m 750 /var/lib/deomablog
sudo install -d -o root -g www-data -m 750 /etc/deomablog
sudo install -o root -g www-data -m 640 deploy/deomablog.env.example /etc/deomablog/deomablog.env
sudo install -m 644 deploy/deomablog.service /etc/systemd/system/deomablog.service
sudo cp deploy/deomablog.nginx.conf /etc/nginx/sites-available/deomablog
sudo ln -s /etc/nginx/sites-available/deomablog /etc/nginx/sites-enabled/
sudo systemctl daemon-reload && sudo systemctl enable --now deomablog
sudo certbot --nginx -d <your-domain> --redirect
```

Containers are supported too (`Dockerfile`, `docker-compose.yml`) — same app, same
configuration keys, for platforms where systemd is not the answer.

## How the quality is kept

- **62 tests** run in CI on every push: 48 unit tests over the domain and use cases
  (hand-written fakes instead of mocks, so the ports stay honest) and 14 integration tests that
  boot the real `Program.cs` over a throwaway SQLite file and assert HTTP behaviour, including
  `401` for an unauthenticated write and `404` as `problem+json`.
- **GitHub Actions** builds, tests with coverage, and publishes the artifact.
- **Warnings are errors**; nullable reference types are on everywhere.
- **Fail fast on start-up**: migrations are applied and the database is seeded before Kestrel
  accepts traffic.
- **Hardened service**: non-root user, `ProtectSystem=strict`, `PrivateTmp`, write access only
  to the database directory.
- **Logs that answer questions**: Serilog request logging with method, path, status and
  duration — enough to reconstruct an incident without re-deploying.

## Design decisions I would defend in an interview

1. **SQLite, not PostgreSQL.** A one-author blog serves reads from a single file with zero
   operational surface on a 1-CPU box. Because persistence is behind `IPostRepository`, moving
   to PostgreSQL is a package reference, a connection string and a second migration set — not a
   rewrite.
2. **No MediatR, no AutoMapper.** At this size an explicit service call is easier to follow in
   a debugger than a pipeline of handlers. Mapping is compiled code, not reflection.
3. **Manual mapping over AutoMapper** — `PostMapper` is nine lines and fails at compile time.
4. **Server-rendered Razor, not an SPA.** Content pages want HTML, not a hydration step; the
   only JavaScript is a copy button and a `/` search shortcut.
5. **Explicit `PostTag` join entity** instead of a skip navigation — the join table is where
   post ordering metadata will land.
6. **`IClock` instead of `DateTime.Now`** — time is an input, which is why `Publish` and
   `Archive` are testable without freezing the process.
7. **Read paths return DTOs, write paths return aggregates.** That is all the CQRS this project
   needs; full CQRS with separate stores would be architecture theatre here.

## Roadmap — what I am adding next

- [ ] Author authentication with ASP.NET Core Identity instead of a shared API key.
- [ ] RSS/Atom feed and Open Graph images per post.
- [ ] PostgreSQL profile with Docker Compose for a "real database" deployment story.
- [ ] Playwright smoke tests against the deployed environment.
- [ ] Full-text search (SQLite FTS5, or `tsvector` on the PostgreSQL profile).

## About me

**Demid Andreev** — .NET developer, student of ETU "LETI" (Department of Electrical Engineering
and Automation). I write about the language features I use, the architecture I try to keep
honest, and the mistakes that taught me the difference.

- GitHub: <https://github.com/deomaaa>
- Telegram: <https://t.me/deoooma>
- Email: <dmdndrv@gmail.com>

Reviews, questions and pull requests are welcome — if something here is wrong, I would rather
hear it than keep it.

## License

[MIT](LICENSE) © 2026 Demid Andreev
