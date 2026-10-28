# Demo video notes

Bullet prompts for a 7 to 9 minute Loom, not a script. Say it in your own words; spend the most time on decisions.

## Before recording

- Two terminals:
  - API with readable logs:
    ```bash
    Observability__LogFormat=text dotnet run --project API 2>&1 | tee api.log
    ```
  - UI:
    ```bash
    cd ui && npm run dev
    ```
- Browser tabs: the UI (`http://localhost:5173`), Swagger (`/swagger`) and the repo on GitHub.
- A fresh adviser id (e.g. `ADV-DEMO-1`), so the list starts empty.
- Have `ui/test-results/videos/` ready as a fallback if the live demo misbehaves.
- Editor tabs open:
  - `QuoteController.cs`
  - `QuoteService.cs`
  - `QuoteRepository.cs` (`AddUnlessDuplicateAsync`)
  - `PremiumCalculator.cs`
  - `ObservabilityServiceCollection.cs`
  - `docs/ai/PROMPTS.md`

## 1. Intro (~30 s)

- Who I am, my role and background in a sentence.
- What I built: an advisor quotes API in .NET 10 and PostgreSQL that captures, prices, deduplicates and progresses quotes, with one trace id to follow any request. A small React UI on top.

## 2. Show it working (~2 min)

- Enter the adviser id. The list is empty.
- New quote: Term Life, 41-year-old smoker, R1m cover, 20 years, giving **R2 103.75** a month. Mention it's priced server-side.
- Submit the same quote again and point at the "already submitted" notice. Same reference, no new row.
- Open it: rating factors, history row `– → Draft`.
- Mark as sent to client (with a reason), then accepted. The history has 3 rows, and "status is final" means no more buttons.
- Quickly show a bad submit (no name, a 2015 date of birth) to bring up the field errors and the **trace id**.
- Copy that trace id and grep `api.log` for it: request line, logs, `quote.create` / `pricing.calculate` / SQL spans, all with the same id.

## 3. Key decisions and trade-offs (~3 min, most of the time here)

- **Duplicates: key plus fingerprint** (`QuoteRepository.AddUnlessDuplicateAsync`)
  - Idempotency-Key gives exact semantics; the fingerprint catches clients that forget it.
  - Same key with a different body is a 409, a client bug.
  - Trade-off: an identical re-quote inside 10 minutes returns the existing quote. I chose that over an accidental duplicate reaching a client.
- **Correctness lives in Postgres**
  - The unique index plus `pg_advisory_xact_lock` mean it's safe with several API instances, not just one process.
  - `xmin` turns concurrent status changes into a 409 instead of a lost update.
  - Point at the parallel double-submit integration test.
- **History in the same transaction as the change**: the audit can't disagree with the quote.
- **Pricing isolated** (`Pricing` references nothing)
  - It stores the factors and pricing version, so old quotes stay explainable.
  - It's the seam for a separate pricing service later.
- **Thin controllers, validation in the service, one error shape**
  - The same rules apply whatever calls the service.
  - 400 means fix your request; 409 means it changed under you; 404 for another adviser's quote, never 403.
- (If time) **The trace id as the one signal**: standard OpenTelemetry, exporters switch per environment (console here, X-Ray or Jaeger elsewhere).

## 4. How I used AI (~1.5 min)

- Claude Code: planning, then one coordinating session plus four parallel sessions (pricing, quotes, observability, UI) on separate branches.
- Show `docs/ai/PROMPTS.md`: every prompt is logged, including the briefs to each session.
- My decisions:
  - duplicate strategy, the signal, the scope of extras;
  - one validated, unit-tested commit per flow;
  - no AI attribution in commits.
- Where I didn't trust it:
  - Real-Postgres integration tests caught a reference-generation bug in the scaffold.
  - Cross-checking the sessions' reports found a duplicate span and clashing assumption IDs.
  - I had branches rebuilt so each commit stands alone, and checked the premium by hand.

## 5. What I'd improve (~1 min)

- Real auth: a JWT claim instead of the `X-Adviser-Id` header.
- Actuarial rate tables instead of illustrative bands.
- `allowedNextStatuses` from the API so the UI can't drift.
- An outbox for quote events.
- CI with a Postgres service container.
- Jaeger untested here (no Docker).
- AWS in one breath:
  - ECS Fargate behind an ALB, RDS PostgreSQL, Secrets Manager;
  - ADOT to X-Ray and CloudWatch;
  - UI on S3 and CloudFront;
  - migrations as a pipeline step.

## Close (~15 s)

- Happy to go deeper on any part in the interview; the README and SOLUTION.md cover the rest.

## Likely interview questions to have an answer for

- Why not just a unique constraint on the business fields for duplicates?
- What happens if the advisory lock hash collides? (Only extra serialisation, never a wrong result: the lookup still compares the full fingerprint.)
- How would you extract pricing without downtime?
- Why `xmin` and not a version column?
- How would you test the 10-minute window boundary? (`TimeProvider` is injected for that.)
- Where would an LLM fit, and where would you refuse to put one? (In SOLUTION.md: capture assistance and client summaries, never pricing or status rules.)
