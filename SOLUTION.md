# Solution

How the Advisor Quotes service is designed, the trade-offs behind it, how it would run on AWS, how AI was used to build it, and where an LLM could fit in the quote flow. Running logs: [docs/DECISIONS.md](docs/DECISIONS.md) (D-01 to D-12) and [docs/ASSUMPTIONS.md](docs/ASSUMPTIONS.md) (A-01 to A-20).

## Design and trade-offs

### Shape

```
HTTP → QuoteController (API) → QuoteService (Business) → QuoteRepository (Data.SQL) → EF Core → PostgreSQL
                                     ↘ IPremiumCalculator (Pricing: no reference to quotes)
```

| Project | Responsibility |
|---|---|
| `API` | Thin versioned controllers, one exception filter, DI modules, CORS, health, observability wiring, startup migration |
| `Business` | Quote service (validate → price → dedupe → persist), status machine, request fingerprint, mappers, metrics |
| `Business.Abstractions` | Domain model, service and repository interfaces, exceptions that map to HTTP codes, telemetry sources |
| `Contracts` | Request/response DTOs and FluentValidation validators: the contract the UI codes against |
| `Pricing` | Premium calculator and product catalogue, bound from configuration and validated at startup |
| `Data.SQL` | `QuotesDbContext`, Fluent configurations, migrations, repository |

The boundaries are enforced by project references rather than folders (D-01). Pricing references nothing, so moving it behind its own service later means swapping one `IPremiumCalculator` implementation for an HTTP client, not untangling code.

Controllers do four things: bind, read the adviser and idempotency headers, call one service method, and map to HTTP. Validation runs **in the service** as its first line (D-02), so the same rules hold for any future caller, such as a queue consumer. Every error, whether model binding, validation, a rule breach, not found, conflict or unexpected, comes back in one `ErrorResponse { statusCode, message, errors[] }` shape (D-11).

| Code | Meaning |
|---|---|
| 201 | Quote created (`Location: /api/v1/Quote/{identifier}`) |
| 200 | Read, search, status change, or a replayed duplicate (`Idempotent-Replayed: true`) |
| 400 | Fix your request: validation, missing `X-Adviser-Id`, product limits, disallowed transition |
| 404 | Unknown quote, or another adviser's (no 403, so existence isn't revealed) |
| 409 | The resource changed under you: idempotency key reused with a different body, or a concurrent update |

### Data model

- `quotes` holds the client and product snapshot, premium, currency, pricing version, a JSON snapshot of the rating factors, current status, notes, the duplicate guards (`idempotency_key`, `request_fingerprint`) and timestamps. It has an internal `Guid Id`, a public `Identifier`, and a readable `QT-000123` reference from a database sequence (A-04).
- `quote_status_changes` holds one row per transition (from, to, reason, who, when), including the initial `null → Draft` row. It is written in the **same transaction** as the change it records, so history can't drift from the quote (D-05).
- The Postgres `xmin` column is the optimistic concurrency token. Two status changes to the same quote at once (two tabs, a retry racing the original) give one success and one 409, not a lost update.
- Storing the factors and pricing version (D-06) keeps every premium explainable after rates change, at the cost of slightly larger rows.

### Duplicate submissions

The brief asks for "sensible" handling of the same submission sent twice in a short window. I handle two cases (D-03):

1. **The client sends an `Idempotency-Key`.** The same key with the same body replays the original quote (200). The same key with a different body is a client bug and returns 409.
2. **No key.** A SHA-256 fingerprint of the adviser plus the normalised payload (trimmed, case-insensitive name and product code, blank notes treated as none) is matched against quotes created in the last 10 minutes (configurable). A match replays (A-14, A-15).

Correctness lives in the database, not in process memory (D-04). A unique index on `(adviser_id, idempotency_key)` settles racing keys. A transaction-scoped `pg_advisory_xact_lock` on the fingerprint serialises identical submissions without keys, across any number of API instances. An integration test fires parallel double submits and asserts that exactly one row exists.

*Trade-off:* an adviser who deliberately wants two identical quotes within 10 minutes gets the existing one back. I judged that rarer and cheaper than an accidental duplicate reaching a client. Changing the notes, or waiting out the window, creates a new quote.

### Pricing

`monthlyPremium = round2(cover / 1000 × baseRatePer1000 × ageFactor × smokerFactor × termFactor)`, rounded half away from zero, in ZAR (D-10, A-11 to A-13).

