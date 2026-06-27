<script setup lang="ts">
import DateRangePicker from '@/components/DateRangePicker.vue'

interface Blocking {
  startDate: string
  endDate: string
}

defineProps<{
  pricePerNight: number
  maxGuests: number
  checkIn: string
  checkOut: string
  guests: number
  nights: number
  total: number
  datesReady: boolean
  isRangeBlocked: boolean
  bookingConfirmed: boolean
  isSubmitting: boolean
  bookingError: string | null
  isLoggedIn: boolean
  blockings: Blocking[]
  variant: 'desktop' | 'mobile'
}>()

const emit = defineEmits<{
  'update:checkIn': [value: string]
  'update:checkOut': [value: string]
  'update:guests': [value: number]
  reserve: []
}>()
</script>

<template>
  <div
    class="bg-white border border-divider shadow-[0_4px_32px_rgba(0,0,0,0.08)] p-6"
    :class="variant === 'desktop' ? 'rounded-3xl' : 'rounded-2xl'"
  >
    <!-- Price (desktop only) -->
    <div v-if="variant === 'desktop'" class="flex items-baseline gap-1 mb-5">
      <span class="text-2xl font-bold text-ink">${{ pricePerNight }}</span>
      <span class="text-sm text-muted font-normal">/ night</span>
    </div>

    <!-- Dates -->
    <div class="mb-3">
      <p class="text-[10px] font-semibold text-ink uppercase tracking-wider mb-1.5">Dates</p>
      <DateRangePicker
        :check-in="checkIn"
        :check-out="checkOut"
        :blockings="blockings"
        @update:check-in="emit('update:checkIn', $event)"
        @update:check-out="emit('update:checkOut', $event)"
      />
    </div>

    <!-- Guests -->
    <div class="border border-divider rounded-xl px-3 py-2.5 mb-4 text-sm">
      <p class="text-[10px] font-semibold text-ink uppercase tracking-wider mb-1">Guests</p>
      <div class="flex items-center justify-between">
        <span class="text-xs text-ink">{{ guests }} {{ guests === 1 ? 'guest' : 'guests' }}</span>
        <div class="flex items-center gap-2">
          <button
            class="w-6 h-6 rounded-full border border-divider flex items-center justify-center text-muted hover:border-ink hover:text-ink transition-colors cursor-pointer disabled:opacity-30"
            :disabled="guests <= 1"
            @click="emit('update:guests', Math.max(1, guests - 1))"
          >
            <span class="text-base leading-none mb-0.5">−</span>
          </button>
          <button
            class="w-6 h-6 rounded-full border border-divider flex items-center justify-center text-muted hover:border-ink hover:text-ink transition-colors cursor-pointer disabled:opacity-30"
            :disabled="guests >= maxGuests"
            @click="emit('update:guests', Math.min(maxGuests, guests + 1))"
          >
            <span class="text-base leading-none mb-0.5">+</span>
          </button>
        </div>
      </div>
    </div>

    <!-- Blocked / error alerts -->
    <div
      v-if="isRangeBlocked"
      class="mb-3 px-3 py-2.5 rounded-xl bg-rose-50 border border-rose-200 text-rose-700 text-xs font-medium"
    >
      These dates are not available. Please choose different dates.
    </div>
    <div
      v-if="bookingError"
      class="mb-3 px-3 py-2.5 rounded-xl bg-rose-50 border border-rose-200 text-rose-700 text-xs font-medium"
    >
      {{ bookingError }}
    </div>

    <!-- Reserve button -->
    <button
      class="w-full text-sm font-semibold py-3.5 rounded-xl transition-colors disabled:cursor-not-allowed"
      :class="
        !datesReady || isRangeBlocked || bookingConfirmed
          ? 'bg-gray-100 text-gray-400 cursor-default'
          : 'bg-ink text-white hover:bg-black cursor-pointer'
      "
      :disabled="!datesReady || isRangeBlocked || isSubmitting || bookingConfirmed"
      @click="emit('reserve')"
    >
      {{
        isSubmitting
          ? 'Reserving…'
          : !datesReady
            ? 'Add dates to check availability'
            : isLoggedIn
              ? 'Reserve'
              : 'Log in to reserve'
      }}
    </button>

    <!-- Price summary -->
    <div
      v-if="datesReady && !isRangeBlocked"
      class="mt-5 pt-5 border-t border-divider-light text-sm"
    >
      <div class="flex justify-between font-semibold text-ink">
        <span>${{ pricePerNight }} × {{ nights }} {{ nights === 1 ? 'night' : 'nights' }}</span>
        <span>${{ total }}</span>
      </div>
    </div>

    <p class="text-xs text-muted text-center mt-3">You won't be charged yet</p>
  </div>
</template>
