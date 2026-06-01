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

## Authentication

### Token strategy

| Token | Lifetime | Storage |
|-------|----------|---------|
| Access token | 15 minutes | Client memory / Authorization header |
| Refresh token | 7 days | Database (`RefreshTokens` table) |

The access token is a signed HS256 JWT containing `sub` (user ID) and `email` claims. It is passed as a Bearer token on every authenticated request.

The refresh token is an opaque random 64-byte string. It is rotated on every use — the old token is revoked and a new one is issued. Logout simply marks the token as revoked in the database.

### Auth flow

```
POST /auth/register  ─→  hash password, create User, return token pair
POST /auth/login     ─→  verify password, return token pair
POST /auth/refresh   ─→  validate DB token, rotate, return new pair
POST /auth/logout    ─→  mark refresh token as revoked in DB
```

### Authorization

There are no user roles. Any user can become a "host" by creating a listing. Authorization for host-only actions (update/delete listing, approve bookings) is enforced in the service layer by checking resource ownership:

```csharp
if (listing.HostId != requesterId)
    throw new UnauthorizedAccessException("Only the host can update this listing.");
```

The error middleware converts `UnauthorizedAccessException` → `403 Forbidden`.

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
