<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import type { Booking } from '@/types'
import { useBookingStore } from '@/stores/useBookingStore'
import Bookings from '@/components/Bookings.vue'

const bookingStore = useBookingStore()
const isLoading = ref(false)
const activeTab = ref<'upcoming' | 'past'>('upcoming')

const bookings = computed<Booking[]>(() => bookingStore.bookings)

function isUpcoming(booking: Booking) {
  const today = new Date()
  today.setHours(0, 0, 0, 0)
  const checkout = new Date(booking.checkOut + 'T00:00:00')
  return checkout >= today
}

const upcomingBookings = computed(() => bookings.value.filter((b) => isUpcoming(b)))
const pastBookings = computed(() => bookings.value.filter((b) => !isUpcoming(b)))
const errorMessage = computed(() => bookingStore.error)

onMounted(async () => {
  isLoading.value = true
  try {
    await bookingStore.fetchMyBookings()
  } finally {
    isLoading.value = false
  }
})
</script>

<template>
  <div class="max-w-3xl mx-auto px-4 sm:px-6 lg:px-10 py-10 lg:py-14">
    <div class="mb-8">
      <h1 class="font-serif text-3xl font-semibold text-ink mb-1">My bookings</h1>
      <p class="text-sm text-muted">Manage your trips and write reviews</p>
    </div>

    <!-- Error -->
    <div
      v-if="errorMessage"
      class="rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700 mb-6"
    >
      {{ errorMessage }}
    </div>

    <!-- Tabs -->
    <div class="flex gap-6 border-b border-divider-light mb-2">
      <button
        @click="activeTab = 'upcoming'"
        class="relative flex items-center gap-2 py-3 text-sm font-medium transition-colors cursor-pointer"
        :class="activeTab === 'upcoming' ? 'text-ink' : 'text-muted'"
      >
        Upcoming
        <span
          class="flex h-5 w-5 items-center justify-center rounded-full text-xs"
          :class="activeTab === 'upcoming' ? 'bg-ink text-white' : 'bg-black/6 text-muted'"
        >
          {{ upcomingBookings.length }}
        </span>
        <div v-if="activeTab === 'upcoming'" class="absolute bottom-0 left-0 h-0.5 w-full bg-ink" />
      </button>

      <button
        @click="activeTab = 'past'"
        class="relative flex items-center gap-2 py-3 text-sm font-medium transition-colors cursor-pointer"
        :class="activeTab === 'past' ? 'text-ink' : 'text-muted'"
      >
        Past
        <span
          class="flex h-5 w-5 items-center justify-center rounded-full text-xs"
          :class="activeTab === 'past' ? 'bg-ink text-white' : 'bg-black/6 text-muted'"
        >
          {{ pastBookings.length }}
        </span>
        <div v-if="activeTab === 'past'" class="absolute bottom-0 left-0 h-0.5 w-full bg-ink" />
      </button>
    </div>

    <!-- Skeleton -->
    <div v-if="isLoading" class="space-y-4 pt-4">
      <div
        v-for="n in 3"
        :key="n"
        class="animate-pulse flex gap-4 border border-divider-light rounded-2xl p-4"
      >
        <div class="w-28 h-28 rounded-xl bg-divider-light shrink-0" />
        <div class="flex-1 space-y-2 py-1">
          <div class="h-4 bg-divider-light rounded w-2/3" />
          <div class="h-3 bg-divider-light rounded w-1/3" />
          <div class="h-3 bg-divider-light rounded w-1/2" />
        </div>
      </div>
    </div>

    <!-- Empty state -->
    <div
      v-else-if="(activeTab === 'upcoming' ? upcomingBookings : pastBookings).length === 0"
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
        <rect x="3" y="4" width="18" height="18" rx="2" ry="2" />
        <line x1="16" y1="2" x2="16" y2="6" />
        <line x1="8" y1="2" x2="8" y2="6" />
        <line x1="3" y1="10" x2="21" y2="10" />
      </svg>
      <p class="font-serif text-2xl text-ink mb-2">
        {{ activeTab === 'upcoming' ? 'No upcoming trips' : 'No past bookings' }}
      </p>
      <p class="text-sm text-muted mb-6 max-w-xs">
        {{
          activeTab === 'upcoming'
            ? 'Book a place and it will show up here.'
            : 'Completed and cancelled bookings appear here.'
        }}
      </p>
      <RouterLink
        v-if="activeTab === 'upcoming'"
        to="/search"
        class="text-sm font-medium text-white bg-ink px-6 py-2.5 rounded-full hover:bg-black transition-colors"
      >
        Browse places
      </RouterLink>
    </div>

    <!-- Booking list via Michele's inline component -->
    <Bookings v-else :bookings="activeTab === 'upcoming' ? upcomingBookings : pastBookings" />
  </div>
</template>
