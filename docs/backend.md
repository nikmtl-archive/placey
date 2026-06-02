# Backend — Architecture & Developer Guide

## Stack

| Layer | Technology |
|-------|-----------|
| Runtime | ASP.NET Core 8 Web API |
| ORM | Entity Framework Core 8 (Npgsql) |
| Database | PostgreSQL 16 |
| Auth | JWT Bearer (access) + opaque refresh tokens (stored in DB) |
| Password hashing | BCrypt.Net-Next |
| API docs | Swagger / Swashbuckle |

---

## Project structure

```
placey-be/
├── Controllers/            HTTP layer — thin, no logic
│   ├── AuthController.cs
│   ├── BookingsController.cs
│   ├── FavoritesController.cs
│   ├── ListingsController.cs
│   ├── ReviewsController.cs
│   └── UsersController.cs
├── Data/
│   └── AppDbContext.cs     EF Core DbContext + model configuration
├── Extensions/
│   ├── ClaimsPrincipalExtensions.cs   GetUserId() helper
│   └── ServiceExtensions.cs           DI wiring for clean Program.cs
├── Middleware/
│   └── ErrorHandlingMiddleware.cs     Maps exceptions → ProblemDetails
├── Models/
│   ├── DTOs/               Request/response records (no entity exposure)
│   │   ├── Auth/
│   │   ├── Bookings/
│   │   ├── Listings/
│   │   ├── Reviews/
│   │   └── Users/
│   └── Entities/           EF Core entity classes
│       ├── Availability.cs
│       ├── Booking.cs
│       ├── Favorite.cs
│       ├── Listing.cs
│       ├── ListingPhoto.cs
│       ├── RefreshToken.cs
│       ├── Review.cs
│       └── User.cs
├── Services/
│   ├── Interfaces/         One interface per domain
│   ├── AuthService.cs
│   ├── BookingService.cs
│   ├── FavoriteService.cs
│   ├── ListingService.cs
│   ├── ReviewService.cs
│   └── UserService.cs
├── appsettings.json        Logging only — no secrets
├── appsettings.Development.json
└── Program.cs
```

---

## Architecture

The backend follows a simple three-layer architecture — no over-engineering for a project of this scope:

```
HTTP request
    │
    ▼
Controller          validates input via [ApiController] model binding
    │
    ▼
Service             all business logic lives here
    │
    ▼
AppDbContext        direct EF Core queries — no extra repository layer
    │
    ▼
PostgreSQL
```

**Controllers** are intentionally thin — they read the current user's ID from the JWT claim, call the service, and return a result. No business logic.

**Services** own every rule: ownership checks, booking conflict detection, review eligibility, price calculation, etc. They throw typed exceptions (`KeyNotFoundException`, `UnauthorizedAccessException`, `InvalidOperationException`) which the error middleware converts to appropriate HTTP status codes.

---

## Database schema

### Entities & relationships

```
User ──< Listing         (one user can host many listings)
User ──< Booking         (one user can make many bookings as a guest)
User ──< Review          (one user can write many reviews)
User ──< Favorite        (one user can save many listings)
User ──< RefreshToken    (one user can have multiple active tokens)

Listing ──< ListingPhoto
Listing ──< Availability
Listing ──< Booking
Listing ──< Review
Listing ──< Favorite

Booking ──< Review       (each booking can have at most one review)
```

### Key constraints

| Table | Constraint |
|-------|-----------|
| `Users` | `Email` is unique |
| `Reviews` | `BookingId` is unique (one review per booking) |
| `Favorites` | `(UserId, ListingId)` composite unique index |

### Booking status flow

```
Pending → Confirmed → Completed
       ↘           ↘
        Cancelled   Cancelled
```

- `Pending` is the initial state when `IsInstantBook = false`.
- `Confirmed` is set immediately when `IsInstantBook = true`, or when the host approves via `PATCH /bookings/:id/status`.
- `Cancelled` can be set by either guest or host as long as the booking is not `Completed`.

---

## Authentication & Authorization

**Authentication** = verifying who the caller is (login).  
**Authorization** = deciding what they are allowed to do (protected endpoints, ownership checks).

### Token strategy

| Token | Lifetime | Storage |
|-------|----------|---------|
| Access token (JWT) | 15 minutes | Client memory — sent in `Authorization` header |
| Refresh token | 7 days | `RefreshTokens` DB table |

### What is a JWT?

A JWT (JSON Web Token) is a self-contained signed string with three base64-encoded parts:

```
header.payload.signature
```

The **payload** carries claims about the user. The claims embedded in the access token are:

| Claim | Value |
|-------|-------|
| `sub` | User GUID — used to identify the caller in every service |
| `email` | User's email |
| `jti` | Random GUID — makes each token unique |

