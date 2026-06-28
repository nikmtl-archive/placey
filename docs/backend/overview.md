# Backend - Overview

## Stack

| Layer            | Technology                                                      |
| ---------------- | --------------------------------------------------------------- |
| Runtime          | ASP.NET Core 8 Web API                                          |
| ORM              | Entity Framework Core 8 (Npgsql)                                |
| Database         | PostgreSQL 16                                                   |
| Auth             | JWT Bearer (access tokens) + opaque refresh tokens stored in DB |
| Password hashing | BCrypt.Net-Next                                                 |
| Real-time        | Server-Sent Events (SSE) via in-process `SseHub`                |
| API docs         | Swagger / Swashbuckle                                           |

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
│   └── SwaggerExamplesFilter.cs       Schema filter providing example values
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
├── appsettings.Development.json  Dev overrides (DB connection, JWT)
├── Dockerfile                  Docker image definition
└── Program.cs                  Minimal — delegates to ServiceExtensions
```

---

## Key NuGet dependencies

| Package                                         | Version | Purpose                       |
| ----------------------------------------------- | ------- | ----------------------------- |
| `Npgsql.EntityFrameworkCore.PostgreSQL`         | 8.0.11  | PostgreSQL driver for EF Core |
| `Microsoft.EntityFrameworkCore`                 | 8.0.15  | ORM                           |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 8.0.15  | JWT middleware                |
| `BCrypt.Net-Next`                               | 4.0.3   | Password hashing              |
| `Swashbuckle.AspNetCore`                        | 6.6.2   | Swagger / OpenAPI             |

---

## Further reading

| Topic                           | Document                                   |
| ------------------------------- | ------------------------------------------ |
| Architecture & request pipeline | [architecture.md](./architecture.md)       |
| Authentication & authorization  | [auth.md](./auth.md)                       |
| Database schema & migrations    | [database.md](./database.md)               |
| Services & business logic       | [services.md](./services.md)               |
| Error handling                  | [error-handling.md](./error-handling.md)   |
| File uploads                    | [file-uploads.md](./file-uploads.md)       |
| Testing                         | [testing.md](./testing.md)                 |
| Configuration & environment     | [configuration.md](./configuration.md)     |
| Messaging feature               | [../messaging.md](../messaging.md)         |
| REST API reference              | [../api-endpoints.md](../api-endpoints.md) |
