# API Endpoints

Base URL (local): `http://localhost:5286`  
Swagger UI (dev): `http://localhost:5286/swagger`

All authenticated endpoints require an `Authorization: Bearer <access-token>` header.

> [!NOTE]
> For detailed technical documentation on the backend architecture, auth flow, database schema, error handling, migrations, and more, see [docs/backend.md](./backend.md).
> A automatically generated API reference is generated with Swagger and available at `http://localhost:5286/swagger` when running in Development.
---

## Auth

| Method | Endpoint | Auth | Description |
|--------|----------|:----:|-------------|
| POST | `/auth/register` | | Register with email + password |
| POST | `/auth/login` | | Login — returns access + refresh tokens |
| POST | `/auth/refresh` | | Exchange refresh token for a new token pair |
| POST | `/auth/logout` | ✅ | Revoke refresh token |

### Token shapes

**Request — register**
```json
{ "email": "user@example.com", "password": "secret123", "name": "Jane" }
```

**Request — login / refresh / logout**
```json
{ "email": "user@example.com", "password": "secret123" }
{ "refreshToken": "<opaque-string>" }
```

**Response — all auth endpoints**
```json
{
  "accessToken": "<jwt>",
  "refreshToken": "<opaque-string>",
  "user": { "id": "...", "email": "...", "name": "...", "bio": null, "avatarUrl": null }
}
```

Access tokens expire after **15 minutes**. Refresh tokens are valid for **7 days** and are rotated on each use.

---

## Listings

| Method | Endpoint | Auth | Description |
|--------|----------|:----:|-------------|
| GET | `/listings` | | Search / filter listings |
| GET | `/listings/:id` | | Get a single listing |
| POST | `/listings` | ✅ | Create a listing |
| PATCH | `/listings/:id` | ✅ | Update listing (host only) |
| DELETE | `/listings/:id` | ✅ | Delete listing (host only) |
| GET | `/listings/:id/availability` | | Get availability ranges |
| PATCH | `/listings/:id/availability` | ✅ | Add a blocked / available range (host only) |
| GET | `/listings/:id/reviews` | | Get all reviews for a listing |

### Search query parameters (`GET /listings`)

```
?location=Berlin
&checkin=2025-06-01
&checkout=2025-06-07
&guests=2
&page=1
&limit=20
```

Results are returned as `{ "items": [...], "total": 42 }`.

### Listing response shape

```json
{
  "id": "...",
  "hostId": "...",
  "hostName": "Jane",
  "hostAvatarUrl": null,
  "title": "Cosy flat in Mitte",
  "description": "...",
  "location": "Berlin",
  "pricePerNight": 89.00,
  "maxGuests": 3,
  "isInstantBook": false,
  "photoUrls": [],
  "averageRating": 4.5,
  "reviewCount": 12,
  "createdAt": "2025-06-01T10:00:00Z"
}
```

---

## Bookings

| Method | Endpoint | Auth | Description |
|--------|----------|:----:|-------------|
| POST | `/bookings` | ✅ | Create a booking request |
| GET | `/bookings/:id` | ✅ | Get booking detail (guest or host only) |
| DELETE | `/bookings/:id` | ✅ | Cancel a booking |
| PATCH | `/bookings/:id/status` | ✅ | Update status (host approves / declines) |

### Booking status flow

```
Pending → Confirmed → Completed
       ↘           ↘
        Cancelled   Cancelled
```

- Listings with `isInstantBook: true` skip `Pending` and go straight to `Confirmed`.
- `Completed` is a terminal state — it cannot be cancelled.

### Booking response shape

```json
{
  "id": "...",
  "listingId": "...",
  "listingTitle": "Cosy flat in Mitte",
  "guestId": "...",
  "guestName": "Max",
  "checkIn": "2025-06-01",
  "checkOut": "2025-06-07",
  "guests": 2,
  "totalPrice": 534.00,
  "status": "Pending",
  "createdAt": "2025-05-20T08:00:00Z"
}
```

---

## Reviews

| Method | Endpoint | Auth | Description |
|--------|----------|:----:|-------------|
| POST | `/reviews` | ✅ | Submit a review (requires a completed booking) |
| DELETE | `/reviews/:id` | ✅ | Delete own review |
| GET | `/listings/:id/reviews` | | All reviews for a listing |
| GET | `/users/:id/reviews` | | All reviews written about a host's listings |

One review is allowed per booking. The booking must have status `Completed`.

---

## Favorites

| Method | Endpoint | Auth | Description |
|--------|----------|:----:|-------------|
| GET | `/favorites` | ✅ | Get current user's saved listings |
| POST | `/favorites/:listingId` | ✅ | Save a listing |
| DELETE | `/favorites/:listingId` | ✅ | Remove a listing from favorites |

---

## Users

| Method | Endpoint | Auth | Description |
|--------|----------|:----:|-------------|
| GET | `/users/me` | ✅ | Get own profile (includes email) |
| PATCH | `/users/me` | ✅ | Update name / bio |
| DELETE | `/users/me` | ✅ | Delete own account |
| POST | `/users/me/avatar` | ✅ | Upload profile photo (`multipart/form-data`) |
| GET | `/users/me/listings` | ✅ | Own listings (as host) |
| GET | `/users/me/bookings` | ✅ | Own bookings (as guest) |
| GET | `/users/me/reservations` | ✅ | Bookings on own listings (as host) |
| GET | `/users/:id` | | Public profile of any user |
| GET | `/users/:id/reviews` | | Reviews about a host's listings |

---

## What's intentionally out of scope (MVP)

- Identity verification (ID upload, selfie check)
- Co-hosts / team management
- Dynamic pricing rules (weekly/monthly discounts, seasonal pricing)
- Taxes & VAT handling
- Resolution centre / dispute management
- Experiences (non-accommodation bookings)
- Superhost status calculation
