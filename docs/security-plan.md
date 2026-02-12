# Security Plan (Locked for v1)

Status: LOCKED
Last updated: 2026-02-12

## Threat model boundary (v1)

In scope:

- Lost/stolen device with local DB extraction attempt
- Token theft from app storage
- Unauthorized API calls
- Basic replay and brute-force attempts

Out of scope for v1:

- Advanced malware on rooted/jailbroken devices
- Hardware-backed attestation
- Full SIEM integration

## Local encryption boundary

Encrypt at rest in local SQLite for sensitive fields:

- `Transaction.Description`
- `Transaction.Amount`
- `Category.Name`

Not encrypted:

- IDs, timestamps, flags, and sync metadata fields needed for indexing and merge behavior

## Key storage

- MAUI Android/Windows uses `SecureStorage` for envelope key material.
- Android backing: Android Keystore.
- Windows backing: DPAPI.
- Data encryption key (DEK) is generated on first run and wrapped by platform store.

## API authentication

- JWT bearer auth for API endpoints except health/auth login.
- Access token lifetime: 15 minutes.
- Refresh token lifetime: 30 days.
- Refresh tokens are rotate-on-use.
- Reuse detection invalidates token family.

## Token storage

- Access and refresh tokens stored only via `SecureStorage`.
- Never stored in plain preferences or logs.

## Transport and API hardening

- HTTPS required in non-local environments.
- CORS allowlist for dashboard origin.
- Request size limits and model validation on API.
- Basic rate limiting on auth and ingest endpoints.

## Telegram bot security

- Bot token stored in secret manager/environment variable, never in source control.
- Accept messages only from mapped ExternalIdentity accounts.
- Reject unknown Telegram users with explicit error.

## Logging rules

- No raw secrets or tokens in logs.
- Message payload logs are truncated and sanitized.
- Security events logged: login failure, token refresh failure, duplicate update, unauthorized access.