- Base rates, product limits and the smoker loading (× 1.5) come from configuration. The API refuses to start if that configuration is inconsistent.
- Age is in completed years on the quote date, priced in five bands (× 0.80 to × 4.00). Term adds 1% per year above 10.
- Worked example: TERM_LIFE, R1 000 000 cover, a 41-year-old smoker, 20 years: 1 000 × 0.85 × 1.50 × 1.5 × 1.10 = **R2 103.75**. The end-to-end test asserts exactly this figure.

The numbers are illustrative, not actuarial. The point is that the calculation is deterministic, explainable factor by factor, and isolated behind one interface.

### Runtime visibility (the one end-to-end signal)

The signal is the **trace id** (D-09). Every response carries `X-Trace-Id`. The same id is:

- the OpenTelemetry trace id. Spans cover the HTTP request, `quote.create`, `quote.duplicate_check`, `pricing.calculate`, `quote.status_change`, and every SQL command via Npgsql;
- the `TraceId` property on every Serilog JSON log line written while handling that request.

The UI shows it on errors. An operator can go from a support ticket ("it failed, trace id …") to every log line and span with one search. Spans go to the console locally, or to any OTLP collector (Jaeger profile in `docker-compose.yml`) when `OTEL_EXPORTER_OTLP_ENDPOINT` is set. Counters (`quotes_created_total`, `quotes_replayed_total`, `quotes_conflicts_total`, `quote_status_changes_total`) and `/health/live` and `/health/ready` round it out. Logs and span tags carry no client name or date of birth.

### Testing

| Suite | What it proves | Count |
|---|---|---|
| `Tests.Unit` | Validators, pricing (every band edge, rounding, limits), status machine table, fingerprint normalisation, services with mocked repositories (including "invalid input never reaches the repository"), controllers' status codes, exception mapping, telemetry | 200 |
| `Tests.Integration` | The real API (`WebApplicationFactory`) on real PostgreSQL: create, replay, 409, parallel double submit, concurrency 409, transitions and history, adviser scoping, health, CORS | 23 |
| `ui` (Vitest) | Status buttons, error display with field paths, idempotency key reuse, adviser input | 51 |
| `ui/e2e` (Playwright) | The full journey in Chrome against the real UI, API and database, with a video per run | 2 |

Integration tests use a disposable database from `TEST_POSTGRES_CONNECTION`, fall back to Testcontainers when Docker is present, and otherwise skip with a reason. The machine this was built on has no Docker.

## Evolving and hosting it in AWS

**Run it as it is (first step).**
- **API:** a container image in ECR, run on **ECS Fargate** behind an **Application Load Balancer**. `/health/ready` is the target-group health check, `/health/live` the container check. Two or more tasks across AZs; the advisory lock and unique index already make duplicate handling safe across instances.
- **Database:** **Amazon RDS for PostgreSQL** (Multi-AZ), or Aurora PostgreSQL if read scaling is needed.
- **Configuration:** the connection string in **Secrets Manager**, other settings in **SSM Parameter Store**, all injected as the same environment variables used locally.
- **Migrations:** run as an explicit pipeline step, either an EF migration bundle as a one-off ECS task before the new task set takes traffic, or `Database__MigrateOnStartup` left off in production (D-07).
- **UI:** built to static files on **S3 + CloudFront**, with `Cors__UiOrigins__0` set to that origin.
- **Observability:** an **ADOT collector** sidecar receives OTLP and sends traces to **X-Ray** and metrics to **CloudWatch**. The JSON logs go to **CloudWatch Logs**, where Logs Insights queries by `TraceId`. No code changes; only `OTEL_EXPORTER_OTLP_ENDPOINT` is set.
- **Security:** a corporate IdP (or **Cognito**) issues JWTs, and the adviser id comes from a claim instead of the `X-Adviser-Id` header. That is a base-controller change; services already take the adviser id as a parameter. **WAF** sits on the ALB and CloudFront. Tasks get least-privilege roles, the database sits in private subnets, and Secrets Manager rotation is enabled.
- **Delivery:** infrastructure as code (CDK or Terraform). GitHub Actions authenticates with OIDC (no long-lived keys), runs build and test, pushes to ECR, runs the migration task, and deploys ECS with a blue/green or rolling strategy and alarms on 5xx and latency.

**Evolve it.**
1. **Separate pricing from quotes.** Deploy Pricing as its own small service, with its own rate tables and versioning, behind an internal ALB or as a Lambda. The quote service calls it through an `IPremiumCalculator` HTTP client with timeouts and retries. Quotes already store the pricing version and factors, so history stays intact.
2. **Publish quote events.** Write `QuoteCreated` and `QuoteStatusChanged` to an outbox table in the same transaction, and relay them to **EventBridge** or **SNS/SQS**. CRM sync, client notifications and reporting then subscribe without coupling to the API. The transaction boundary is already in place.
3. **Scale reads.** Use Aurora read replicas or a read model for list and search screens; keep writes on the primary.
4. **Harden idempotency at scale.** Expire old idempotency keys with a scheduled job or a partitioned table, and expose the dedupe window as a per-channel setting.

