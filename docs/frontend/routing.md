# Frontend - Routing

Routes are defined in [placey-fe/src/router/index.ts](../../placey-fe/src/router/index.ts).

---

## Route table

| Path                | Name             | Auth required | Component           | Notes                                    |
| ------------------- | ---------------- | :-----------: | ------------------- | ---------------------------------------- |
| `/`                 | `home`           |               | `HomeView`          | Featured listings by category            |
| `/search`           | `search`         |               | `SearchView`        | Full-text + filter search                |
| `/listing/:id`      | `listing`        |               | `ListingView`       | Detail, booking panel, reviews           |
| `/listing/new`      | `listing-new`    |       ✅       | `ListingFormView`   | Create listing                           |
| `/listing/:id/edit` | `listing-edit`   |       ✅       | `ListingFormView`   | Edit existing listing                    |
| `/favorites`        | `favorites`      |       ✅       | `FavoritesView`     | Saved listings                           |
| `/bookings`         | `bookings`       |       ✅       | `BookingsView`      | Guest bookings dashboard                 |
| `/host-dashboard`   | `host-dashboard` |       ✅       | `HostDashboardView` | Host reservations + listing management   |
| `/host/:id`         | `host-profile`   |               | `HostProfileView`   | Public host profile                      |
| `/become-a-host`    | `become-a-host`  |               | `BecomeAHostView`   | Host onboarding landing                  |
| `/login`            | `login`          |               | `AuthView`          | Login form                               |
| `/register`         | `register`       |               | `AuthView`          | Register form                            |
| `/messages/:id?`    | `messages`       |       ✅       | `MessagesView`      | Inbox; `:id` selects active conversation |
| `/settings`         | `settings`       |       ✅       | `SettingsView`      | Account settings / profile               |
| `/about`            | `about`          |               | `PlaceholderView`   | Out-of-scope info page                   |
| `/privacy`          | `privacy`        |               | `PlaceholderView`   | Out-of-scope info page                   |
| `/terms`            | `terms`          |               | `PlaceholderView`   | Out-of-scope info page                   |
| `/:pathMatch(.*)*`  | —                |               | `PlaceholderView`   | 404 catch-all                            |

All routes use lazy imports (`() => import('@/views/...')`) for automatic code-splitting.

---

## Auth guard

`router.beforeEach` inspects `route.meta.requiresAuth`:

1. If the route requires auth and the user is not logged in, the user is redirected to `/login?redirect=<original-path>`.
2. After a successful login or register, `AuthView` reads the `redirect` query param and navigates back to the original destination.
3. If no `redirect` param is present after login, the user is sent to `/`.

---

## Scroll behaviour

All navigations reset scroll to the top:

```ts
scrollBehavior: () => ({ top: 0 })
```

---

## Footer visibility

`App.vue` watches the current route and hides `SiteFooter` on any route whose path starts with `/messages`, because the messaging view uses the full viewport height.
