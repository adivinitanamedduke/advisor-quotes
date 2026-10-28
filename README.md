# Advisor Quotes

A small, runnable slice of an adviser-facing quoting capability: an adviser captures a quote for a client, the service calculates and stores a premium, and the adviser views quotes and moves them through their lifecycle. Built with C# / .NET 10 and PostgreSQL.

Design, AWS plan, AI usage and where an LLM could fit: [SOLUTION.md](SOLUTION.md).

## How to run it

### Prerequisites

- .NET SDK 10 (`dotnet --version`)
- PostgreSQL 15+ — either Docker (`docker compose up -d postgres`) or a local install
- Node 20+ (only for the optional UI)

### Steps

1. Start PostgreSQL:
   ```bash
   docker compose up -d postgres
   ```
   Using a local PostgreSQL instead? Create a database (e.g. `advisor_quotes`) and set the connection string:
   ```bash
   export ConnectionStrings__Quotes="Host=127.0.0.1;Port=5432;Database=advisor_quotes;Username=<user>;Password=<password>;SSL Mode=Disable"
   ```
   The database is created by the first migration if it does not exist. On a native Windows install, use `127.0.0.1` and `SSL Mode=Disable`: `localhost` can try IPv6 and SSL first and time out.
2. Run the API (applies migrations on startup in Development):
   ```bash
   dotnet run --project API
   ```
3. Open Swagger at `http://localhost:5222/swagger`.

All configuration can come from environment variables; see [.env.example](.env.example).

### Health

- `GET /health/live`: 200 while the process is up.
- `GET /health/ready`: 200 when PostgreSQL is reachable, 503 when not.

Both return JSON (`{"status":"Healthy","checks":[...]}`) and need no `X-Adviser-Id`.

### Following a request

Every response carries an `X-Trace-Id` header (errors included). The same id is the OpenTelemetry trace id and the `TraceId` property on every log line written while handling that request, so one id links the response, the logs and the spans.

1. Run the API and keep its output in a file:
   ```bash
   dotnet run --project API 2>&1 | tee api.log
   ```
   Logs are one compact JSON object per line. For readable lines (still with the TraceId) set `Observability__LogFormat=text`.
2. Create a quote and read the trace id from the response headers (or from the Swagger UI "Response headers" box). `Idempotency-Key` is optional:
   ```bash
   curl -i -X POST http://localhost:5222/api/v1/Quote -H "X-Adviser-Id: adv-1" -H "Idempotency-Key: demo-1" -H "Content-Type: application/json" -d '{"client":{"fullName":"Thandi Mokoena","dateOfBirth":"1985-04-12","isSmoker":false},"product":{"code":"TERM_LIFE","coverAmount":1000000,"termYears":20},"notes":"Demo quote"}'
   ```
   The response is `201 Created` with the quote (note its `identifier`) and:
   ```
   X-Trace-Id: 65c0f870c25bd7f54d5d44240ca9af28
   ```
3. Find the logs for that request. Every line written while handling it carries the id as `TraceId` (and `@tr`), ending with the request summary line:
   ```bash
   grep 65c0f870c25bd7f54d5d44240ca9af28 api.log
   ```
   ```json
   {"@t":"...","@mt":"HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms","@tr":"65c0f870c25bd7f54d5d44240ca9af28","@sp":"a64caef88a18030d","RequestMethod":"POST","RequestPath":"/api/v1/Quote","StatusCode":201,"Elapsed":48.2,"TraceId":"65c0f870c25bd7f54d5d44240ca9af28","SpanId":"a64caef88a18030d",...}
   ```
4. See the spans. Without a collector they are printed to the console, one block per span, each starting with the same `Activity.TraceId` (`grep -A14 65c0f870c25bd7f54d5d44240ca9af28 api.log`):
   ```
   Activity.TraceId:            65c0f870c25bd7f54d5d44240ca9af28
   Activity.SpanId:             3f1d0c9a7be24411
   Activity.ParentSpanId:       a64caef88a18030d
   Activity.DisplayName:        quote.create
   Activity.Tags:
       adviser.id: adv-1
       product.code: TERM_LIFE
       quote.reference: QT-000123
       quote.premium: 1402.50
       idempotency.outcome: created
   ```
   For a create you will see `POST /api/v1/Quote` (the HTTP span), `quote.create`, `quote.duplicate_check`, `pricing.calculate` and one `postgresql` span per SQL command, all with that trace id.
5. Change the quote's status with the `identifier` from step 2. This is a new request, so it gets its own `X-Trace-Id`; repeat steps 3 and 4 with it and look for the `quote.status_change` span (tags `quote.status.from: Draft`, `quote.status.to: SentToClient`):
   ```bash
   curl -i -X PUT http://localhost:5222/api/v1/Quote/<identifier>/status -H "X-Adviser-Id: adv-1" -H "Content-Type: application/json" -d '{"status":"SentToClient"}'
   ```
