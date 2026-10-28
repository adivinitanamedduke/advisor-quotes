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

## Automatic entries
