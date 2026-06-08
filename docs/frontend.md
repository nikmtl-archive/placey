# Frontend — Architecture & Developer Guide

## Stack

| Layer | Technology |
|-------|-----------|
| Framework | Vue 3 (Composition API + `<script setup>`) |
| State management | Pinia |
| Routing | Vue Router 4 |
| Styling | Tailwind CSS v4 (Vite plugin) |
| Icons | `@lucide/vue` |
| Build tool | Vite |
| Package manager | pnpm |
| Serving (production) | nginx (inside Docker) |

---

## Project structure

```
placey-fe/
├── public/
├── src/
│   ├── assets/
│   ├── components/             Reusable UI components
│   │   ├── CategoryFilter.vue
│   │   ├── ConversationList.vue
│   │   ├── ConversationView.vue
│   │   ├── DateRangePicker.vue
│   │   ├── FeaturedSection.vue
│   │   ├── HeroSection.vue
│   │   ├── ListingCard.vue
│   │   ├── NavBar.vue
│   │   ├── SearchBar.vue
│   │   └── SiteFooter.vue
│   ├── composables/            Shared logic hooks
│   │   └── useClickOutside.ts
│   ├── router/
│   │   └── index.ts            Route definitions + auth guard
│   ├── stores/                 Pinia stores
│   │   ├── index.ts            Re-exports all stores
│   │   ├── useAuthStore.ts
│   │   ├── useBookingStore.ts
│   │   ├── useFavoritesStore.ts
│   │   ├── useListingStore.ts  Single listing detail
│   │   ├── useListingsStore.ts Paginated search results
│   │   └── useMessagingStore.ts
│   ├── types/
│   │   └── index.ts            TypeScript interfaces matching API response shapes
│   ├── views/                  Route-level page components
│   │   ├── AuthView.vue        Shared login / register page
│   │   ├── BecomeAHostView.vue
│   │   ├── FavoritesView.vue
│   │   ├── HomeView.vue
│   │   ├── ListingView.vue
│   │   ├── MessagesView.vue
│   │   ├── PlaceholderView.vue Generic placeholder for out-of-scope pages
│   │   └── SearchView.vue
│   ├── App.vue                 Root component — SSE lifecycle, NavBar, Footer
│   └── main.ts
├── nginx.conf                  Production nginx config (handles SPA routing)
├── vite.config.ts
└── package.json
```

---

## Routing

Routes are defined in [src/router/index.ts](../placey-fe/src/router/index.ts).

| Path | Name | Auth required | Component |
|------|------|:---:|-----------|
| `/` | `home` | | `HomeView` |
| `/listing/:id` | `listing` | | `ListingView` |
| `/search` | `search` | | `SearchView` |
| `/favorites` | `favorites` | ✅ | `FavoritesView` |
| `/become-a-host` | `become-a-host` | | `BecomeAHostView` |
| `/login` | `login` | | `AuthView` |
| `/register` | `register` | | `AuthView` |
| `/messages/:id?` | `messages` | ✅ | `MessagesView` |
| `/about` | `about` | | `PlaceholderView` |
| `/privacy` | `privacy` | | `PlaceholderView` |
| `/terms` | `terms` | | `PlaceholderView` |

**Auth guard:** `router.beforeEach` checks `meta.requiresAuth`. Unauthenticated users are redirected to `/login?redirect=<original-path>`. After successful login/register, `AuthView` reads the `redirect` query param and navigates back to the original destination.

All routes use `scrollBehavior: () => ({ top: 0 })` to reset scroll position on navigation.

---

## State management (Pinia stores)

All stores use the **composition API style** (`defineStore('id', () => { ... })`). They are re-exported from `stores/index.ts` for a single import path.

### `useAuthStore`

Central auth store. All other stores that call authenticated endpoints use `authStore.apiFetch()` instead of raw `fetch()`.

**State**

| Ref | Type | Description |
|-----|------|-------------|
| `user` | `AuthUser \| null` | Logged-in user profile |
| `accessToken` | `string \| null` | JWT — stored in memory only |
| `refreshToken` | `string \| null` | Opaque refresh token |
| `isLoading` | `bool` | Auth action in progress |
| `error` | `string \| null` | Last auth error message |

**Computed**

- `isLoggedIn` — `true` when `accessToken` is non-null

**Key methods**

| Method | Description |
|--------|-------------|
| `login(email, password)` | Calls `POST /auth/login`, stores session |
| `register(email, password, name)` | Calls `POST /auth/register`, stores session |
| `logout()` | Calls `POST /auth/logout`, clears local state |
| `refreshSession()` | Calls `POST /auth/refresh`. Deduplicates concurrent calls — only one refresh request goes out even if multiple in-flight API calls all 401 simultaneously |
| `apiFetch(input, init)` | Drop-in `fetch` wrapper. Attaches `Authorization` header. On 401, transparently refreshes the token and retries once. If the refresh also fails, the session is cleared |
| `authHeaders()` | Returns `{ Authorization: 'Bearer <token>' }` or `{}` |

**Session persistence:** on store creation, the session is synchronously restored from `localStorage` (key: `placey_session`). `setSession()` writes to localStorage on every login/refresh.

---

### `useListingsStore`

