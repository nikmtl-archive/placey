# Backend - Configuration & Environment

---

## Configuration files

### `appsettings.json`

Production baseline. Contains only logging configuration — no secrets.

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### `appsettings.Development.json`

Development overrides. Contains local DB connection and JWT settings. **Not committed with real secrets** in production.

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=placey;Username=placey;Password=placey_dev_password"
  },
  "Jwt": {
    "Secret": "dev-secret-key-minimum-32-characters",
    "Issuer": "placey-be",
    "Audience": "placey-fe"
  }
}
```

---

## Environment variables

ASP.NET Core maps `__` to `:` in environment variable names, so `Jwt__Secret` resolves to `Jwt:Secret` in `IConfiguration`. This is the standard pattern for injecting configuration in Docker.

| Variable                     | Purpose                                    |
| ---------------------------- | ------------------------------------------ |
| `ConnectionStrings__Default` | Full PostgreSQL connection string          |
| `Jwt__Secret`                | HMAC signing key — must be ≥ 32 characters |
| `Jwt__Issuer`                | JWT `iss` claim (default: `placey-be`)     |
| `Jwt__Audience`              | JWT `aud` claim (default: `placey-fe`)     |
| `ASPNETCORE_ENVIRONMENT`     | `Development` or `Production`              |

Generate a secure JWT secret:
```bash
openssl rand -base64 32
```

---

## `.env` file (Docker Compose)

Copy `.env.example` to `.env` at the repo root and fill in values. Docker Compose reads `.env` automatically.

```bash
cp .env.example .env
```

| Variable                 | Description                                |
| ------------------------ | ------------------------------------------ |
| `POSTGRES_USER`          | PostgreSQL username                        |
| `POSTGRES_PASSWORD`      | PostgreSQL password                        |
| `POSTGRES_DB`            | PostgreSQL database name                   |
| `JWT_SECRET`             | HMAC signing key — must be ≥ 32 characters |
| `ASPNETCORE_ENVIRONMENT` | `Development` or `Production`              |

The `docker-compose.yml` maps these variables into the backend container:

```yaml
environment:
  - ConnectionStrings__Default=Host=postgres;Port=5432;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}
  - Jwt__Secret=${JWT_SECRET}
  - Jwt__Issuer=placey
  - Jwt__Audience=placey
  - ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT}
```

Note: JWT issuer and audience are hardcoded to `placey` in `docker-compose.yml`. For local development outside Docker, `appsettings.Development.json` is used instead.

---

## Docker Compose services

```yaml
services:
  postgres:
    image: postgres:16-alpine
    ports: ["5432:5432"]
    volumes: [postgres_data:/var/lib/postgresql/data]
    healthcheck: # retries until pg_isready succeeds

  backend:
    build: ./placey-be
    ports: ["5001:5000"]
    volumes: [uploads:/app/wwwroot]
    depends_on:
      postgres:
        condition: service_healthy

  frontend:
    build: ./placey-fe
    ports: ["8080:80"]
    depends_on: [backend]
```

The backend waits for the Postgres healthcheck before starting. The frontend waits for the backend.

Volumes:
- `postgres_data` — persists database files across restarts
- `uploads` — persists uploaded photos and avatars across restarts

---

## Local development (without Docker)

**Prerequisites:** .NET 8 SDK, PostgreSQL running locally.

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
# API: https://localhost:7220 (port varies — check launchSettings.json)
# Swagger UI: https://localhost:7220/swagger
```

JWT config and the dev DB connection string are in `appsettings.Development.json` — no extra env setup needed for local dev.

---

## CI/CD

The backend CI workflow (`.github/workflows/be-ci.yml`) runs:

1. **Format check** — `dotnet csharpier --check .`
2. **Build** — `dotnet build`
3. **Tests** — `dotnet test placey.sln`

All steps must pass before a PR can be merged.

CSharpier is configured in `dotnet-tools.json` as a .NET local tool.
