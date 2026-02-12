# Telegram Message Contract (Locked for v1)

Status: LOCKED
Owner: Personal project
Last updated: 2026-02-12

## Supported commands

1. `/add <amount> <description> [category] [date]`
2. `/help`

`/cat` is OUT of v1. Categories are managed from app/dashboard in v1.

## `/add` grammar

`/add <amount> <description> [category] [date]`

- Token separator: one or more spaces.
- Description can be wrapped in double quotes when it contains spaces.
- Category can be wrapped in double quotes when it contains spaces.

Examples:

- `/add 120 coffee`
- `/add 450 groceries food`
- `/add 120.50 "coffee beans" groceries 2026-02-11`
- `/add 999.99 "Laptop sleeve" "Office Supplies" 2026-02-10`

## Parsing and validation rules

1. Command name must be exactly `/add`.
2. `amount` is required.
3. `amount` format: positive decimal with max 2 decimal places.
4. `amount` range: `0.01` to `9999999.99`.
5. `description` is required.
6. `description` length: 1 to 50 chars after trim.
7. `category` is optional; default is `Uncategorized`.
8. `category` length: 1 to 30 chars after trim.
9. `date` is optional; default is server local date at ingest time.
10. `date` accepted format: `yyyy-MM-dd`.
11. `date` must be between `2000-01-01` and `today + 1 day`.
12. Any extra token after `date` is invalid.
13. Empty quoted values are invalid.

## Normalization rules

- Decimal is stored as numeric, scale 2.
- Description and category are trimmed and collapsed for internal repeated spaces.
- Category compare is case-insensitive for existing category match.

## Error responses

Telegram response style is short, deterministic, and user-actionable.

- `ERR_FORMAT`: `Invalid format. Use: /add <amount> <description> [category] [yyyy-MM-dd]`
- `ERR_AMOUNT_REQUIRED`: `Amount is required.`
- `ERR_AMOUNT_INVALID`: `Amount must be a positive number with up to 2 decimals.`
- `ERR_AMOUNT_RANGE`: `Amount must be between 0.01 and 9999999.99.`
- `ERR_DESCRIPTION_REQUIRED`: `Description is required.`
- `ERR_DESCRIPTION_LENGTH`: `Description must be 1-50 chars.`
- `ERR_CATEGORY_LENGTH`: `Category must be 1-30 chars.`
- `ERR_DATE_INVALID`: `Date must be yyyy-MM-dd.`
- `ERR_DATE_RANGE`: `Date is out of allowed range.`
- `ERR_DUPLICATE_UPDATE`: `Message already processed.`
- `ERR_INTERNAL`: `Could not process command. Try again later.`

## Success response

- `OK_ADD`: `Saved: <amount> <description> [<category>] on <yyyy-MM-dd>`

Example:

`Saved: 120.50 coffee beans [groceries] on 2026-02-11`

## Idempotency rule (locked)

- Telegram `update_id` is stored in `IngestedMessage.TelegramUpdateId`.
- Unique index on `TelegramUpdateId`.
- If duplicate `update_id` arrives:
1. Do not create/update transaction again.
2. Return `ERR_DUPLICATE_UPDATE`.
3. Keep first processing outcome as source of truth.

## Ingestion pipeline

1. Long-poll Telegram `getUpdates`.
2. Persist raw message audit record (IngestedMessage).
3. Validate command and parse into canonical DTO.
4. Resolve ExternalIdentity to User.
5. Create Transaction with `Source = Telegram`.
6. Commit transaction + mark ingested status in one DB transaction.
