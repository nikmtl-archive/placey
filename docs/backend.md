# Backend — Architecture & Developer Guide

## Stack

| Layer | Technology |
|-------|-----------|
| Runtime | ASP.NET Core 8 Web API |
| ORM | Entity Framework Core 8 (Npgsql) |
| Database | PostgreSQL 16 |
| Auth | JWT Bearer (access tokens) + opaque refresh tokens stored in DB |
| Password hashing | BCrypt.Net-Next |
| Real-time | Server-Sent Events (SSE) via in-process `SseHub` |
| API docs | Swagger / Swashbuckle |

---

## Project structure

```
placey-be/
├── Controllers/                HTTP layer — thin, no business logic
│   ├── AuthController.cs
│   ├── BookingsController.cs
│   ├── FavoritesController.cs
│   ├── ListingsController.cs
│   ├── MessagesController.cs
│   ├── ReviewsController.cs
│   └── UsersController.cs
├── Data/
│   └── AppDbContext.cs         EF Core DbContext + all model configuration
├── Extensions/
│   ├── ClaimsPrincipalExtensions.cs   GetUserId() helper — reads sub claim
│   ├── ServiceExtensions.cs           DI wiring, JWT config, Swagger config
│   └── SwaggerExamplesFilter.cs       Hides internal fields from Swagger UI
├── Middleware/
│   └── ErrorHandlingMiddleware.cs     Maps typed exceptions → ProblemDetails
├── Migrations/                 EF Core migration history
├── Models/
│   ├── DTOs/                   Request/response records (entities never exposed directly)
│   │   ├── Auth/
│   │   ├── Bookings/
│   │   ├── Listings/
│   │   ├── Messages/
│   │   ├── Reviews/
│   │   └── Users/
│   └── Entities/               EF Core entity classes
│       ├── Availability.cs
│       ├── Booking.cs
│       ├── Conversation.cs
│       ├── Favorite.cs
│       ├── Listing.cs
│       ├── ListingEnums.cs     ListingCategory, PropertyType enums
│       ├── ListingPhoto.cs
│       ├── Message.cs
│       ├── RefreshToken.cs
│       ├── Review.cs
│       └── User.cs
├── Services/
│   ├── Interfaces/             One interface per domain
│   ├── AuthService.cs
│   ├── BookingService.cs
│   ├── FavoriteService.cs
│   ├── ListingService.cs
│   ├── MessageService.cs
│   ├── ReviewService.cs
│   ├── SseHub.cs               In-process pub/sub for real-time messaging
│   └── UserService.cs
├── appsettings.json            Logging config only — no secrets here
├── appsettings.Development.json  Dev overrides (DB, JWT)
└── Program.cs                  Minimal — delegates to ServiceExtensions
```

---

## Architecture

A straightforward three-layer architecture with no extra abstractions:

```
HTTP request
    │
    ▼
Controller          model binding + [Authorize] check + reads caller ID from JWT
    │
    ▼
Service             all business logic: ownership checks, conflict detection, etc.
    │
    ▼
AppDbContext        direct EF Core queries — no separate repository layer
    │
    ▼
PostgreSQL
```

**Controllers** are intentionally thin. They extract the caller's user ID from the JWT claim via `User.GetUserId()`, call the relevant service method, and return the result. No business logic lives in controllers.

**Services** own every rule: ownership checks, date conflict detection, review eligibility, price calculation, etc. They throw typed exceptions (`KeyNotFoundException`, `UnauthorizedAccessException`, `InvalidOperationException`) which the error middleware converts to the appropriate HTTP status codes.

**DTOs** (in `Models/DTOs/`) decouple the API surface from the database schema. Entity classes are never serialised directly.

---

## Database schema

### Entities & relationships

```
User ──< Listing                (host–listing)
User ──< Booking                (guest–booking)
User ──< Review                 (author–review)
User ──< Favorite               (user saves many listings)
User ──< RefreshToken           (one user, many active sessions)
User ──< Conversation (as User1 or User2)
User ──< Message (as Sender)

Listing ──< ListingPhoto
Listing ──< Availability
Listing ──< Booking
Listing ──< Review
Listing ──< Favorite

Booking ──< Review              (one review per completed booking)

Conversation ──< Message
```

