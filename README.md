# Personal Finance Offline Tracker

Offline-first personal finance tracker with Telegram command ingest, MAUI Hybrid app, and a web dashboard.

## Current status

- v1 boundaries and architecture are documented and locked.
- Solution and project skeleton is scaffolded.
- Domain entities are implemented with core invariants and audit behavior.
- Infrastructure EF Core persistence is implemented (DbContext + entity configurations).
- Initial EF Core migration is generated in `src/PersonalFinanceOfflineTracker.Infrastructure/Persistence/Migrations`.
- API is wired to SQLite and applies migrations at startup (`FinanceDbContext.Database.Migrate()`).
- API, Domain, Infrastructure, Sync, Worker, and WebDashboard projects build successfully.

## v1 locked scope

- Telegram capture: slash commands + long polling + idempotency.
- Backend API: JWT auth, transactions/categories CRUD, sync push/pull.
- MAUI Blazor Hybrid app: offline CRUD, local SQLite, background sync.
- Web dashboard: transaction views and category management.
- Sync foundations: outbox, tombstones, audit fields, LWW conflict handling.

## Locked docs

1. Message contract: `docs/telegram-message-contract.md`
2. Domain model: `docs/domain-model.md`
3. Sync rules: `docs/sync-spec.md`
4. Security plan: `docs/security-plan.md`
5. Architecture: `docs/architecture.md`
6. Decision log: `docs/decision-log.md`

## Solution structure

- `PersonalFinanceOfflineTracker.slnx`
- `src/PersonalFinanceOfflineTracker.Shared`
- `src/PersonalFinanceOfflineTracker.Domain`
- `src/PersonalFinanceOfflineTracker.Infrastructure`
- `src/PersonalFinanceOfflineTracker.Sync`
- `src/PersonalFinanceOfflineTracker.Api`
- `src/PersonalFinanceOfflineTracker.Workers.TelegramIngest`
- `src/PersonalFinanceOfflineTracker.Apps.Maui`
- `src/PersonalFinanceOfflineTracker.Apps.WebDashboard`

## Suggested implementation order

1. Implement Telegram command parser and ingestion worker pipeline.
2. Implement API auth and transaction/category endpoints.
3. Implement sync endpoints and outbox processing.
4. Implement MAUI local DB + offline CRUD + sync client.
5. Implement dashboard API integration.
6. Add integration tests for idempotency and sync conflict cases.

## Database migrations

Local EF tool is installed via `dotnet-tools.json`.

Create migration:

`dotnet dotnet-ef migrations add <MigrationName> --project src/PersonalFinanceOfflineTracker.Infrastructure/PersonalFinanceOfflineTracker.Infrastructure.csproj --startup-project src/PersonalFinanceOfflineTracker.Api/PersonalFinanceOfflineTracker.Api.csproj --context FinanceDbContext --output-dir Persistence/Migrations`

Apply migration:

`dotnet dotnet-ef database update --project src/PersonalFinanceOfflineTracker.Infrastructure/PersonalFinanceOfflineTracker.Infrastructure.csproj --startup-project src/PersonalFinanceOfflineTracker.Api/PersonalFinanceOfflineTracker.Api.csproj --context FinanceDbContext`

## Run and test (current)

### Web dashboard

Run:

`dotnet run --project src/PersonalFinanceOfflineTracker.Apps.WebDashboard/PersonalFinanceOfflineTracker.Apps.WebDashboard.csproj`

Default local URL:

`http://localhost:5000` or `https://localhost:5001` (depending on launch profile/port availability)

### API

Run:

`dotnet run --project src/PersonalFinanceOfflineTracker.Api/PersonalFinanceOfflineTracker.Api.csproj`

### Telegram worker

Run:

`dotnet run --project src/PersonalFinanceOfflineTracker.Workers.TelegramIngest/PersonalFinanceOfflineTracker.Workers.TelegramIngest.csproj`

### MAUI Hybrid app

Build (Windows target):

`dotnet build src/PersonalFinanceOfflineTracker.Apps.Maui/PersonalFinanceOfflineTracker.Apps.Maui.csproj -f net10.0-windows10.0.19041.0`

Run (Windows target):

`dotnet run --project src/PersonalFinanceOfflineTracker.Apps.Maui/PersonalFinanceOfflineTracker.Apps.Maui.csproj -f net10.0-windows10.0.19041.0`

Notes:

- Android target may require additional local Android workload/tooling setup.
- If build output is locked, stop any running worker/app process before rebuilding.
