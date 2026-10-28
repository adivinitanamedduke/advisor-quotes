# Prompt log

Every prompt sent to Claude Code while building this project, in order. Appended automatically by `.claude/hooks/log-prompt.ps1` (UserPromptSubmit hook) from every session, including parallel worktree sessions. Entries before the hook existed were back-filled by hand. References to internal codebases outside this project are masked as `[internal reference]`.

## Planning session (back-filled)

### 2026-10-28 | planning | prompt 1

> @Assessment__Demo_Video_Prep_Guide.pdf @Senior_Dev_Tech_Lead_Sanlam_Connect_Assignment.pdf
> Advisor Quote Service Core
> I want to run subsessions for this work.
>
> 1. Will document the list of prompts I send during the development and planning of this item.
> 2. Keep track of assumptions made where there's ambiguity + README update as we develop the solution.
>
> Gather all requirements from the documents and let table a solution.
>
> This project must follow the data flows as described in [internal reference] repo and follow skills described there for controller setup and status codes.

### 2026-10-28 | planning | answers to clarifying questions

> Repo location: C:\Projects\SanlamConnect\advisor-quotes
> Duplicate handling: Idempotency-Key header + payload fingerprint window
> Runtime signal: OpenTelemetry traces + correlated structured logs
> Optional extras: health/readiness endpoint, minimal React UI, Docker Compose

### 2026-10-28 | planning | plan review feedback

> The must be no mention of [internal reference] in the document.

### 2026-10-28 | planning | plan review feedback

> Continue

### 2026-10-28 | planning | answer

> Approve and start Phase 0

### 2026-10-28 | phase 0 | after a transient tool failure

> Try again

### 2026-10-28 | phase 0 | tooling

> install docker in this computer so all docker requirements can be fullfilled.

### 2026-10-28 | phase 0 | tooling

> Skip Docker for now; keep using the native PostgreSQL on port 5437.

### 2026-10-28 | phase 0 | git setup

> I am loggen in to GitHub, so git must be setup on project folders and be ready for commits.

> In chrome

> Commit identity: Njongo Villo <njongo.villo@gmail.com>. Create a private GitHub repo: yes.

### 2026-10-28 | phase 0 | commit policy

> I neee individual commits as we build

> a flow like endpoint->service-repository-EF-Data must have a commit with short precise description.

> Each flow must be validated and unit tested.

> Ensure no claud related text in commits

### 2026-10-28 | phase 0 | GitHub remote

> I've signed in to GitHub in the browser window the extension controls. Create the private repo adivinitanamedduke/advisor-quotes and push main.

> I'm logged in in the chrome nrowser

> I created the empty private repo in Chrome; here is the URL:

> Give me a good short description to use for the repo

> repo created

> https://github.com/adivinitanamedduke/advisor-quotes

### 2026-10-28 | phase 1 | sessions and local database

> Start all session cards

> I've clicked Trust repository. Restart all four Phase 1 sessions.

> For postgres db password try [two candidate local passwords] one of them should work

## Automatic entries

### 2026-10-28 20:05 | branch `claude/pricing-engine-f31370` | session `1cdcc646`

