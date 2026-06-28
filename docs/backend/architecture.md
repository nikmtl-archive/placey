# Backend - Architecture

---

## Three-layer design

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

**Controllers** are intentionally thin. They:
1. Extract the caller's user ID from the JWT via `User.GetUserId()`.
2. Call the relevant service method with validated input.
3. Return the result as an `IActionResult`.

No business logic lives in controllers.

**Services** own every rule: ownership checks, date conflict detection, review eligibility, price calculation, status transition enforcement. They throw typed exceptions which the error middleware maps to HTTP status codes.

**DTOs** (in `Models/DTOs/`) decouple the API surface from the database schema. Entity classes are never serialised directly in responses.

---

## Dependency injection wiring

All services are registered in `ServiceExtensions.AddApplicationServices()`:

```csharp
services.AddScoped<IAuthService, AuthService>();
services.AddScoped<IListingService, ListingService>();
services.AddScoped<IBookingService, BookingService>();
services.AddScoped<IReviewService, ReviewService>();
services.AddScoped<IUserService, UserService>();
services.AddScoped<IFavoriteService, FavoriteService>();
services.AddScoped<IMessageService, MessageService>();
services.AddSingleton<SseHub>();   // singleton: holds in-memory channels
```

`SseHub` is a singleton because it manages long-lived `Channel<T>` objects that persist across requests. All other services are scoped (one instance per HTTP request).

---

## Request pipeline (middleware order)

```
Static files (wwwroot — uploaded photos, avatars)
    │
ErrorHandlingMiddleware (catches all unhandled exceptions → ProblemDetails)
    │
UseAuthentication (JWT middleware — validates Bearer token or ?access_token=)
    │
UseAuthorization ([Authorize] attribute enforcement)
    │
MapControllers (route matching and action dispatch)
```

---

## Program.cs

`Program.cs` is intentionally minimal — it delegates all wiring to extension methods:

```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerWithAuth();
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddJwtAuth(builder.Configuration);
builder.Services.AddApplicationServices();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    await db.Database.MigrateAsync();   // auto-apply migrations
    await DbSeeder.SeedAsync(db);       // seed example data if empty
}
```

JSON enum serialisation is configured globally: enums are serialised as strings (e.g. `"Pending"` not `0`).

---

## Swagger

Swagger UI is available at `/swagger` in Development. It is configured with JWT Bearer auth support:

1. Click **Authorize** in the top-right corner.
2. Enter `Bearer <your-access-token>` (obtain one from `POST /auth/login`).
3. All subsequent Swagger requests include the JWT.

`SwaggerExamplesFilter` provides example request bodies for all DTOs in the Swagger UI.

---

## Development seed data

On first Development startup, `DbSeeder.SeedAsync` creates:
- 3 users (Alice, Bob, Carol) with BCrypt-hashed passwords (`password123`)
- 5 sample listings with photos, amenities, and pricing
- 3 bookings with varying statuses

Seeding is idempotent, it checks if data already exists before inserting.
