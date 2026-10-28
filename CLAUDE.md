# Advisor Quotes: Claude Code instructions

Take-home assessment: an "Advisor Quotes" API in C# / .NET 10 + PostgreSQL. Brief summary and requirement mapping: `README.md` and `SOLUTION.md`. Data flows: `docs/data-flows.md`.

## Working rules (every session, including worktree sub-sessions)

1. **Assumptions.** When you resolve anything the brief leaves open, add a row to `docs/ASSUMPTIONS.md` (next `A-NN`) **and** update the README "Assumptions" section in the same commit.
2. **Decisions.** When you pick between real alternatives, add a row to `docs/DECISIONS.md` (next `D-NN`). README "Key decisions" holds only the top 3 to 5; do not bloat it.
3. **Prompts** are logged automatically to `docs/ai/PROMPTS.md` by the `UserPromptSubmit` hook. Do not edit past entries.
4. **README stays runnable.** If you change how to run, configure or test anything, update README "How to run it" in the same commit.
5. **Stay in your lane.** Parallel sessions own separate folders (see "Session ownership"). Do not edit another session's files; if you need a contract change, change it minimally in `Contracts/` or `Business.Abstractions/` and say so in your final report.
6. **No references to other companies' internal codebases** anywhere in this repo.
7. **One commit per flow, as you build.** Each vertical flow (endpoint → service → repository → EF → data, plus its tests) is its own commit, e.g. `Add create quote flow`, `Add quote status change flow`. Supporting pieces outside a flow (a calculator, a middleware) get their own commit too. Never one large commit at the end. **A flow is not committed until it is validated and unit tested:** its request has a FluentValidation validator run first in the service, and the commit includes unit tests for the validator, the service (happy path, each rule breach, and invalid input throwing before any repository call) and the controller's status codes. `dotnet build` and `dotnet test Tests.Unit` pass before every commit. Subject: short, precise, imperative, under 60 characters; body only when the why is not obvious. Docs the flow changes (assumptions, decisions, README) go in the same commit. Commit identity for this repo is local config (`Njongo Villo <njongo.villo@gmail.com>`); do not change it. **Commit messages and PR descriptions contain no AI or tool attribution**: no `Co-Authored-By` trailers, no "Generated with" lines, no mention of Claude or Anthropic. Merge commits are named after the work (`Merge pricing engine`), never after a tool-generated branch name; check `git log --format=%B origin/main..HEAD` for these words before every push.
8. Before finishing: `dotnet build AdvisorQuotes.sln` and `dotnet test Tests.Unit` pass; inspect `git diff`.

## Architecture

```
HTTP → Controller (API) → Service (Business) → Repository (Data.SQL) → QuotesDbContext → PostgreSQL
                              ↘ IPremiumCalculator (Pricing, no dependency on quotes)
```

| Project | Owns |
|---|---|
| `API` | Controllers, exception filter, DI modules (`Middleware/DependencyInjection/<Module>ModuleServiceCollection.cs`), observability, startup migration |
| `Business` | Services, status machine, fingerprinting, mappers, `Validators/ValidatorExtensions.cs` |
| `Business.Abstractions` | Domain models, service and repository interfaces, exceptions, options, `Telemetry/QuoteTelemetry` |
| `Pricing` | `IPremiumCalculator`, `IProductCatalog`, `PricingOptions`. Must never reference other projects |
| `Contracts` | Request/response DTOs and their FluentValidation validators (the API contract the UI codes against) |
| `Data.SQL` | `QuotesDbContext`, `EF/Configurations`, `Migrations`, `Repositories` |

## API conventions

