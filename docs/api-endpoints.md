# Backend API Endpoints

Base URL: `https://api.yourapp.com/v1`

All protected routes require `Authorization: Bearer <token>` header.

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

---

## Listings

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/listings` | ❌ | Search/filter listings |
| GET | `/listings/:id` | ❌ | Get single listing detail |
| POST | `/listings` | ✅ | Create a new listing |
| PATCH | `/listings/:id` | ✅ | Update listing (host only) |
| DELETE | `/listings/:id` | ✅ | Delete listing (host only) |
| POST | `/listings/:id/photos` | ✅ | Upload photos to listing |
| DELETE | `/listings/:id/photos/:photoId` | ✅ | Remove a photo |
| PATCH | `/listings/:id/availability` | ✅ | Set blocked/available dates |
| GET | `/listings/:id/availability` | ❌ | Get available date ranges |

### Search Query Parameters (`GET /listings`)
```
?location=Berlin
&checkin=2025-06-01
&checkout=2025-06-07
&guests=2
&min_price=50
&max_price=300
&property_type=apartment
&amenities=wifi,pool
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
`pending` → `confirmed` → `completed` / `cancelled`

---

## Payments

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/payments/intent` | ✅ | Create Stripe PaymentIntent for a booking |
| POST | `/payments/webhook` | ❌ | Stripe webhook (server-to-server) |
| GET | `/payments/history` | ✅ | Get payment history for current user |
| POST | `/payouts/onboard` | ✅ | Start Stripe Connect onboarding for hosts |
| GET | `/payouts/status` | ✅ | Get host payout account status |

---

## Reviews

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/reviews` | ✅ | Submit a review (after completed booking) |
| GET | `/listings/:id/reviews` | ❌ | Get all reviews for a listing |
| GET | `/users/:id/reviews` | ❌ | Get reviews about a host |
| DELETE | `/reviews/:id` | ✅ | Delete own review |

---

## Messaging

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/conversations` | ✅ | List all conversations |
| GET | `/conversations/:id` | ✅ | Get messages in a conversation |
| POST | `/conversations` | ✅ | Start a conversation (e.g., pre-booking inquiry) |
| POST | `/conversations/:id/messages` | ✅ | Send a message |

---

## Wishlist / Favorites

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/wishlists` | ✅ | Get user's wishlists |
| POST | `/wishlists` | ✅ | Create a new wishlist |
| POST | `/wishlists/:id/listings/:listingId` | ✅ | Add listing to wishlist |
| DELETE | `/wishlists/:id/listings/:listingId` | ✅ | Remove listing from wishlist |
| DELETE | `/wishlists/:id` | ✅ | Delete a wishlist |

---

## Admin (internal use)

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/admin/users` | ✅ Admin | List all users |
| PATCH | `/admin/users/:id/ban` | ✅ Admin | Ban/unban a user |
| GET | `/admin/listings` | ✅ Admin | List all listings |
| PATCH | `/admin/listings/:id/status` | ✅ Admin | Approve/suspend a listing |

---

## What's intentionally excluded from this MVP

- Identity verification (ID upload, selfie check)
- Co-hosts / team management
- Pricing rules (weekly/monthly discounts, seasonal pricing)
- Taxes & VAT handling (jurisdiction-specific)
- Resolution center / dispute management
- Experiences (non-accommodation bookings)
- Superhost status calculation

These are real Airbnb features that add significant complexity — design them only when the core loop is stable.