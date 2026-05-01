---
tags: [claude, project-template]
---

# Project: flibuget

> Get and read write info about audio books

## Stack
- .NET 10 / C# 14
- EF Core last (writes) + Dapper (reads)
- PostgreSQL \<VERSION>
- Serilog -> file,json
- xUnit, Autofixture, NSubstitute, FluentAssertions, Testcontainers

## Solution layout
- `src/Domain/`              — entities, value objects, domain services. **No external deps.**
- `src/ApplicationServices/` — use cases (one folder per feature), `*Request` / `*Response` DTOs
- `src/Infrastructure/`      — EF DbContext, Dapper, HTTP clients, brokers, file I/O
- `src/Api/`                 — composition root, controllers, middleware, hosted services
- `tests/Unit/`              — xUnit, no external deps
- `tests/Integration/`       — Testcontainers for DB / broker

Composition root: `src/Api/Program.cs` + `src/Api/CompositionRoot.cs`.

## Commands
| Action          | Command                                                                                  |
|-----------------|------------------------------------------------------------------------------------------|
| Build           | `dotnet build`                                                                           |
| Format          | `dotnet format`                                                                          |
| Unit tests      | `dotnet test --filter Category=Unit`                                                     |
| Integration     | `dotnet test --filter Category=Integration`                                              |
| Run API         | `dotnet run --project src/Api`                                                           |
| Add migration   | `dotnet ef migrations add <Name> --project src/Infrastructure --startup-project src/Api` |
| Apply migration | `dotnet ef database update --project src/Infrastructure --startup-project src/Api`       |

## Hard rules (non-negotiable)
1. **`Result<T>`** for expected failures. Never throw for business logic.
2. **Primary constructors** for DI. No `_field = ctor param` boilerplate.
3. **Structured logging**: `logger.LogX("msg {Placeholder}", value)`. No interpolation in `LogX` calls.
4. **`IFileSystem`** abstraction for file I/O. No direct `File.*` outside `Program.cs`.
5. **`CancellationToken`** propagated through every async chain.
6. **NRT enabled**, no `!` operator without justification comment.
7. **Domain layer** cannot reference Infrastructure, Api, or external packages (except `CSharpFunctionalExtensions` and validation libs).
8. **Parameterized SQL only.** No string concat into queries.

Full standards: [[dotnet-standards]] (`@.claude/docs/dotnet-standards.md`)

## Do not touch
- `src/Legacy/`         — frozen, scheduled for removal in \<quarter>
- `**/*.Generated.cs`   — regenerated from OpenAPI / EF
- `db/migrations/`      — append-only, never edit existing migrations

## Known antipatterns in this codebase (being migrated)
<!-- Add as discovered. Remove when fixed. -->
- \<Example> `OldOrderService` still throws — migration tracked in #1234
- \<Example> Direct `DateTime.Now` in 3 files — replace with `IClock` when touched
- \<Example> Some controllers return domain entities directly — wrap in DTO when touched

## Project-specific conventions
- API routes: `kebab-case` lowercase, plural resources
- DTO suffix: `Request` / `Response`, never `Dto`
- Database: `snake_case`, mapped via EF naming convention
- Time: always UTC, store as `timestamptz`
- IDs: strongly-typed (`OrderId`, `CustomerId`), not raw `Guid`

## When generating code here
- Read [[dotnet-standards]] (`@.claude/docs/dotnet-standards.md`) before non-trivial changes
- Use Plan mode for changes touching multiple layers
- Add a unit test alongside any new domain or application service method
- Run `/review` before declaring a task done
