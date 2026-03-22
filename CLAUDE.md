# Project Instructions

## Stack
- .NET 9, ASP.NET Core Web API, Entity Framework Core 9, PostgreSQL (Npgsql)
- xUnit + EF Core InMemory for tests
- No authentication (public API)

## Clean Code Rules

### General
- Follow SOLID principles: single responsibility, no god classes or methods
- DRY — extract shared logic; no copy-paste code
- YAGNI — only build what is asked for; no speculative features
- Methods do one thing and are named for what they do, not how they do it
- Max ~30 lines per method; if longer, extract
- No magic numbers or strings — use named constants

### C# / .NET Conventions
- File-scoped namespaces (`namespace Foo.Bar;`)
- Use `var` when the type is obvious from the right-hand side
- Prefer `async/await` throughout; no `.Result` or `.Wait()`
- Use `string.Empty` instead of `""`
- Nullable reference types enabled; no `!` suppression unless justified with a comment
- Use `record` for immutable value objects; `class` for entities and services

### Architecture
- Controllers handle HTTP only — no business logic
- All business logic lives in services
- Services depend on interfaces, not concrete classes
- DTOs are separate from EF entities — never expose entities directly in API responses
- Validation lives on DTOs (Data Annotations), not on entities

### EF Core
- Use `FindAsync` for PK lookups; use LINQ projections with `Select` for read queries
- Always project to DTOs in queries — do not load full entity graphs unless needed
- Explicit relationship configuration in `OnModelCreating`, not implicit conventions
- Seed data via `HasData()` with fixed IDs

### Testing
- Follow TDD: write failing test → implement → pass
- Unit tests for service logic; integration tests for controllers via `WebApplicationFactory`
- Use `IClassFixture` for shared factory; unique `Guid` database name per test to isolate state
- Tests assert behavior, not implementation details
- Test names: `MethodName_Scenario_ExpectedResult`

### Error Handling
- Use exceptions to signal domain errors: `KeyNotFoundException` for not-found, `InvalidOperationException` for business rule violations
- Controllers catch domain exceptions and map them to HTTP status codes using `Problem()`
- All error responses follow RFC 7807 ProblemDetails format

### What NOT to do
- No inline SQL or raw queries unless EF cannot express it
- No `[Required]` on non-nullable value types (`int`, `bool`) — use `[Range]` instead
- No committing secrets or connection string passwords to source control
- No swallowing exceptions silently
- No `Thread.Sleep` or blocking async code