Manages the paginated search result list (used by `SearchView` and `HomeView`).

Holds `listings`, `total`, `isLoading`, `error`, and the current `filters`. Calling `search(params)` updates filters and fetches page 1; `loadMore()` appends the next page.

---

### `useListingStore`

Manages a single listing page. Loaded by `ListingView` on mount.

| Ref | Description |
|-----|-------------|
| `listing` | Current listing detail |
| `reviews` | Reviews for the listing |
| `availability` | Availability ranges (used to block dates in the date picker) |

Exposes `fetchListing`, `fetchReviews`, `fetchAvailability`, and `reset`.

---

### `useBookingStore`

Thin store for the booking creation flow in `ListingView`.

| Ref | Description |
|-----|-------------|
| `isSubmitting` | Disables the booking button during the POST |
| `error` | Error message shown inline |
| `lastBooking` | Last created booking (for success messaging) |

`createBooking(listingId, checkIn, checkOut, guests)` calls `POST /bookings` via `authStore.apiFetch` and stores the result.

---

### `useFavoritesStore`

| Method | Description |
|--------|-------------|
| `hydrate()` | Loads all favorited listing IDs from `GET /favorites` |
| `isFavorited(listingId)` | Synchronous check — used to drive the heart icon |
| `toggleFavorite(listingId)` | Optimistic toggle with automatic revert on error |
| `reset()` | Clears state on logout |

Only IDs are stored (not full listing objects), keeping memory usage low. The `FavoritesView` fetches full listing data independently.

---

### `useMessagingStore`

See [docs/messaging.md](./messaging.md) for full detail.

---

## API communication

All API calls go through the Vite dev proxy (`/api → http://localhost:5001`), configured in `vite.config.ts`. In production, nginx forwards `/api/` to the backend container.

Authenticated calls go through `authStore.apiFetch()`. Unauthenticated calls (listings, availability, reviews) use raw `fetch()` directly.

---

## App-level wiring (`App.vue`)

- Watches `authStore.isLoggedIn` and calls `messagingStore.connectStream()` / `disconnectStream()` accordingly — so the SSE connection is opened immediately on login and closed on logout.
- `SiteFooter` is hidden on `/messages/*` routes (the messaging view uses the full viewport height).

---

## Views

### `HomeView`

Landing page. Renders `HeroSection`, `CategoryFilter`, `FeaturedSection`, and a listing grid from `useListingsStore`.

### `SearchView`

Full-page listing search with `SearchBar` at the top and a results grid. Syncs filters to/from URL query params so searches are bookmarkable and the back button works correctly.

### `ListingView`

The most complex view. Key features:

- **Photo gallery** with a full-screen lightbox (prev/next navigation, keyboard support).
- **Booking panel** — date range picker, guest selector, price breakdown. Availability ranges from `useListingStore` are passed to `DateRangePicker` to disable blocked dates.
- **Booking button logic** — disabled if dates are missing, the range overlaps a blocked availability, or the user is the host.
- **"Message host" button** — calls `messagingStore.getOrCreateConversation(hostId)` and navigates to `/messages/:id`. Redirects unauthenticated users to `/login` with a `redirect` param.
- **Reviews section** — star rating display, sorted list, scrollable.
- **Favorites** — heart icon wired to `useFavoritesStore.toggleFavorite`.

### `MessagesView`

Two-panel inbox layout. See [docs/messaging.md](./messaging.md).

### `AuthView`

Shared login and register page. The active mode is determined by the current route name (`login` vs `register`). After success it navigates to the `redirect` query param or `/`.

### `FavoritesView`

Fetches and displays the current user's saved listings as a grid of `ListingCard` components.

---

## Key components

### `NavBar`

- Sticky header with scroll shadow.
- Shows "Become a Host" + "Log in" when logged out.
- Shows a dropdown menu with avatar/initials when logged in. The dropdown includes the **Messages** link with an unread badge driven by `messagingStore.unreadCount`, which is fetched on mount.
- Unread count is fetched once on mount; real-time updates arrive via the SSE stream (handled in `useMessagingStore`).

### `DateRangePicker`

Calendar component used in `ListingView`. Accepts `blockedRanges` and highlights unavailable dates. Emits `update:checkIn` / `update:checkOut`.

### `ListingCard`

Card used in listing grids (search results, favorites, featured). Displays photo carousel, price, rating, and a favorites heart.

### `SearchBar`

Date + location + guest count inputs. Used in `HomeView` (compact) and `SearchView` (expanded).

### `ConversationList` / `ConversationView`

See [docs/messaging.md](./messaging.md).

---

## Production build & nginx

```bash
cd placey-fe
pnpm build       # outputs to dist/
```

In Docker, the frontend is served by nginx. `nginx.conf` is configured to:

- Serve static assets from `dist/`.
- Route all non-asset requests to `index.html` (SPA fallback) so client-side routes work on direct access / refresh.
- Proxy `/api/` to the backend container.

---

## Running locally

```bash
# Backend and database must be running (see backend.md)
cd placey-fe
pnpm install
pnpm dev
# Frontend: http://localhost:5173
```

The Vite dev server proxies `/api/*` to `http://localhost:5001`, so no CORS configuration is needed in development.
