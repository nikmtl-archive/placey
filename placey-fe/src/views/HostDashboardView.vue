<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useAuthStore, parseErrorMessage } from '@/stores/useAuthStore'
import ListingCard from '@/components/ListingCard.vue'
import ConfirmModal from '@/components/ConfirmModal.vue'
import type { Listing } from '@/types'

const authStore = useAuthStore()

type Tab = 'listings' | 'reservations'

interface Reservation {
  id: string
  listingId: string
  listingTitle: string
  listingPhotoUrl: string | null
  guestId: string
  guestName: string
  guestAvatarUrl: string | null
  checkIn: string
  checkOut: string
  guests: number
  totalPrice: number
  status: 'Pending' | 'Confirmed' | 'Completed' | 'Cancelled'
  createdAt: string
}

const activeTab = ref<Tab>('listings')

// Listings

const listings = ref<Listing[]>([])
const listingsLoading = ref(false)
const listingsError = ref<string | null>(null)

async function fetchListings() {
  listingsLoading.value = true
  listingsError.value = null
  try {
    const res = await authStore.apiFetch('/api/users/me/listings')
    if (!res.ok) throw new Error(`HTTP ${res.status}`)
    listings.value = await res.json()
  } catch (e: unknown) {
    listingsError.value = e instanceof Error ? e.message : 'Failed to load listings'
  } finally {
    listingsLoading.value = false
  }
}

// Reservations

const reservations = ref<Reservation[]>([])
const reservationsLoading = ref(false)
const reservationsError = ref<string | null>(null)
const reservationTab = ref<'pending' | 'upcoming' | 'past'>('pending')
const reservationSearch = ref('')

const pendingReservations = computed(() => reservations.value.filter((r) => r.status === 'Pending'))
const upcomingReservations = computed(() =>
  reservations.value.filter((r) => r.status === 'Confirmed'),
)
const pastReservations = computed(() =>
  reservations.value.filter((r) => r.status === 'Completed' || r.status === 'Cancelled'),
)

const currentReservations = computed(() => {
  let list: Reservation[]
  if (reservationTab.value === 'pending') list = pendingReservations.value
  else if (reservationTab.value === 'upcoming') list = upcomingReservations.value
  else list = pastReservations.value
  const q = reservationSearch.value.trim().toLowerCase()
  if (!q) return list
  return list.filter(
    (r) => r.listingTitle.toLowerCase().includes(q) || r.guestName.toLowerCase().includes(q),
  )
})

async function fetchReservations() {
  reservationsLoading.value = true
  reservationsError.value = null
  try {
    const res = await authStore.apiFetch('/api/users/me/reservations')
    if (!res.ok) throw new Error(`HTTP ${res.status}`)
    reservations.value = await res.json()
  } catch (e: unknown) {
    reservationsError.value = e instanceof Error ? e.message : 'Failed to load reservations'
  } finally {
    reservationsLoading.value = false
  }
}

// Approve / decline reservation

const updatingId = ref<string | null>(null)
const updateError = ref<string | null>(null)
const confirmCancelId = ref<string | null>(null)

function confirmCancel() {
  if (!confirmCancelId.value) return
  const id = confirmCancelId.value
  confirmCancelId.value = null
  updateReservationStatus(id, 'Cancelled')
}

async function updateReservationStatus(id: string, status: 'Confirmed' | 'Cancelled') {
  updatingId.value = id
  updateError.value = null
  try {
    const res = await authStore.apiFetch(`/api/bookings/${id}/status`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ status }),
    })
    if (!res.ok) throw new Error(await parseErrorMessage(res))
    const updated: Reservation = await res.json()
    const idx = reservations.value.findIndex((r) => r.id === id)
    if (idx !== -1) reservations.value[idx] = updated
    authStore.fetchPendingReservationsCount()
  } catch (e: unknown) {
    updateError.value = e instanceof Error ? e.message : 'Failed to update reservation'
  } finally {
    updatingId.value = null
  }
}

// Helpers

function formatDate(d: string) {
  const date = d.length === 10 ? new Date(`${d}T00:00:00`) : new Date(d)
  return date.toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' })
}

function nights(checkIn: string, checkOut: string) {
  const a = checkIn.length === 10 ? new Date(`${checkIn}T00:00:00`) : new Date(checkIn)
  const b = checkOut.length === 10 ? new Date(`${checkOut}T00:00:00`) : new Date(checkOut)
  return Math.round((b.getTime() - a.getTime()) / 86_400_000)
}

const statusLabel: Record<Reservation['status'], string> = {
  Pending: 'Pending',
  Confirmed: 'Confirmed',
  Completed: 'Completed',
  Cancelled: 'Cancelled',
}

