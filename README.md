# Personal Finance Offline Tracker

Offline-first personal finance tracker with Telegram command ingest, MAUI Hybrid app, and a web dashboard.

## Current status

- v1 boundaries and architecture are documented and locked.
- Solution and project skeleton is scaffolded.
- Implementation is not started yet.

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

1. Implement domain entities and EF Core schema.
2. Implement Telegram command parser and ingestion worker pipeline.
3. Implement API auth and transaction/category endpoints.
4. Implement sync endpoints and outbox processing.
5. Implement MAUI local DB + offline CRUD + sync client.
6. Implement dashboard API integration.
7. Add integration tests for idempotency and sync conflict cases.
