# Testing

Placey has automated test suites for both the backend service layer and the frontend Pinia stores, plus a manual testing checklist for UI scenarios.

---

## Backend tests

Unit tests cover the service layer using an in-memory EF Core database. No external dependencies (database, running server) are needed.

**Full documentation:** [backend/testing.md](./backend/testing.md)

### Quick summary

| Service           | Key scenarios                                                                                                                                                                                                                                                                                                                                                                                                                          |
| ----------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `AuthService`     | Register (happy path, duplicate email), login (valid, unknown email, wrong password), refresh (valid, invalid, revoked), logout                                                                                                                                                                                                                                                                                                        |
| `BookingService`  | Create (instant-book → Confirmed, non-instant → Pending, host self-booking, invalid dates, date conflict, blocked dates, cancelled-then-rebook, listing not found), get by ID (guest, host, unauthorized, not found), cancel (guest, host, unauthorized, completed, not found), update status (host, non-host, not found)                                                                                                              |
| `ListingService`  | Create, get by ID (found, not found, deleted), update (host, non-host, not found), delete (host, non-host, cascades active bookings), search (no filter, location, guest count, category, property type, min bedrooms, price range, date range, pagination), add/delete blocking (host, non-host, not found), get blockings (ordering, includes confirmed bookings, excludes cancelled), get/delete photos (host, non-host, not found) |
| `ReviewService`   | Create (valid completed booking, wrong guest, booking not completed, duplicate, booking not found), delete (author, non-author), get by listing, get by host user                                                                                                                                                                                                                                                                      |
| `FavoriteService` | Add (valid, listing not found, idempotent), remove (existing, idempotent), get favorites (empty)                                                                                                                                                                                                                                                                                                                                       |
| `UserService`     | Get me (found, not found), update me (fields updated, null fields preserved), delete me, get public profile                                                                                                                                                                                                                                                                                                                            |
| `MessageService`  | Get/create conversation (new pair, existing pair, reversed order, self, unknown user), list conversations (empty, after create, unread count), send message (valid, conversation not found, non-member), get conversation (as participant, non-member, not found, marks as read), unread count                                                                                                                                         |

### Running backend tests

```bash
# Run from the repo root
dotnet test placey-be.Tests/

# Or run the full solution
dotnet test placey.sln
```

---

## Frontend tests

Frontend unit tests use [Vitest](https://vitest.dev/) and [`@vue/test-utils`](https://test-utils.vuejs.org/). All five Pinia stores are tested with mocked API calls.

**Full documentation:** [frontend/testing.md](./frontend/testing.md)

### Quick summary

| Store               | Key scenarios                                                                                                                                                                                                                                                                                                                 |
| ------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `useAuthStore`      | Initial state (empty, restored from localStorage, corrupt data), login (success, failure, loading flag, persistence), register (success, duplicate email), logout (success, network error), patchUser, refreshSession (success, no token, deduplication), authHeaders, apiFetch (401 retry, clears session on failed refresh) |
| `useFavoritesStore` | isFavorited, hydrate (success, failure), toggleFavorite (add optimistic, remove optimistic, rollback add on error, rollback remove on error), reset                                                                                                                                                                           |
| `useListingsStore`  | fetchListings (success, HTTP error, query string for location/guests, optional filters, omits absent params, ignores AbortError, loading flag)                                                                                                                                                                                |
| `useBookingStore`   | fetchMyBookings (success, error, non-array response), createBooking (success, conflict error, loading flag), cancelBooking (removes from list, clears lastBooking, error, loading flag), rateBooking (refreshes list, error, loading flag)                                                                                    |
| `useMessagingStore` | fetchConversations (success, error), fetchConversation (sets active, resets before load, error), getOrCreateConversation (success, error), sendMessage (success + appends, wrong conversation, error, loading flag), fetchUnreadCount (success, silent error), resetActive                                                    |

### Running frontend tests

```bash
cd placey-fe

# Run once
pnpm test

# Watch mode
pnpm test:watch

# With coverage report
pnpm test:coverage
```

---

## Manual testing

The areas below are not covered by automated tests and should be verified manually against the running application. Use the seed accounts from the [Example seed data](../README.md#example-seed-data) section to get started quickly.

| Area                   | What to verify                                                                                                                                                                             |
| ---------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **Auth flow**          | Register a new account → redirected to home, logged in. Login with wrong password → error shown. Logout → session cleared, protected pages redirect to login.                              |
| **Search & filters**   | Search by location, adjust guest count, category, property type, price range, min bedrooms, date range. Results update correctly; no results state shown when nothing matches.             |
| **Listing detail**     | Photos display and lightbox opens. Reviews and average rating shown. Book button hidden for own listing. Unavailable dates blocked in date picker.                                         |
| **Booking flow**       | Create a booking on an instant-book listing → status Confirmed, confirmation message appears in messages. Create on a non-instant listing → status Pending.                                |
| **Guest dashboard**    | Booked listings appear. Cancel a booking → removed from list. Submit a review on a completed booking → review appears on the listing.                                                      |
| **Host dashboard**     | Created listings appear. Approve/complete a pending booking. Block dates on a listing → dates unavailable for guests.                                                                      |
| **Listing management** | Create a listing with photos, edit fields, delete listing → listing removed and active bookings cancelled. Photo upload and reorder.                                                       |
| **Messaging**          | Send a message from guest to host → appears in conversation. Open a second browser tab as the recipient → message arrives in real time via SSE without page refresh. Unread badge updates. |
| **Favorites**          | Heart a listing → appears in favorites page. Un-heart → removed. Optimistic toggle is instant; error state reverts the icon.                                                               |
| **Profile & settings** | Update name and bio → changes reflected in navbar and public profile.                                                                                                                      |