6. With Docker, Jaeger gives the timeline view instead of console blocks:
   ```bash
   docker compose --profile observability up -d jaeger
   export OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
   dotnet run --project API
   ```
   Open http://localhost:16686, paste the trace id into "Lookup by Trace ID" (or pick service `advisor-quotes-api`) to see the same spans nested under the HTTP span.

Health probes are logged at Debug so they do not drown real traffic. Metrics are not printed by default; set `Observability__ConsoleMetrics=true` to dump them to the console every minute. When `OTEL_EXPORTER_OTLP_ENDPOINT` is set, metrics go to that endpoint instead (Jaeger ignores them; a full OpenTelemetry collector keeps them).

### Tests

```bash
dotnet test Tests.Unit
dotnet test Tests.Integration   # needs TEST_POSTGRES_CONNECTION or Docker
```

Integration tests run the real API (`WebApplicationFactory`) against PostgreSQL with a fixed fake premium calculator. They pick the database in this order:

1. `TEST_POSTGRES_CONNECTION`, pointing at a **disposable** database (it is migrated at start and its quote tables are truncated before every test):
   ```bash
   export TEST_POSTGRES_CONNECTION="Host=127.0.0.1;Port=5432;Database=advisor_quotes_tests;Username=<user>;Password=<password>;SSL Mode=Disable"
   ```
2. Otherwise a `postgres:16-alpine` Testcontainer, when Docker is running.
3. Otherwise every integration test is reported as skipped, with the reason.

### UI (optional)

With the API running:

```bash
cd ui
npm install
npm run dev
```

Open `http://localhost:5173` and enter an adviser id in the header bar (applied on Enter or blur). The API URL defaults to `http://localhost:5222` (override with `VITE_API_BASE_URL`). `npm test`, `npm run lint` and `npm run build` check it. Details: [ui/README.md](ui/README.md).

### Browser end-to-end tests

Playwright drives the installed Chrome through the real UI, API and PostgreSQL. It starts the API and the dev server itself (or reuses ones already running) and records a video of every test into `ui/test-results/`:

```bash
cd ui
npx playwright install ffmpeg   # once, for video recording
npm run e2e
```

`ConnectionStrings__Quotes` must be set, as for running the API. `E2E_VIDEO=off` skips recording, and `E2E_BROWSER_CHANNEL=msedge` uses Edge. The two journeys are:

- creating a quote, resubmitting it (the duplicate notice), and moving it Draft → Sent to client → Accepted with its history;
- a rejected submission showing the server's field errors and trace id.

## Approach

Layered service with explicit project boundaries:

```
HTTP → Controller (API) → Service (Business) → Repository (Data.SQL) → PostgreSQL
                              ↘ Pricing (no dependency on quotes)
```

- **API**: thin controllers, one error shape, versioned routes (`/api/v1/...`).
- **Business**: validation, pricing call, duplicate detection, status rules.
- **Pricing**: premium calculation behind `IPremiumCalculator`, configuration driven. Kept independent so it can become its own service.
- **Data.SQL**: EF Core with migrations; quotes and their status history.

Request flow details: [docs/data-flows.md](docs/data-flows.md).

## Key decisions and trade-offs

Full log with alternatives: [docs/DECISIONS.md](docs/DECISIONS.md). Reasoning: [SOLUTION.md](SOLUTION.md).

1. **Duplicate submissions: key plus fingerprint.** Honour an `Idempotency-Key` header; without one, an identical submission from the same adviser within 10 minutes is a replay. Replays return 200 with the original quote; the same key with a different body is 409. *Considered:* key only (clients that forget it still duplicate), or a unique constraint on business fields (blocks legitimate re-quotes forever). *Trade-off:* a deliberate identical re-quote inside the window returns the existing one.
2. **Correctness in the database, not in memory.** A unique index on the idempotency key plus a transaction-scoped advisory lock on the fingerprint make concurrent double clicks safe across any number of API instances; `xmin` turns concurrent status changes into a 409. *Considered:* in-process locks (break with two instances), or serialisable isolation (retries everywhere). *Trade-off:* one small raw SQL call in an otherwise LINQ repository.
3. **History in the same transaction as the change.** Each status change writes its history row atomically with the quote update, so the audit can never disagree with the current status. *Considered:* event sourcing (too heavy here), or JSON history on the quote (hard to query). *Trade-off:* two writes per change.
4. **Pricing isolated and explainable.** Pricing is its own project with no reference to quotes; every stored premium keeps its pricing version and the factors used. *Considered:* calculating inside the quote service, or recalculating on read. *Trade-off:* a little more structure now, for an easy split into a pricing service later.
5. **One trace id as the runtime signal.** OpenTelemetry spans and JSON logs share the trace id, returned as `X-Trace-Id` on every response and shown in the UI on errors. *Considered:* a custom correlation id in logs only. *Trade-off:* a few OpenTelemetry packages; exporters change per environment with no code change.

