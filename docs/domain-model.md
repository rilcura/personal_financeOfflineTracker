# Domain Data Model (Locked for v1)

Status: LOCKED
Last updated: 2026-02-12

## Conventions

- IDs are GUID strings generated client-side for app-originated records.
- Timestamps are UTC.
- Soft delete uses `IsDeleted` + nullable `DeletedAt`.
- Audit fields exist on all root entities.

## Entity: User

Fields:

- `Id` (string, PK)
- `Email` (string, unique)
- `DisplayName` (string, required)
- `PasswordHash` (string, required)
- `CreatedAt` (datetime, required)
- `UpdatedAt` (datetime, required)
- `IsDeleted` (bool, required, default false)
- `DeletedAt` (datetime, nullable)
- `Source` (string, required, `System`)

Indexes:

- Unique: `Email`

## Entity: Category

Fields:

- `Id` (string, PK)
- `UserId` (string, FK -> User.Id)
- `Name` (string, required)
- `NormalizedName` (string, required)
- `CreatedAt` (datetime, required)
- `UpdatedAt` (datetime, required)
- `IsDeleted` (bool, required, default false)
- `DeletedAt` (datetime, nullable)
- `Source` (string, required: `App`, `Telegram`, `Web`)

Indexes:

- Unique per user: (`UserId`, `NormalizedName`) where `IsDeleted = false`

## Entity: Transaction

Fields:

- `Id` (string, PK)
- `UserId` (string, FK -> User.Id)
- `CategoryId` (string, FK -> Category.Id, nullable)
- `Amount` (decimal(18,2), required)
- `Description` (string, required)
- `TransactionDate` (date, required)
- `CreatedAt` (datetime, required)
- `UpdatedAt` (datetime, required)
- `IsDeleted` (bool, required, default false)
- `DeletedAt` (datetime, nullable)
- `Source` (string, required: `App`, `Telegram`, `Web`)

Indexes:

- (`UserId`, `TransactionDate`)
- (`UserId`, `UpdatedAt`)

## Entity: ExternalIdentity

Fields:

- `Id` (string, PK)
- `UserId` (string, FK -> User.Id)
- `Provider` (string, required, v1 fixed: `Telegram`)
- `ProviderUserId` (string, required)
- `ProviderChatId` (string, nullable)
- `CreatedAt` (datetime, required)
- `UpdatedAt` (datetime, required)
- `IsDeleted` (bool, required, default false)
- `DeletedAt` (datetime, nullable)
- `Source` (string, required, `System`)

Indexes:

- Unique: (`Provider`, `ProviderUserId`)
- Non-unique: (`Provider`, `ProviderChatId`)

## Entity: IngestedMessage

Fields:

- `Id` (string, PK)
- `Provider` (string, required, `Telegram`)
- `TelegramUpdateId` (long, required)
- `TelegramMessageId` (long, nullable)
- `RawText` (string, nullable)
- `NormalizedCommand` (string, nullable)
- `ParseStatus` (string, required: `Parsed`, `Rejected`, `Duplicate`, `Failed`)
- `ErrorCode` (string, nullable)
- `UserId` (string, FK -> User.Id, nullable)
- `CreatedTransactionId` (string, FK -> Transaction.Id, nullable)
- `ReceivedAt` (datetime, required)
- `ProcessedAt` (datetime, nullable)
- `CreatedAt` (datetime, required)
- `UpdatedAt` (datetime, required)
- `IsDeleted` (bool, required, default false)
- `DeletedAt` (datetime, nullable)
- `Source` (string, required, `Telegram`)

Indexes:

- Unique: (`Provider`, `TelegramUpdateId`)
- (`ReceivedAt`)
- (`ParseStatus`)

## Relationship summary

- User 1..* Category
- User 1..* Transaction
- User 1..* ExternalIdentity
- User 1..* IngestedMessage (optional link)
- Category 1..* Transaction (optional on transaction)

## v1 invariants

1. Every Transaction must have UserId.
2. Every Transaction must have Source.
3. Soft deleted rows are never physically removed in v1.
4. `UpdatedAt` changes on every mutation.
5. Telegram-originated inserts must create an IngestedMessage record.
