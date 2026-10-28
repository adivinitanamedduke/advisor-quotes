# Data flows

What this service owns, what calls it, and what it calls.

## Owns

| Entity | Store |
|---|---|
| Quotes (client snapshot, product, premium, rating factors, status, duplicate guards) | PostgreSQL `quotes` |
| Quote status history | PostgreSQL `quote_status_changes` |
| Quote reference sequence | PostgreSQL `quote_reference_seq` |

Product definitions and rates are configuration (`Pricing` section), not data, in this version.

## Inbound

| From | Interface | Contract |
|---|---|---|
| Adviser UI (`ui/`) or any HTTP client | `POST /api/v1/Quote` (+ optional `Idempotency-Key`) | `CreateQuoteRequest` → `QuoteDto` |
| Adviser UI | `POST /api/v1/Quote/search` | `SearchQuotesRequest` → `PagedResult<QuoteSummaryDto>` |
| Adviser UI | `GET /api/v1/Quote/{identifier}` | `QuoteDto` |
| Adviser UI | `PUT /api/v1/Quote/{identifier}/status` | `UpdateQuoteStatusRequest` → `QuoteDto` |
| Adviser UI | `GET /api/v1/Product` | `ProductDto[]` |
| Orchestrator / load balancer | `GET /health/live`, `GET /health/ready` | 200 / 503 |

Every quote call carries `X-Adviser-Id` (see A-03).

## Outbound

| To | Interface | Notes |
|---|---|---|
| PostgreSQL | Npgsql / EF Core | Only external dependency |
| OTLP collector (optional) | OpenTelemetry OTLP | Only when `OTEL_EXPORTER_OTLP_ENDPOINT` is set; console exporter otherwise |

## Create quote flow

```
POST /api/v1/Quote
  QuoteController.CreateAsync          bind body, read X-Adviser-Id + Idempotency-Key
  QuoteService.CreateQuoteAsync        validate → price (IPremiumCalculator) → fingerprint
  QuoteRepository.AddUnlessDuplicateAsync
      BEGIN; advisory lock(fingerprint); find by key / fingerprint window;
      INSERT quote + initial history row; COMMIT
  ← 201 new quote | 200 replay (Idempotent-Replayed: true) | 409 key reused with another body
```

## Status change flow

```
PUT /api/v1/Quote/{identifier}/status
  QuoteController.UpdateStatusAsync    bind body, read X-Adviser-Id
  QuoteService.UpdateQuoteStatusAsync  validate → load tracked (adviser scoped) → status machine
                                       → set status + UpdatedAt, add history row (ChangedBy, Reason)
  QuoteRepository.SaveChangesAsync     UPDATE quote WHERE xmin matches + INSERT history, one transaction
  ← 200 quote | 400 bad transition | 404 unknown or other adviser | 409 concurrent update (xmin changed)
```

## Layer rules

- Pricing never reads or writes quotes; quotes call pricing through `IPremiumCalculator` only. This is the seam for a separate pricing service.
- Controllers never touch `QuotesDbContext`; repositories never call pricing.
- History is written in the same transaction as the status it records.