The whole token is signed with **HMAC-SHA256** using `Jwt:Secret`. If anyone tampers with the payload the signature won't match and the token is rejected. This means the server can verify the token without a database lookup — the signature alone is proof of authenticity.

Because the server never needs to look up the token, JWTs cannot be invalidated before they expire. This is why the access token lifetime is kept short at 15 minutes.

### Auth flow

```
POST /auth/register  ─→  hash password with BCrypt, create User, issue token pair
POST /auth/login     ─→  verify BCrypt hash, issue token pair
POST /auth/refresh   ─→  validate refresh token in DB, rotate, issue new pair
POST /auth/logout    ─→  mark refresh token as revoked in DB
```

Sequence for a typical client session:

```
Client                          Server
  |                               |
  |-- POST /auth/login ---------->|  password verified, tokens issued
  |<- { accessToken, refreshToken}|
  |                               |
  |-- GET /listings               |
  |   Authorization: Bearer <jwt>->  JWT middleware validates signature + expiry
  |<- 200 OK --------------------|  [Authorize] passes, request handled
  |                               |
  |  (15 min later — token expired)|
  |-- POST /auth/refresh -------->|  refresh token validated, both tokens rotated
  |<- { new accessToken, ... } ---|
```

### How `[Authorize]` works

The JWT middleware (configured in `ServiceExtensions.AddJwtAuth`) runs on every request before the controller. It:

1. Reads the token from the `Authorization: Bearer ...` header
2. Validates the signature, issuer, audience, and expiry
3. If valid, populates `HttpContext.User` with the token's claims

`[Authorize]` on a controller or action simply checks that `HttpContext.User` was populated by step 3. If it wasn't (missing token, expired, bad signature) ASP.NET returns **401 Unauthorized** before the controller method is even called.

Endpoints without `[Authorize]` (`POST /auth/login`, `POST /auth/register`) are public.

### Resource-level authorization

There are no user roles. Any authenticated user can become a host by creating a listing. Authorization for owner-only actions (edit/delete listing, approve bookings) is enforced in the **service layer** by comparing the caller's ID against the resource's owner field:

```csharp
if (listing.HostId != requesterId)
    throw new UnauthorizedAccessException("Only the host can update this listing.");
```

The error middleware maps `UnauthorizedAccessException` → **403 Forbidden**.

The caller's ID is read from the `sub` claim via the `ClaimsPrincipalExtensions.GetUserId()` helper and passed into the service from the controller.

### Refresh token rotation

The refresh token is an opaque random 64-byte string stored in the database. Unlike JWTs it can be revoked:

- On every `POST /auth/refresh` the old token's `RevokedAt` is set and a new token is created — so each refresh token can only be used once.
- On `POST /auth/logout` the token is revoked immediately.
- `IsActive` checks both expiry (`ExpiresAt`) and revocation (`RevokedAt`).

---

## Error handling

`ErrorHandlingMiddleware` catches all unhandled exceptions and returns a `ProblemDetails` JSON response:

| Exception | HTTP status |
|-----------|-------------|
| `KeyNotFoundException` | 404 Not Found |
| `UnauthorizedAccessException` | 403 Forbidden |
| `InvalidOperationException` | 400 Bad Request |
| anything else | 500 Internal Server Error |

---

## Environment variables

All secrets are injected via environment variables. ASP.NET Core maps `__` to `:` so `Jwt__Secret` becomes `Jwt:Secret` in the config system.

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
# 1. Start Postgres (or use the compose postgres service on its own)
docker compose up postgres -d

# 2. Restore dependencies
cd placey-be
dotnet restore

# 3. Create the database schema (first time only)
dotnet ef migrations add Init
dotnet ef database update

# 4. Run
dotnet run
```

Swagger UI is available at `https://localhost:7220/swagger`.  
Click **Authorize** and paste a Bearer token to test protected endpoints.

---

## Running with Docker Compose

```bash
# Copy and fill in secrets
cp .env.example .env

# Build and start everything
docker compose up --build

# API is available at http://localhost:5001
# Frontend at http://localhost:8080
```

The backend waits for the Postgres healthcheck to pass before starting, so no race conditions on first boot.

To run migrations inside the container:

```bash
docker compose exec backend dotnet ef database update
```

---

## Adding a new migration

After changing an entity:

```bash
cd placey-be
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

To revert the last migration:

```bash
dotnet ef migrations remove
```

---

## Swagger / API docs

When running in Development, Swagger UI is served at `/swagger`. It is configured with Bearer auth support — click **Authorize**, enter `Bearer <your-access-token>`, then all subsequent requests will include the JWT.

For the full API reference see [api-endpoints.md](./api-endpoints.md).
