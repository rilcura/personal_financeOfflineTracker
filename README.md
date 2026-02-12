# Personal Finance Offline Tracker

Offline-first personal finance tracker with Telegram command ingest, MAUI Hybrid app, and a web dashboard.

## Current status

- v1 boundaries and architecture are documented and locked.
- Solution and project skeleton is scaffolded.
- Domain entities are implemented with core invariants and audit behavior.
- Infrastructure EF Core persistence is implemented (DbContext + entity configurations).
- Initial EF Core migration is generated in `src/PersonalFinanceOfflineTracker.Infrastructure/Persistence/Migrations`.
- API is wired to SQLite and applies migrations at startup (`FinanceDbContext.Database.Migrate()`).
- JWT auth is implemented with seeded single-user login.
- Authenticated categories and transactions endpoints are implemented with EF Core persistence.
- Sync service is now DB-backed (no in-memory sync state).
- MAUI app now has local SQLite outbox infrastructure and a background sync worker scaffold.
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
2. Wire MAUI UI flows to enqueue sync changes through the outbox service.
3. Implement MAUI local transaction/category storage and offline CRUD screens.
4. Implement dashboard API integration.
5. Add integration tests for idempotency and sync conflict cases.

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

Login (seeded dev user):

- Email: `owner@local.dev`
- Password: `P@ssword123!`

Change these in `src/PersonalFinanceOfflineTracker.Api/appsettings.json` (`SeedUser` section) before non-local usage.

JWT settings are in `src/PersonalFinanceOfflineTracker.Api/appsettings.json` (`Jwt` section). Replace `SigningKey` for production.

Implemented API routes:

- `POST /api/auth/login`
- `GET /api/auth/me` (Bearer token required)
- `GET|POST|PUT|DELETE /api/categories`
- `GET|POST|PUT|DELETE /api/transactions`
- `POST /api/sync/push` and `GET /api/sync/pull` (Bearer token required)

Sync notes:

- Server sync service uses database state for pull/push conflict checks (LWW by `UpdatedAt`, then `Id`).
- Soft-deleted records are included in sync pull using tombstone semantics.

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
- MAUI local sync worker now runs on app startup and reads pending outbox rows from `local_sync.db` in app data.
- To enable authenticated push from MAUI, store API token/session through `ISyncTokenStore` (`sync_access_token`, `sync_user_id` keys).
