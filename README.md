# Placey

Placey is a short-term property rental platform inspired by Airbnb. Hosts publish and manage accommodations; guests search, check availability, and book stays.

---

## Features

### Agreed scope
- [x] Property listings with photos and availability
- [x] Search and filter by location, dates, and guest count
- [x] Availability checks during the booking flow
- [x] Host dashboard: manage listings and view incoming reservations
- [x] Guest dashboard: view and manage own bookings

### Optional Scope
- [x] Reviews and ratings for listings
- [x] Messaging between host and guest

### Out of Scope
- [ ] Payments via PayPal Sandbox

---

## Technologies

| Layer     | Technology                      |
| --------- | ------------------------------- |
| Frontend  | Vue 3, Vue Router, Tailwind CSS |
| Backend   | ASP.NET Core 8 Web API          |
| Database  | PostgreSQL 16                   |
| Auth      | JWT Bearer + refresh tokens     |
| Container | Docker, Docker Compose          |

---

## Project structure

> [!NOTE]
> For detailed technical documentation, see the `docs/` folder.
> For an overview of the avalable documentation, see the the documentation section below.

```
placey/
├── placey-be/          ASP.NET Core backend (REST API)
├── placey-be.Tests/    xUnit unit tests for the backend services
├── placey-fe/          Vue 3 frontend
├── docs/               Architecture and API documentation
├── .env.example        Environment variable template
└── docker-compose.yml
```

---

## Setup

### Requirements

- Docker and Docker Compose
- .NET 8 SDK (for local backend development only)
- Node.js 20 + pnpm (for local frontend development only)

### Quick start with Docker Compose (Start everything)

The following commands will build and start all services (frontend, backend, database) in Docker containers. The backend will automatically apply migrations and seed example data on first startup if the environment variables are set correctly.

```bash
# 1. Copy the env template and fill in your values (see the Environment variables section below for details)
cp .env.example .env

# 2. Build and start all services
docker compose up --build
```

After startup, the following services are available (ports may vary):
| Service    | URL                           |
| ---------- | ----------------------------- |
| Frontend   | http://localhost:8080         |
| API        | http://localhost:5001         |
| Swagger UI | http://localhost:5001/swagger |

### Local backend development

JWT config and the dev database connection string are set in `placey-be/appsettings.Development.json`, no extra environment setup needed.

```bash
# Start only the database
docker compose up postgres -d

cd placey-be
dotnet restore

# First time only: apply existing migrations
dotnet ef database update

dotnet run
# API available at https://localhost:7220 (port may vary)
```

### Local frontend development

```bash
# Start the backend
docker compose up backend -d

cd placey-fe
pnpm install
pnpm dev
```

### Environment variables

Used by Docker Compose only. Copy `.env.example` to `.env` and fill in:

| Variable                 | Description                                                            |
| ------------------------ | ---------------------------------------------------------------------- |
| `POSTGRES_USER`          | Database username                                                      |
| `POSTGRES_PASSWORD`      | Database password                                                      |
| `POSTGRES_DB`            | Database name                                                          |
| `JWT_SECRET`             | HMAC signing key — must be ≥ 32 characters (`openssl rand -base64 32`) |
| `ASPNETCORE_ENVIRONMENT` | `Development` or `Production`                                          |

JWT issuer and audience are hardcoded to `placey` in `docker-compose.yml`. For local development outside Docker they are set in `appsettings.Development.json`.


### Example seed data

> [!NOTE]
> The seed data is automatically applied on first startup if the database is empty and the environment variable `ASPNETCORE_ENVIRONMENT` is set to `Development`. You can also run the seed script manually with `dotnet run --project placey-be/Placey.SeedData`.

Seed data includes example users, listings, bookings, reviews, and favorites:

**Users**

