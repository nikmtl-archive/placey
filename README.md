# Placey

Placey is a short-term property rental platform inspired by Airbnb. Hosts publish and manage accommodations; guests search, check availability, and book stays.

![Placey homepage](docs/assets/screenshot-home.png)

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

> [!NOTE]
> Placey is available to test under [placey.mtzel.com](https://placey.mtzel.com)

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

| Variable                 | Description                                                           |
| ------------------------ | --------------------------------------------------------------------- |
| `POSTGRES_USER`          | Database username                                                     |
| `POSTGRES_PASSWORD`      | Database password                                                     |
| `POSTGRES_DB`            | Database name                                                         |
| `JWT_SECRET`             | HMAC signing key, must be ≥ 32 characters (`openssl rand -base64 32`) |
| `ASPNETCORE_ENVIRONMENT` | `Development` or `Production`                                         |

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

Seed data also includes completed bookings, reviews, and favorites so main API endpoints return realistic data immediately.

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

## CI / CD

### CI (GitHub Actions)

Two workflows run on every pull request:

| Workflow    | Trigger | Jobs                                                                     |
| ----------- | ------- | ------------------------------------------------------------------------ |
| Frontend CI | PR      | Prettier format check, oxlint + ESLint, vue-tsc type-check, Vitest tests |
| Backend CI  | PR      | CSharpier format check, `dotnet build`, `dotnet test`                    |

Workflows are defined in [.github/workflows/](.github/workflows/).

### CD (Dokploy)

Deployments are handled by [Dokploy](https://dokploy.com). On every push to `main`, Dokploy pulls the latest image and redeploys the application automatically. No manual deployment step is needed — merging a PR to `main` is sufficient to ship.

The live deployment is available at [placey.mtzel.com](https://placey.mtzel.com).

---

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for branch naming, commit message conventions, PR guidelines, and how to run CI checks locally.

---

## Documentation

Detailed documentation is available in the `docs/` folder.

### Backend

| Document                                                         | Description                                                          |
| ---------------------------------------------------------------- | -------------------------------------------------------------------- |
| [docs/backend/overview.md](docs/backend/overview.md)             | Stack, project structure, key dependencies                           |
| [docs/backend/architecture.md](docs/backend/architecture.md)     | Three-layer design, DI wiring, request pipeline, Swagger             |
| [docs/backend/auth.md](docs/backend/auth.md)                     | JWT, refresh tokens, BCrypt, resource-level authorization            |
| [docs/backend/database.md](docs/backend/database.md)             | Full entity reference, relationships, constraints, enums, migrations |
| [docs/backend/services.md](docs/backend/services.md)             | All 8 services + SseHub: responsibilities, methods, business rules   |
| [docs/backend/error-handling.md](docs/backend/error-handling.md) | ErrorHandlingMiddleware, ProblemDetails, exception-to-status map     |
| [docs/backend/file-uploads.md](docs/backend/file-uploads.md)     | Photo & avatar upload flow, storage, Docker volume                   |
| [docs/backend/testing.md](docs/backend/testing.md)               | xUnit setup, in-memory DB, all 7 service test classes                |
| [docs/backend/configuration.md](docs/backend/configuration.md)   | appsettings, environment variables, Docker Compose wiring            |

### Frontend

| Document                                                                 | Description                                                 |
| ------------------------------------------------------------------------ | ----------------------------------------------------------- |
| [docs/frontend/overview.md](docs/frontend/overview.md)                   | Stack, project structure, design theme                      |
| [docs/frontend/routing.md](docs/frontend/routing.md)                     | Route table, auth guard, scroll behaviour, lazy loading     |
| [docs/frontend/state-management.md](docs/frontend/state-management.md)   | All 6 Pinia stores: state, computed, methods                |
| [docs/frontend/components.md](docs/frontend/components.md)               | All 15 components: props, emits, purpose                    |
| [docs/frontend/composables.md](docs/frontend/composables.md)             | useClickOutside, useAmenityIcons, useCategories             |
| [docs/frontend/views.md](docs/frontend/views.md)                         | All 13 views: purpose, key behaviours, store usage          |
| [docs/frontend/api-communication.md](docs/frontend/api-communication.md) | apiFetch wrapper, dev proxy, AbortController, SSE auth      |
| [docs/frontend/testing.md](docs/frontend/testing.md)                     | Vitest setup, patterns, all 5 store test suites, coverage   |
| [docs/frontend/tooling.md](docs/frontend/tooling.md)                     | Vite config, ESLint/Oxlint/Prettier, tsconfig, pnpm scripts |

### Features & API

| Document                                       | Description                                                  |
| ---------------------------------------------- | ------------------------------------------------------------ |
| [docs/messaging.md](docs/messaging.md)         | Messaging feature: SSE design, store, components, data model |
| [docs/api-endpoints.md](docs/api-endpoints.md) | Full REST API reference with request/response shapes         |
| [docs/testing.md](docs/testing.md)             | Test suites overview and manual testing checklist            |
| [docs/SECURITY.md](docs/SECURITY.md)           | Security controls: auth, validation, headers, known limits   |

---

## Tests

Full test documentation is in [docs/testing.md](docs/testing.md), including backend service tests (unit tests for service layer), frontend store tests (Pinia store test suites), and the manual testing checklist.

---

### Frontend tests

Frontend unit tests use [Vitest](https://vitest.dev/) and [`@vue/test-utils`](https://test-utils.vuejs.org/). All five Pinia stores are tested with mocked API calls (`vi.stubGlobal` / `vi.spyOn`).

| Store               | Scenarios covered                                                                                                                                                                                                                                                                                                             |
| ------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `useAuthStore`      | Initial state (empty, restored from localStorage, corrupt data), login (success, failure, loading flag, persistence), register (success, duplicate email), logout (success, network error), patchUser, refreshSession (success, no token, deduplication), authHeaders, apiFetch (401 retry, clears session on failed refresh) |
| `useFavoritesStore` | isFavorited, hydrate (success, failure), toggleFavorite (add optimistic, remove optimistic, rollback add on error, rollback remove on error), reset                                                                                                                                                                           |
| `useListingsStore`  | fetchListings (success, HTTP error, query string for location/guests, optional filters, omits absent params, ignores AbortError, loading flag)                                                                                                                                                                                |
| `useBookingStore`   | fetchMyBookings (success, error, non-array response), createBooking (success, conflict error, loading flag), cancelBooking (removes from list, clears lastBooking, error, loading flag), rateBooking (refreshes list, error, loading flag)                                                                                    |
| `useMessagingStore` | fetchConversations (success, error), fetchConversation (sets active, resets before load, error), getOrCreateConversation (success, error), sendMessage (success + appends, wrong conversation, error, loading flag), fetchUnreadCount (success, silent error), resetActive                                                    |


### Running frontend tests

```bash
cd placey-fe

# Run once
pnpm test

# Watch mode
pnpm test:watch

# With coverage report
pnpm test:coverage
```

---


## Known Limitations
- At the moment, there are no email notifications for new messages or bookings. Users must check the app to see updates.
- Payments are out of scope. The booking flow does not include any payment processing, and bookings are confirmed without any financial transaction.

---


## Contributors

| Name                 | Matr.-Nr. | GitHub                                           |
| -------------------- | --------- | ------------------------------------------------ |
| Michelle Niedersberg | 8569189   | [@michelle128n](https://github.com/michelle128n) |
| Dominik Mitzel       | 6633800   | [@nikmtl](https://github.com/nikmtl)             |
