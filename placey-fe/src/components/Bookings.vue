<!-- eslint-disable vue/multi-word-component-names -->
<script setup lang="ts">
import { ref } from 'vue'
import { Star, Image } from '@lucide/vue'
import { useRouter } from 'vue-router'
import type { Booking } from '@/types'
import { useBookingStore } from '@/stores/useBookingStore'
import ConfirmModal from '@/components/ConfirmModal.vue'

const props = defineProps<{
  bookings: Booking[]
}>()

const router = useRouter()
const bookingStore = useBookingStore()
const activeReviewBookingId = ref<string | null>(null)
const reviewRating = ref(5)
const reviewComment = ref('')
const reviewError = ref<string | null>(null)
const confirmCancelId = ref<string | null>(null)

function getNightCount(checkIn: string, checkOut: string) {
  const start = new Date(checkIn)
  const end = new Date(checkOut)
  return Math.max(0, Math.round((end.getTime() - start.getTime()) / (1000 * 60 * 60 * 24)))
}

async function cancelBooking() {
  if (!confirmCancelId.value) return
  const id = confirmCancelId.value
  confirmCancelId.value = null
  await bookingStore.cancelBooking(id)
}

function openListing(listingId: string) {
  router.push(`/listing/${listingId}`)
}

function openReviewForm(bookingId: string) {
  activeReviewBookingId.value = bookingId
  reviewRating.value = 5
  reviewComment.value = ''
  reviewError.value = null
}

function closeReviewForm() {
  activeReviewBookingId.value = null
  reviewComment.value = ''
  reviewError.value = null
}

async function submitReview(booking: Booking) {
  reviewError.value = null

  if (reviewRating.value < 1 || reviewRating.value > 5) {
    reviewError.value = 'Rating must be between 1 and 5.'
    return
  }

  const comment = reviewComment.value.trim()
  if (!comment) {
    reviewError.value = 'Please add a short comment.'
    return
  }

  try {
    await bookingStore.rateBooking(booking.id, reviewRating.value, comment)
    closeReviewForm()
  } catch {
    const message = bookingStore.error ?? 'Failed to submit review.'
    if (message.toLowerCase().includes('already exists')) {
      await bookingStore.fetchMyBookings()
      closeReviewForm()
      return
    }

    reviewError.value = message
  }
}
</script>

