# Backend - Services & Business Logic

All business logic lives in the service layer. Services are registered as scoped dependencies and injected into controllers via their interfaces (`IAuthService`, `IListingService`, etc.).

---

## `AuthService`

**Interface:** `IAuthService`  
**File:** [placey-be/Services/AuthService.cs](../../placey-be/Services/AuthService.cs)

| Method                                 | Description                                                                              |
| -------------------------------------- | ---------------------------------------------------------------------------------------- |
| `RegisterAsync(email, password, name)` | Validates email uniqueness, hashes password with BCrypt, creates User, issues token pair |
| `LoginAsync(email, password)`          | Looks up user by email, verifies BCrypt hash, checks IsDeleted, issues token pair        |
| `RefreshAsync(refreshToken)`           | Validates token in DB (not expired, not revoked), rotates it, issues new pair            |
| `LogoutAsync(refreshToken)`            | Marks the refresh token as revoked                                                       |

**Business rules:**
- Duplicate email → `InvalidOperationException` (→ 400)
- Wrong password or unknown email → `UnauthorizedAccessException` (→ 403, deliberately vague to prevent user enumeration)
- Soft-deleted user → `UnauthorizedAccessException`
- Expired or revoked refresh token → `UnauthorizedAccessException`

Token pair generation creates a signed JWT (15-minute expiry) and a 64-byte random opaque refresh token stored in the DB.

---

## `ListingService`

**Interface:** `IListingService`  
**File:** [placey-be/Services/ListingService.cs](../../placey-be/Services/ListingService.cs)

| Method                                        | Description                                                                                            |
| --------------------------------------------- | ------------------------------------------------------------------------------------------------------ |
| `SearchAsync(params)`                         | Builds a composable EF Core query from `ListingSearchParams`, applies all active filters, paginates    |
| `GetByIdAsync(id)`                            | Returns listing or throws 404/410 (if IsDeleted)                                                       |
| `CreateAsync(hostId, request)`                | Creates listing with all fields                                                                        |
| `UpdateAsync(id, hostId, request)`            | Owner check, then applies non-null fields                                                              |
| `DeleteAsync(id, hostId)`                     | Owner check, sets IsDeleted, cancels any active bookings                                               |
| `GetBlockingsAsync(id)`                       | Returns date blocks ordered by StartDate, also includes confirmed/pending bookings as effective blocks |
| `AddBlockingAsync(id, hostId, start, end)`    | Owner check, validates date range, creates ListingBlock                                                |
| `DeleteBlockingAsync(id, blockingId, hostId)` | Owner check, deletes blocking                                                                          |
| `GetPhotosAsync(id, requesterId)`             | Owner check, returns ordered photos                                                                    |
| `UploadPhotoAsync(id, hostId, file)`          | Owner check, saves file to wwwroot, creates ListingPhoto record                                        |
| `DeletePhotoAsync(id, photoId, hostId)`       | Owner check, deletes file and DB record                                                                |
| `GetReviewsAsync(id)`                         | Returns all reviews for a listing                                                                      |

**Search filters** (all optional, combined with AND):
- `location` — `ILIKE` match against city and country
- `checkIn`/`checkOut` — excludes listings with conflicting Confirmed/Pending bookings
- `guests` — `maxGuests >= guests`
- `category`, `propertyType` — exact match
- `minBedrooms` — `bedrooms >= minBedrooms`
- `minPrice`/`maxPrice` — `pricePerNight` range
- `page`/`limit` — offset pagination (default page 1, limit 20)

Soft-deleted listings (`IsDeleted = true`) are excluded from all search results and detail lookups (detail returns 410 instead of 404).

---

## `BookingService`

**Interface:** `IBookingService`  
**File:** [placey-be/Services/BookingService.cs](../../placey-be/Services/BookingService.cs)

| Method                                     | Description                                                                                           |
| ------------------------------------------ | ----------------------------------------------------------------------------------------------------- |
| `CreateAsync(guestId, request)`            | Validates dates, checks conflicts, computes total price, creates booking                              |
| `GetByIdAsync(id, requesterId)`            | Access check (guest or host), returns booking                                                         |
| `CancelAsync(id, requesterId)`             | Access check, validates status is not Completed, sets Cancelled                                       |
| `UpdateStatusAsync(id, hostId, newStatus)` | Host-only, validates transition (Pending→Confirmed, Confirmed→Completed, any non-Completed→Cancelled) |

**Business rules for `CreateAsync`:**
- `checkIn` must be before `checkOut` and both must be in the future.
- Guest cannot book their own listing.
- Date range must not overlap any existing Confirmed/Pending booking or ListingBlock.
- `TotalPrice` = nights × `PricePerNight` (computed server-side).
- `IsInstantBook = true` → Status starts as `Confirmed`; otherwise `Pending`.

