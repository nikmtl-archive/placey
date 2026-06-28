# Frontend - State Management (Pinia)

All stores use the **composition API style** (`defineStore('id', () => { ... })`). They are re-exported from `stores/index.ts` for a single import path.

---

## `useAuthStore`

**File:** [placey-fe/src/stores/useAuthStore.ts](../../placey-fe/src/stores/useAuthStore.ts)

Central auth store. All other stores that call authenticated endpoints use `authStore.apiFetch()` instead of raw `fetch()`.

### State

| Ref            | Type               | Description                                      |
| -------------- | ------------------ | ------------------------------------------------ |
| `user`         | `AuthUser \| null` | Logged-in user profile                           |
| `accessToken`  | `string \| null`   | JWT — stored in memory only, never persisted     |
| `refreshToken` | `string \| null`   | Opaque refresh token — persisted in localStorage |
| `isLoading`    | `boolean`          | Auth action in progress                          |
| `error`        | `string \| null`   | Last auth error message                          |

### Computed

| Computed     | Description                           |
| ------------ | ------------------------------------- |
| `isLoggedIn` | `true` when `accessToken` is non-null |
| `isHost`     | Derived from user profile             |

### Methods

| Method                            | Description                                                                                                                                                             |
| --------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `login(email, password)`          | Calls `POST /api/auth/login`, stores session                                                                                                                            |
| `register(email, password, name)` | Calls `POST /api/auth/register`, stores session                                                                                                                         |
| `logout()`                        | Calls `POST /api/auth/logout`, clears local state                                                                                                                       |
| `refreshSession()`                | Calls `POST /api/auth/refresh`. Serialises concurrent calls — only one refresh request is in-flight even if multiple API calls 401 simultaneously                       |
| `apiFetch(input, init)`           | Drop-in `fetch` wrapper. Attaches `Authorization` header. On 401, transparently refreshes the token and retries once. If the refresh also fails, the session is cleared |
| `authHeaders()`                   | Returns `{ Authorization: 'Bearer <token>' }` or `{}`                                                                                                                   |
| `patchUser(fields)`               | Merges partial updates into the local `user` object                                                                                                                     |

### Session persistence

On store creation the session is synchronously restored from `localStorage` (key: `placey_session`). `setSession()` writes to localStorage on every login/refresh. On logout, the key is deleted.

---

## `useListingsStore`

**File:** [placey-fe/src/stores/useListingsStore.ts](../../placey-fe/src/stores/useListingsStore.ts)

Manages the paginated search result list used by `SearchView` and the category sections in `HomeView`.

### State

| Ref         | Type             | Description                              |
| ----------- | ---------------- | ---------------------------------------- |
| `listings`  | `Listing[]`      | Current page of results                  |
| `total`     | `number`         | Total matching listings (for pagination) |
| `isLoading` | `boolean`        | Fetch in progress                        |
| `error`     | `string \| null` | Error message                            |

### Methods

| Method                  | Description                                                                                                                                                                  |
| ----------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `fetchListings(params)` | Builds query string from `ListingSearchParams`, fires `GET /api/listings`, replaces `listings`. Uses `AbortController` to cancel in-flight requests when a new search starts |

Search params include: `location`, `checkIn`, `checkOut`, `guests`, `category`, `propertyType`, `minBedrooms`, `minPrice`, `maxPrice`, `page`, `limit`. Absent or falsy params are omitted from the query string.

---

## `useListingStore`

**File:** [placey-fe/src/stores/useListingStore.ts](../../placey-fe/src/stores/useListingStore.ts)

Manages a single listing detail page. Loaded by `ListingView` on mount.

### State

| Ref         | Type              | Description                                      |
| ----------- | ----------------- | ------------------------------------------------ |
| `listing`   | `Listing \| null` | Current listing                                  |
| `reviews`   | `Review[]`        | Reviews for the listing                          |
| `blockings` | `ListingBlock[]`  | Host-set date blocks (used by `DateRangePicker`) |
| `isLoading` | `boolean`         |                                                  |
| `error`     | `string \| null`  |                                                  |
| `isDeleted` | `boolean`         | Set when API returns 410 Gone                    |

### Methods

