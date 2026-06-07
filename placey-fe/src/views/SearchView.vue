<script setup lang="ts">
import { ref, watch, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import type { LocationQuery } from 'vue-router'
import { SlidersHorizontal, X } from '@lucide/vue'
import ListingCard from '@/components/ListingCard.vue'
import SearchBar from '@/components/SearchBar.vue'
import { useListingsStore } from '@/stores'
import { storeToRefs } from 'pinia'
import { useCategories } from '@/composables/useCategories'
import type { ListingSearchParams, ListingCategory, PropertyType } from '@/types'

const route  = useRoute()
const router = useRouter()
const { categories } = useCategories()
const listingsStore = useListingsStore()
const { listings, total, isLoading, error } = storeToRefs(listingsStore)
const { fetchListings } = listingsStore

const LIMIT = 20

function parseQuery(q: LocationQuery): ListingSearchParams {
  return {
    location:     q.location     ? String(q.location)                     : undefined,
    checkIn:      q.checkIn      ? String(q.checkIn)                      : undefined,
    checkOut:     q.checkOut     ? String(q.checkOut)                     : undefined,
    guests:       q.guests       ? Number(q.guests)                       : undefined,
    category:     q.category     ? String(q.category) as ListingCategory  : undefined,
    propertyType: q.propertyType ? String(q.propertyType) as PropertyType : undefined,
    minBedrooms:  q.minBedrooms  ? Number(q.minBedrooms)                  : undefined,
    minPrice:     q.minPrice     ? Number(q.minPrice)                     : undefined,
    maxPrice:     q.maxPrice     ? Number(q.maxPrice)                     : undefined,
    page:         q.page         ? Number(q.page)                         : 1,
    limit:        LIMIT,
  }
}

const currentPage = computed(() => Number(route.query.page ?? 1))
const totalPages  = computed(() => Math.ceil(total.value / LIMIT))

function visiblePages(current: number, total: number): (number | '…')[] {
  if (total <= 7) return Array.from({ length: total }, (_, i) => i + 1)
  const pages: (number | '…')[] = [1]
  if (current > 3) pages.push('…')
  for (let p = Math.max(2, current - 1); p <= Math.min(total - 1, current + 1); p++) pages.push(p)
  if (current < total - 2) pages.push('…')
  pages.push(total)
  return pages
}

function applyFilter(partial: Partial<Record<string, string | number | undefined>>) {
  const next: Record<string, string> = {}
  const merged = { ...route.query, ...partial, page: 1 }
  for (const [k, v] of Object.entries(merged)) {
    // coerce to string when checking emptiness to avoid comparing different primitive types
    if (v !== undefined && v !== null && String(v) !== '') next[k] = String(v)
  }
  router.replace({ query: next })
}

function clearFilters() {
  const keep: Record<string, string> = {}
  for (const k of ['location', 'checkIn', 'checkOut', 'guests']) {
    if (route.query[k]) keep[k] = String(route.query[k])
  }
  router.replace({ query: keep })
}

function goToPage(page: number) {
  router.replace({ query: { ...route.query, page: String(page) } })
}

watch(() => route.query, (q) => fetchListings(parseQuery(q)), { immediate: true })

// Filter state driven by URL
const activeCategory     = computed(() => route.query.category     ? String(route.query.category)     as ListingCategory : undefined)
const activePropertyType = computed(() => route.query.propertyType ? String(route.query.propertyType) as PropertyType    : undefined)
const activeMinBedrooms  = computed(() => route.query.minBedrooms  ? Number(route.query.minBedrooms)                     : undefined)

const minPriceInput = ref(String(route.query.minPrice ?? ''))
const maxPriceInput = ref(String(route.query.maxPrice ?? ''))

function toggleCategory(id: ListingCategory) {
  applyFilter({ category: activeCategory.value === id ? undefined : id })
}
function togglePropertyType(type: PropertyType) {
  applyFilter({ propertyType: activePropertyType.value === type ? undefined : type })
}
function toggleBedrooms(n: number) {
  applyFilter({ minBedrooms: activeMinBedrooms.value === n ? undefined : n })
}
function applyPrice() {
  applyFilter({ minPrice: minPriceInput.value || undefined, maxPrice: maxPriceInput.value || undefined })
}

const hasFilters = computed(() =>
  ['category', 'propertyType', 'minBedrooms', 'minPrice', 'maxPrice'].some((k) => route.query[k]),
)

const activeFilterCount = computed(() =>
  ['category', 'propertyType', 'minBedrooms', 'minPrice', 'maxPrice'].filter((k) => route.query[k]).length,
)

// SearchBar initial values
const initialLocation = String(route.query.location ?? '')
const initialCheckIn  = String(route.query.checkIn  ?? '')
const initialCheckOut = String(route.query.checkOut ?? '')
const initialGuests   = route.query.guests ? Number(route.query.guests) : undefined

// Mobile drawer
const drawerOpen = ref(false)
</script>

<template>
  <!-- Sticky top bar: search bar -->
  <div class=" sticky top-16 z-40 py-3 px-4 sm:px-6 sm:mt-4 lg:px-10">
    <div class="max-w-3xl mx-auto">
      <SearchBar
        :initial-location="initialLocation"
        :initial-check-in="initialCheckIn"
        :initial-check-out="initialCheckOut"
        :initial-guests="initialGuests"
      />
    </div>
  </div>

  <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-10 py-6 lg:py-8">

    <!-- Mobile: filter button row -->
    <div class="flex items-center gap-3 mb-5 lg:hidden">
      <button
        class="flex items-center gap-2 px-4 py-2 rounded-full border text-sm font-medium transition-all cursor-pointer"
        :class="hasFilters ? 'border-ink bg-ink text-white' : 'border-divider text-ink hover:border-ink/40 bg-white'"
        @click="drawerOpen = true"
      >
        <SlidersHorizontal class="w-4 h-4" :stroke-width="2" />
        Filters
        <span v-if="activeFilterCount" class="bg-white text-ink rounded-full w-4 h-4 text-[10px] font-bold flex items-center justify-center leading-none">{{ activeFilterCount }}</span>
      </button>
      <button
        v-if="hasFilters"
        class="text-[12px] text-muted underline underline-offset-2 hover:text-ink transition-colors cursor-pointer"
        @click="clearFilters"
      >Clear filters</button>
      <span class="ml-auto text-[13px] text-muted">
        <span v-if="!isLoading">{{ total }} place{{ total !== 1 ? 's' : '' }}</span>
        <span v-else>Searching…</span>
      </span>
    </div>

    <div class="flex gap-10">

      <!-- Desktop sidebar -->
      <aside class="w-64 shrink-0 hidden lg:block">
        <div class="sticky top-36 space-y-6">

          <div class="flex items-center justify-between">
            <h2 class="text-[11px] font-bold tracking-widest text-ink/50 uppercase">Filters</h2>
            <button
              v-if="hasFilters"
              class="text-[12px] text-muted underline underline-offset-2 hover:text-ink transition-colors cursor-pointer"
              @click="clearFilters"
            >Clear</button>
          </div>

          <!-- Category -->
          <div>
            <p class="text-[11px] font-bold tracking-widest text-ink/40 uppercase mb-3">Category</p>
            <div class="flex flex-wrap gap-1.5">
              <button
                v-for="cat in categories" :key="cat.id"
                class="flex items-center gap-1.5 px-3 py-1.5 rounded-full border text-[12px] font-medium transition-all cursor-pointer"
                :class="activeCategory === cat.id ? 'bg-ink text-white border-ink' : 'bg-white text-ink border-divider hover:border-ink/40'"
                @click="toggleCategory(cat.id as ListingCategory)"
              >
                <component :is="cat.icon" class="w-3.5 h-3.5 shrink-0" :stroke-width="2" />{{ cat.label }}
              </button>
            </div>
          </div>

          <div class="h-px bg-divider-light" />

          <!-- Property type -->
          <div>
            <p class="text-[11px] font-bold tracking-widest text-ink/40 uppercase mb-3">Property type</p>
            <div class="flex gap-2">
              <button
                v-for="type in (['Apartment', 'House'] as PropertyType[])" :key="type"
                class="flex-1 py-2 rounded-xl border text-[13px] font-medium transition-all cursor-pointer"
                :class="activePropertyType === type ? 'bg-ink text-white border-ink' : 'bg-white text-ink border-divider hover:border-ink/40'"
                @click="togglePropertyType(type)"
              >{{ type }}</button>
            </div>
          </div>

          <div class="h-px bg-divider-light" />

          <!-- Bedrooms -->
          <div>
            <p class="text-[11px] font-bold tracking-widest text-ink/40 uppercase mb-3">Min bedrooms</p>
            <div class="flex gap-2">
              <button
                v-for="n in [1, 2, 3, 4, 5]" :key="n"
                class="w-10 h-10 rounded-full border text-[13px] font-medium transition-all cursor-pointer"
                :class="activeMinBedrooms === n ? 'bg-ink text-white border-ink' : 'bg-white text-ink border-divider hover:border-ink/40'"
                @click="toggleBedrooms(n)"
              >{{ n }}+</button>
            </div>
          </div>

          <div class="h-px bg-divider-light" />

          <!-- Price -->
          <div>
            <p class="text-[11px] font-bold tracking-widest text-ink/40 uppercase mb-3">Price per night</p>
            <div class="flex items-center gap-2">
              <input v-model="minPriceInput" type="number" placeholder="Min $" min="0"
                class="w-full border border-divider rounded-xl px-3 py-2 text-[13px] text-ink placeholder:text-dim outline-none focus:border-ink/40 transition-colors bg-white"
                @blur="applyPrice" @keydown.enter="applyPrice" />
              <span class="text-dim shrink-0 text-sm">–</span>
              <input v-model="maxPriceInput" type="number" placeholder="Max $" min="0"
                class="w-full border border-divider rounded-xl px-3 py-2 text-[13px] text-ink placeholder:text-dim outline-none focus:border-ink/40 transition-colors bg-white"
                @blur="applyPrice" @keydown.enter="applyPrice" />
            </div>
          </div>

        </div>
      </aside>

      <!-- Results -->
      <section class="flex-1 min-w-0">
        <p class="hidden lg:block text-[13px] text-muted mb-6">
          <span v-if="!isLoading">{{ total }} place{{ total !== 1 ? 's' : '' }} found</span>
          <span v-else>Searching…</span>
        </p>

        <!-- Skeleton -->
        <div v-if="isLoading" class="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-5">
          <div v-for="n in 6" :key="n" class="animate-pulse">
            <div class="aspect-4/3 rounded-2xl bg-divider-light mb-3" />
            <div class="h-3.5 bg-divider-light rounded w-3/4 mb-2" />
            <div class="h-3 bg-divider-light rounded w-1/2" />
          </div>
        </div>

        <!-- Error -->
        <div v-else-if="error" class="text-center py-20">
          <p class="text-sm text-muted mb-4">{{ error }}</p>
          <button class="text-sm font-medium text-ink underline cursor-pointer" @click="fetchListings(parseQuery(route.query))">Try again</button>
        </div>

        <!-- Empty -->
        <div v-else-if="listings.length === 0" class="text-center py-24">
          <p class="font-serif text-2xl text-ink mb-2">No places found</p>
          <p class="text-sm text-muted mb-6">Try adjusting your filters or search somewhere new.</p>
          <button
            v-if="hasFilters"
            class="text-sm font-medium text-ink border border-divider rounded-full px-5 py-2.5 hover:border-ink/40 transition-colors cursor-pointer"
            @click="clearFilters"
          >Clear all filters</button>
        </div>

        <!-- Grid -->
        <div v-else class="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-5">
          <ListingCard v-for="listing in listings" :key="listing.id" v-bind="listing" />
        </div>

        <!-- Pagination -->
        <div v-if="!isLoading && totalPages > 1" class="flex justify-center items-center gap-1.5 mt-12">
          <button
            class="w-9 h-9 rounded-full border border-divider flex items-center justify-center text-ink hover:border-ink/40 transition-colors disabled:opacity-30 disabled:cursor-not-allowed cursor-pointer"
            :disabled="currentPage <= 1" aria-label="Previous page" @click="goToPage(currentPage - 1)"
          >
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><polyline points="15 18 9 12 15 6" /></svg>
          </button>
          <template v-for="p in visiblePages(currentPage, totalPages)" :key="String(p)">
            <span v-if="p === '…'" class="w-9 h-9 flex items-center justify-center text-muted text-sm">…</span>
            <button v-else
              class="w-9 h-9 rounded-full border text-sm font-medium transition-all cursor-pointer"
              :class="p === currentPage ? 'bg-ink text-white border-ink' : 'bg-white text-ink border-divider hover:border-ink/40'"
              @click="goToPage(p as number)"
            >{{ p }}</button>
          </template>
          <button
            class="w-9 h-9 rounded-full border border-divider flex items-center justify-center text-ink hover:border-ink/40 transition-colors disabled:opacity-30 disabled:cursor-not-allowed cursor-pointer"
            :disabled="currentPage >= totalPages" aria-label="Next page" @click="goToPage(currentPage + 1)"
          >
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><polyline points="9 18 15 12 9 6" /></svg>
          </button>
        </div>
      </section>
    </div>
  </div>

  <!-- Mobile filter drawer -->
  <Teleport to="body">
    <Transition name="overlay">
      <div v-if="drawerOpen" class="fixed inset-0 bg-black/40 z-50 lg:hidden" @click="drawerOpen = false" />
    </Transition>
    <Transition name="drawer">
      <div v-if="drawerOpen" class="fixed bottom-0 left-0 right-0 z-50 bg-white rounded-t-3xl lg:hidden max-h-[85dvh] flex flex-col">
        <!-- Handle + header -->
        <div class="flex items-center justify-between px-6 pt-5 pb-4 border-b border-divider-light shrink-0">
          <div class="w-10 h-1 bg-divider rounded-full absolute top-3 left-1/2 -translate-x-1/2" />
          <h2 class="font-semibold text-base text-ink">Filters</h2>
          <button class="w-8 h-8 flex items-center justify-center rounded-full hover:bg-gray-100 transition-colors cursor-pointer" @click="drawerOpen = false">
            <X class="w-4 h-4 text-ink" :stroke-width="2" />
          </button>
        </div>

        <!-- Scrollable filter content -->
        <div class="overflow-y-auto flex-1 px-6 py-5 space-y-7">

          <!-- Category -->
          <div>
            <p class="text-[11px] font-bold tracking-widest text-ink/40 uppercase mb-3">Category</p>
            <div class="flex flex-wrap gap-2">
              <button
                v-for="cat in categories" :key="cat.id"
                class="flex items-center gap-1.5 px-3.5 py-2 rounded-full border text-sm font-medium transition-all cursor-pointer"
                :class="activeCategory === cat.id ? 'bg-ink text-white border-ink' : 'bg-white text-ink border-divider'"
                @click="toggleCategory(cat.id as ListingCategory)"
              >
                <component :is="cat.icon" class="w-4 h-4 shrink-0" :stroke-width="2" />{{ cat.label }}
              </button>
            </div>
          </div>

          <div class="h-px bg-divider-light" />

          <!-- Property type -->
          <div>
            <p class="text-[11px] font-bold tracking-widest text-ink/40 uppercase mb-3">Property type</p>
            <div class="flex gap-3">
              <button
                v-for="type in (['Apartment', 'House'] as PropertyType[])" :key="type"
                class="flex-1 py-3 rounded-2xl border text-sm font-medium transition-all cursor-pointer"
                :class="activePropertyType === type ? 'bg-ink text-white border-ink' : 'bg-white text-ink border-divider'"
                @click="togglePropertyType(type)"
              >{{ type }}</button>
            </div>
          </div>

          <div class="h-px bg-divider-light" />

          <!-- Bedrooms -->
          <div>
            <p class="text-[11px] font-bold tracking-widest text-ink/40 uppercase mb-3">Min bedrooms</p>
            <div class="flex gap-2.5">
              <button
                v-for="n in [1, 2, 3, 4, 5]" :key="n"
                class="w-12 h-12 rounded-full border text-sm font-medium transition-all cursor-pointer"
                :class="activeMinBedrooms === n ? 'bg-ink text-white border-ink' : 'bg-white text-ink border-divider'"
                @click="toggleBedrooms(n)"
              >{{ n }}+</button>
            </div>
          </div>

          <div class="h-px bg-divider-light" />

          <!-- Price -->
          <div>
            <p class="text-[11px] font-bold tracking-widest text-ink/40 uppercase mb-3">Price per night</p>
            <div class="flex items-center gap-3">
              <input v-model="minPriceInput" type="number" placeholder="Min $" min="0"
                class="w-full border border-divider rounded-2xl px-4 py-3 text-sm text-ink placeholder:text-dim outline-none focus:border-ink/40 transition-colors bg-white"
                @blur="applyPrice" @keydown.enter="applyPrice" />
              <span class="text-dim shrink-0">–</span>
              <input v-model="maxPriceInput" type="number" placeholder="Max $" min="0"
                class="w-full border border-divider rounded-2xl px-4 py-3 text-sm text-ink placeholder:text-dim outline-none focus:border-ink/40 transition-colors bg-white"
                @blur="applyPrice" @keydown.enter="applyPrice" />
            </div>
          </div>
        </div>

        <!-- Footer actions -->
        <div class="px-6 py-4 border-t border-divider-light flex items-center justify-between shrink-0 gap-4">
          <button
            class="text-sm font-medium text-muted underline underline-offset-2 cursor-pointer"
            @click="clearFilters"
          >Clear all</button>
          <button
            class="flex-1 bg-ink text-white rounded-full py-3 text-sm font-semibold hover:bg-black transition-colors cursor-pointer"
            @click="drawerOpen = false"
          >
            Show {{ total }} place{{ total !== 1 ? 's' : '' }}
          </button>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<style scoped>
.overlay-enter-active, .overlay-leave-active { transition: opacity 0.25s ease; }
.overlay-enter-from, .overlay-leave-to { opacity: 0; }

.drawer-enter-active, .drawer-leave-active { transition: transform 0.3s cubic-bezier(0.32, 0.72, 0, 1); }
.drawer-enter-from, .drawer-leave-to { transform: translateY(100%); }
</style>
