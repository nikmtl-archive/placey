# Frontend - Components Reference

All reusable components live in [placey-fe/src/components/](../../placey-fe/src/components/).

---

## Layout & Navigation

### `NavBar`

Sticky header present on every page.

- Displays the Placey logo (links to `/`).
- **Logged-out state**: shows "Become a Host" link and a "Log in" button.
- **Logged-in state**: shows a user avatar/initials dropdown menu with links to Messages, Bookings, Host Dashboard, Settings, and Logout.
- The Messages link includes an unread badge driven by `messagingStore.unreadCount`. The count is fetched once on mount and updated in real-time via the SSE stream.
- Applies a drop shadow on scroll (scroll event listener).

### `SiteFooter`

Simple footer with links to About, Privacy, and Terms pages. Hidden on `/messages/*` routes (the messaging view occupies full viewport height).

---

## Listing Display

### `ListingCard`

Card used in listing grids (search results, favorites, featured).

| Prop      | Type      | Description            |
| --------- | --------- | ---------------------- |
| `listing` | `Listing` | The listing to display |

- Displays a photo carousel (click left/right arrows), price per night, average rating, review count, and location.
- Favorites heart icon wired to `useFavoritesStore.toggleFavorite`.
- Navigates to `/listing/:id` on click.

### `ListingPhotoGrid`

Photo grid layout shown at the top of `ListingView`.

| Prop     | Type       | Description         |
| -------- | ---------- | ------------------- |
| `photos` | `string[]` | Array of photo URLs |

- Shows up to 5 photos in a CSS grid (1 large + 4 small).
- "Show all photos" button opens `ListingLightbox`.

### `ListingLightbox`

Full-screen image modal viewer.

| Prop           | Type       | Description                 |
| -------------- | ---------- | --------------------------- |
| `photos`       | `string[]` | Array of photo URLs         |
| `initialIndex` | `number`   | Which photo to show first   |
| `modelValue`   | `boolean`  | Open/closed state (v-model) |

**Emits:** `update:modelValue`

- Previous/next navigation buttons and keyboard arrow key support.
- Click-outside or Escape closes the modal.

### `FeaturedSection`

Category-based listing carousel on `HomeView`.

| Prop       | Type     | Description                        |
| ---------- | -------- | ---------------------------------- |
| `category` | `string` | Category name to fetch and display |
| `title`    | `string` | Section heading                    |

- Fetches listings filtered by `category` using `useListingsStore`.
- Renders a horizontal scrollable row of `ListingCard` components.

---

## Search & Filtering

### `SearchBar`

Location + date range + guest count inputs. Used in compact form on `HomeView` and expanded on `SearchView`.

| Prop      | Type      | Description                            |
| --------- | --------- | -------------------------------------- |
| `compact` | `boolean` | Renders smaller variant for `HomeView` |

**Emits:** `search` with the assembled `ListingSearchParams`

- Internally uses `DateRangePicker` for the date inputs.
- Guest count is an inline increment/decrement control.

### `SearchFilters`

Sidebar/panel of additional filters used in `SearchView`.

**Emits:** `update:filters` with the filter state object

- Controls: property type (Apartment/House), minimum bedrooms (1–6+), minimum price, maximum price.
- Changes emit immediately for reactive search.

### `CategoryFilter`

Horizontal chip strip of listing categories.

| Prop         | Type             | Description                 |
| ------------ | ---------------- | --------------------------- |
| `modelValue` | `string \| null` | Currently selected category |

**Emits:** `update:modelValue`

- Displays all categories from `useCategories` composable with their Lucide icons.
- Clicking the active category deselects it (sets to `null`).

### `DateRangePicker`

Custom calendar component used in `SearchBar` and `ListingView`'s booking panel.

| Prop            | Type             | Description                                  |
| --------------- | ---------------- | -------------------------------------------- |
| `checkIn`       | `string \| null` | Selected check-in date (`YYYY-MM-DD`)        |
| `checkOut`      | `string \| null` | Selected check-out date (`YYYY-MM-DD`)       |
| `blockedRanges` | `ListingBlock[]` | Date ranges to disable                       |
| `minDate`       | `string`         | Earliest selectable date (defaults to today) |

**Emits:** `update:checkIn`, `update:checkOut`

- Two-month calendar view with navigation.
- Blocked and past dates are visually disabled and not selectable.
- Click sequence: first click sets check-in, second click sets check-out (must be after check-in).

---

## Booking & Messaging

### `BookingCard`

Displays a single booking with its status and a review submission form for completed bookings.

| Prop      | Type      | Description            |
| --------- | --------- | ---------------------- |
| `booking` | `Booking` | The booking to display |

**Emits:** `cancelled`, `rated`

- Shows listing title, dates, guests, total price, and status badge.
- For `Completed` bookings without a review: shows a star rating widget and comment input.
- Cancel button shown for non-`Completed` bookings.

### `Bookings`

Container component that renders a list of `BookingCard` components.

| Prop        | Type             | Description             |
| ----------- | ---------------- | ----------------------- |
| `bookings`  | `Booking[]`      | The bookings to display |
| `isLoading` | `boolean`        | Show skeleton           |
| `error`     | `string \| null` | Show error state        |

**Emits:** `cancelled`, `rated` (forwarded from individual cards)

### `ConversationList`

Left panel of the messaging inbox.

| Prop            | Type                    | Description                        |
| --------------- | ----------------------- | ---------------------------------- |
| `conversations` | `ConversationSummary[]` | Inbox entries                      |
| `activeId`      | `string \| null`        | Currently selected conversation ID |
| `isLoading`     | `boolean`               |                                    |
| `error`         | `string \| null`        |                                    |

**Emits:** `select(id)`, `retry`

- Renders three states: loading skeleton, empty state, list.
- Unread conversations show an avatar dot and bold unread count badge.
- Timestamps: time for today, weekday for this week, date otherwise.

### `ConversationView`

Right panel — the chat interface for a single conversation.

| Prop             | Type     | Description                       |
| ---------------- | -------- | --------------------------------- |
| `conversationId` | `string` | ID of the conversation to display |

**Emits:** `back` (for mobile — returns to `ConversationList`)

- Fetches conversation on mount and scrolls to the bottom.
- Watches `messages.length` to auto-scroll as new messages arrive via SSE.
- Send on Enter (Shift+Enter inserts a newline).
- Textarea auto-resizes up to 128 px.
- If sending fails, the typed message is restored.
- Date separator pills are shown when the date changes between consecutive messages.

---

## Hero

### `HeroSection`

Landing page hero banner with headline and a `SearchBar` in compact mode.