### Entity field reference

**User**

| Field | Type | Notes |
|-------|------|-------|
| `Id` | `uuid` | PK |
| `Email` | `text` | Unique index |
| `PasswordHash` | `text` | BCrypt hash — never returned in responses |
| `Name` | `text` | Display name |
| `Bio` | `text?` | Optional profile bio |
| `AvatarUrl` | `text?` | URL to uploaded avatar |
| `CreatedAt` | `timestamp` | UTC |

**Listing**

| Field | Type | Notes |
|-------|------|-------|
| `Id` | `uuid` | PK |
| `HostId` | `uuid` | FK → Users (Restrict) |
| `Title` | `text` | |
| `Description` | `text` | |
| `Street / City / Country` | `text` | Address split into three fields |
| `Bedrooms / Bathrooms` | `int` | |
| `PricePerNight` | `numeric(10,2)` | |
| `MaxGuests` | `int` | |
| `IsInstantBook` | `bool` | Skips Pending → goes straight to Confirmed |
| `Amenities` | `text[]` | PostgreSQL text array |
| `Category` | enum | Beach, Mountains, Urban, Luxe, Countryside, Island, Lake |
| `PropertyType` | enum | Apartment, House |
| `CreatedAt` | `timestamp` | UTC |

**Booking**

| Field | Type | Notes |
|-------|------|-------|
| `Id` | `uuid` | PK |
| `ListingId` | `uuid` | FK → Listings |
| `GuestId` | `uuid` | FK → Users (Restrict) |
| `CheckIn / CheckOut` | `DateOnly` | Date only — no time component |
| `Guests` | `int` | Headcount |
| `TotalPrice` | `numeric(10,2)` | Computed at booking time |
| `Status` | enum | Pending, Confirmed, Completed, Cancelled |
| `CreatedAt` | `timestamp` | UTC |

**Review**

| Field | Type | Notes |
|-------|------|-------|
| `Id` | `uuid` | PK |
| `ListingId` | `uuid` | FK → Listings |
| `BookingId` | `uuid` | FK → Bookings; unique index (one review per booking) |
| `AuthorId` | `uuid` | FK → Users (Restrict) |
| `Rating` | `int` | 1–5 |
| `Comment` | `text` | |
| `CreatedAt` | `timestamp` | UTC |

**Availability**

| Field | Type | Notes |
|-------|------|-------|
| `Id` | `uuid` | PK |
| `ListingId` | `uuid` | FK → Listings |
| `StartDate / EndDate` | `DateOnly` | Inclusive range |
| `IsBlocked` | `bool` | `true` = blocked; `false` = explicitly available |

**Conversation**

| Field | Type | Notes |
|-------|------|-------|
| `Id` | `uuid` | PK |
| `User1Id` | `uuid` | FK → Users; always the lower GUID |
| `User2Id` | `uuid` | FK → Users; always the higher GUID |
| `CreatedAt` | `timestamp` | UTC |

Unique index on `(User1Id, User2Id)` — at most one conversation between any two users.

**Message**

| Field | Type | Notes |
|-------|------|-------|
| `Id` | `uuid` | PK |
| `ConversationId` | `uuid` | FK → Conversations (Cascade delete) |
| `SenderId` | `uuid` | FK → Users (Restrict) |
| `Body` | `varchar(4000)` | |
| `IsRead` | `bool` | Set to `true` when recipient opens the conversation |
| `CreatedAt` | `timestamp` | UTC |

**RefreshToken**

| Field | Type | Notes |
|-------|------|-------|
| `Id` | `uuid` | PK |
| `UserId` | `uuid` | FK → Users |
| `Token` | `text` | 64-byte random opaque string |
| `ExpiresAt` | `timestamp` | 7 days from creation |
| `RevokedAt` | `timestamp?` | Null until revoked; `IsActive` checks both |

