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

### Tests

```bash
dotnet test Tests.Unit
dotnet test Tests.Integration   # needs TEST_POSTGRES_CONNECTION or Docker
```

### UI (optional)

With the API running:

```bash
cd ui
npm install
npm run dev
```

Open `http://localhost:5173` and enter an adviser id in the header bar. The API URL defaults to `http://localhost:5222` (override with `VITE_API_BASE_URL`). `npm test`, `npm run lint` and `npm run build` check it. Details: [ui/README.md](ui/README.md).

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
- The UI runs on its own origin (`http://localhost:5173`) and calls the API directly, so the API allows CORS from it and exposes `Idempotent-Replayed`.
- The UI issues one `Idempotency-Key` per submission and reuses it while the form body is unchanged, so retries and double clicks replay rather than duplicate.
- The UI does no rule validation of its own; it shows the API's `ErrorResponse.errors` next to the matching field, or in a summary.
- The UI mirrors the status transitions above to choose which buttons to show; the API remains the authority.

## What I'd do with more time

_To be completed._

## How I used AI

_To be completed. Every prompt is logged in [docs/ai/PROMPTS.md](docs/ai/PROMPTS.md)._
