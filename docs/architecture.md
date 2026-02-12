# Architecture and Solution Structure (Locked for v1)

Status: LOCKED
Last updated: 2026-02-12

## Solution

- `PersonalFinanceOfflineTracker.slnx`

## Project layout

- `src/PersonalFinanceOfflineTracker.Shared`
- `src/PersonalFinanceOfflineTracker.Domain`
- `src/PersonalFinanceOfflineTracker.Infrastructure`
- `src/PersonalFinanceOfflineTracker.Sync`
- `src/PersonalFinanceOfflineTracker.Api`
- `src/PersonalFinanceOfflineTracker.Workers.TelegramIngest`
- `src/PersonalFinanceOfflineTracker.Apps.Maui`
- `src/PersonalFinanceOfflineTracker.Apps.WebDashboard`

## Responsibilities

1. Shared

    - Cross-cutting primitives, constants, common result types.

2. Domain

    - Entities, value objects, business rules, domain services.

3. Infrastructure

    - EF Core DbContext, repositories, encryption providers, JWT services, Telegram integrations.

4. Sync

    - Outbox processor, sync DTOs, merge logic, cursor handling.

5. Api

    - REST endpoints, auth endpoints, sync endpoints, validation, composition root.

6. Workers.TelegramIngest

    - Long polling loop, command parsing pipeline, idempotent ingestion orchestration.

7. Apps.Maui

    - Offline local DB, UI, background sync trigger, secure token/key usage.

8. Apps.WebDashboard

    - Browser UI for listing/filtering transactions and category management via API.

## Naming conventions

- Namespace prefix: `PersonalFinanceOfflineTracker.*`
- Public types: PascalCase
- Private fields: `_camelCase`
- Async methods: `*Async`
- DTO suffix: `Dto`
- Command/query suffixes: `Command`, `Query`

## Folder conventions inside each project

- `Abstractions/`
- `Models/`
- `Services/`
- `Persistence/` (where applicable)
- `Endpoints/` (API only)
- `Jobs/` (worker/sync only)

## Dependency direction

- `Shared` has no project dependencies.
- `Domain` depends on `Shared` only.
- `Infrastructure` depends on `Domain` and `Shared`.
- `Sync` depends on `Domain`, `Infrastructure`, `Shared`.
- `Api` depends on `Sync`, `Infrastructure`, `Domain`, `Shared`.
- `Workers.TelegramIngest` depends on `Sync`, `Infrastructure`, `Domain`, `Shared`.
- `Apps.Maui` depends on `Sync`, `Infrastructure`, `Domain`, `Shared`.
- `Apps.WebDashboard` depends on `Domain` and `Shared`.