> Background (from the session that started this one):
> Take-home assessment: "Advisor Quotes" API in C#/.NET 10 + PostgreSQL at C:\Projects\SanlamConnect\advisor-quotes (GitHub: adivinitanamedduke/advisor-quotes, private). Phase 0 is on `main`: projects, contracts, interfaces, EF schema and conventions. Read `CLAUDE.md` first. It covers layering, status codes, validation in the service, session ownership, and assumptions/decisions logging. Its commit rules: one validated, unit-tested commit per flow, short precise subjects, and NO Co-Authored-By or any Claude/Anthropic text in commits. Then read `docs/ASSUMPTIONS.md` and `docs/DECISIONS.md`. Three other sessions run in parallel (quote lifecycle, observability, React UI) and own other folders. No references to other companies' internal codebases anywhere.
> 
> Task:
> Build the pricing engine. Your files: `Pricing/**`, `API/Controllers/Public/v1/Products/**`, `API/Middleware/DependencyInjection/PricingModuleServiceCollection.cs`, `Tests.Unit/Pricing/**`, `Tests.Unit/API/Controllers/Products/**`. Keep the interfaces in `Pricing/IPremiumCalculator.cs` and `Pricing/IProductCatalog.cs` stable; the quote lifecycle session codes against them.
> 
> 1. `ProductCatalog : IProductCatalog` bound from `PricingOptions` (config already in `API/appsettings.json`), case-insensitive lookup.
> 2. `PremiumCalculator : IPremiumCalculator`: monthlyPremium = round2(cover/1000 × baseRatePer1000 × ageFactor(age at QuoteDate) × (smoker ? SmokerLoading : 1) × termFactor(term)), `MidpointRounding.AwayFromZero`, currency and version from options. Pick simple, documented age bands and a term factor. Return every factor in `Factors`. Throw `PricingRuleException` for an unknown product, an entry age outside the product's range, or a term or cover outside the range. Start a `pricing.calculate` span. Pricing must not reference other projects, so use a small local ActivitySource named "AdvisorQuotes".
> 3. Register both as singletons in `PricingModuleServiceCollection`.
> 4. `ProductController` at `GET api/v1/Product` returning `ProductDto[]` (Contracts), following BaseController conventions. It does not need `X-Adviser-Id`.
> 5. xUnit tests: base case worked by hand, smoker loading, each age band edge, term factor, rounding at a midpoint, every rule failure, catalogue case-insensitivity, and the controller's 200.
> 6. Add the formula and a worked example to `docs/DECISIONS.md` (new D row), plus any new assumptions to `docs/ASSUMPTIONS.md` and the README.
> 
> Commit per piece: the calculator with its tests, then the product list flow with its tests. Done when `dotnet build AdvisorQuotes.sln` and `dotnet test Tests.Unit` pass, the commits are on your branch, and the branch is pushed to origin. Report the branch name and anything you changed outside your files.

### 2026-10-28 20:05 | branch `claude/react-ui-4efc1b` | session `ac0ab32e`

> Background (from the session that started this one):
> Take-home assessment: "Advisor Quotes" API in C#/.NET 10 + PostgreSQL at C:\Projects\SanlamConnect\advisor-quotes (GitHub: adivinitanamedduke/advisor-quotes, private). Phase 0 is on `main` with the full API contract in `Contracts/` (DTOs and validators) and the endpoint list in `docs/data-flows.md`. The API itself is being built in parallel, so code against the contract. Read `CLAUDE.md`: session ownership, assumptions logging, and the commit rules (one tested commit per flow or screen, short precise subjects, NO Co-Authored-By or any Claude/Anthropic text). The UI is optional in the brief and React is assessed in the live interview, so keep it small, clean and idiomatic. No references to other companies' internal codebases anywhere.
> 
> Task:
> Build a minimal adviser UI in `ui/` only (Vite + React + TypeScript; Node 22 is installed). One commit per screen or flow, each with its tests.
> 
> 1. **Scaffold and API client:** a typed client mirroring the Contracts DTOs. Base URL from `VITE_API_BASE_URL` (default `http://localhost:5222`). Send `X-Adviser-Id` from an "Adviser" input in the header bar, persisted in localStorage. Generate an `Idempotency-Key` (`crypto.randomUUID()`) once per form submission attempt, so retries and double clicks reuse it.
> 2. **Quote list:** `POST api/v1/Quote/search` with a status filter and paging.
> 3. **Create quote:** products from `GET api/v1/Product`; client name, DOB, smoker, cover, term, notes. Show server validation errors from `ErrorResponse.errors`, and a notice when the response carried `Idempotent-Replayed`.
> 4. **Quote detail and status change:** premium, rating factors and status history, with buttons only for the allowed next statuses and an optional reason.
> 5. Keep dependencies minimal: no UI kit, plain CSS or CSS modules. Handle loading and error states. Use Vitest + Testing Library for the status buttons, error display and the idempotency key reuse.
> 6. Do not edit `API/`. List the exact CORS change the API needs in your final report.
> 7. Add `ui/README.md` with run steps and a short "UI" subsection in the root README "How to run it". Log assumptions in `docs/ASSUMPTIONS.md` and the README.
> 
> `npm run build`, `npm run lint` and `npm test` must pass. Done when the branch is pushed to origin. Report the branch and the CORS change.

