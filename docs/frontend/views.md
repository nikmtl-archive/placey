# Frontend - Views Reference

Views are route-level page components in [placey-fe/src/views/](../../placey-fe/src/views/). Each maps 1:1 to a route (see [routing.md](./routing.md)).

---

## `HomeView`

**Route:** `/`

Landing page. Renders:
- `HeroSection` — hero banner with compact `SearchBar`
- `CategoryFilter` — horizontal category chip strip
- `FeaturedSection` per category — horizontal scrollable listing rows

`useListingsStore` powers each `FeaturedSection` with a category-filtered fetch.

---

## `SearchView`

**Route:** `/search`

Full-page listing search. Key behaviours:

- `SearchBar` at the top for location, dates, guest count.
- `SearchFilters` panel for property type, bedrooms, price range.
- Results grid of `ListingCard` components.
- **URL sync**: all active filters are written to the URL query string on every search, so searches are bookmarkable and the browser back button restores the previous search state.
- Pagination: "Load more" appends next page to the existing results.
- Shows empty state when no results match.

---

## `ListingView`

**Route:** `/listing/:id`

The most complex view. Sections:

### Photo gallery
`ListingPhotoGrid` with a 5-photo CSS grid. Clicking any photo opens `ListingLightbox` with keyboard prev/next navigation.

### Booking panel (right-hand sidebar)
- `DateRangePicker` with blocked ranges from `useListingStore.blockings`.
- Guest selector (increment/decrement).
- Price breakdown: nights × price per night + totals.
- Booking button disabled if: dates are missing, the range overlaps a blocked range, or the viewer is the host.
- On success, a confirmation message appears and the host is automatically sent an initial system message via the messaging store.

### "Message host" button
Calls `messagingStore.getOrCreateConversation(listing.hostId)` and navigates to `/messages/:id`. Unauthenticated users are redirected to `/login?redirect=/listing/:id`.

### Reviews section
Star rating display, sorted review list, scrollable. Average rating shown in the header.

### Favorites
Heart icon wired to `useFavoritesStore.toggleFavorite`. Optimistic update.

---

## `AuthView`

**Route:** `/login`, `/register`

Shared login and register page. The active mode is determined by the current route name. After a successful login or register, navigates to the `redirect` query param or `/`.

---

## `BecomeAHostView`

**Route:** `/become-a-host`

Marketing/onboarding landing page explaining the hosting process. Contains a CTA button that navigates to `ListingFormView` (requires auth; if not logged in, redirects to login first).

---

## `ListingFormView`

**Route:** `/listing/new` (create), `/listing/:id/edit` (edit)

Multipurpose form used for both creating and editing listings. Detects create vs. edit mode from the route.

Fields: title, description, street, city, country, category, property type, bedrooms, bathrooms, max guests, price per night, instant book toggle, amenities checkboxes.

Photo management:
- Upload photos (multipart form POST to `/api/listings/:id/photos`).
- Preview uploaded photos.
- Delete individual photos.

On save: `POST /api/listings` (create) or `PATCH /api/listings/:id` (edit). On success, navigates to the listing detail page.

---

## `HostDashboardView`

**Route:** `/host-dashboard`

Two-tab host management interface.

**Listings tab:**
- Lists all listings owned by the current user.
- "Edit" and "Delete" actions per listing.
- "Add listing" button → `ListingFormView`.

**Reservations tab:**
- Lists all bookings on the host's listings.
- Host can Approve (Pending → Confirmed), Complete (Confirmed → Completed), or Cancel bookings.
- Date blocking per listing: add/remove blocked date ranges to prevent guest bookings.

---

## `BookingsView`

**Route:** `/bookings`

Guest bookings dashboard. Renders `Bookings` component with the current user's bookings from `useBookingStore`.

- Cancel button for non-completed bookings.
- Review form for completed bookings without a review.

---

## `HostProfileView`

**Route:** `/host/:id`

Public host profile page. Displays:
- Host avatar, name, bio, member since date.
- "Message" button → `MessagesView`.
- Grid of the host's active listings.
- Reviews written about the host's listings.

---

## `FavoritesView`

**Route:** `/favorites`

Fetches and displays the current user's saved listings as a `ListingCard` grid via `GET /api/favorites`.

---

## `MessagesView`

**Route:** `/messages/:id?`

Two-panel messaging inbox layout. See [../messaging.md](../messaging.md) for full detail.

- Left panel: `ConversationList`
- Right panel: `ConversationView`
- On mobile, only one panel is visible at a time — selecting a conversation hides the list, and `ConversationView`'s back button returns to it.

---

## `SettingsView`

**Route:** `/settings`

Account settings for the logged-in user.

- Update display name and bio (PATCH `/api/users/me`).
- Upload avatar photo (POST `/api/users/me/avatar`).
- Delete account (DELETE `/api/users/me`) — prompts for confirmation.

---

## `PlaceholderView`

**Routes:** `/about`, `/privacy`, `/terms`, `/:pathMatch(.*)*`

Generic placeholder used for pages that are explicitly out of scope and for the 404 catch-all.
