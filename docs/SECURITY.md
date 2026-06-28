# Security

This document describes the security controls in place for Placey, the decisions behind them, and known limitations.

---

## Threat model

Placey is a web-based property rental platform. The assets it protects are:

- **User accounts**: credentials, personal data (name, email, avatar)
- **Booking data**: dates, guest/host identity, status
- **Private messages**: conversation content between host and guest

The assumed deployment context is a single Docker Compose stack where an nginx reverse proxy serves the Vue frontend and proxies `/api/*` to the ASP.NET Core backend. Frontend and backend share the same origin in production.

Threats considered: credential theft, unauthorised access to other users' data, injection attacks, information disclosure via error responses, token interception, and UI redress attacks (clickjacking).

Threats explicitly out of scope: volumetric DoS, infrastructure-level attacks, physical security.

---

## Authentication

- **Passwords** are hashed with BCrypt (via BCrypt.Net-Next) with automatic salt generation. Raw passwords are never stored or logged.
- **Access tokens** are signed JWTs (HMAC-SHA256) with a 15-minute lifetime. Issuer, audience, lifetime, and signing key are all validated on every request.
- **Refresh tokens** are 64-byte cryptographically random values stored server-side. They expire after 7 days and are revoked on logout or account deletion.
- The JWT signing secret must be provided via the `JWT_SECRET` environment variable in production. The development placeholder in `appsettings.Development.json` must never be used in production.

---

## Authorisation

All write endpoints and user-specific read endpoints require a valid `Authorization: Bearer <token>` header (`[Authorize]` attribute).

Ownership is enforced at the service layer. For example, only the listing's host can update or delete it, and only the guest or host of a booking can view its details. Passing a valid token for a different user returns `403 Forbidden`.

**Intentionally public endpoints** (no authentication required):

| Endpoint                       | Reason                                                      |
| ------------------------------ | ----------------------------------------------------------- |
| `GET /listings`                | Public search                                               |
| `GET /listings/{id}`           | Public listing detail                                       |
| `GET /listings/{id}/blockings` | Availability calendar: no PII, needed for the booking flow |
| `GET /listings/{id}/reviews`   | Public listing reviews                                      |
| `GET /users/{id}`              | Public host/guest profile                                   |
| `GET /users/{id}/listings`     | Public host listings                                        |
| `GET /users/{id}/reviews`      | Public reviews                                              |

---

## Input validation

All API request bodies are validated automatically via the `[ApiController]` attribute combined with DTO data annotations (`[Required]`, `[Range]`, `[StringLength]`, `[EmailAddress]`). Invalid requests are rejected with `400 Bad Request` before reaching service code.

Examples of enforced constraints:

| Field                | Rule                 |
| -------------------- | -------------------- |
| Email                | Valid email format   |
| Password             | Minimum 8 characters |
| Listing price        | 1–100,000            |
| Message body         | 1–4,000 characters   |
| Review rating        | 1–5                  |

**File uploads** (avatars and listing photos) are validated for MIME type (`image/jpeg`, `image/png`, `image/webp`, `image/heic`, `image/heif`) and size (5 MB for avatars, 10 MB for listing photos). Saved filenames are UUIDs, preventing path traversal.

---

## Error handling

The `ErrorHandlingMiddleware` catches all unhandled exceptions and maps them to structured `application/problem+json` responses.

For known, intentionally thrown exceptions (`KeyNotFoundException`, `InvalidOperationException`, `UnauthorizedAccessException`, `ResourceDeletedException`) the exception message is returned as `detail`. These messages are written by the application and are safe to expose.

For all other (unexpected) exceptions the `detail` field is always `"An unexpected error occurred."`. Stack traces and internal details are never sent to the client; they are written to the server log only.

---

## Injection protection

**SQL injection:** all database queries use Entity Framework Core LINQ. There are no raw SQL queries (`FromSqlRaw`, `ExecuteSqlRaw`, etc.) anywhere in the codebase. EF Core uses parameterised queries by default.

**XSS:** the Vue 3 frontend renders all user-generated content via `{{ }}` template interpolation, which HTML-escapes output automatically. There is no use of `v-html` or `innerHTML` with user-controlled data.

**CSRF:** not applicable. Authentication uses JWT tokens sent in the `Authorization` request header, which browsers do not auto-submit. Cookies are not used for auth.

---

## Security headers

The following headers are set on every API response by middleware in `Program.cs`, and on every frontend response by nginx:

| Header                      | Value                                 | Purpose                          |
| --------------------------- | ------------------------------------- | -------------------------------- |
| `X-Content-Type-Options`    | `nosniff`                             | Prevents MIME-type sniffing      |
| `X-Frame-Options`           | `DENY`                                | Prevents clickjacking            |
| `Referrer-Policy`           | `strict-origin-when-cross-origin`     | Limits referrer leakage          |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | Enforces HTTPS (production only) |

---

## CORS

An explicit CORS policy (`"Frontend"`) is registered and applied in `Program.cs`. It allows requests only from the configured frontend origin (`Cors:AllowedOrigin` in app settings). In production this should be set to the actual deployment URL via environment variable. The default same-origin setup via the nginx proxy means browsers will not send cross-origin requests, but the explicit policy makes the intent clear and guards against misconfiguration.

---

## SSE authentication

The real-time message stream (`GET /messages/stream`) uses Server-Sent Events. Because the native `EventSource` API does not support custom request headers, the frontend uses a `fetch`-based SSE client that sends the standard `Authorization: Bearer <token>` header. Tokens are never passed as query parameters, so they do not appear in server access logs or browser history.

---

## Secrets management

| Secret            | Development                                                      | Production                               |
| ----------------- | ---------------------------------------------------------------- | ---------------------------------------- |
| JWT signing key   | `appsettings.Development.json` (placeholder, not for production) | `JWT_SECRET` environment variable        |
| Database password | `appsettings.Development.json`                                   | `POSTGRES_PASSWORD` environment variable |

The `.env` file used by Docker Compose for local development is listed in `.gitignore` and must not be committed. Use `.env.example` as a template.

---

## Known limitations

- **Rate limiting** is not implemented. The login and registration endpoints are not protected against brute-force or credential-stuffing attacks. Mitigation would require infrastructure-level tooling (e.g., nginx `limit_req`, a reverse proxy with rate limiting, or a WAF).
- **Token storage:** access and refresh tokens are stored in `localStorage`. This makes them accessible to JavaScript running on the page. The risk is mitigated by the absence of any XSS vectors (no `v-html`, no unsafe DOM writes) and the short 15-minute access token lifetime, but httpOnly cookies would be a stronger defence in depth.
