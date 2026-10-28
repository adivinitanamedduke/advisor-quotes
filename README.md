# Advisor Quotes

A small, runnable slice of an adviser-facing quoting capability: an adviser captures a quote for a client, the service calculates and stores a premium, and the adviser views quotes and moves them through their lifecycle. Built with C# / .NET 10 and PostgreSQL.

> **Status:** in progress. Phase 0 (scaffold, contracts, schema) is done. Pricing, quote lifecycle, observability and the React UI are being built next. This README is updated as each piece lands.

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
       quote.premium: 850.00
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

_Top 3 to 5 decisions, finalised at the end. Full running log: [docs/DECISIONS.md](docs/DECISIONS.md)._

1. **Duplicate submissions** — honour an `Idempotency-Key` header; without one, treat an identical submission from the same adviser within 10 minutes as a replay. Replays return 200 with the original quote. Trade-off: a deliberate identical re-quote within the window is not created.
2. **Correctness in the database** — a unique index on the idempotency key and a transaction-scoped lock on the payload fingerprint make concurrent double clicks safe across instances.
3. **Pricing as a separate project** — no reference to quotes, so it can be extracted behind an HTTP/queue boundary later; every stored premium keeps its pricing version and factors.

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

_To be completed._

## How I used AI

_To be completed. Every prompt is logged in [docs/ai/PROMPTS.md](docs/ai/PROMPTS.md)._
