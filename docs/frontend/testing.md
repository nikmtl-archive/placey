# Frontend - Testing

---

## Setup

| Tool                | Version | Role                                    |
| ------------------- | ------- | --------------------------------------- |
| Vitest              | 4.1.9   | Test runner                             |
| @vue/test-utils     | 2.4.11  | Vue component mounting utilities        |
| @pinia/testing      | 1.0.3   | Pinia test helpers (createTestingPinia) |
| jsdom               | 29.1.1  | Browser environment simulation          |
| @vitest/coverage-v8 | 4.1.9   | Coverage reports                        |

Test files live in [placey-fe/src/\_\_tests\_\_/](../../placey-fe/src/__tests__/).

`vitest.config.ts` sets:
- `environment: 'jsdom'`
- `globals: true` (no explicit imports for `describe`/`it`/`expect`)
- Setup file: `src/__tests__/setup.ts`
- Path alias: `@` → `./src`

**setup.ts** runs before each test file: initialises Pinia with `setActivePinia(createPinia())` and clears `localStorage`.

---

## Running tests

```bash
cd placey-fe

# Run once
pnpm test

# Watch mode (re-runs on save)
pnpm test:watch

# With coverage report (output in coverage/)
pnpm test:coverage
```

---

## Testing patterns

- `vi.stubGlobal('fetch', vi.fn())` — mocks the global fetch for all tests in a file.
- `vi.spyOn(store, 'method')` — spies on store methods to verify calls or replace implementations.
- `createTestingPinia()` from `@pinia/testing` provides isolated Pinia instances per test.
- `localStorage` is cleared in `setup.ts` before each test suite.
- Async flows tested with `await store.action()` + `expect(store.state).toBe(...)`.

---

## Test suites

### `useAuthStore.test.ts`

| Scenario group     | Scenarios                                                                        |
| ------------------ | -------------------------------------------------------------------------------- |
| Initial state      | Empty state, session restored from localStorage, corrupt localStorage data       |
| `login()`          | Success (stores tokens + user), failure (sets error), loading flag               |
| `register()`       | Success, duplicate email error                                                   |
| `logout()`         | Success (clears session), network error (still clears local state)               |
| `patchUser()`      | Merges fields into user object                                                   |
| `refreshSession()` | Success, no refresh token, deduplication (two concurrent calls send one request) |
| `authHeaders()`    | Returns Bearer header when logged in, empty object when logged out               |
| `apiFetch()`       | 401 triggers refresh + retry, clears session if refresh fails                    |

---

### `useFavoritesStore.test.ts`

| Scenario group     | Scenarios                                                                                                                                        |
| ------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------ |
| `isFavorited()`    | Returns true/false based on Set membership                                                                                                       |
| `hydrate()`        | Populates favoriteIds from API, handles API failure                                                                                              |
| `toggleFavorite()` | Add: optimistic update + confirmed by API; Remove: optimistic update + confirmed by API; Rollback add on API error; Rollback remove on API error |
| `reset()`          | Clears all state                                                                                                                                 |

---

### `useListingsStore.test.ts`

| Scenario group    | Scenarios                                                                                                                                                                                                  |
| ----------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `fetchListings()` | Success (populates listings + total), HTTP error (sets error), correct query string for location + guests, optional filter params included/excluded, AbortError silently ignored, loading flag set/cleared |

---

### `useBookingStore.test.ts`

| Scenario group      | Scenarios                                                                |
| ------------------- | ------------------------------------------------------------------------ |
| `fetchMyBookings()` | Success, error, non-array response handled                               |
| `createBooking()`   | Success (sets lastBooking), conflict error (409), loading flag           |
| `cancelBooking()`   | Removes booking from list, clears lastBooking, error state, loading flag |
| `rateBooking()`     | Refreshes bookings list after review, error state, loading flag          |

---

### `useMessagingStore.test.ts`

| Scenario group              | Scenarios                                                                                                          |
| --------------------------- | ------------------------------------------------------------------------------------------------------------------ |
| `fetchConversations()`      | Success, error                                                                                                     |
| `fetchConversation()`       | Sets activeConversation, resets before load, error                                                                 |
| `getOrCreateConversation()` | Success (returns conversation ID), error                                                                           |
| `sendMessage()`             | Success and appends message to activeConversation; wrong conversation active (doesn't append); error; loading flag |
| `fetchUnreadCount()`        | Success, silent error (doesn't disrupt UI)                                                                         |
| `resetActive()`             | Clears activeConversation                                                                                          |

---

## Coverage

Run `pnpm test:coverage` to generate a coverage report in `placey-fe/coverage/`. The report covers all store actions, computed properties, and error paths across all five test suites.