- Controllers derive from `API/Controllers/Public/BaseController` (`[ApiController]`, `[ApiExceptionHandler]`, route `api/v{version:apiVersion}/[controller]`) and carry `[ApiVersion("1.0")]`. Folder: `API/Controllers/Public/v1/<Module>/`.
- **Controllers are thin:** bind the body, read `AdviserId` / `IdempotencyKey` from the base controller, call **one** service method, map to HTTP. No validation, no loops, no orchestration.
- Every action declares `[ProducesResponseType]` for each status it can return.
- Routes: nouns, `{identifier:guid}` constraint, named GET route used by `CreatedAtRoute`. Search is `POST <controller>/search` with a body.
- Response DTOs never expose `Id`, `IdempotencyKey` or `RequestFingerprint`.

### Status codes

| Code | When | How |
|---|---|---|
| 200 | Get, search, update returning the DTO; idempotent replay of a create | `Ok(...)` (+ `Idempotent-Replayed: true` header on replay) |
| 201 | Create | `CreatedAtRoute("GetQuote", new { version = "1", identifier }, dto)` (gives `/api/v1/...`, see D-12) |
| 400 | Validation failure, missing `X-Adviser-Id`, business rule breach (bad transition, product limits) | throw `BadRequestException` |
| 404 | Unknown identifier, or one owned by another adviser (never 403) | `NotFound()` on a null get, else throw `NotFoundException` |
| 409 | Idempotency-Key reused with a different body; optimistic concurrency clash | throw `ConflictException` |
| 500 | Anything unexpected | filter logs it and returns a generic message |

Error body is always `ErrorResponse { statusCode, message, errors[{ field, message }] }`.

## Validation

- Every request DTO has a validator in `Contracts/API/Public/<Module>/Requests/Validators/`, registered explicitly in the module's DI file (no assembly scan).
- Validators are injected into the **service** (`IValidator<T>`), never the controller. First line of the service method: `await validator.ValidateOrThrowAsync(request, cancellationToken);`
- Rules that need the database or the pricing catalogue run in the service after validation.

## Data access

- EF Core LINQ, `AsNoTracking()` for reads, snake_case naming (EFCore.NamingConventions), adviser scoped queries.
- `Guid Id` internal key, `Guid Identifier` public id, `CreatedAt` / `UpdatedAt` (UTC) on every row.
- History rows are written in the **same transaction** as the change they record.
- Raw SQL only where LINQ cannot express it (the advisory lock); comment why.
- New migration: `dotnet ef migrations add <Name> --project Data.SQL --startup-project API --output-dir Migrations`. Always reversible.

## Observability

- Start spans from `QuoteTelemetry.ActivitySource` using the names and tag keys in `QuoteTelemetry.Spans` / `.Tags`. Exporter wiring lives in `API/Observability` only.
- Log with message templates and named properties (`{QuoteIdentifier}`), never string interpolation. No client names or dates of birth in logs or span tags.

## Tests

- xUnit + Moq. Names `Method_Scenario_Expected`, Arrange / Act / Assert comments.
- `Tests.Unit`: validators, pricing, status machine, services (mock repositories), filter. Each service method has a test proving invalid input throws before any repository call (`Times.Never`).
- `Tests.Integration`: real PostgreSQL. Uses `TEST_POSTGRES_CONNECTION` if set, else Testcontainers if Docker is available, else skips.

## Session ownership (Phase 1)

| Session | Owns |
|---|---|
| Pricing engine | `Pricing/**`, `API/Controllers/Public/v1/Products/**`, `PricingModuleServiceCollection.cs`, `Tests.Unit/Pricing/**`, `Tests.Unit/API/Controllers/Products/**` |
| Quote lifecycle | `Business/Services/Quotes/**`, `Business/Mappers/**`, `Data.SQL/Repositories/**`, `API/Controllers/Public/v1/Quotes/**`, `QuotesModuleServiceCollection.cs`, quote tests in both test projects |
| Observability + health | `API/Observability/**`, logging config in `appsettings*.json`, `docker-compose.yml` observability profile |
| React UI | `ui/**` |

Shared files every session may touch, minimally: `README.md`, `docs/ASSUMPTIONS.md`, `docs/DECISIONS.md`, `API/API.csproj` (packages), `API/Program.cs` (one line to wire your module).
