# Decision Log (v1)

## 2026-02-12

1. v1 scope locked to command-based Telegram ingest + MAUI app + web dashboard.
2. Telegram command set locked to `/add` and `/help`; `/cat` deferred.
3. Idempotency locked on Telegram `update_id` unique index.
4. Domain entities locked: Transaction, Category, User, ExternalIdentity, IngestedMessage.
5. Required audit fields locked across root entities.
6. Sync approach locked: client-generated IDs, outbox, cursor pull, LWW conflict resolution.
7. Security baseline locked: encrypted local sensitive fields, SecureStorage-backed keys, JWT auth.
8. Solution structure locked with 8 projects under `src/`.

## Change policy

- Any change to a LOCKED item requires explicit new log entry with date and rationale.
- `README.md` and docs must remain consistent with latest decision log entry.