**Conflict detection:** EF Core query checks for any booking where the existing range overlaps the requested range (standard interval overlap: `existingCheckIn < requestedCheckOut && existingCheckOut > requestedCheckIn`).

---

## `ReviewService`

**Interface:** `IReviewService`  
**File:** [placey-be/Services/ReviewService.cs](../../placey-be/Services/ReviewService.cs)

| Method                           | Description                                                                 |
| -------------------------------- | --------------------------------------------------------------------------- |
| `CreateAsync(authorId, request)` | Validates booking ownership, status, and no existing review; creates Review |
| `DeleteAsync(id, requesterId)`   | Author check, deletes review                                                |
| `GetByListingAsync(listingId)`   | Returns all reviews for a listing, ordered by date                          |
| `GetByUserAsync(hostUserId)`     | Returns all reviews for a user's listings (for public host profile)         |

**Business rules for `CreateAsync`:**
- Booking must exist and must have `Status = Completed`.
- Caller must be the booking's `GuestId`.
- One review per booking (unique index on `BookingId` also enforces at DB level).

---

## `FavoriteService`

**Interface:** `IFavoriteService`  
**File:** [placey-be/Services/FavoriteService.cs](../../placey-be/Services/FavoriteService.cs)

| Method                           | Description                                                              |
| -------------------------------- | ------------------------------------------------------------------------ |
| `GetFavoritesAsync(userId)`      | Returns full listing objects for all favorited listings                  |
| `AddAsync(userId, listingId)`    | Idempotent — does nothing if already favorited; validates listing exists |
| `RemoveAsync(userId, listingId)` | Idempotent — does nothing if not favorited                               |

---

## `UserService`

**Interface:** `IUserService`  
**File:** [placey-be/Services/UserService.cs](../../placey-be/Services/UserService.cs)

| Method                            | Description                                                                                        |
| --------------------------------- | -------------------------------------------------------------------------------------------------- |
| `GetMeAsync(userId)`              | Returns private profile (includes email)                                                           |
| `UpdateMeAsync(userId, request)`  | Updates non-null fields; null fields preserve existing values. Rejects blank name.                 |
| `DeleteMeAsync(userId)`           | Sets IsDeleted, wipes PII fields (email → deleted@..., name cleared, bio cleared, avatar cleared)  |
| `UploadAvatarAsync(userId, file)` | Validates size (≤ 5 MB) and MIME type (JPEG/PNG/WebP), then saves to wwwroot and updates AvatarUrl |
| `GetPublicAsync(userId)`          | Returns public profile (no email)                                                                  |
| `GetListingsAsync(hostId)`        | Returns host's active listings                                                                     |
| `GetReviewsAsync(hostId)`         | Returns reviews for the host's listings                                                            |

**Profile update constraints (enforced by DTO + service):**
- `Name`: optional (omit to keep existing), max 100 characters, must not be blank if provided.
- `Bio`: optional (omit to keep existing), max 500 characters.

---

## `MessageService`

**Interface:** `IMessageService`  
**File:** [placey-be/Services/MessageService.cs](../../placey-be/Services/MessageService.cs)

See [../messaging.md](../messaging.md) for the full messaging feature design.

| Method                                              | Description                                                                                                    |
| --------------------------------------------------- | -------------------------------------------------------------------------------------------------------------- |
| `GetOrCreateConversationAsync(userId, otherUserId)` | Validates other user exists, normalises participant order (lower GUID = User1), creates conversation if absent |
| `GetConversationsAsync(userId)`                     | Returns all conversations ordered by most recent message; computes unread count per conversation               |
| `GetConversationAsync(id, userId)`                  | Returns conversation with full message list; marks all unread messages from the other user as read atomically  |
| `SendMessageAsync(conversationId, senderId, body)`  | Validates sender is participant, persists message, calls `SseHub.Push`                                         |
| `GetUnreadCountAsync(userId)`                       | Total unread messages across all conversations                                                                 |

---

## `SseHub`

**File:** [placey-be/Services/SseHub.cs](../../placey-be/Services/SseHub.cs)  
**Registration:** Singleton

Manages in-memory pub/sub channels for real-time message delivery.

```csharp
Subscribe(userId)       // called when client opens GET /messages/stream
                        // returns ChannelReader<MessageResponse>
Unsubscribe(userId)     // called when the HTTP connection closes
Push(recipientId, msg)  // called by MessageService after persisting a message
```

Internally: `ConcurrentDictionary<Guid, Channel<MessageResponse>>` keyed by user ID.

`SendMessageAsync` in `MessageService` calls `Push` after saving — so the recipient receives the message immediately if they have an active SSE connection.

**Scaling limitation:** Channels are in-process only. Horizontal scaling across multiple API instances requires a shared pub/sub backend (e.g. Redis Pub/Sub).
