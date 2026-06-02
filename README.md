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

| Layer | Technology |
|-------|-----------|
| Frontend | Vue 3, Vue Router, Tailwind CSS |
| Backend | ASP.NET Core 8 Web API |
| Database | PostgreSQL 16 |
| Auth | JWT Bearer + refresh tokens |
| Container | Docker, Docker Compose |

---

## Project structure

> [!NOTE]
> For detailed technical documentation, see the documentation section below.

```
placey/
├── placey-be/      ASP.NET Core backend (REST API)
├── placey-fe/      Vue 3 frontend
├── docs/           Architecture and API documentation
│   ├── backend.md          Backend architecture & developer guide
│   └── api-endpoints.md    Full API reference
├── .env.example    Environment variable template
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

| Service | URL |
|---------|-----|
| Frontend | http://localhost:8080 |
| API | http://localhost:5001 |
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

| Variable | Description |
|----------|-------------|
| `POSTGRES_USER` | Database username |
| `POSTGRES_PASSWORD` | Database password |
| `POSTGRES_DB` | Database name |
| `JWT_SECRET` | HMAC signing key — must be ≥ 32 characters (`openssl rand -base64 32`) |
| `ASPNETCORE_ENVIRONMENT` | `Development` or `Production` |

JWT issuer and audience are hardcoded to `placey` in `docker-compose.yml`. For local development outside Docker they are set in `appsettings.Development.json`.


### Test Data
*Test users*:
- _To be filled in_


## Documentation

Detailed documentation is available in the `docs/` folder:

| Document | Description |
|----------|-------------|
| [docs/backend.md](docs/backend.md) | Architecture, auth flow, database schema, error handling, migrations |
| [docs/api-endpoints.md](docs/api-endpoints.md) | Full REST API reference with request/response shapes |

---

## Tests

### What was tested
- _To be filled in_

### Running tests
- _To be filled in_

---

<!--
## Known Limitations
- Which points are still open?
- What limitations does the project have?
- What would be improved in a future version?

---
-->

## Contributors

| Name | Student ID | GitHub |
|------|-----------|--------|
| Michelle Niedersberg | 8569189 | [@michelle128n](https://github.com/michelle128n) |
| Dominik Mitzel | 6633800 | [@nikmtl](https://github.com/nikmtl) |