## How I used AI tools, and how I validated the output

I used **Claude Code** as a pair programmer, and as a coordinator of parallel sessions. Every prompt I sent is logged in [docs/ai/PROMPTS.md](docs/ai/PROMPTS.md), automatically by a hook, including the briefs the coordinating session sent to each sub-session.

**How the work was split**
1. **Planning.** Claude read the brief and the prep guide and proposed a requirements matrix and solution. I made the key decisions: duplicate handling via key + fingerprint, traces + correlated logs as the signal, which optional extras, and the repo layout and conventions.
2. **Phase 0, sequential.** It scaffolded the solution, contracts, interfaces, schema, conventions (`CLAUDE.md`) and the assumption, decision and prompt logs, so parallel work had stable contracts.
3. **Phase 1, parallel.** Four sessions each took one part (pricing, quote lifecycle, observability, React UI) on its own branch. Each owned its files, committed one validated, unit-tested flow at a time, and logged its assumptions.
4. **Integration.** The coordinating session asked each session to audit its own work against its brief, had each finish its gaps, merged everything, and added CORS and the browser end-to-end tests.

**Validation, rather than trusting the output**
- Every flow has unit tests, and the API is tested against **real PostgreSQL**, not an in-memory fake. That caught a real bug in my own scaffold: EF was inserting an empty quote reference instead of letting the database sequence generate it, so the second quote failed. The fix needed no schema change.
- I checked the premium by hand (R2 103.75) and pinned it in the end-to-end test.
- The coordinating session cross-checked the sessions' claims against the code rather than taking their reports at face value. That found a duplicate `pricing.calculate` span that neither session had noticed, and clashing assumption IDs between branches.
- Before merging, I trial-merged all branches in a scratch worktree and ran every suite there. I also checked that EF reported no pending model changes.
- Browser tests in real Chrome, with video, prove the UI, API and database work together.

**Where I corrected or rejected the output**
- I asked for **one commit per vertical flow** (endpoint → service → repository → EF), each validated and unit tested, instead of large commits. Where sessions had fixed things in later commits, I had them rebuild clean branches so every commit stands on its own.
- I required that **no commit carry AI attribution**, and had merge commits renamed when they picked up generated branch names.
- I kept a separate work identity out of the history, and kept references to other internal codebases out of the repo and the prompt log.
- I chose not to install Docker on this machine; the tests and setup adapt to a native PostgreSQL instead.
- The tests found several of the generated defects above. I treat AI output as a draft I have to be able to explain line by line.

## Where an LLM or AI agent could fit in this quote flow

**Not in pricing or status rules.** The premium must be deterministic, explainable and auditable, and transitions are business rules. Both stay in plain code.

**Where it helps**
1. **Capture assistance.** An adviser pastes call notes or an email, and the model extracts a *draft* request (client name, date of birth, smoker, product, cover, term) to pre-fill the form. The adviser reviews and submits, and the same server-side validation and pricing run as always. This cuts typing and transcription errors.
2. **Client-ready summary.** From the stored quote (premium, factors, product limits), the model drafts a plain-language explanation or email for the client: why the premium is what it is, what changes it. The adviser edits before sending, and the "Sent to client" status still records who sent it.
3. **Operations.** An agent with read-only access to logs and traces could summarise a failing request from its trace id for support staff.

**Trade-offs**
- **Cost:** per-token pricing is small per call, but it scales with usage. Call it only on demand (a button, not on every create), keep prompts small by sending structured fields rather than whole records, cache summaries per quote and pricing version, and use the smallest model that meets a quality bar.
- **Latency:** model calls take seconds, so keep them **off the create and status-change path**. Pre-fill and summaries are optional, asynchronous helpers with streaming, timeouts and a graceful "write it yourself" fallback. The core flow never waits on a model.
- **Data privacy:** quotes hold personal and health-related information (name, date of birth, smoker status), so POPIA applies.
  - Send the minimum: initials and age band instead of name and date of birth where possible.
  - Use a provider with no training on customer data, an in-region endpoint (for example through Amazon Bedrock inside the AWS account), encryption in transit, and audit logs of what was sent.
  - Treat free-text notes as untrusted input that may contain prompt injection.
  - Never let model output change a quote without the adviser confirming it.
- **Quality and accountability:** a human stays in the loop for anything client-facing. Measure the quality of extraction and summaries against a labelled set before rollout, and log the model version alongside each generated draft.
