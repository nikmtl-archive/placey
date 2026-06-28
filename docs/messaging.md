# Messaging

This document covers the design and implementation of the real-time messaging feature added in the `feat/messaging` branch.

---

## Overview

Users can send direct messages to one another. The primary entry point is the **"Message host"** button on a listing page, which opens (or creates) a conversation with the listing's host and navigates to the messages inbox. Conversations are always 1-to-1.

For general frontend architecture (routing, Pinia patterns, API proxy setup) see [docs/frontend.md](./frontend.md).  
For the REST API shapes see [docs/api-endpoints.md](./api-endpoints.md#messages).

---

## Data model

### `Conversation`

| Column      | Type        | Notes                |
| ----------- | ----------- | -------------------- |
| `Id`        | `uuid`      | PK, auto-generated   |
| `User1Id`   | `uuid`      | FK → `Users`         |
| `User2Id`   | `uuid`      | FK → `Users`         |
| `CreatedAt` | `timestamp` | UTC, set on creation |

A unique index on `(User1Id, User2Id)` enforces that at most one conversation exists between any two users. Participants are stored with the lower GUID as `User1` so the constraint is order-independent.

### `Message`

| Column           | Type            | Notes                                                   |
| ---------------- | --------------- | ------------------------------------------------------- |
| `Id`             | `uuid`          | PK, auto-generated                                      |
| `ConversationId` | `uuid`          | FK → `Conversations` (cascade delete)                   |
| `SenderId`       | `uuid`          | FK → `Users`                                            |
| `Body`           | `varchar(4000)` | Message text                                            |
| `IsRead`         | `bool`          | Set to `true` when the recipient opens the conversation |
| `CreatedAt`      | `timestamp`     | UTC, set on creation                                    |

### User additions

`User` now has three navigation properties wired up in `AppDbContext`:

- `ConversationsAsUser1` / `ConversationsAsUser2` — the two sides of the conversation relationship
- `SentMessages` — messages sent by the user

---

## Backend

### Service layer — `MessageService`

`IMessageService` / `MessageService` handle all business logic:

| Method                         | What it does                                                                                                                                   |
| ------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------- |
| `GetOrCreateConversationAsync` | Validates the other user exists, normalises participant order, creates the conversation row if absent, returns a `ConversationSummaryResponse` |
| `GetConversationsAsync`        | Returns all conversations for a user ordered by most recent activity; computes unread count per conversation in a single query                 |
| `GetConversationAsync`         | Returns a conversation with its full message list; marks all unread messages (sent by the other user) as read atomically                       |
| `SendMessageAsync`             | Validates the caller is a participant, persists the message, then calls `SseHub.Push` to deliver it in real time to the recipient              |
| `GetUnreadCountAsync`          | Returns the total number of unread messages across all conversations                                                                           |

### Real-time delivery — `SseHub`

`SseHub` is registered as a **singleton** and keeps an in-memory `ConcurrentDictionary<Guid, Channel<MessageResponse>>` keyed by user ID.

```
SseHub
  .Subscribe(userId)  → returns ChannelReader (called when client opens SSE connection)
  .Unsubscribe(userId) → removes the channel (called when the HTTP connection closes)
  .Push(recipientId, msg) → writes to the recipient's channel if they are connected
```

`MessageService.SendMessageAsync` calls `SseHub.Push` after persisting the message, so the recipient receives the message without polling.

### Controller — `MessagesController`

Route prefix: `messages`. All actions require `[Authorize]`.

The SSE endpoint (`GET /messages/stream`) holds the HTTP connection open and writes `data: <json>\n\n` lines as messages arrive. Because `EventSource` cannot send custom headers, JWT auth for this endpoint is handled via the `?access_token=` query parameter — the JWT bearer options in `ServiceExtensions.AddJwtAuth` read the token from the query string when the path starts with `/messages/stream`.

---

## Frontend

### Route

```
/messages/:id?   →  MessagesView  (requiresAuth: true)
```

The `:id` param is optional — omitting it shows the inbox with no conversation selected.

### Pinia store — `useMessagingStore`

Located at [placey-fe/src/stores/useMessagingStore.ts](../placey-fe/src/stores/useMessagingStore.ts).

**State**

| Ref                  | Type                         | Purpose                                        |
| -------------------- | ---------------------------- | ---------------------------------------------- |
| `conversations`      | `ConversationSummary[]`      | Inbox list                                     |
| `activeConversation` | `ConversationDetail \| null` | Currently open conversation                    |
| `unreadCount`        | `number`                     | Badge shown in the nav                         |
| `isLoading`          | `bool`                       | Conversation/message load spinner              |
| `isSending`          | `bool`                       | Disables the send button during in-flight POST |
| `error`              | `string \| null`             | Error message for UI                           |

**Key actions**

| Action                                 | Description                                                                                                                  |
| -------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------- |
| `fetchConversations()`                 | Loads the inbox                                                                                                              |
| `fetchConversation(id)`                | Loads a single conversation and refreshes `unreadCount`                                                                      |
| `getOrCreateConversation(otherUserId)` | POSTs to create/retrieve a conversation; returns the conversation ID for navigation                                          |
| `sendMessage(conversationId, body)`    | Sends a message and appends it to `activeConversation.messages` optimistically                                               |
| `fetchUnreadCount()`                   | Refreshes the nav badge                                                                                                      |
| `connectStream()`                      | Opens the SSE connection; on new message, appends to the active conversation or increments unread counters on the list entry |
| `disconnectStream()`                   | Closes the SSE connection (called on logout)                                                                                 |

**SSE reconnect**: on connection error the store waits 5 seconds then calls `connectStream()` again.

### Components

#### `MessagesView`

Two-panel layout: conversation list on the left, open conversation on the right. On mobile, only one panel is shown at a time — selecting a conversation hides the list (and the back button in `ConversationView` returns to it).

#### `ConversationList`

Receives the `conversations` array as a prop and emits `select` / `retry`. Renders three states: loading skeleton, empty state, and the list. Unread conversations show a dot on the avatar and a bold count badge. Timestamps are formatted relative to now (time today, weekday this week, date otherwise).

#### `ConversationView`

Receives `conversationId` as a prop. On mount it fetches the conversation and scrolls to the bottom. A watcher on `messages.length` keeps the view scrolled to the latest message as new ones arrive via SSE.

- Send on `Enter` (Shift+Enter inserts a newline).
- The textarea auto-resizes up to 128 px.
- If sending fails the typed message is restored.
- Date separator pills are shown when the date changes between consecutive messages.

### TypeScript types

Added to [placey-fe/src/types/index.ts](../placey-fe/src/types/index.ts):

```ts
interface ConversationSummary { id, otherUserId, otherUserName, otherUserAvatarUrl, lastMessageBody, lastMessageAt, unreadCount, createdAt }
interface ChatMessage         { id, conversationId, senderId, senderName, senderAvatarUrl, body, isRead, createdAt }
interface ConversationDetail  { id, otherUserId, otherUserName, otherUserAvatarUrl, messages: ChatMessage[] }
```

### NavBar unread badge

On mount, `NavBar` calls `messagingStore.fetchUnreadCount()` when the user is logged in. The count is displayed next to the "Messages" link in the dropdown menu.

### "Message host" in ListingView

A new `messageHost()` function in `ListingView` calls `messagingStore.getOrCreateConversation(listing.hostId)` and navigates to `/messages/:id`. Unauthenticated users are redirected to `/login` with a `redirect` query param so they return to the listing after logging in.

---

## Database migration

Migration: `20260608131335_AddMessaging`

Adds the `Conversations` and `Messages` tables with the constraints described in the data model section above. The previous migration (`20260604165507_AddListingDetails`) was replaced by this one.

---

## Limitations / future work

- SSE connections are in-process only — this design does not scale horizontally across multiple API instances without a shared pub/sub backend (e.g. Redis).
- Messages are fetched in full on every conversation open; there is no pagination for long histories.
- No typing indicators.
- No message deletion or editing.
