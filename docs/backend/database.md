# Backend - Database

---

## Technology

- **Database**: PostgreSQL 16
- **ORM**: Entity Framework Core 8 (Code-First)
- **Driver**: Npgsql 8 with dynamic JSON support

Connection is configured in `ServiceExtensions.AddDatabase()` using `NpgsqlDataSourceBuilder` with `EnableDynamicJson()` to support JSONB columns.

---

## Entity relationships

```
User ──< Listing                (host publishes listings)
User ──< Booking                (guest creates bookings)
User ──< Review                 (guest writes reviews)
User ──< Favorite               (user saves listings)
User ──< RefreshToken           (one user, many active sessions)
User ──< Conversation           (as User1 or User2)
User ──< Message                (as Sender)

Listing ──< ListingPhoto
Listing ──< ListingBlock        (host-blocked date ranges)
Listing ──< Booking
Listing ──< Review
Listing ──< Favorite

Booking ──< Review              (one review per completed booking)

Conversation ──< Message
```

---

## Entity field reference

### User

| Field          | Type        | Notes                                     |
| -------------- | ----------- | ----------------------------------------- |
| `Id`           | `uuid`      | PK, auto-generated                        |
| `Email`        | `text`      | Unique index                              |
| `PasswordHash` | `text`      | BCrypt hash — never returned in responses |
| `Name`         | `text`      | Display name                              |
| `Bio`          | `text?`     | Optional profile bio                      |
| `AvatarUrl`    | `text?`     | Relative URL to uploaded avatar           |
| `IsDeleted`    | `bool`      | Soft-delete flag                          |
| `CreatedAt`    | `timestamp` | UTC                                       |

### Listing

| Field           | Type            | Notes                                                    |
| --------------- | --------------- | -------------------------------------------------------- |
| `Id`            | `uuid`          | PK                                                       |
| `HostId`        | `uuid`          | FK → Users (Restrict)                                    |
| `Title`         | `text`          |                                                          |
| `Description`   | `text`          |                                                          |
| `Street`        | `text`          |                                                          |
| `City`          | `text`          |                                                          |
| `Country`       | `text`          |                                                          |
| `Bedrooms`      | `int`           |                                                          |
| `Bathrooms`     | `int`           |                                                          |
| `PricePerNight` | `numeric(10,2)` |                                                          |
| `MaxGuests`     | `int`           |                                                          |
| `IsInstantBook` | `bool`          | Skips Pending state — booking goes straight to Confirmed |
| `Amenities`     | `text[]`        | PostgreSQL JSONB array with custom EF value converter    |
| `Category`      | `text` (enum)   | Beach, Mountains, Urban, Luxe, Countryside, Island, Lake |
| `PropertyType`  | `text` (enum)   | Apartment, House                                         |
| `IsDeleted`     | `bool`          | Soft-delete flag                                         |
| `CreatedAt`     | `timestamp`     | UTC                                                      |

### ListingPhoto

| Field       | Type   | Notes                          |
| ----------- | ------ | ------------------------------ |
| `Id`        | `uuid` | PK                             |
| `ListingId` | `uuid` | FK → Listings (Cascade delete) |
| `Url`       | `text` | Relative URL to uploaded file  |
| `Order`     | `int`  | Display sequence               |

### ListingBlock

| Field       | Type       | Notes         |
| ----------- | ---------- | ------------- |
| `Id`        | `uuid`     | PK            |
| `ListingId` | `uuid`     | FK → Listings |
| `StartDate` | `DateOnly` | Inclusive     |
| `EndDate`   | `DateOnly` | Inclusive     |

### Booking

| Field        | Type            | Notes                                            |
| ------------ | --------------- | ------------------------------------------------ |
| `Id`         | `uuid`          | PK                                               |
| `ListingId`  | `uuid`          | FK → Listings                                    |
| `GuestId`    | `uuid`          | FK → Users (Restrict)                            |
| `CheckIn`    | `DateOnly`      |                                                  |
| `CheckOut`   | `DateOnly`      |                                                  |
| `Guests`     | `int`           | Headcount                                        |
| `TotalPrice` | `numeric(10,2)` | Computed at booking time: nights × PricePerNight |
| `Status`     | `text` (enum)   | Pending, Confirmed, Completed, Cancelled         |
| `CreatedAt`  | `timestamp`     | UTC                                              |

### Review