### 2026-10-28 20:05 | branch `claude/observability-health-037f1b` | session `db2d5467`

> Background (from the session that started this one):
> Take-home assessment: "Advisor Quotes" API in C#/.NET 10 + PostgreSQL at C:\Projects\SanlamConnect\advisor-quotes (GitHub: adivinitanamedduke/advisor-quotes, private). Phase 0 is on `main`. Read `CLAUDE.md` first: conventions, session ownership, assumptions/decisions logging, and the commit rules (one validated, unit-tested commit per piece, short precise subjects, NO Co-Authored-By or any Claude/Anthropic text). The brief asks for ONE end-to-end runtime signal that lets an operator follow a user action (create a quote, change its status) through the back end locally, shown in a demo video. The chosen signal is OpenTelemetry traces plus Serilog JSON logs correlated by TraceId. Business code starts spans from `Business.Abstractions/Telemetry/QuoteTelemetry.cs` (a parallel session writes the quote code). There is no Docker. A native PostgreSQL 18 runs on 127.0.0.1:5437; the working connection string is in the gitignored file `C:\Projects\SanlamConnect\advisor-quotes\.env`. Never commit or print the password, and use `127.0.0.1` with `SSL Mode=Disable`. No references to other companies' internal codebases anywhere.
> 
> Task:
> Implement runtime visibility and health. Your files: `API/Observability/**`, logging config in `API/appsettings*.json`, and an `observability` profile in `docker-compose.yml`. One commit per piece, each with its tests.
> 
> 1. **Tracing and metrics:** fill `AddObservability()` / `UseObservability()` in `API/Observability/ObservabilityServiceCollection.cs` (already called from `Program.cs`). Use OpenTelemetry with ASP.NET Core, HttpClient and Npgsql/EF Core instrumentation plus the `AdvisorQuotes` source and meter. Use the console exporter by default, and OTLP when `OTEL_EXPORTER_OTLP_ENDPOINT` is set.
> 2. **Structured logs:** Serilog compact JSON to the console, enriched with TraceId and SpanId from `Activity.Current`, plus request logging (one line per request: method, path, status, elapsed, TraceId). Readable output in Development is fine if TraceId is shown.
> 3. **Trace header:** middleware that adds `X-Trace-Id` to every response, error responses included. Unit-test it.
> 4. **Health:** `/health/live` (process up) and `/health/ready` (Postgres reachable via the DbContext check), returning JSON, with no `X-Adviser-Id` needed. Add a `WebApplicationFactory<Program>` test that `/health/live` returns 200 with `X-Trace-Id`, and that `/health/ready` returns 200 when the DB connection string is set.
> 5. **Jaeger profile:** a `jaeger` service under compose profile `observability` (all-in-one, UI 16686, OTLP 4317), documented.
> 6. In README "How to run it", add "Following a request": how to read the trace id and find the matching logs and spans. Add a D row in `docs/DECISIONS.md` if you deviate from D-09.
> 
> Done when the build and tests pass and the branch is pushed to origin. Report the branch and the exact steps to demo the signal.

### 2026-10-28 20:05 | branch `claude/quote-lifecycle-83ab1f` | session `9fa6ad27`

