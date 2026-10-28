# Advisor Quotes UI

A small adviser UI for the Advisor Quotes API: list quotes, create a quote, view a quote and move it through its statuses. Vite + React + TypeScript, plain CSS, no UI kit or router.

## Run it

Needs Node 20+ and the API running (see the root README).

```bash
cd ui
npm install
npm run dev
```

Open `http://localhost:5173` and type an adviser id (any text, e.g. `ADV-001`) in the header bar, then press Enter or tab away. It is sent as `X-Adviser-Id` on every call and remembered in `localStorage`.

The API base URL defaults to `http://localhost:5222`. To point elsewhere, copy `.env.example` to `.env.local` and set `VITE_API_BASE_URL`.

The UI calls the API from another origin, so the API must allow CORS from `http://localhost:5173` and expose the `Idempotent-Replayed` and `X-Trace-Id` response headers. Errors show the trace id so it can be found in the API logs.

## Scripts

| Command | What it does |
|---|---|
| `npm run dev` | Dev server on port 5173 |
| `npm run build` | Type check and production build into `dist/` |
| `npm run lint` | ESLint |
| `npm test` | Vitest + Testing Library, once |
| `npm run e2e` | Playwright browser tests against the real API and database, with a video per test in `test-results/` (see the root README) |

## Layout

| Path | What |
|---|---|
| `src/api/types.ts` | TypeScript mirror of the `Contracts/` DTOs |
| `src/api/client.ts` | Typed fetch client: base URL, `X-Adviser-Id`, `ErrorResponse` parsing, `Idempotent-Replayed` |
| `src/api/idempotency.ts` | One `Idempotency-Key` per submission attempt, reused on retry |
| `src/adviser/` | Adviser id input and its `localStorage` persistence |
| `src/quotes/` | Quote screens |
