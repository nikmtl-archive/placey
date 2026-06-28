# Backend - Authentication & Authorization

---

## Token strategy

| Token              | Lifetime   | Where stored             | How sent                               |
| ------------------ | ---------- | ------------------------ | -------------------------------------- |
| Access token (JWT) | 15 minutes | Client memory only       | `Authorization: Bearer <token>` header |
| Refresh token      | 7 days     | `RefreshTokens` DB table | Request body of `POST /auth/refresh`   |

---

## What is a JWT?

A JWT (JSON Web Token) is a self-contained signed string in three base64-encoded parts: `header.payload.signature`.

The **payload** carries claims the server trusts without a DB lookup because the **signature** (HMAC-SHA256 over `header + payload` using `Jwt:Secret`) proves the payload was not tampered with.

Claims embedded in the access token:

| Claim   | Value                                                  |
| ------- | ------------------------------------------------------ |
| `sub`   | User GUID — the primary identity used in every service |
| `email` | User email                                             |
| `jti`   | Random GUID — makes each token unique                  |

Because JWTs are self-verifying, they cannot be revoked before they expire — hence the short 15-minute lifetime.

---

## Password hashing

BCrypt.Net-Next is used for all password storage:

- **Register**: `BCrypt.HashPassword(password)` — cost factor is the BCrypt default.
- **Login**: `BCrypt.Verify(password, storedHash)` — constant-time comparison.

Password hashes are never returned in any API response.

---

## Auth flow

```
POST /auth/register  →  BCrypt hash password, create User, issue token pair
POST /auth/login     →  verify BCrypt hash, issue token pair
POST /auth/refresh   →  validate refresh token in DB, rotate, issue new pair
POST /auth/logout    →  mark refresh token as RevokedAt = now
```

Typical client session:

```
Client                              Server
  |── POST /auth/login ────────────>|  password verified, tokens issued
  |<── { accessToken, refreshToken }|
  |                                 |
  |── GET /listings                 |
  |   Authorization: Bearer <jwt> ->|  JWT middleware validates; passes
  |<── 200 OK ─────────────────────|
  |                                 |
  |  (15 min later — JWT expired)   |
  |── POST /auth/refresh ──────────>|  refresh token validated, both rotated
  |<── { new accessToken, ... } ───|
```

---

## How `[Authorize]` works

`ServiceExtensions.AddJwtAuth` configures the JWT middleware to run on every request. It:

1. Reads the `Authorization: Bearer <token>` header (or `?access_token=` query param for SSE endpoints).
2. Validates signature, issuer, audience, and expiry.
3. If valid, populates `HttpContext.User` with the claims.

`[Authorize]` on a controller or action simply checks that step 3 succeeded. Missing, expired, or tampered tokens return **401 Unauthorized** before the controller method is called.

**SSE exception:** `EventSource` cannot send custom headers. The JWT for `GET /messages/stream` is read from the `?access_token=` query parameter. `AddJwtAuth` handles this via `OnMessageReceived` — it checks `context.Request.Path` and extracts the token from the query string when the path starts with `/messages/stream`.

---

## Resource-level authorization

No roles exist. Any authenticated user can host by creating a listing. Owner-only actions are enforced in the **service layer** by comparing the caller's ID against the resource's owner field:

```csharp
if (listing.HostId != requesterId)
    throw new UnauthorizedAccessException("Only the host can update this listing.");
```

`ErrorHandlingMiddleware` maps `UnauthorizedAccessException` → **403 Forbidden**.

Examples of resource-level checks:

| Resource              | Check                                                  |
| --------------------- | ------------------------------------------------------ |
| Listing update/delete | Caller must be `listing.HostId`                        |
| Booking cancellation  | Caller must be `booking.GuestId` or the listing's host |
| Booking status update | Caller must be the listing's host                      |
| Review deletion       | Caller must be `review.AuthorId`                       |
| Conversation access   | Caller must be `User1Id` or `User2Id`                  |
| Message send          | Caller must be a conversation participant              |

---

## Refresh token rotation

Every `POST /auth/refresh`:

1. Validates the submitted token is in the DB, not expired, and not revoked (`IsActive` checks both `ExpiresAt` and `RevokedAt`).
2. Sets `RevokedAt = DateTime.UtcNow` on the old token.
3. Creates a new refresh token and a new access token, returns both.

Each refresh token can therefore be used **only once**. If a token is used after revocation (replay attack), the request fails with 401.

`POST /auth/logout` simply sets `RevokedAt` immediately without issuing a new pair.

---

## Soft-deleted users

Users can soft-delete their account (`DELETE /users/me`). The `IsDeleted` flag is set and PII fields (email, name, bio, avatar) are wiped. Auth endpoints check `IsDeleted` and return 401 if the account is deleted, preventing login with stale refresh tokens.
