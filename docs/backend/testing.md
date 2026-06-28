# Backend - Testing

---

## Setup

| Tool                                   | Version | Role                                  |
| -------------------------------------- | ------- | ------------------------------------- |
| xUnit                                  | 2.5.3   | Test framework                        |
| xunit.runner.visualstudio              | 2.5.3   | Test discovery in VS/Rider            |
| Microsoft.EntityFrameworkCore.InMemory | 8.0.15  | In-memory DB for service tests        |
| BCrypt.Net-Next                        | 4.0.3   | Password hashing (same as production) |
| coverlet.collector                     | 6.0.0   | Code coverage collection              |

Test project: `placey-be.Tests` (targets `net8.0`).  
Test files: [placey-be.Tests/Services/](../../placey-be.Tests/Services/)  
Helpers: [placey-be.Tests/Helpers/](../../placey-be.Tests/Helpers/)

---

## Running tests

```bash
# Run from the repo root
dotnet test placey-be.Tests/

# Or run the full solution (includes backend tests)
dotnet test placey.sln
```

No external dependencies (database, running server) are needed.

---

## Testing patterns

- Each test class creates a fresh `AppDbContext` backed by EF Core's `InMemoryDatabase` provider via `DbHelper.CreateContext()`.
- Tests seed their own data within each test method — no shared state between tests.
- xUnit `[Fact]` for single scenarios, `[Theory]` with `[InlineData]` for parameterised inputs.
- `async Task` test methods with `await` for all service calls.
- `Assert.ThrowsAsync<T>` for expected exceptions (validates exception type and message where applicable).
- `NullMessageService` is a no-op `IMessageService` stub used in `BookingServiceTests` to satisfy the constructor dependency without triggering SSE side effects.

---

## Helper classes

### `DbHelper`

`Helpers/DbHelper.cs` — creates an isolated in-memory `AppDbContext` with a unique database name per call (using `Guid.NewGuid()`) so tests never share state.

### `NullMessageService`

`Helpers/NullMessageService.cs` — a no-op implementation of `IMessageService` used to satisfy `BookingService`'s dependency during tests.

---

## Test suites

### `AuthServiceTests`

**File:** `Services/AuthServiceTests.cs`

| Scenario                   | What is verified                         |
| -------------------------- | ---------------------------------------- |
| Register — happy path      | User created, tokens returned            |
| Register — duplicate email | `InvalidOperationException` thrown       |
| Login — valid credentials  | Tokens returned, user data correct       |
| Login — unknown email      | `UnauthorizedAccessException` thrown     |
| Login — wrong password     | `UnauthorizedAccessException` thrown     |
| Login — deleted user       | `UnauthorizedAccessException` thrown     |
| Refresh — valid token      | New token pair issued, old token revoked |
| Refresh — invalid token    | `UnauthorizedAccessException` thrown     |
| Refresh — revoked token    | `UnauthorizedAccessException` thrown     |
| Logout — valid token       | Token marked as revoked                  |
| Logout — unknown token     | Does not throw (idempotent)              |

---

### `BookingServiceTests`

**File:** `Services/BookingServiceTests.cs`

| Scenario                                     | What is verified              |
| -------------------------------------------- | ----------------------------- |
| Create — instant-book listing                | Status starts as Confirmed    |
| Create — non-instant listing                 | Status starts as Pending      |
| Create — host self-booking                   | `InvalidOperationException`   |
| Create — invalid dates (checkOut ≤ checkIn)  | `InvalidOperationException`   |
| Create — equal dates                         | `InvalidOperationException`   |
| Create — date conflict with existing booking | `InvalidOperationException`   |
| Create — date conflict with listing block    | `InvalidOperationException`   |
| Create — cancelled booking allows rebook     | Succeeds                      |
| Create — listing not found                   | `KeyNotFoundException`        |
| Get by ID — as guest                         | Returns booking               |
| Get by ID — as host                          | Returns booking               |
| Get by ID — unauthorized                     | `UnauthorizedAccessException` |
| Get by ID — not found                        | `KeyNotFoundException`        |
| Cancel — as guest                            | Status = Cancelled            |
| Cancel — as host                             | Status = Cancelled            |
| Cancel — unauthorized                        | `UnauthorizedAccessException` |
| Cancel — completed booking                   | `InvalidOperationException`   |
| Cancel — not found                           | `KeyNotFoundException`        |
| Update status — host approves                | Status = Confirmed            |
| Update status — non-host                     | `UnauthorizedAccessException` |
| Update status — not found                    | `KeyNotFoundException`        |

