# Personal Finance Offline Tracker

A C#/.NET prototype exploring expense tracking across a MAUI Blazor Hybrid app, an ASP.NET Core API, and a web dashboard, with foundations for offline synchronization and Telegram capture.

**Status:** In development. The API and Sync Lab are implemented development surfaces. A finished consumer app, live Telegram integration, and production readiness are not claimed.

**Stack:** .NET 10, C#, .NET MAUI, Blazor, ASP.NET Core, EF Core, SQLite, and JWT.

## Engineering focus

- Separate domain, persistence, API, synchronization, and UI projects.
- Authenticated transaction/category endpoints and user-scoped data access.
- Local outbox persistence and a background push worker.
- Audit fields, soft deletes, and documented conflict rules.
- A Sync Lab interface for examining changes before a complete finance UI exists.

## What remains unfinished

The MAUI app retains template pages alongside Sync Lab. The web dashboard still needs finance screens and API integration. Telegram parsing and ingestion classes exist, but the worker registers in-memory update and storage implementations rather than a live Telegram connection. No automated test project is present in the current tree.

Screenshots, a recorded offline-to-online demonstration, integration tests, and device validation are next milestones.

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
- MAUI Sync Lab UI is wired to enqueue category/transaction upsert/delete sync changes into the local outbox.
- Earlier repository documentation reports successful builds for API, Domain, Infrastructure, Sync, Worker, and WebDashboard. This README refresh did not rerun builds.

## Intended v1 scope

- Telegram capture: slash commands + long polling + idempotency.
- Backend API: JWT auth, transactions/categories CRUD, sync push/pull.
- MAUI Blazor Hybrid app: offline CRUD, local SQLite, background sync.
- Web dashboard: transaction views and category management.
- Sync foundations: outbox, tombstones, audit fields, LWW conflict handling.

## Design documents

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

1. Connect the existing Telegram parser and ingestion pipeline to a real update source and durable storage.
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

## Local development

Prerequisites: .NET 10 SDK and the platform-specific MAUI workloads/tooling for the target you intend to build. Windows MAUI commands below require Windows. Target declarations do not establish tested device support.

### Web dashboard

Run:

`dotnet run --project src/PersonalFinanceOfflineTracker.Apps.WebDashboard/PersonalFinanceOfflineTracker.Apps.WebDashboard.csproj`

Default local URL:

`http://localhost:5000` or `https://localhost:5001` (depending on launch profile/port availability)

### API

The MAUI sync client defaults to `http://localhost:5089`, while the API launch profiles use ports 5283/7002. For a Windows local preview, explicitly bind the API to the client default:

```sh
dotnet run --project src/PersonalFinanceOfflineTracker.Api/PersonalFinanceOfflineTracker.Api.csproj --no-launch-profile --urls http://localhost:5089
```

Alternatively, update `SyncClientOptions.ApiBaseUrl` to the address printed by the API. An emulator or physical device needs a host address reachable from that device.

To use the repository launch profile instead:

`dotnet run --project src/PersonalFinanceOfflineTracker.Api/PersonalFinanceOfflineTracker.Api.csproj`

Development authentication:

The API seeds a local user from the `SeedUser` configuration section. Configure your own development login and a `Jwt:SigningKey` using environment variables or another local configuration provider. Do not publish real credentials. Existing source defaults are development placeholders and need replacement before non-local use.

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

This currently runs with in-memory update/store registrations. Starting the process does not connect to a live Telegram bot.

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
- Use the **Sync Lab** menu page to login and enqueue test upsert/delete changes for categories and transactions.