<template>
  <div class="py-6 space-y-4">
    <div v-if="props.bookings.length === 0" class="text-sm text-muted">No bookings to show.</div>
    <div v-else class="space-y-4">
      <div
        v-for="booking in props.bookings"
        :key="booking.id"
        class="rounded-3xl border border-neutral-200 p-4"
      >
        <button
          type="button"
          class="flex w-full gap-4 text-left cursor-pointer"
          @click="openListing(booking.listingId)"
        >
          <img
            v-if="booking.listingPhotoUrl"
            :src="booking.listingPhotoUrl"
            :alt="booking.listingTitle"
            class="h-28 w-28 rounded-xl object-cover"
          />
          <div
            v-else
            class="flex h-28 w-28 items-center justify-center rounded-xl bg-[#e8e3dc] text-sm text-[#b5afa8]"
          >
            <Image />
          </div>

          <div class="flex-1">
            <p class="font-medium text-sm leading-5 l">{{ booking.listingTitle }}</p>
            <p class="font-normal text-xs leading-4 text-neutral-500">
              {{ booking.checkIn }} - {{ booking.checkOut }} ·
              {{ getNightCount(booking.checkIn, booking.checkOut) }} nights ·
              {{ booking.guests }} guests
            </p>
            <p class="font-medium text-xs leading-4">${{ booking.totalPrice }} total</p>
          </div>

          <div>
            <span
              class="rounded-full border px-3 py-1 text-xs font-medium"
              :class="{
                'border-amber-300 bg-amber-50 text-amber-600': booking.status === 'Pending',
                'border-emerald-300 bg-emerald-50 text-emerald-600': booking.status === 'Confirmed',
                'border-sky-200 bg-sky-50 text-sky-600': booking.status === 'Completed',
                'border-neutral-300 bg-neutral-50 text-neutral-500': booking.status === 'Cancelled',
              }"
            >
              {{ booking.status }}
            </span>
          </div>
        </button>
        <div
          v-if="
            booking.status === 'Pending' ||
            (booking.status === 'Confirmed' && new Date(booking.checkOut) > new Date())
          "
          class="mt-4 border-t border-neutral-200 pt-3 -mr-4 -ml-4"
        >
          <button
            class="font-medium text-xs leading-4 text-center text-neutral-500 ml-4 mr-4 disabled:opacity-50"
            :disabled="bookingStore.isCancelling"
            @click="confirmCancelId = booking.id"
          >
            {{ bookingStore.isCancelling ? 'Cancelling…' : 'Cancel booking' }}
          </button>
        </div>
        <div
          v-if="booking.status === 'Completed' && booking.review"
          class="mt-4 border-t border-neutral-200 pt-3 -mr-4 -ml-4"
        >
          <div class="ml-4 mr-4 flex items-start justify-between gap-3">
            <div class="w-28 flex items-center gap-0.5 shrink-0">
              <Star
                v-for="s in 5"
                :key="s"
                :size="12"
                :class="
                  s <= booking.review.rating ? 'fill-yellow-400 text-yellow-500' : 'text-divider'
                "
              />
              <span class="ml-1 text-xs text-neutral-500">{{ booking.review.rating }}/5</span>
            </div>
            <p class="text-xs leading-4 text-neutral-500 text-left">{{ booking.review.comment }}</p>
          </div>
        </div>
        <div
          v-if="booking.status === 'Completed' && !booking.review"
          class="mt-4 border-t border-neutral-200 pt-3 -mr-4 -ml-4"
        >
          <div class="ml-4 mr-4 space-y-2">
            <button
              v-if="activeReviewBookingId !== booking.id"
              class="font-medium text-xs leading-4 text-center text-neutral-500"
              @click="openReviewForm(booking.id)"
            >
              Rate Booking
            </button>

            <form v-else class="space-y-2" @submit.prevent="submitReview(booking)">
              <div class="flex items-center gap-2">
                <label for="rating" class="text-xs text-neutral-600">Rating</label>
                <select
                  id="rating"
                  v-model.number="reviewRating"
                  class="rounded border border-neutral-300 px-2 py-1 text-xs"
                >
                  <option :value="5">5</option>
                  <option :value="4">4</option>
                  <option :value="3">3</option>
                  <option :value="2">2</option>
                  <option :value="1">1</option>
                </select>
              </div>

              <textarea
                v-model="reviewComment"
                rows="3"
                class="w-full rounded border border-neutral-300 px-2 py-1 text-xs"
                placeholder="Share a quick note about your stay"
              />

              <p v-if="reviewError" class="text-xs text-red-600">{{ reviewError }}</p>

              <div class="flex gap-2">
                <button
                  type="submit"
                  class="rounded border border-neutral-300 px-2 py-1 text-xs text-neutral-700 disabled:opacity-50"
                  :disabled="bookingStore.isRating"
                >
                  {{ bookingStore.isRating ? 'Submitting…' : 'Submit review' }}
                </button>
                <button
                  type="button"
                  class="rounded border border-neutral-200 px-2 py-1 text-xs text-neutral-500"
                  @click="closeReviewForm"
                >
                  Cancel
                </button>
              </div>
            </form>
          </div>
        </div>
      </div>
    </div>
  </div>

  <ConfirmModal
    v-if="confirmCancelId"
    title="Cancel booking?"
    message="Are you sure you want to cancel this booking? This cannot be undone."
    confirm-label="Cancel booking"
    cancel-label="Never mind"
    :danger="true"
    @confirm="cancelBooking"
    @cancel="confirmCancelId = null"
  />
</template>