### Key constraints

| Table | Constraint |
|-------|-----------|
| `Users` | `Email` is unique |
| `Reviews` | `BookingId` unique (one review per booking) |
| `Favorites` | `(UserId, ListingId)` composite unique |
| `Conversations` | `(User1Id, User2Id)` composite unique |

### Booking status flow

```
Pending → Confirmed → Completed
       ↘           ↘
        Cancelled   Cancelled
```

- `Pending` — initial state when `IsInstantBook = false`. Host must approve.
- `Confirmed` — set immediately when `IsInstantBook = true`, or when the host calls `PATCH /bookings/:id/status`.
- `Completed` — terminal; set by the host. Reviews are only allowed after this state.
- `Cancelled` — either guest or host can cancel, unless the booking is `Completed`.

---

## Authentication & Authorization

### Token strategy

| Token | Lifetime | Where stored |
|-------|----------|-------------|
| Access token (JWT) | 15 minutes | Client memory — sent in `Authorization: Bearer` header |
| Refresh token | 7 days | `RefreshTokens` DB table; rotated on each use |

### What is a JWT?

A JWT (JSON Web Token) is a self-contained signed string in three base64-encoded parts:

```
header.payload.signature
```

The **payload** carries claims the server trusts without a DB lookup because the **signature** (HMAC-SHA256 over header + payload using `Jwt:Secret`) proves the payload wasn't tampered with.

Claims embedded in the access token:

| Claim | Value |
|-------|-------|
| `sub` | User GUID — the primary identity used in every service |
| `email` | User email |
| `jti` | Random GUID — makes each token unique |

Because JWTs are self-verifying, they cannot be revoked before they expire — hence the short 15-minute lifetime.

### Auth flow

```
POST /auth/register  →  BCrypt hash password, create User, issue token pair
POST /auth/login     →  verify BCrypt hash, issue token pair
POST /auth/refresh   →  validate refresh token in DB, rotate, issue new pair
POST /auth/logout    →  mark refresh token as RevokedAt = now
```

Typical client session:

```
Client                              Server
  |                                   |
  |── POST /auth/login ──────────────>|  password verified, tokens issued
  |<── { accessToken, refreshToken } ─|
  |                                   |
  |── GET /listings                   |
  |   Authorization: Bearer <jwt> ──>|  JWT middleware validates; [Authorize] passes
  |<── 200 OK ────────────────────────|
  |                                   |
  |   (15 min later — JWT expired)    |
  |── POST /auth/refresh ────────────>|  refresh token validated, both rotated
  |<── { new accessToken, ... } ──────|
```

### How `[Authorize]` works

`ServiceExtensions.AddJwtAuth` configures the JWT middleware to run on every request. It:

1. Reads the `Authorization: Bearer <token>` header (or `?access_token=` query param for SSE endpoints).
2. Validates signature, issuer, audience, and expiry.
3. If valid, populates `HttpContext.User` with the claims.

`[Authorize]` on a controller or action simply checks that step 3 succeeded. Missing, expired, or tampered tokens result in **401 Unauthorized** before the controller method is called.

**SSE exception:** `EventSource` cannot send custom headers, so the JWT for the `/messages/stream` endpoint is read from the `?access_token=` query parameter. `AddJwtAuth` handles this via `OnMessageReceived`.

### Resource-level authorization

No roles exist. Any authenticated user can host by creating a listing. Owner-only actions are enforced in the service layer by comparing the caller's ID against the resource's owner field:

```csharp
if (listing.HostId != requesterId)
    throw new UnauthorizedAccessException("Only the host can update this listing.");
```

The error middleware maps `UnauthorizedAccessException` → **403 Forbidden**.

### Refresh token rotation

Every `POST /auth/refresh`:
1. Validates the submitted token is in the DB, not expired, and not revoked.
2. Sets `RevokedAt` on the old token.
3. Creates and returns a new token pair.

Each refresh token can therefore only be used once. `POST /auth/logout` simply sets `RevokedAt` immediately without issuing a new pair.

