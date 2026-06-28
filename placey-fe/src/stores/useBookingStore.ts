import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { Booking } from '@/types'
import { useAuthStore, parseErrorMessage } from './useAuthStore'

type BookingReviewFallback = {
  id: string
  rating: number
  comment: string
  createdAt: string
}

export const useBookingStore = defineStore('booking', () => {
  const authStore = useAuthStore()

  const isSubmitting = ref(false)
  const isCancelling = ref(false)
  const isRating = ref(false)
  const error = ref<string | null>(null)
  const bookings = ref<Booking[]>([])
  const lastBooking = ref<Booking | null>(null)

  async function fetchMyBookings(): Promise<Booking[]> {
    error.value = null
    try {
      const res = await authStore.apiFetch('/api/users/me/bookings')
      if (!res.ok) throw new Error(await parseErrorMessage(res))

      const data = await res.json()
      bookings.value = Array.isArray(data) ? data : []

      // Fallback hydration for environments where the embedded review is missing.
      const completedWithoutReview = bookings.value.filter(
        (booking) => booking.status === 'Completed' && !booking.review,
      )

      await Promise.all(
        completedWithoutReview.map(async (booking) => {
          const reviewRes = await authStore
            .apiFetch(`/api/bookings/${booking.id}/review`)
            .catch(() => null)

          if (!reviewRes || !reviewRes.ok) return

          const review = (await reviewRes.json()) as BookingReviewFallback
          booking.review = {
            id: review.id,
            listingId: booking.listingId,
            authorId: booking.guestId,
            authorName: booking.guestName,
            authorAvatarUrl: booking.guestAvatarUrl,
            rating: review.rating,
            comment: review.comment,
            createdAt: review.createdAt,
          }
        }),
      )

      return bookings.value
    } catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to load bookings'
      throw e
    }
  }

  async function createBooking(
    listingId: string,
    checkIn: string,
    checkOut: string,
    guests: number,
  ): Promise<Booking> {
    isSubmitting.value = true
    error.value = null
    try {
      const res = await authStore.apiFetch('/api/bookings', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ listingId, checkIn, checkOut, guests }),
      })
      if (!res.ok) throw new Error(await parseErrorMessage(res))
      lastBooking.value = await res.json()
      return lastBooking.value!
    } catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Booking failed'
      throw e
    } finally {
      isSubmitting.value = false
    }
  }

  async function cancelBooking(bookingId: string) {
    isCancelling.value = true
    error.value = null
    try {
      const res = await authStore.apiFetch(`/api/bookings/${bookingId}`, {
        method: 'DELETE',
      })
      if (!res.ok) throw new Error(await parseErrorMessage(res))

      const booking = bookings.value.find((b) => b.id === bookingId)
      if (booking) booking.status = 'Cancelled'
      if (lastBooking.value?.id === bookingId) lastBooking.value.status = 'Cancelled'
    } catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to cancel booking'
      throw e
    } finally {
      isCancelling.value = false
    }
  }

  async function rateBooking(bookingId: string, rating: number, comment: string) {
    isRating.value = true
    error.value = null
    try {
      const res = await authStore.apiFetch('/api/reviews', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ bookingId, rating, comment }),
      })

      if (!res.ok) throw new Error(await parseErrorMessage(res))

      // Keep booking cards in sync so the new review appears on the right booking.
      await fetchMyBookings()
    } catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to submit review'
      throw e
    } finally {
      isRating.value = false
    }
  }

  return {
    isSubmitting,
    isCancelling,
    isRating,
    error,
    bookings,
    lastBooking,
    fetchMyBookings,
    createBooking,
    cancelBooking,
    rateBooking,
  }
})
