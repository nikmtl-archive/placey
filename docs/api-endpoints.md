# API Endpoints

Base URL (local, Docker): `http://localhost:5001`  
Base URL (local, dotnet run): `https://localhost:7220`  
Swagger UI (dev): `<base-url>/swagger`

All authenticated endpoints require an `Authorization: Bearer <access-token>` header.  
On 401, refresh the token via `POST /auth/refresh` before retrying.

> For backend architecture, auth flow, and database schema see [docs/backend.md](./backend.md).  
> For the frontend store and component design see [docs/messaging.md](./messaging.md) (messaging) and [docs/frontend.md](./frontend.md) (general).

---

## Auth

| Method | Endpoint         | Auth  | Description                                   |
| ------ | ---------------- | :---: | --------------------------------------------- |
| POST   | `/auth/register` |       | Create an account                             |
| POST   | `/auth/login`    |       | Log in — returns access + refresh tokens      |
| POST   | `/auth/refresh`  |       | Exchange a refresh token for a new token pair |
| POST   | `/auth/logout`   |   ✅   | Revoke the current refresh token              |

**Request — register**
```json
{ "email": "user@example.com", "password": "secret123", "name": "Jane" }
```

**Request — login**
```json
{ "email": "user@example.com", "password": "secret123" }
```

**Request — refresh**
```json
{ "refreshToken": "<opaque-string>" }
```

**Request — logout**
```json
{ "refreshToken": "<opaque-string>" }
```

**Response — register / login / refresh**
```json
{
  "accessToken": "<jwt>",
  "refreshToken": "<opaque-string>",
  "user": {
    "id": "<guid>",
    "email": "user@example.com",
    "name": "Jane",
    "bio": null,
    "avatarUrl": null
  }
}
```

**Token lifetimes**

| Token              | Lifetime   | Notes                                                  |
| ------------------ | ---------- | ------------------------------------------------------ |
| Access token (JWT) | 15 minutes | Sent in `Authorization` header                         |
| Refresh token      | 7 days     | Rotated on each use — old token is immediately revoked |

---

## Listings

| Method | Endpoint                     | Auth  | Description                                     |
| ------ | ---------------------------- | :---: | ----------------------------------------------- |
| GET    | `/listings`                  |       | Search / filter listings                        |
| GET    | `/listings/:id`              |       | Get a single listing with host details          |
| POST   | `/listings`                  |   ✅   | Create a listing                                |
| PATCH  | `/listings/:id`              |   ✅   | Update a listing (host only)                    |
| DELETE | `/listings/:id`              |   ✅   | Delete a listing (host only)                    |
| GET    | `/listings/:id/availability` |       | Get availability ranges                         |
| PATCH  | `/listings/:id/availability` |   ✅   | Add or update an availability range (host only) |
| GET    | `/listings/:id/reviews`      |       | Get all reviews for a listing                   |

### Search query parameters (`GET /listings`)

| Parameter               | Type    | Description                                                |
| ----------------------- | ------- | ---------------------------------------------------------- |
| `location`              | string  | Free-text match against city / country                     |
| `checkIn`               | date    | `YYYY-MM-DD`                                               |
| `checkOut`              | date    | `YYYY-MM-DD` — excludes listings with conflicting bookings |
| `guests`                | int     | Excludes listings where `maxGuests < guests`               |
| `category`              | string  | Beach, Mountains, Urban, Luxe, Countryside, Island, Lake   |
| `propertyType`          | string  | Apartment, House                                           |
| `minBedrooms`           | int     |                                                            |
| `minPrice` / `maxPrice` | decimal | Price per night                                            |
| `page`                  | int     | Default 1                                                  |
| `limit`                 | int     | Default 20                                                 |

Response:
```json
{ "items": [ /* Listing objects */ ], "total": 42 }
```

### Listing response shape

```json
{
  "id": "<guid>",
  "hostId": "<guid>",
  "hostName": "Jane",
  "hostAvatarUrl": null,
  "title": "Cosy flat in Mitte",
  "description": "A bright, spacious studio...",
  "street": "Torstraße 12",
  "city": "Berlin",
  "country": "Germany",
  "pricePerNight": 89.00,
  "bedrooms": 1,
  "bathrooms": 1,
  "maxGuests": 3,
  "isInstantBook": false,
  "amenities": ["WiFi", "Kitchen"],
  "category": "Urban",
  "propertyType": "Apartment",
  "photoUrls": ["https://..."],
  "averageRating": 4.5,
  "reviewCount": 12,
  "createdAt": "2025-06-01T10:00:00Z"
}
```

