# Backend API Endpoints

Base URL: ` `

---

## Listings

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/listings` | ❌ | Search/filter listings |
| GET | `/listings/:id` | ❌ | Get single listing detail |
| POST | `/listings` | ✅ | Create a new listing (host only) |
| PATCH | `/listings/:id` | ✅ | Update listing (host only) |
| DELETE | `/listings/:id` | ✅ | Delete listing (host only) |
| PATCH | `/listings/:id/availability` | ✅ | Set blocked/available dates |
| GET | `/listings/:id/availability` | ❌ | Get available date ranges |

### Search Query Parameters (`GET /listings`)
```
?location=Berlin
&checkin=2025-06-01
&checkout=2025-06-07
&guests=2
&page=1
&limit=20
```

---

## Bookings

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/bookings` | ✅ | Create a booking request |
| GET | `/bookings/:id` | ✅ | Get booking detail |
| DELETE | `/bookings/:id` | ✅ | Cancel a booking |
| PATCH | `/bookings/:id/status` | ✅ | Host approves/declines (if not instant-book) |

### Booking statuses
`pending` → `confirmed` → `completed`

---

## Reviews

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/reviews` | ✅ | Submit a review (after completed booking) |
| DELETE | `/reviews/:id` | ✅ | Delete own review |
| GET | `/listings/:id/reviews` | ❌ | Get all reviews for a listing |
| GET | `/users/:id/reviews` | ❌ | Get reviews about a host |

---

## Favorites

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/favorites` | ✅ | Get user's favorites |
| POST | `/favorites/:id/listings/:listingId` | ✅ | Add listing to favorites |
| DELETE | `/favorites/:id/listings/:listingId` | ✅ | Remove listing from favorites |

---

## Auth

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/auth/register` | ❌ | Register with email + password |
| POST | `/auth/login` | ❌ | Login, returns access + refresh tokens |
| POST | `/auth/logout` | ✅ | Invalidate refresh token |
| POST | `/auth/refresh` | ❌ | Exchange refresh token for new access token |
| POST | `/auth/forgot-password` | ❌ | Send password reset email |
| POST | `/auth/reset-password` | ❌ | Reset password via token |
| POST | `/auth/oauth/:provider` | ❌ | OAuth login (google, facebook) |

---

## Users

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/users/me` | ✅ | Get current user profile |
| PATCH | `/users/me` | ✅ | Update profile (name, bio, avatar, etc.) |
| DELETE | `/users/me` | ✅ | Delete own account |
| GET | `/users/:id` | ❌ | Get public profile of any user |
| POST | `/users/me/avatar` | ✅ | Upload profile photo |
| GET | `/users/me/listings` | ✅ | Get own listings |
| GET | `/users/me/bookings` | ✅ | Get own bookings (as guest) |
| GET | `/users/me/reservations` | ✅ | Get own reservations (as host) |

## What's intentionally excluded from this MVP

- Identity verification (ID upload, selfie check)
- Co-hosts / team management
- Pricing rules (weekly/monthly discounts, seasonal pricing)
- Taxes & VAT handling (jurisdiction-specific)
- Resolution center / dispute management
- Experiences (non-accommodation bookings)
- Superhost status calculation

These are real Airbnb features that add significant complexity — design them only when the core loop is stable.