| Method               | Description                                                                |
| -------------------- | -------------------------------------------------------------------------- |
| `fetchListing(id)`   | `GET /api/listings/:id`; sets `isDeleted` on 410                           |
| `fetchReviews(id)`   | `GET /api/listings/:id/reviews`                                            |
| `fetchBlockings(id)` | `GET /api/listings/:id/blockings` (host-only action, called conditionally) |
| `reset()`            | Clears all refs — called before loading a new listing                      |

---

## `useBookingStore`

**File:** [placey-fe/src/stores/useBookingStore.ts](../../placey-fe/src/stores/useBookingStore.ts)

Handles the booking creation flow in `ListingView` and the guest bookings dashboard.

### State

| Ref            | Type              | Description                                              |
| -------------- | ----------------- | -------------------------------------------------------- |
| `bookings`     | `Booking[]`       | Guest's bookings (loaded in `BookingsView`)              |
| `lastBooking`  | `Booking \| null` | Most recently created booking (for confirmation message) |
| `isSubmitting` | `boolean`         | Disables the booking button during POST                  |
| `isCancelling` | `boolean`         | Disables cancel action during DELETE                     |
| `isRating`     | `boolean`         | Disables rate action during review POST                  |
| `error`        | `string \| null`  |                                                          |

### Methods

| Method                                                | Description                                                      |
| ----------------------------------------------------- | ---------------------------------------------------------------- |
| `fetchMyBookings()`                                   | `GET /api/users/me/bookings`                                     |
| `createBooking(listingId, checkIn, checkOut, guests)` | `POST /api/bookings` via `authStore.apiFetch`                    |
| `cancelBooking(id)`                                   | `DELETE /api/bookings/:id`, removes booking from `bookings` list |
| `rateBooking(bookingId, listingId, rating, comment)`  | `POST /api/reviews`, then refreshes `bookings` list              |

---

## `useFavoritesStore`

**File:** [placey-fe/src/stores/useFavoritesStore.ts](../../placey-fe/src/stores/useFavoritesStore.ts)

### State

| Ref           | Type             | Description                                            |
| ------------- | ---------------- | ------------------------------------------------------ |
| `favoriteIds` | `Set<string>`    | IDs of favorited listings — only IDs, not full objects |
| `isLoading`   | `boolean`        |                                                        |
| `error`       | `string \| null` |                                                        |

### Methods

| Method                      | Description                                                     |
| --------------------------- | --------------------------------------------------------------- |
| `hydrate()`                 | `GET /api/favorites` — loads all favorited listing IDs on login |
| `isFavorited(listingId)`    | Synchronous check used to drive the heart icon state            |
| `toggleFavorite(listingId)` | Optimistic toggle: updates UI immediately, reverts on API error |
| `reset()`                   | Clears state on logout                                          |

Only IDs are stored (not full listing objects). `FavoritesView` fetches full listing data independently via `GET /api/favorites`.

---

## `useMessagingStore`

**File:** [placey-fe/src/stores/useMessagingStore.ts](../../placey-fe/src/stores/useMessagingStore.ts)

See [../messaging.md](../messaging.md) for full detail on the SSE design and messaging feature.

### State

| Ref                  | Type                         | Description                      |
| -------------------- | ---------------------------- | -------------------------------- |
| `conversations`      | `ConversationSummary[]`      | Inbox list                       |
| `activeConversation` | `ConversationDetail \| null` | Currently open conversation      |
| `unreadCount`        | `number`                     | Badge shown in `NavBar`          |
| `isLoading`          | `boolean`                    |                                  |
| `isSending`          | `boolean`                    | Disables send button during POST |
| `error`              | `string \| null`             |                                  |

### Methods

| Method                                 | Description                                                                                                    |
| -------------------------------------- | -------------------------------------------------------------------------------------------------------------- |
| `fetchConversations()`                 | Loads the inbox                                                                                                |
| `fetchConversation(id)`                | Loads a single conversation with messages; marks messages as read                                              |
| `getOrCreateConversation(otherUserId)` | `POST /api/messages/conversations`; returns conversation ID for navigation                                     |
| `sendMessage(conversationId, body)`    | Sends message and appends to `activeConversation.messages`                                                     |
| `fetchUnreadCount()`                   | Refreshes the nav badge                                                                                        |
| `connectStream()`                      | Opens `EventSource` SSE connection; on new message, updates `activeConversation` or increments unread counters |
| `disconnectStream()`                   | Closes the SSE connection (called on logout)                                                                   |

SSE reconnect: on connection error the store waits 5 seconds then calls `connectStream()` again.