### Availability range shape

```json
{
  "id": "<guid>",
  "startDate": "2025-07-01",
  "endDate": "2025-07-14",
  "isBlocked": true
}
```

`isBlocked: true` = dates are unavailable. `isBlocked: false` = host has explicitly marked dates as open.

---

## Bookings

| Method | Endpoint               | Auth  | Description                             |
| ------ | ---------------------- | :---: | --------------------------------------- |
| POST   | `/bookings`            |   ✅   | Request a booking                       |
| GET    | `/bookings/:id`        |   ✅   | Get booking detail (guest or host only) |
| DELETE | `/bookings/:id`        |   ✅   | Cancel a booking (guest or host)        |
| PATCH  | `/bookings/:id/status` |   ✅   | Change booking status (host only)       |

**Request — create booking**
```json
{
  "listingId": "<guid>",
  "checkIn": "2025-07-01",
  "checkOut": "2025-07-07",
  "guests": 2
}
```

`TotalPrice` is computed server-side as `nights × pricePerNight`.

**Request — update status**
```json
{ "status": "Confirmed" }
```

Valid host transitions: `Pending → Confirmed`, `Confirmed → Completed`, any non-`Completed` → `Cancelled`.

### Booking status flow

```
Pending → Confirmed → Completed
       ↘           ↘
        Cancelled   Cancelled
```

- Listings with `isInstantBook: true` skip `Pending` and go straight to `Confirmed`.
- `Completed` is terminal — it cannot be cancelled.

### Booking response shape

```json
{
  "id": "<guid>",
  "listingId": "<guid>",
  "listingTitle": "Cosy flat in Mitte",
  "guestId": "<guid>",
  "guestName": "Max",
  "checkIn": "2025-07-01",
  "checkOut": "2025-07-07",
  "guests": 2,
  "totalPrice": 534.00,
  "status": "Pending",
  "createdAt": "2025-05-20T08:00:00Z"
}
```

---

## Reviews

| Method | Endpoint                | Auth  | Description                       |
| ------ | ----------------------- | :---: | --------------------------------- |
| POST   | `/reviews`              |   ✅   | Submit a review                   |
| DELETE | `/reviews/:id`          |   ✅   | Delete own review                 |
| GET    | `/listings/:id/reviews` |       | All reviews for a listing         |
| GET    | `/users/:id/reviews`    |       | All reviews for a host's listings |

**Request — create review**
```json
{
  "bookingId": "<guid>",
  "listingId": "<guid>",
  "rating": 5,
  "comment": "Wonderful place!"
}
```

**Rules:**
- The booking must have status `Completed`.
- Only the guest on the booking can submit a review.
- One review per booking (enforced by unique index on `BookingId`).

### Review response shape

```json
{
  "id": "<guid>",
  "listingId": "<guid>",
  "authorId": "<guid>",
  "authorName": "Max",
  "authorAvatarUrl": null,
  "rating": 5,
  "comment": "Wonderful place!",
  "createdAt": "2025-08-10T14:00:00Z"
}
```

---

## Favorites

| Method | Endpoint                | Auth  | Description                                  |
| ------ | ----------------------- | :---: | -------------------------------------------- |
| GET    | `/favorites`            |   ✅   | Get current user's saved listings            |
| POST   | `/favorites/:listingId` |   ✅   | Save a listing (idempotent)                  |
| DELETE | `/favorites/:listingId` |   ✅   | Remove a listing from favorites (idempotent) |

`GET /favorites` returns an array of full listing objects (same shape as `GET /listings` items).

---

## Users

