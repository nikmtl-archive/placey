# Placey

Placey is a short-term property rental platform inspired by Airbnb. Hosts publish and manage accommodations; guests search, check availability, and book stays.

---

## Features

### Agreed scope
- [ ] Property listings with photos and availability
- [ ] Search and filter by location, dates, and guest count
- [ ] Availability checks during the booking flow
- [ ] Host dashboard — manage listings and view incoming reservations
- [ ] Guest dashboard — view and manage own bookings

### Optional Scope
- [ ] Reviews and ratings for listings
- [ ] Messaging between host and guest
- [ ] Payments via PayPal Sandbox

<!--
### Out of Scope
- [ ] Item that was no longer implemented
- [ ] Item that is intentionally not part of the project
-->

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
> For detailed technical documentation, see the documentation section below.

```
placey/
├── placey-be/          ASP.NET Core backend (REST API)
├── placey-be.Tests/    xUnit unit tests for the backend services
├── placey-fe/          Vue 3 frontend
├── docs/               Architecture and API documentation
│   ├── backend.md          Backend architecture & developer guide
│   └── api-endpoints.md    Full API reference
├── .env.example        Environment variable template
└── docker-compose.yml
```

---

## Setup

### Requirements

- [Docker](https://www.docker.com/) and Docker Compose
- .NET 8 SDK (for local backend development)
- Node.js 20 + pnpm (for local frontend development)

### Quick start with Docker Compose

```bash
# 1. Copy the env template and fill in your values
cp .env.example .env

# 2. Build and start all services
docker compose up --build
```

| Service    | URL                           |
| ---------- | ----------------------------- |
| Frontend   | http://localhost:8080         |
| API        | http://localhost:5001         |
| Swagger UI | http://localhost:5001/swagger |

### Local backend development

JWT config and the dev database connection string are already set in `placey-be/appsettings.Development.json` — no extra environment setup needed.

```bash
# Start only the database
docker compose up postgres -d

cd placey-be
dotnet restore

# First time only: apply existing migrations
dotnet ef database update

dotnet run
# API available at https://localhost:7220
```

### Local frontend development

```bash
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

When running in `Development` mode, the backend automatically seeds example data on startup. All seed accounts share the password **`password123`**.

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


## Documentation

Detailed documentation is available in the `docs/` folder:

| Document                                       | Description                                                          |
| ---------------------------------------------- | -------------------------------------------------------------------- |
| [docs/backend.md](docs/backend.md)             | Architecture, auth flow, database schema, error handling, migrations |
| [docs/api-endpoints.md](docs/api-endpoints.md) | Full REST API reference with request/response shapes                 |

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

<!--
## Known Limitations
- Which points are still open?
- What limitations does the project have?
- What would be improved in a future version?

---
-->

## Contributors

| Name                 | Matr.-Nr. | GitHub                                           |
| -------------------- | --------- | ------------------------------------------------ |
| Michelle Niedersberg | 8569189   | [@michelle128n](https://github.com/michelle128n) |
| Dominik Mitzel       | 6633800   | [@nikmtl](https://github.com/nikmtl)             |