| Name          | Email             | Role         | Password    |
| ------------- | ----------------- | ------------ | ----------- |
| Alice Müller  | alice@example.com | Host & Guest | password123 |
| Bob Schneider | bob@example.com   | Host         | password123 |
| Carol Jensen  | carol@example.com | Guest        | password123 |

**Listings**

| Title                           | Location               | Price/night | Max guests |
| ------------------------------- | ---------------------- | ----------- | ---------- |
| Cozy Studio in Mitte            | Berlin, Germany        | €85         | 2          |
| Modern Loft with Rooftop Access | Vienna, Austria        | €140        | 4          |
| Architect-Designed Townhouse    | Prague, Czech Republic | €195        | 6          |
| Lakeside Cabin Retreat          | Bled, Slovenia         | €120        | 4          |
| Bright Apartment near Old Town  | Tallinn, Estonia       | €70         | 2          |

Seed data also includes completed bookings, reviews, and favorites so all API endpoints return realistic data immediately.

### Troubleshooting

#### Backend not reachable from frontend
If you get an error like this in the frontend console:

```
7:37:24 PM [vite] http proxy error: <an api request URL>
AggregateError [ECONNREFUSED]: 
    at internalConnectMultiple (node:net:1193:18)
    at afterConnectMultiple (node:net:1783:7)
```

Make sure the backend is running and the proxy target in `vite.config.ts` matches the backend Port (default: `http://localhost:5001`).

---

## Documentation

Detailed documentation is available in the `docs/` folder:

| Document                                         | Description                                                          |
| ------------------------------------------------ | -------------------------------------------------------------------- |
| [docs/backend.md](docs/backend.md)               | Architecture, auth flow, database schema, error handling, migrations |
| [docs/frontend.md](docs/frontend.md)             | Vue 3 app structure, routing, Pinia stores, components               |
| [docs/messaging.md](docs/messaging.md)           | Messaging feature: SSE design, store, components, data model         |
| [docs/api-endpoints.md](docs/api-endpoints.md)   | Full REST API reference with request/response shapes                 |

---

## Tests

### What was tested

Unit tests cover all six service layers of the backend using an in-memory database (EF Core InMemory). No external dependencies (database, running server) are needed to run the suite.

| Service           | Scenarios tested                                                                                                                                                                                                                                                      |
| ----------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `AuthService`     | Register (happy path, duplicate email), login (valid, unknown email, wrong password), refresh (valid, invalid, revoked token), logout                                                                                                                                 |
| `BookingService`  | Create (instant-book → Confirmed, non-instant → Pending, host self-booking, invalid dates, date conflicts, cancelled-then-rebook), get by ID (as guest, as host, unauthorized), cancel (guest, host, unauthorized, completed booking), update status (host, non-host) |
| `ListingService`  | Create, get by ID (found, not found), update (host, non-host, not found), delete (host, non-host), search (no filter, location, guest count, pagination), set/get availability (host, non-host)                                                                       |
| `ReviewService`   | Create (valid completed booking, wrong guest, booking not completed, duplicate, booking not found), delete (author, non-author), get by listing, get by host user                                                                                                     |
| `FavoriteService` | Add (valid, listing not found, idempotent), remove (existing, idempotent), get favorites                                                                                                                                                                              |
| `UserService`     | Get me (found, not found), update me (fields updated, null fields preserved), delete me, get public profile                                                                                                                                                           |

**Total: 66 tests**

### Running tests

```bash
# Run from the repo root
dotnet test placey-be.Tests/

# Or run the full solution
dotnet test placey.sln
```

---


## Known Limitations
- At the moment, there are no email notifications for new messages or bookings. Users must check the app to see updates.

---


## Contributors

| Name                 | Matr.-Nr. | GitHub                                           |
| -------------------- | --------- | ------------------------------------------------ |
| Michelle Niedersberg | 8569189   | [@michelle128n](https://github.com/michelle128n) |
| Dominik Mitzel       | 6633800   | [@nikmtl](https://github.com/nikmtl)             |
