# Sync Specification (Locked for v1)

Status: LOCKED
Last updated: 2026-02-12

## Model

- Offline-first source of interaction is the MAUI app local database.
- Server is system-of-record for cross-device consistency.
- v1 conflict strategy: last-write-wins by `UpdatedAt` UTC.

## Client-generated IDs

- App creates GUID string IDs for Transaction and Category before local save.
- Server must accept client IDs for app-originated entities.
- Telegram-originated entities are server-generated IDs.

## Outbox pattern

Local outbox table: `OutboxItem`

Fields:

- `Id` (string, PK)
- `EntityType` (`Transaction` | `Category`)
- `EntityId` (string)
- `Operation` (`Upsert` | `Delete`)
- `PayloadJson` (string)
- `AttemptCount` (int)
- `NextAttemptAt` (datetime UTC)
- `LastError` (string, nullable)
- `CreatedAt` (datetime UTC)
- `UpdatedAt` (datetime UTC)
- `Status` (`Pending` | `Processing` | `Failed`)

Processing rules:

1. Read oldest `Pending` where `NextAttemptAt <= now`.
2. Mark as `Processing`.
3. Push to API batch endpoint.
4. On success, remove outbox item.
5. On failure, increment attempts and set exponential backoff.

## Sync API shape

1. `POST /api/sync/push`
- Body: list of local changes with IDs and `UpdatedAt`.
- Response: accepted/rejected items and server winning versions where conflicts occurred.

2. `GET /api/sync/pull?cursor=<opaque>`
- Returns all changed entities since cursor.
- Includes tombstones for soft-deleted rows.
- Returns `nextCursor`.

## Cursor approach

- Cursor is server-issued opaque token representing high-water mark.
- Internally based on ordered (`UpdatedAt`, `Id`) pair.
- Client stores last successful cursor in local metadata table.

## Conflict resolution (LWW)

1. Compare server `UpdatedAt` and client `UpdatedAt`.
2. Newer timestamp wins.
3. If equal timestamp, lexicographically larger `Id` wins (deterministic tie-break).
4. Losing version is overwritten with winner payload.

## Deletes and tombstones

- Delete is a soft delete mutation.
- Sync must transmit `IsDeleted = true` and `DeletedAt`.
- Tombstones are returned in pull results until every active client has had chance to sync (v1 simplification: retain indefinitely).

## Reliability and telemetry

Capture per sync run:

- Start and end time
- Items pushed/pulled
- Conflicts encountered
- Failed items
- Last successful cursor

## v1 non-goals

- Field-level merge
- CRDT logic
- Per-entity custom conflict policies
