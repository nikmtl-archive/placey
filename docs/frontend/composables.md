# Frontend - Composables

Composables live in [placey-fe/src/composables/](../../placey-fe/src/composables/). They are thin, reusable Composition API utilities.

---

## `useClickOutside`

**File:** `composables/useClickOutside.ts`

Detects clicks outside a given element. Used for dropdown menus and modals.

```ts
useClickOutside(target: Ref<HTMLElement | null>, handler: () => void): void
```

- Attaches a `mousedown` listener to `document` on mount.
- If the click target is not contained within `target.value`, `handler` is called.
- Listener is removed on component unmount.

**Used by:** `NavBar` (user dropdown), `SearchBar` (guest picker dropdown), `SearchFilters`.

---

## `useAmenityIcons`

**File:** `composables/useAmenityIcons.ts`

Maps amenity name strings to their corresponding Lucide Vue icon components.

```ts
const { getIcon } = useAmenityIcons()
// getIcon('WiFi') → WifiIcon component
// getIcon('unknown') → SparklesIcon (default)
```

Supported amenities: WiFi, Parking, Kitchen, Pool, Gym, Air conditioning, Heating, Washer, Dryer, TV, Balcony, Garden, BBQ, Fireplace, Workspace, Pet friendly.

Falls back to `SparklesIcon` for any amenity string not in the map.

**Used by:** `ListingView` (amenities list).

---

## `useCategories`

**File:** `composables/useCategories.ts`

Exports the canonical `CATEGORIES` array used across the app for category chips, featured sections, and search filtering.

```ts
interface Category {
  id: ListingCategory
  label: string
  icon: Component  // Lucide icon component
}
```

| id            | label       | Icon        |
| ------------- | ----------- | ----------- |
| `Beach`       | Beach       | `Waves`     |
| `Mountains`   | Mountains   | `Mountain`  |
| `Urban`       | Urban       | `Building2` |
| `Luxe`        | Luxe        | `Gem`       |
| `Countryside` | Countryside | `Tractor`   |
| `Island`      | Island      | `Palmtree`  |
| `Lake`        | Lake        | `Droplets`  |

**Used by:** `CategoryFilter`, `FeaturedSection`, `ListingFormView`.