## Assumptions

Full table with reasoning: [docs/ASSUMPTIONS.md](docs/ASSUMPTIONS.md).

- .NET 10 (LTS) satisfies ".NET 8 or later".
- Premiums are monthly, in ZAR.
- Auth is out of scope, so the adviser is identified by an `X-Adviser-Id` header; advisers see only their own quotes (others get 404).
- Quotes have an internal id, a public GUID identifier, and a readable reference like `QT-000123`.
- Status flow: Draft → SentToClient or Declined; SentToClient → Accepted or Declined; Accepted and Declined are final.
- Duplicate window is 10 minutes (configurable).
- Without an `Idempotency-Key`, a duplicate is the same adviser, client, product and notes, ignoring case and extra spaces.
- An identical body inside the window is a replay even when it carries a new `Idempotency-Key`.
- Rule breaches are 400; 409 only for idempotency-key mismatches and concurrent updates.
- Products and rates are illustrative configuration, not real actuarial tables.
- Age is completed years on the quote date, priced in five bands (under 30 × 0.80 up to 60+ × 4.00); term adds 1% per year above 10 years.
- Product limits are inclusive; product codes are case-insensitive.
- Integration tests run against `TEST_POSTGRES_CONNECTION`, or Testcontainers when Docker is present.
- The UI runs on its own origin and calls the API directly; the API allows CORS only from `Cors:UiOrigins` (default `http://localhost:5173`) and exposes `Idempotent-Replayed`, `X-Trace-Id` and `Location`.
- The UI issues one `Idempotency-Key` per submission and reuses it while the form body is unchanged, so retries and double clicks replay rather than duplicate.
- The UI does no rule validation of its own; it shows the API's `ErrorResponse.errors` next to the matching field, or in a summary.
- The UI mirrors the status transitions above to choose which buttons to show; the API remains the authority.

## What I'd do with more time

**Gaps**
- **Real authentication.** Replace the `X-Adviser-Id` header with a JWT claim from the corporate IdP. Services already take the adviser id as a parameter, so this is a base-controller change.
- **Pricing.** Move the age bands and term slope from code into versioned rate tables, and validate them with actuaries; today's numbers are illustrative.
- **Status rules in one place.** The UI mirrors the transition table to choose its buttons. The API should return `allowedNextStatuses` on each quote so the two can't drift.
- **Idempotency housekeeping.** Expire old keys and fingerprints with a scheduled job; today they sit on the quote row forever.
- **Jaeger.** The OTLP/Jaeger path is configured but untested here (no Docker on this machine); console exporters were verified instead.

**Improvements**
- Run the integration and browser tests in CI against a Postgres service container on every pull request.
- Add contract tests between the UI client and the API (for example by generating the TypeScript types from OpenAPI) instead of a hand-mirrored client.
- Add an outbox for `QuoteCreated` and `QuoteStatusChanged` events, so CRM sync and client notifications don't couple to the API.

**At scale**
- Extract pricing as its own service, behind the existing `IPremiumCalculator` seam, with timeouts and retries.
- Add read replicas or a read model for search, cursor-based paging, and partitioning of the history table by month.
- Make the dedupe window configurable per channel, plus rate limiting per adviser, and alerts on 5xx rate, latency and conflicts (the counters already exist).

The AWS hosting plan is in [SOLUTION.md](SOLUTION.md#evolving-and-hosting-it-in-aws).

## How I used AI

- **Tools.** Claude Code: one coordinating session, plus four parallel sessions (pricing, quote lifecycle, observability, React UI), each on its own branch. Every prompt, including each sub-session brief, is logged in [docs/ai/PROMPTS.md](docs/ai/PROMPTS.md).
- **What I used it for.** Turning the brief into a requirements matrix and plan; scaffolding; implementing each flow with its tests; Playwright browser tests; drafting these docs.
- **Decisions that were mine.**
  - Duplicate handling (key plus fingerprint), the runtime signal, which optional extras to build, and the layered structure and status codes.
  - One validated, unit-tested commit per flow.
  - No AI attribution in commits.
  - Running on a native PostgreSQL instead of installing Docker.
- **Where I corrected or rejected output.**
  - Had sessions rebuild their branches so each commit stands on its own, instead of fixes landing in later commits.
  - Renamed merge commits that picked up generated branch names.
  - Rejected the first prompt log, which captured tool noise and local paths.
  - Fixed tests and docs that described the API wrongly (route names, the example premium).
- **How I verified it.**
  - Unit tests on every flow, and integration tests against real PostgreSQL. These caught a bug where quote references weren't generated by the database.
  - A hand-checked premium (R2 103.75), pinned in a browser test.
  - Cross-checking each session's claims against the code, which found a duplicate tracing span and clashing assumption IDs.
  - A trial merge of all branches with every suite run before merging.
  - Recorded Playwright runs in Chrome.
