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
2. Make a call and read the trace id from the response headers (or from the Swagger UI "Response headers" box):
   ```bash
   curl -i -X POST http://localhost:5222/api/v1/quotes -H "X-Adviser-Id: adv-1" -H "Content-Type: application/json" -d @quote.json
   ```
   `quote.json` is any create-quote body (Swagger shows the shape). The response includes:
   ```
   X-Trace-Id: 65c0f870c25bd7f54d5d44240ca9af28
   ```
3. Find the logs for that request, including the request summary line (`HTTP POST /api/v1/quotes responded 201 in 42.1 ms`):
   ```bash
   grep 65c0f870c25bd7f54d5d44240ca9af28 api.log
   ```
4. See the spans. Without a collector they are printed to the console (`Activity.TraceId: <id>` blocks, `grep -A12 <id> api.log`). With Docker, Jaeger gives the timeline view:
   ```bash
   docker compose --profile observability up -d jaeger
   export OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
   dotnet run --project API
   ```
   Open http://localhost:16686, paste the trace id into "Lookup by Trace ID" (or pick service `advisor-quotes-api`). A quote create shows the HTTP span, then `quote.create`, `quote.duplicate_check`, `pricing.calculate` and one `postgresql` span per SQL command; a status change shows `quote.status_change`.

Health probes are logged at Debug so they do not drown real traffic. Metrics are not printed by default; set `Observability__ConsoleMetrics=true` to dump them to the console every minute. When `OTEL_EXPORTER_OTLP_ENDPOINT` is set, metrics go to that endpoint instead (Jaeger ignores them; a full OpenTelemetry collector keeps them).

### Tests

```bash
dotnet test Tests.Unit
dotnet test Tests.Integration   # needs TEST_POSTGRES_CONNECTION or Docker
```

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
- Rule breaches are 400; 409 only for idempotency-key mismatches and concurrent updates.
- Products and rates are illustrative configuration, not real actuarial tables.
- Integration tests run against `TEST_POSTGRES_CONNECTION`, or Testcontainers when Docker is present.

## What I'd do with more time

_To be completed._

## How I used AI

_To be completed. Every prompt is logged in [docs/ai/PROMPTS.md](docs/ai/PROMPTS.md)._
