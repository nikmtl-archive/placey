<script setup lang="ts">
import { ref, watch, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import type { LocationQuery } from 'vue-router'
import ListingCard from '@/components/ListingCard.vue'
import { useListings } from '@/composables/useListings'
import { useCategories } from '@/composables/useCategories'
import type { ListingSearchParams, ListingCategory, PropertyType } from '@/types'

const route = useRoute()
const router = useRouter()
const { categories } = useCategories()
const { listings, total, isLoading, error, fetchListings } = useListings()

const LIMIT = 20

function parseQuery(q: LocationQuery): ListingSearchParams {
  return {
    location:     q.location     ? String(q.location)              : undefined,
    checkIn:      q.checkIn      ? String(q.checkIn)               : undefined,
    checkOut:     q.checkOut     ? String(q.checkOut)              : undefined,
    guests:       q.guests       ? Number(q.guests)                : undefined,
    category:     q.category     ? String(q.category) as ListingCategory : undefined,
    propertyType: q.propertyType ? String(q.propertyType) as PropertyType : undefined,
    minBedrooms:  q.minBedrooms  ? Number(q.minBedrooms)           : undefined,
    minPrice:     q.minPrice     ? Number(q.minPrice)              : undefined,
    maxPrice:     q.maxPrice     ? Number(q.maxPrice)              : undefined,
    page:         q.page         ? Number(q.page)                  : 1,
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
    if (v !== undefined && v !== '' && v !== null) next[k] = String(v)
  }
  router.replace({ query: next })
}

function clearFilters() {
  router.replace({ query: {} })
}

function goToPage(page: number) {
  router.replace({ query: { ...route.query, page: String(page) } })
}

watch(() => route.query, (q) => {
  fetchListings(parseQuery(q))
}, { immediate: true })

// Local form state (synced from URL on mount)
const locationInput  = ref('')
const checkInInput   = ref('')
const checkOutInput  = ref('')
const guestsInput    = ref(1)
const minPriceInput  = ref('')
const maxPriceInput  = ref('')
const minBedroomsInput = ref<number | undefined>(undefined)

onMounted(() => {
  locationInput.value  = String(route.query.location  ?? '')
  checkInInput.value   = String(route.query.checkIn   ?? '')
  checkOutInput.value  = String(route.query.checkOut  ?? '')
  guestsInput.value    = route.query.guests     ? Number(route.query.guests)     : 1
  minPriceInput.value  = String(route.query.minPrice  ?? '')
  maxPriceInput.value  = String(route.query.maxPrice  ?? '')
  minBedroomsInput.value = route.query.minBedrooms ? Number(route.query.minBedrooms) : undefined
})

const activeCategory   = computed(() => route.query.category     ? String(route.query.category)     : undefined)
const activePropertyType = computed(() => route.query.propertyType ? String(route.query.propertyType) : undefined)

function toggleCategory(id: string) {
  applyFilter({ category: activeCategory.value === id ? undefined : id })
}

function togglePropertyType(type: string) {
  applyFilter({ propertyType: activePropertyType.value === type ? undefined : type })
}

function applyTextFilters() {
  applyFilter({
    location:    locationInput.value  || undefined,
    checkIn:     checkInInput.value   || undefined,
    checkOut:    checkOutInput.value  || undefined,
    guests:      guestsInput.value > 1 ? guestsInput.value : undefined,
    minPrice:    minPriceInput.value  || undefined,
    maxPrice:    maxPriceInput.value  || undefined,
    minBedrooms: minBedroomsInput.value || undefined,
  })
}

const hasFilters = computed(() =>
  Object.keys(route.query).some((k) => k !== 'page' && route.query[k]),
)
</script>

<template>
  <div class="max-w-7xl mx-auto px-6 lg:px-10 py-10">
    <div class="flex gap-10">
      <!-- Filter sidebar -->
      <aside class="w-72 shrink-0 hidden lg:block">
        <div class="sticky top-28 space-y-8">
          <div class="flex items-center justify-between">
            <h2 class="font-display text-lg font-medium text-ink">Filters</h2>
            <button
              v-if="hasFilters"
              class="text-xs text-muted underline hover:text-ink transition-colors"
              @click="clearFilters"
            >Clear all</button>
          </div>

          <!-- Location -->
          <div>
            <label class="block text-[10px] font-semibold tracking-widest text-ink uppercase mb-2">Location</label>
            <input
              v-model="locationInput"
              type="text"
              placeholder="City or country"
              class="w-full border border-divider rounded-lg px-3 py-2 text-sm text-ink placeholder:text-dim outline-none focus:border-ink/40 transition-colors bg-white"
              @keydown.enter="applyTextFilters"
              @blur="applyTextFilters"
            />
          </div>

          <!-- Dates -->
          <div class="grid grid-cols-2 gap-2">
            <div>
              <label class="block text-[10px] font-semibold tracking-widest text-ink uppercase mb-2">Check in</label>
              <input
                v-model="checkInInput"
                type="date"
                class="w-full border border-divider rounded-lg px-3 py-2 text-sm text-ink outline-none focus:border-ink/40 transition-colors bg-white scheme-light"
                @change="applyTextFilters"
              />
            </div>
            <div>
              <label class="block text-[10px] font-semibold tracking-widest text-ink uppercase mb-2">Check out</label>
              <input
                v-model="checkOutInput"
                type="date"
                class="w-full border border-divider rounded-lg px-3 py-2 text-sm text-ink outline-none focus:border-ink/40 transition-colors bg-white scheme-light"
                @change="applyTextFilters"
              />
            </div>
          </div>

          <!-- Guests -->
          <div>
            <label class="block text-[10px] font-semibold tracking-widest text-ink uppercase mb-2">Guests</label>
            <div class="flex items-center gap-3">
              <button
                class="w-8 h-8 rounded-full border border-divider flex items-center justify-center text-ink hover:border-ink transition-colors"
                @click="guestsInput = Math.max(1, guestsInput - 1); applyTextFilters()"
              >−</button>
              <span class="text-sm font-medium text-ink w-6 text-center">{{ guestsInput }}</span>
              <button
                class="w-8 h-8 rounded-full border border-divider flex items-center justify-center text-ink hover:border-ink transition-colors"
                @click="guestsInput++; applyTextFilters()"
              >+</button>
            </div>
          </div>

          <!-- Category -->
          <div>
            <label class="block text-[10px] font-semibold tracking-widest text-ink uppercase mb-3">Category</label>
            <div class="flex flex-wrap gap-2">
              <button
                v-for="cat in categories"
                :key="cat.id"
                class="flex items-center gap-1.5 px-3 py-1.5 rounded-full border text-xs font-medium transition-all"
                :class="activeCategory === cat.id
                  ? 'bg-ink text-white border-ink'
                  : 'bg-white text-ink border-divider hover:border-ink/40'"
                @click="toggleCategory(cat.id)"
              >
                <span>{{ cat.icon }}</span>
                <span>{{ cat.label }}</span>
              </button>
            </div>
          </div>

          <!-- Property type -->
          <div>
            <label class="block text-[10px] font-semibold tracking-widest text-ink uppercase mb-3">Property type</label>
            <div class="flex gap-2">
              <button
                v-for="type in ['Apartment', 'House']"
                :key="type"
                class="flex-1 py-2 rounded-lg border text-sm font-medium transition-all"
                :class="activePropertyType === type
                  ? 'bg-ink text-white border-ink'
                  : 'bg-white text-ink border-divider hover:border-ink/40'"
                @click="togglePropertyType(type)"
              >
                {{ type }}
              </button>
            </div>
          </div>

          <!-- Min bedrooms -->
          <div>
            <label class="block text-[10px] font-semibold tracking-widest text-ink uppercase mb-3">Bedrooms</label>
            <div class="flex gap-2 flex-wrap">
              <button
                v-for="n in [1, 2, 3, 4, 5]"
                :key="n"
                class="w-10 h-10 rounded-full border text-sm font-medium transition-all"
                :class="minBedroomsInput === n
                  ? 'bg-ink text-white border-ink'
                  : 'bg-white text-ink border-divider hover:border-ink/40'"
                @click="minBedroomsInput = minBedroomsInput === n ? undefined : n; applyTextFilters()"
              >
                {{ n }}+
              </button>
            </div>
          </div>

          <!-- Price range -->
          <div>
            <label class="block text-[10px] font-semibold tracking-widest text-ink uppercase mb-2">Price per night</label>
            <div class="flex items-center gap-2">
              <input
                v-model="minPriceInput"
                type="number"
                placeholder="Min $"
                min="0"
                class="w-full border border-divider rounded-lg px-3 py-2 text-sm text-ink placeholder:text-dim outline-none focus:border-ink/40 transition-colors bg-white"
                @blur="applyTextFilters"
                @keydown.enter="applyTextFilters"
              />
              <span class="text-dim shrink-0">–</span>
              <input
                v-model="maxPriceInput"
                type="number"
                placeholder="Max $"
                min="0"
                class="w-full border border-divider rounded-lg px-3 py-2 text-sm text-ink placeholder:text-dim outline-none focus:border-ink/40 transition-colors bg-white"
                @blur="applyTextFilters"
                @keydown.enter="applyTextFilters"
              />
            </div>
          </div>
        </div>
      </aside>

      <!-- Results -->
      <section class="flex-1 min-w-0">
        <div class="flex items-center justify-between mb-6">
          <p class="text-sm text-muted">
            <span v-if="!isLoading">{{ total }} place{{ total !== 1 ? 's' : '' }} found</span>
            <span v-else>Searching…</span>
          </p>
        </div>

        <!-- Loading skeleton -->
        <div v-if="isLoading" class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
          <div v-for="n in 6" :key="n" class="animate-pulse">
            <div class="aspect-square rounded-xl bg-divider-light mb-3" />
            <div class="h-3.5 bg-divider-light rounded w-3/4 mb-2" />
            <div class="h-3 bg-divider-light rounded w-1/2" />
          </div>
        </div>

        <!-- Error -->
        <div v-else-if="error" class="text-center py-20">
          <p class="text-base text-muted mb-4">{{ error }}</p>
          <button
            class="text-sm font-medium text-ink underline"
            @click="fetchListings(parseQuery(route.query))"
          >Try again</button>
        </div>

        <!-- Empty -->
        <div v-else-if="listings.length === 0" class="text-center py-20">
          <p class="font-display text-xl text-ink mb-2">No places found</p>
          <p class="text-sm text-muted mb-6">Try adjusting your filters or search somewhere new.</p>
          <button
            class="text-sm font-medium text-ink border border-divider rounded-full px-5 py-2.5 hover:border-ink/40 transition-colors"
            @click="clearFilters"
          >Clear all filters</button>
        </div>

        <!-- Grid -->
        <div v-else class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
          <ListingCard
            v-for="listing in listings"
            :key="listing.id"
            v-bind="listing"
          />
        </div>

        <!-- Pagination -->
        <div v-if="!isLoading && totalPages > 1" class="flex justify-center items-center gap-1.5 mt-12">
          <button
            class="w-9 h-9 rounded-full border border-divider flex items-center justify-center text-ink hover:border-ink/40 transition-colors disabled:opacity-30 disabled:cursor-not-allowed"
            :disabled="currentPage <= 1"
            @click="goToPage(currentPage - 1)"
            aria-label="Previous page"
          >
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
              <polyline points="15 18 9 12 15 6" />
            </svg>
          </button>

          <template v-for="p in visiblePages(currentPage, totalPages)" :key="String(p)">
            <span v-if="p === '…'" class="w-9 h-9 flex items-center justify-center text-muted text-sm">…</span>
            <button
              v-else
              class="w-9 h-9 rounded-full border text-sm font-medium transition-all"
              :class="p === currentPage
                ? 'bg-ink text-white border-ink'
                : 'bg-white text-ink border-divider hover:border-ink/40'"
              @click="goToPage(p as number)"
            >{{ p }}</button>
          </template>

          <button
            class="w-9 h-9 rounded-full border border-divider flex items-center justify-center text-ink hover:border-ink/40 transition-colors disabled:opacity-30 disabled:cursor-not-allowed"
            :disabled="currentPage >= totalPages"
            @click="goToPage(currentPage + 1)"
            aria-label="Next page"
          >
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
              <polyline points="9 18 15 12 9 6" />
            </svg>
          </button>
        </div>
      </section>
    </div>
  </div>
</template>