| Method | Endpoint                 | Auth  | Description                                                 |
| ------ | ------------------------ | :---: | ----------------------------------------------------------- |
| GET    | `/users/me`              |   ✅   | Own profile (includes email)                                |
| PATCH  | `/users/me`              |   ✅   | Update name / bio                                           |
| DELETE | `/users/me`              |   ✅   | Delete own account                                          |
| POST   | `/users/me/avatar`       |   ✅   | Upload profile photo (`multipart/form-data`, field: `file`) |
| GET    | `/users/me/listings`     |   ✅   | Own listings (as host)                                      |
| GET    | `/users/me/bookings`     |   ✅   | Own bookings (as guest)                                     |
| GET    | `/users/me/reservations` |   ✅   | Bookings on own listings (as host)                          |
| GET    | `/users/:id`             |       | Public profile of any user                                  |
| GET    | `/users/:id/reviews`     |       | Reviews written about a host's listings                     |

**Request — update profile**
```json
{ "name": "Jane Smith", "bio": "I love hosting." }
```

Omit a field (or set it to `null`) to leave it unchanged.

---

## Messages

All message endpoints require authentication.

| Method | Endpoint                               | Description                                                   |
| ------ | -------------------------------------- | ------------------------------------------------------------- |
| GET    | `/messages`                            | All conversations for the current user                        |
| POST   | `/messages/conversations`              | Get or create a conversation with another user                |
| GET    | `/messages/conversations/:id`          | Conversation with full message history (marks unread as read) |
| POST   | `/messages/conversations/:id/messages` | Send a message                                                |
| GET    | `/messages/unread-count`               | Total unread message count across all conversations           |
| GET    | `/messages/stream`                     | SSE stream — pushes new messages in real time                 |

### Request shapes

**POST `/messages/conversations`**
```json
{ "otherUserId": "<guid>" }
```

This is idempotent — returns the existing conversation if one already exists.

**POST `/messages/conversations/:id/messages`**
```json
{ "body": "Hello!" }
```

`body` is required, 1–4000 characters.

### Response shapes

**Conversation summary** (list item, returned by `GET /messages` and `POST /messages/conversations`)
```json
{
  "id": "<guid>",
  "otherUserId": "<guid>",
  "otherUserName": "Jane",
  "otherUserAvatarUrl": null,
  "lastMessageBody": "Hello!",
  "lastMessageAt": "2026-06-08T12:00:00Z",
  "unreadCount": 2,
  "createdAt": "2026-06-08T10:00:00Z"
}
```

**Conversation detail** (returned by `GET /messages/conversations/:id`)
```json
{
  "id": "<guid>",
  "otherUserId": "<guid>",
  "otherUserName": "Jane",
  "otherUserAvatarUrl": null,
  "messages": [ /* Message objects, ordered by createdAt asc */ ]
}
```

**Message** (returned by `POST .../messages`, pushed over SSE)
```json
{
  "id": "<guid>",
  "conversationId": "<guid>",
  "senderId": "<guid>",
  "senderName": "Max",
  "senderAvatarUrl": null,
  "body": "Hello!",
  "isRead": false,
  "createdAt": "2026-06-08T12:00:00Z"
}
```

**GET `/messages/unread-count`**
```json
{ "count": 3 }
```

### SSE stream (`GET /messages/stream`)

`EventSource` cannot send custom headers, so the JWT is passed as a query parameter:

```
GET /api/messages/stream?access_token=<jwt>
```

The connection stays open indefinitely. The server writes one `data:` line per inbound message (same JSON shape as the Message object above). The frontend retries automatically after 5 seconds on connection error.

---

## Error responses

All errors follow the RFC 7807 `ProblemDetails` format:

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Bad Request",
  "status": 400,
  "detail": "The check-out date must be after check-in."
}
```

| HTTP status               | When                                                            |
| ------------------------- | --------------------------------------------------------------- |
| 400 Bad Request           | Invalid input, business rule violation                          |
| 401 Unauthorized          | Missing or expired JWT                                          |
| 403 Forbidden             | Valid JWT but caller doesn't own the resource                   |
| 404 Not Found             | Resource doesn't exist                                          |
| 422 Unprocessable Entity  | Model validation failure (`[Required]`, `[StringLength]`, etc.) |
| 500 Internal Server Error | Unexpected server error                                         |

---

## What's intentionally out of scope (MVP)

- Identity verification (ID upload, selfie check)
- Co-hosts / team management
- Dynamic pricing (weekly/monthly discounts, seasonal rates)
- Taxes & VAT
- Payment processing (PayPal sandbox planned but not implemented)
- Resolution centre / dispute management
- Experiences (non-accommodation bookings)
- Superhost status
- Group conversations (messaging is 1-to-1 only)