---

### `ListingServiceTests`

**File:** `Services/ListingServiceTests.cs`

| Scenario group         | Scenarios                                                                                          |
| ---------------------- | -------------------------------------------------------------------------------------------------- |
| Create                 | Happy path                                                                                         |
| Get by ID              | Found, not found, deleted (410)                                                                    |
| Update                 | Host updates, non-host throws, not found throws                                                    |
| Delete                 | Host deletes (cascades active bookings), non-host throws                                           |
| Search — no filter     | Returns all listings                                                                               |
| Search — location      | ILIKE match on city/country                                                                        |
| Search — guest count   | Filters by maxGuests                                                                               |
| Search — category      | Exact match                                                                                        |
| Search — property type | Exact match                                                                                        |
| Search — min bedrooms  | Range filter                                                                                       |
| Search — price range   | min/maxPrice filter                                                                                |
| Search — date range    | Excludes conflicting confirmed bookings                                                            |
| Search — pagination    | page/limit slicing                                                                                 |
| Add blocking           | Host adds block, non-host throws, not found throws                                                 |
| Delete blocking        | Host removes, non-host throws, not found throws                                                    |
| Get blockings          | Ordered by StartDate, includes confirmed bookings as effective blocks, excludes cancelled bookings |
| Get photos             | Returns ordered photos (host only)                                                                 |
| Delete photo           | Host deletes, non-host throws                                                                      |

---

### `ReviewServiceTests`

**File:** `Services/ReviewServiceTests.cs`

| Scenario                         | What is verified                    |
| -------------------------------- | ----------------------------------- |
| Create — valid completed booking | Review created                      |
| Create — wrong guest             | `UnauthorizedAccessException`       |
| Create — booking not completed   | `InvalidOperationException`         |
| Create — duplicate review        | `InvalidOperationException`         |
| Create — booking not found       | `KeyNotFoundException`              |
| Delete — author                  | Review deleted                      |
| Delete — non-author              | `UnauthorizedAccessException`       |
| Get by listing                   | Returns reviews for listing         |
| Get by host user — matching      | Returns reviews for host's listings |
| Get by host user — other user    | Returns empty list                  |

---

### `FavoriteServiceTests`

**File:** `Services/FavoriteServiceTests.cs`

| Scenario                             | What is verified          |
| ------------------------------------ | ------------------------- |
| Add — valid listing                  | Favorite created          |
| Add — listing not found              | `KeyNotFoundException`    |
| Add — idempotent (already favorited) | Does not create duplicate |
| Remove — existing                    | Favorite deleted          |
| Remove — idempotent (not favorited)  | Does not throw            |
| Get favorites — empty                | Returns empty list        |

---

### `UserServiceTests`

**File:** `Services/UserServiceTests.cs`

| Scenario                          | What is verified                |
| --------------------------------- | ------------------------------- |
| Get me — found                    | Returns private profile         |
| Get me — not found                | `KeyNotFoundException`          |
| Update me — fields updated        | Changes persisted               |
| Update me — null fields preserved | Existing values not overwritten |
| Delete me                         | IsDeleted set, PII cleared      |
| Get public — found                | Returns public profile          |
| Get public — not found            | `KeyNotFoundException`          |

---

### `MessageServiceTests`

**File:** `Services/MessageServiceTests.cs`

| Scenario group          | Scenarios                                                                                      |
| ----------------------- | ---------------------------------------------------------------------------------------------- |
| GetOrCreateConversation | New pair, existing pair, reversed order, self-conversation throws, unknown user throws         |
| GetConversations        | Empty list, populated list with unread count and last message                                  |
| SendMessage             | Valid send, conversation not found, non-member throws                                          |
| GetConversation         | As participant with full messages, non-member throws, not found throws, marks messages as read |
| GetUnreadCount          | Zero, after new message, after opening conversation                                            |

---

## Total coverage

**66 unit tests** across 7 service test classes. All tests run in-memory with no external dependencies.
