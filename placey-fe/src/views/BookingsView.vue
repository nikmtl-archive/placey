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
  const checkout = new Date(booking.checkOut)
  return checkout >= today && booking.status.toLowerCase() !== 'cancelled'
}

const upcomingBookings = computed(() => bookings.value.filter((booking) => isUpcoming(booking)))
const pastBookings = computed(() =>
  bookings.value.filter((booking) => booking.status.toLowerCase() === 'completed'),
)
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
  <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-10 py-10 lg:py-14">
    <div class="mb-8">
      <h1 class="font-serif text-3xl font-semibold text-ink mb-1">My bookings</h1>
      <p class="text-sm text-muted">Manage your trips and write reviews</p>
    </div>

    <div v-if="errorMessage" class="rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700 mb-4">
      {{ errorMessage }}
    </div>

    <!-- Skeleton -->
    <div
      v-if="isLoading"
      class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-5"
    >
      <div v-for="n in 8" :key="n" class="animate-pulse">
        <div class="aspect-4/3 rounded-3xl bg-divider-light mb-3" />
        <div class="h-3.5 bg-divider-light rounded w-3/4 mb-2" />
        <div class="h-3 bg-divider-light rounded w-1/2" />
      </div>
    </div>


  <div>
  <div class="flex gap-6 border-b border-gray-200">
    <button
      @click="activeTab = 'upcoming'"
      class="relative flex items-center gap-2 py-3 text-sm font-medium"
      :class="activeTab === 'upcoming' ? 'text-gray-900' : 'text-gray-500'"
    >
      Upcoming

      <span
        class="flex h-5 w-5 items-center justify-center rounded-full  text-xs" 
        :class="activeTab === 'upcoming' ? 'bg-gray-900! text-gray-200!' : 'text-gray-500! bg-gray-200!'"
      >
        {{ upcomingBookings.length }}
      </span>

      <div
        v-if="activeTab === 'upcoming'"
        class="absolute bottom-0 left-0 h-0.5 w-full bg-gray-900"
      />
    </button>

    <button
      @click="activeTab = 'past'"
      class="relative flex items-center gap-2 py-3 text-sm font-medium"
      :class="activeTab === 'past' ? 'text-gray-900' : 'text-gray-500'"
    >
      Past

      <span
        class="flex h-5 w-5 items-center justify-center rounded-full text-xs"
        :class="activeTab === 'past' ? 'bg-gray-900! text-gray-200!' : 'text-gray-500! bg-gray-200!'"
      >
        {{ pastBookings.length }}
      </span>

      <div
        v-if="activeTab === 'past'"
        class="absolute bottom-0 left-0 h-0.5 w-full bg-gray-900"
      />
    </button>
  </div>

  <Bookings
    v-if="activeTab === 'upcoming'"
    :bookings="upcomingBookings"
  />

  <Bookings
    v-else
    :bookings="pastBookings"
  />
</div>
</div>

</template>
