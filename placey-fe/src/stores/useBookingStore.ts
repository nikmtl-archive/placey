import { defineStore } from 'pinia'
import { ref } from 'vue'
import { useAuthStore } from './useAuthStore'

export interface BookingResponse {
  id: string
  listingId: string
  listingTitle: string
  guestId: string
  guestName: string
  checkIn: string
  checkOut: string
  guests: number
  totalPrice: number
  status: string
  createdAt: string
}

export const useBookingStore = defineStore('booking', () => {
  const authStore = useAuthStore()

  const isSubmitting = ref(false)
  const error = ref<string | null>(null)
  const lastBooking = ref<BookingResponse | null>(null)

  async function createBooking(
    listingId: string,
    checkIn: string,
    checkOut: string,
    guests: number,
  ): Promise<BookingResponse> {
    isSubmitting.value = true
    error.value = null
    try {
      const res = await authStore.apiFetch('/api/bookings', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ listingId, checkIn, checkOut, guests }),
      })
      if (!res.ok) {
        const body = await res.json().catch(() => ({}))
        throw new Error(body.detail ?? body.message ?? `HTTP ${res.status}`)
      }
      lastBooking.value = await res.json()
      return lastBooking.value!
    } catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Booking failed'
      throw e
    } finally {
      isSubmitting.value = false
    }
  }

  return { isSubmitting, error, lastBooking, createBooking }
})