| Field       | Type        | Notes                                                    |
| ----------- | ----------- | -------------------------------------------------------- |
| `Id`        | `uuid`      | PK                                                       |
| `ListingId` | `uuid`      | FK → Listings                                            |
| `BookingId` | `uuid`      | FK → Bookings; **unique index** — one review per booking |
| `AuthorId`  | `uuid`      | FK → Users (Restrict)                                    |
| `Rating`    | `int`       | 1–5                                                      |
| `Comment`   | `text`      |                                                          |
| `CreatedAt` | `timestamp` | UTC                                                      |

### Favorite

| Field       | Type   | Notes         |
| ----------- | ------ | ------------- |
| `Id`        | `uuid` | PK            |
| `UserId`    | `uuid` | FK → Users    |
| `ListingId` | `uuid` | FK → Listings |

Composite unique index on `(UserId, ListingId)` — a user can only favorite a listing once.

### RefreshToken

| Field       | Type         | Notes                        |
| ----------- | ------------ | ---------------------------- |
| `Id`        | `uuid`       | PK                           |
| `UserId`    | `uuid`       | FK → Users                   |
| `Token`     | `text`       | 64-byte random opaque string |
| `ExpiresAt` | `timestamp`  | 7 days from creation         |
| `RevokedAt` | `timestamp?` | Null until revoked           |

`IsActive` is a computed property: `RevokedAt == null && ExpiresAt > DateTime.UtcNow`.

### Conversation

| Field       | Type        | Notes                              |
| ----------- | ----------- | ---------------------------------- |
| `Id`        | `uuid`      | PK                                 |
| `User1Id`   | `uuid`      | FK → Users; always the lower GUID  |
| `User2Id`   | `uuid`      | FK → Users; always the higher GUID |
| `CreatedAt` | `timestamp` | UTC                                |

Composite unique index on `(User1Id, User2Id)` — at most one conversation between any two users. Participants are stored with the lower GUID as User1 so the constraint is order-independent.

### Message

| Field            | Type            | Notes                                               |
| ---------------- | --------------- | --------------------------------------------------- |
| `Id`             | `uuid`          | PK                                                  |
| `ConversationId` | `uuid`          | FK → Conversations (Cascade delete)                 |
| `SenderId`       | `uuid`          | FK → Users (Restrict)                               |
| `Body`           | `varchar(4000)` |                                                     |
| `IsRead`         | `bool`          | Set to `true` when recipient opens the conversation |
| `CreatedAt`      | `timestamp`     | UTC                                                 |

---

## Enums

Enums are stored as strings in the database (configured via `JsonStringEnumConverter` and EF `HasConversion`):

| Enum              | Values                                                   |
| ----------------- | -------------------------------------------------------- |
| `ListingCategory` | Beach, Mountains, Urban, Luxe, Countryside, Island, Lake |
| `PropertyType`    | Apartment, House                                         |
| `BookingStatus`   | Pending, Confirmed, Completed, Cancelled                 |

---

## Key database constraints

| Table           | Constraint                                     |
| --------------- | ---------------------------------------------- |
| `Users`         | `Email` is unique                              |
| `Reviews`       | `BookingId` is unique (one review per booking) |
| `Favorites`     | `(UserId, ListingId)` composite unique         |
| `Conversations` | `(User1Id, User2Id)` composite unique          |

---

## Booking status flow

```
Pending → Confirmed → Completed
       ↘           ↘
        Cancelled   Cancelled
```

- `Pending` — initial state when `IsInstantBook = false`. Host must approve.
- `Confirmed` — set immediately when `IsInstantBook = true`, or when the host calls `PATCH /bookings/:id/status`.
- `Completed` — terminal; set by the host. Reviews are only allowed after this state.
- `Cancelled` — either guest or host can cancel any non-Completed booking.

---

## Soft-delete pattern

Both `User` and `Listing` use a soft-delete approach (`IsDeleted` flag) instead of physical deletion. This preserves FK integrity for related bookings, reviews, and messages. Soft-deleted users have their PII wiped; soft-deleted listings return 410 Gone on direct access.

---

## Migrations

Managed with EF Core CLI. After changing an entity:

```bash
cd placey-be
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

To remove the last migration (only if not yet applied):

```bash
dotnet ef migrations remove
```

In Development, migrations are applied automatically on startup via `db.Database.MigrateAsync()`.