const statusClass: Record<Reservation['status'], string> = {
  Pending: 'bg-amber-50 text-amber-700 border border-amber-200',
  Confirmed: 'bg-emerald-50 text-emerald-700 border border-emerald-200',
  Completed: 'bg-sky-50 text-sky-700 border border-sky-200',
  Cancelled: 'bg-zinc-100 text-zinc-500 border border-zinc-200',
}

onMounted(() => {
  Promise.all([fetchListings(), fetchReservations()])
})
</script>

<template>
  <div class="max-w-5xl mx-auto px-4 sm:px-6 lg:px-10 py-10 lg:py-14">
    <!-- Header -->
    <div class="flex items-center justify-between mb-8">
      <div>
        <h1 class="font-serif text-3xl font-semibold text-ink mb-1">Host dashboard</h1>
        <p class="text-sm text-muted">Manage your listings, reservations and availability</p>
      </div>
      <RouterLink
        to="/listing/new"
        class="flex items-center gap-2 text-sm font-medium text-white bg-ink px-5 py-2.5 rounded-full hover:bg-black transition-colors"
      >
        <svg
          width="14"
          height="14"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="2.5"
          stroke-linecap="round"
          stroke-linejoin="round"
        >
          <line x1="12" y1="5" x2="12" y2="19" />
          <line x1="5" y1="12" x2="19" y2="12" />
        </svg>
        New listing
      </RouterLink>
    </div>

    <!-- Top tabs -->
    <div class="flex gap-1 mb-8 border-b border-divider-light">
      <button
        v-for="tab in ['listings', 'reservations'] as const"
        :key="tab"
        class="pb-3 px-1 text-sm font-medium capitalize transition-colors border-b-2 -mb-px cursor-pointer"
        :class="
          activeTab === tab ? 'border-ink text-ink' : 'border-transparent text-muted hover:text-ink'
        "
        @click="activeTab = tab"
      >
        {{ tab === 'listings' ? 'My listings' : 'Reservations' }}
        <span
          v-if="tab === 'listings' || pendingReservations.length > 0"
          class="ml-1.5 text-xs rounded-full w-5 h-5 inline-flex items-center justify-center font-normal"
          :class="
            tab === 'reservations' && pendingReservations.length > 0
              ? 'bg-rose-500 text-white'
              : activeTab === tab
                ? 'bg-ink text-white'
                : 'bg-black/6 text-muted'
          "
        >
          {{ tab === 'listings' ? listings.length : pendingReservations.length }}
        </span>
      </button>
    </div>

    <!-- Listings content -->
    <div v-if="activeTab === 'listings'">
      <!-- Skeleton -->
      <div v-if="listingsLoading" class="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <div
          v-for="n in 4"
          :key="n"
          class="animate-pulse rounded-2xl border border-divider-light overflow-hidden"
        >
          <div class="h-40 bg-divider-light" />
          <div class="p-4 space-y-2">
            <div class="h-4 bg-divider-light rounded w-3/4" />
            <div class="h-3 bg-divider-light rounded w-1/3" />
          </div>
        </div>
      </div>

      <!-- Error -->
      <div v-else-if="listingsError" class="text-center py-20">
        <p class="text-sm text-muted mb-4">{{ listingsError }}</p>
        <button
          class="text-sm font-medium text-ink underline cursor-pointer"
          @click="fetchListings"
        >
          Try again
        </button>
      </div>

      <!-- Empty -->
      <div
        v-else-if="listings.length === 0"
        class="flex flex-col items-center justify-center py-28 text-center"
      >
        <svg
          class="mb-5 text-ink/20"
          width="48"
          height="48"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="1.5"
          stroke-linecap="round"
          stroke-linejoin="round"
        >
          <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z" />
          <polyline points="9 22 9 12 15 12 15 22" />
        </svg>
        <p class="font-serif text-2xl text-ink mb-2">No listings yet</p>
        <p class="text-sm text-muted mb-6 max-w-xs">
          Create your first listing to start receiving bookings.
        </p>
        <RouterLink
          to="/listing/new"
          class="text-sm font-medium text-white bg-ink px-6 py-2.5 rounded-full hover:bg-black transition-colors"
          >Create a listing</RouterLink
        >
      </div>

      <!-- Grid -->
      <div v-else class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-x-5 gap-y-8">
        <div v-for="l in listings" :key="l.id" class="relative">
          <ListingCard v-bind="l" :hide-wishlist="true" />
          <!-- Edit button overlaid on the top-left of the card image -->
          <RouterLink
            :to="`/listing/${l.id}/edit`"
            class="absolute top-2.5 left-2.5 w-8 h-8 flex items-center justify-center rounded-full bg-white/80 backdrop-blur-sm shadow-sm text-ink/60 hover:text-ink hover:scale-110 active:scale-95 transition-all duration-150"
            title="Edit listing"
            @click.stop
          >
            <svg
              width="13"
              height="13"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              stroke-width="2"
              stroke-linecap="round"
              stroke-linejoin="round"
            >
              <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" />
              <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z" />
            </svg>
          </RouterLink>
        </div>
      </div>
    </div>

    <!-- Reservations content -->
    <div v-if="activeTab === 'reservations'">
      <!-- Filters + search -->
      <div class="flex flex-col sm:flex-row sm:items-center gap-3 mb-6">
        <!-- Status pill filters -->
        <div class="flex items-center gap-2 flex-wrap">
          <button
            v-for="tab in ['pending', 'upcoming', 'past'] as const"
            :key="tab"
            class="flex items-center gap-1.5 text-sm font-medium px-4 py-1.5 rounded-full border transition-colors cursor-pointer"
            :class="
              reservationTab === tab
                ? 'bg-ink text-white border-ink'
                : 'bg-white text-muted border-divider hover:border-ink/30 hover:text-ink'
            "
            @click="reservationTab = tab"
          >
            {{ tab === 'pending' ? 'Pending' : tab === 'upcoming' ? 'Confirmed' : 'Past' }}
            <span
              class="text-[11px] font-semibold rounded-full px-1.5 py-0.5 leading-none"
              :class="reservationTab === tab ? 'bg-white/20 text-white' : 'bg-black/6 text-muted'"
            >
              {{
                tab === 'pending'
                  ? pendingReservations.length
                  : tab === 'upcoming'
                    ? upcomingReservations.length
                    : pastReservations.length
              }}
            </span>
          </button>
        </div>
        <!-- Search -->
        <div class="relative sm:ml-auto">
          <svg
            class="absolute left-3 top-1/2 -translate-y-1/2 text-muted pointer-events-none"
            width="14"
            height="14"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="2"
            stroke-linecap="round"
            stroke-linejoin="round"
          >
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="reservationSearch"
            type="text"
            placeholder="Search by listing or guest…"
            class="w-full sm:w-64 pl-9 pr-4 py-1.5 text-sm border border-divider rounded-full focus:outline-none focus:border-ink/40 transition-colors"
          />
          <button
            v-if="reservationSearch"
            class="absolute right-3 top-1/2 -translate-y-1/2 text-muted hover:text-ink transition-colors cursor-pointer"
            @click="reservationSearch = ''"
          >
            <svg
              width="12"
              height="12"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              stroke-width="2.5"
              stroke-linecap="round"
            >
              <line x1="18" y1="6" x2="6" y2="18" />
              <line x1="6" y1="6" x2="18" y2="18" />
            </svg>
          </button>
        </div>
      </div>

      <!-- Skeleton -->
      <div v-if="reservationsLoading" class="space-y-4">
        <div
          v-for="n in 3"
          :key="n"
          class="animate-pulse flex gap-4 border border-divider-light rounded-2xl p-4"
        >
          <div class="w-24 h-24 rounded-xl bg-divider-light shrink-0" />
          <div class="flex-1 space-y-2 py-1">
            <div class="h-4 bg-divider-light rounded w-2/3" />
            <div class="h-3 bg-divider-light rounded w-1/3" />
            <div class="h-3 bg-divider-light rounded w-1/2" />
          </div>
        </div>
      </div>

      <!-- Error -->
      <div v-else-if="reservationsError" class="text-center py-20">
        <p class="text-sm text-muted mb-4">{{ reservationsError }}</p>
        <button
          class="text-sm font-medium text-ink underline cursor-pointer"
          @click="fetchReservations"
        >
          Try again
        </button>
      </div>

      <!-- Empty -->
      <div
        v-else-if="currentReservations.length === 0"
        class="flex flex-col items-center py-24 text-center"
      >
        <svg
          class="mb-5 text-ink/20"
          width="40"
          height="40"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="1.5"
          stroke-linecap="round"
          stroke-linejoin="round"
        >
          <rect x="3" y="4" width="18" height="18" rx="2" />
          <line x1="16" y1="2" x2="16" y2="6" />
          <line x1="8" y1="2" x2="8" y2="6" />
          <line x1="3" y1="10" x2="21" y2="10" />
        </svg>
        <p class="font-serif text-xl text-ink mb-2">No {{ reservationTab }} reservations</p>
        <p class="text-sm text-muted">They'll appear here once guests book your listings.</p>
      </div>

      <!-- Update error banner -->
      <div
        v-if="updateError"
        class="mb-4 text-xs text-rose-600 bg-rose-50 border border-rose-200 rounded-xl px-4 py-3"
      >
        {{ updateError }}
      </div>

      <!-- List -->
      <div v-if="!reservationsLoading && !reservationsError" class="space-y-4">
        <div
          v-for="r in currentReservations"
          :key="r.id"
          class="border border-divider-light rounded-3xl overflow-hidden"
        >
          <div class="flex gap-4 p-4">
            <!-- Listing photo -->
            <RouterLink :to="`/listing/${r.listingId}`" class="shrink-0">
              <div class="w-24 h-24 rounded-xl overflow-hidden bg-divider-light">
                <img
                  v-if="r.listingPhotoUrl"
                  :src="r.listingPhotoUrl"
                  :alt="r.listingTitle"
                  class="w-full h-full object-cover"
                />
                <div v-else class="w-full h-full flex items-center justify-center">
                  <svg
                    class="text-ink/20"
                    width="24"
                    height="24"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    stroke-width="1.5"
                  >
                    <rect x="3" y="3" width="18" height="18" rx="2" />
                    <circle cx="8.5" cy="8.5" r="1.5" />
                    <polyline points="21 15 16 10 5 21" />
                  </svg>
                </div>
              </div>
            </RouterLink>

            <div class="flex-1 min-w-0">
              <div class="flex items-start justify-between gap-2 mb-2">
                <div>
                  <RouterLink
                    :to="`/listing/${r.listingId}`"
                    class="font-medium text-ink text-sm hover:underline line-clamp-1"
                    >{{ r.listingTitle }}</RouterLink
                  >
                  <!-- Guest info -->
                  <div class="flex items-center gap-1.5 mt-0.5">
                    <div
                      class="w-5 h-5 rounded-full bg-[#e8e3dc] border border-divider-light overflow-hidden flex items-center justify-center shrink-0"
                    >
                      <img
                        v-if="r.guestAvatarUrl"
                        :src="r.guestAvatarUrl"
                        :alt="r.guestName"
                        class="w-full h-full object-cover"
                      />
                      <span v-else class="text-[8px] font-semibold text-ink/60">{{
                        (r.guestName?.[0] ?? '').toUpperCase()
                      }}</span>
                    </div>
                    <RouterLink
                      :to="`/messages`"
                      class="text-xs text-muted hover:text-ink transition-colors"
                      >{{ r.guestName }}</RouterLink
                    >
                  </div>
                </div>
                <span
                  class="text-xs font-medium px-2 py-0.5 rounded-full whitespace-nowrap shrink-0"
                  :class="statusClass[r.status]"
                >
                  {{ statusLabel[r.status] }}
                </span>
              </div>

              <p class="text-xs text-muted mb-1">
                {{ formatDate(r.checkIn) }} – {{ formatDate(r.checkOut) }}
                <span class="mx-1 text-muted/40">·</span>
                {{ nights(r.checkIn, r.checkOut) }} night{{
                  nights(r.checkIn, r.checkOut) !== 1 ? 's' : ''
                }}
                <span class="mx-1 text-muted/40">·</span>
                {{ r.guests }} guest{{ r.guests !== 1 ? 's' : '' }}
              </p>
              <p class="text-xs font-medium text-ink">${{ r.totalPrice.toFixed(2) }} total</p>
            </div>
          </div>

          <!-- Pending actions -->
          <div
            v-if="r.status === 'Pending'"
            class="border-t border-divider-light px-4 py-3 flex items-center gap-3"
          >
            <button
              class="text-xs font-medium text-white bg-ink px-4 py-1.5 rounded-full hover:bg-black transition-colors disabled:opacity-50 cursor-pointer"
              :disabled="updatingId === r.id"
              @click="updateReservationStatus(r.id, 'Confirmed')"
            >
              {{ updatingId === r.id ? 'Confirming…' : 'Confirm' }}
            </button>
            <button
              class="text-xs font-medium text-muted hover:text-rose-600 transition-colors disabled:opacity-50 cursor-pointer"
              :disabled="updatingId === r.id"
              @click="confirmCancelId = r.id"
            >
              Decline
            </button>
          </div>

          <!-- Confirmed — host can still cancel -->
          <div v-else-if="r.status === 'Confirmed'" class="border-t border-divider-light px-4 py-3">
            <button
              class="text-xs font-medium text-muted hover:text-rose-600 transition-colors disabled:opacity-50 cursor-pointer"
              :disabled="updatingId === r.id"
              @click="confirmCancelId = r.id"
            >
              {{ updatingId === r.id ? 'Cancelling…' : 'Cancel reservation' }}
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>

  <ConfirmModal
    v-if="confirmCancelId"
    title="Cancel reservation?"
    message="Are you sure you want to cancel this reservation? This cannot be undone."
    confirm-label="Cancel reservation"
    cancel-label="Never mind"
    :danger="true"
    @confirm="confirmCancel"
    @cancel="confirmCancelId = null"
  />
</template>