---

## Real-time messaging (SseHub)

`SseHub` is registered as a **singleton** and holds an in-memory `ConcurrentDictionary<Guid, Channel<MessageResponse>>` keyed by user ID.

```csharp
Subscribe(userId)       // called when the client opens GET /messages/stream
Unsubscribe(userId)     // called when the HTTP connection closes
Push(recipientId, msg)  // called by MessageService after persisting a message
```

`MessageService.SendMessageAsync` calls `Push` after saving — so the recipient gets the message immediately if they have an active SSE connection, with no polling.

**Limitation:** channels are in-process only. Horizontal scaling across multiple API instances requires a shared pub/sub backend (e.g. Redis).

---

## Error handling

`ErrorHandlingMiddleware` catches all unhandled exceptions and returns RFC 7807 `ProblemDetails` JSON:

| Exception type | HTTP status |
|----------------|-------------|
| `KeyNotFoundException` | 404 Not Found |
| `UnauthorizedAccessException` | 403 Forbidden |
| `InvalidOperationException` | 400 Bad Request |
| anything else | 500 Internal Server Error |

In Development, the `detail` field includes the exception message. In Production it is suppressed.

---

## Environment variables

All secrets are injected at runtime via environment variables. ASP.NET Core maps `__` to `:`, so `Jwt__Secret` resolves to `Jwt:Secret` in `IConfiguration`.

| Variable | Purpose |
|----------|---------|
| `ConnectionStrings__Default` | Full PostgreSQL connection string |
| `Jwt__Secret` | HMAC signing key — must be ≥ 32 characters |
| `Jwt__Issuer` | JWT `iss` claim (default: `placey`) |
| `Jwt__Audience` | JWT `aud` claim (default: `placey`) |
| `ASPNETCORE_ENVIRONMENT` | `Development` or `Production` |

Copy `.env.example` to `.env` at the repo root and fill in your values. Docker Compose reads `.env` automatically.

---

## Running locally (without Docker)

**Prerequisites:** .NET 8 SDK, a running PostgreSQL instance.

```bash
# 1. Start only the database
docker compose up postgres -d

# 2. Restore dependencies
cd placey-be
dotnet restore

# 3. Apply migrations (first time)
dotnet ef database update

# 4. Run
dotnet run
# API: https://localhost:7220
# Swagger UI: https://localhost:7220/swagger
```

JWT config and the dev DB connection string are already in `appsettings.Development.json` — no extra env setup needed for local dev.

---

## Running with Docker Compose

```bash
cp .env.example .env        # fill in your values
docker compose up --build   # builds and starts postgres + backend + frontend
```

| Service | URL |
|---------|-----|
| Frontend | http://localhost:8080 |
| API | http://localhost:5001 |
| Swagger UI | http://localhost:5001/swagger |

The backend uses a `depends_on` healthcheck on the Postgres service, so it waits for the database to be ready before starting.

Apply migrations inside the container:

```bash
docker compose exec backend dotnet ef database update
```

---

## Migrations

After changing an entity:

```bash
cd placey-be
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

To remove the last migration (if it hasn't been applied yet):

```bash
dotnet ef migrations remove
```

### Migration history

| Migration | What it adds |
|-----------|-------------|
| `InitialCreate` | All core tables: Users, Listings, ListingPhotos, Availabilities, Bookings, Reviews, Favorites, RefreshTokens |
| `AddListingDetails` | Bedrooms, Bathrooms, Category, PropertyType, Amenities, Street, City, Country fields on Listings |
| `AddMessaging` | Conversations and Messages tables |

---

## Swagger / API docs

In Development, Swagger UI is at `/swagger`. It is configured with Bearer auth support via `AddSwaggerWithAuth`:

1. Click **Authorize** in the top-right corner.
2. Enter `Bearer <your-access-token>` (get one from `POST /auth/login`).
3. All subsequent requests will include the JWT.

For the full endpoint reference see [api-endpoints.md](./api-endpoints.md).