> Background (from the session that started this one):
> Take-home assessment: "Advisor Quotes" API in C#/.NET 10 + PostgreSQL at C:\Projects\SanlamConnect\advisor-quotes (GitHub: adivinitanamedduke/advisor-quotes, private). Phase 0 is on `main`: projects, contracts, interfaces, EF schema with its initial migration (already applied to the local DB), and conventions. Read `CLAUDE.md` first: layering, status codes, validation in the service, session ownership, and the commit rules (one validated, unit-tested commit per flow, short precise subjects, NO Co-Authored-By or any Claude/Anthropic text). Then read `docs/ASSUMPTIONS.md`, `docs/DECISIONS.md` and `docs/data-flows.md`. A pricing session implements `IPremiumCalculator` in parallel, so mock it in unit tests and use a small fake in integration tests. There is no Docker. A native PostgreSQL 18 runs on 127.0.0.1:5437; working connection strings are in the gitignored file `C:\Projects\SanlamConnect\advisor-quotes\.env` (`ConnectionStrings__Quotes`, and `TEST_POSTGRES_CONNECTION` pointing at database `advisor_quotes_tests`). Read them from there, never commit or print the password, and use `127.0.0.1` with `SSL Mode=Disable` (localhost times out). No references to other companies' internal codebases anywhere.
> 
> Task:
> Implement the quote lifecycle. Your files: `Business/Services/Quotes/**`, `Business/Mappers/**`, `Data.SQL/Repositories/**`, `API/Controllers/Public/v1/Quotes/**`, `API/Middleware/DependencyInjection/QuotesModuleServiceCollection.cs`, and quote tests in `Tests.Unit` and `Tests.Integration`.
> 
> Building blocks (one commit each, with tests):
> 1. `QuoteStatusMachine`: Draft to SentToClient or Declined; SentToClient to Accepted or Declined; Accepted and Declined are final; same status again is rejected. A breach throws `BadRequestException`.
> 2. `QuoteFingerprint`: SHA-256 hex of canonical JSON of adviser, client (trimmed, case-normalised name), product (upper-case code) and notes (trimmed).
> 
> Flows (endpoint → service → repository → EF; one commit per flow, each validated and unit tested per CLAUDE.md):
> 3. **Create quote flow:** `POST api/v1/Quote`.
>    - The service validates, prices (mapping `PricingRuleException` to 400), builds the candidate with an initial `null → Draft` history row, and calls `AddUnlessDuplicateAsync` with now minus `IdempotencyOptions.WindowMinutes`.
>    - If a different quote comes back: the same key with a different fingerprint is `ConflictException`; otherwise it is a replay.
>    - The repository runs one transaction: `pg_advisory_xact_lock(hashtext(fingerprint))`, look up by key, then by fingerprint inside the window, then insert. On a unique violation, re-read and return the existing quote.
>    - The controller returns 201 via `CreatedAtRoute("GetQuote", ...)`, or 200 with `Idempotent-Replayed: true`.
> 4. **Get quote flow:** `GET api/v1/Quote/{identifier:guid}` (route name `GetQuote`), adviser-scoped `AsNoTracking`, 404 on null.
> 5. **Search quotes flow:** `POST api/v1/Quote/search` returning `PagedResult<QuoteSummaryDto>`.
> 6. **Status change flow:** `PUT api/v1/Quote/{identifier:guid}/status`. Load for update, apply the status machine, add a history row (ChangedBy = adviser, reason), set UpdatedAt, save. A `DbUpdateConcurrencyException` becomes `ConflictException`.
> 
> Every service method runs `ValidateOrThrowAsync` first, adds `QuoteTelemetry` spans and tags, and logs with templates and no client PII. Controllers stay thin with `[ProducesResponseType]` for every code. Register services in the module DI file.
> 
> 7. **Integration tests commit:** real Postgres (`TEST_POSTGRES_CONNECTION`, else Testcontainers when Docker is present, else skip with a message), via `WebApplicationFactory<Program>`. Migrate the test database at fixture start and clean it between tests. Cover: create 201; replay 200 with the same identifier; key reused with a different body 409; a parallel double submit creates one row; the full transition path with 3 history rows; a bad transition 400; another adviser's GET 404; a missing header 400.
> 
> Log new assumptions and decisions in the docs and the README within the relevant commit. Done when the build, unit tests and integration tests pass, and the branch is pushed to origin. Report the branch, test results and any contract changes.
