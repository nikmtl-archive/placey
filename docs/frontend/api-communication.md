# Frontend - API Communication

---

## Base strategy

The frontend uses the **native Fetch API** (no axios or SDK). All requests target relative paths starting with `/api/`, which are proxied to the backend in both development and production.

---

## Dev proxy (Vite)

`vite.config.ts` configures a proxy:

```
/api/* → http://localhost:5001
```

This means the frontend code always calls `/api/listings`, `/api/auth/login`, etc. — no CORS issues and no environment-specific base URLs in source code.

---

## Production proxy (nginx)

`nginx.conf` forwards `/api/` requests to the backend container, maintaining the same relative-URL convention used in development.

---

## Authenticated requests `apiFetch`

`useAuthStore` exposes `apiFetch(input, init)`, a drop-in replacement for `fetch` that:

1. Reads the current `accessToken` and injects `Authorization: Bearer <token>` into the request headers.
2. If the response is `401 Unauthorized`, calls `refreshSession()` to get a new access token.
3. Retries the original request once with the new token.
4. If the retry also fails (refresh itself returned 401/403), clears the session and redirects to login.

All stores that call protected endpoints use `authStore.apiFetch()`. Public endpoints (listing search, listing detail, reviews) use raw `fetch()` directly.

---

## Concurrent refresh deduplication

`refreshSession()` uses a shared promise internally so that if multiple in-flight requests all receive a 401 at the same moment, only **one** refresh request is sent to the server. All callers await the same promise and then retry with the new token.

---

## Request cancellation (AbortController)

`useListingsStore.fetchListings` creates an `AbortController` on each call and passes its `signal` to `fetch`. If a new search is triggered before the previous one completes, the previous request is aborted. `AbortError` is caught and silently ignored.

---

## Server-Sent Events (SSE)

`EventSource` is used for the real-time messaging stream:

```
EventSource → GET /api/messages/stream?access_token=<jwt>
```

The JWT is passed as a query parameter because `EventSource` cannot send custom headers. The backend JWT middleware accepts this via `OnMessageReceived` when the request path matches `/messages/stream`.

The `useMessagingStore` manages the `EventSource` instance:
- `connectStream()` creates the `EventSource` and wires `onmessage`.
- On receiving a message, the store either appends it to `activeConversation.messages` (if the conversation is open) or increments the `unreadCount` and updates the conversation summary in the inbox list.
- On `onerror`, the store waits 5 seconds then calls `connectStream()` again.
- `disconnectStream()` closes the connection on logout.

---

## Error handling patterns

| Scenario             | What the store does                                                                                      |
| -------------------- | -------------------------------------------------------------------------------------------------------- |
| Non-OK HTTP response | Reads the `ProblemDetails` body and surfaces `detail` (or a generic fallback) in the store's `error` ref |
| Network error        | Catches the thrown `TypeError` and sets a user-friendly message                                          |
| 410 Gone             | `useListingStore` catches this specifically and sets `isDeleted = true`                                  |
| `AbortError`         | Silently ignored (expected when a new search supersedes the previous one)                                |
