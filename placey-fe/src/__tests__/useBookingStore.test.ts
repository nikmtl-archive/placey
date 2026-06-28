import { describe, it, expect, vi, beforeEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useBookingStore } from '@/stores/useBookingStore'
import { useAuthStore } from '@/stores/useAuthStore'
import type { Booking } from '@/types'

const makeBooking = (overrides: Partial<Booking> = {}): Booking => ({
  id: 'b1',
  listingId: 'l1',
  listingTitle: 'Beach House',
  listingPhotoUrl: null,
  guestId: 'u1',
  guestName: 'Alice',
  guestAvatarUrl: null,
  checkIn: '2026-08-01',
  checkOut: '2026-08-07',
  guests: 2,
  totalPrice: 600,
  status: 'Confirmed',
  createdAt: new Date().toISOString(),
  review: null,
  ...overrides,
})

function mockApiFetch(body: unknown, status = 200) {
  return vi.fn().mockResolvedValue({
    ok: status >= 200 && status < 300,
    status,
    json: () => Promise.resolve(body),
  })
}

beforeEach(() => {
  setActivePinia(createPinia())
  localStorage.clear()
  vi.restoreAllMocks()
})

describe('useBookingStore', () => {
  describe('fetchMyBookings', () => {
    it('populates bookings on success', async () => {
      const auth = useAuthStore()
      vi.spyOn(auth, 'apiFetch').mockImplementation(mockApiFetch([makeBooking()]))
      const store = useBookingStore()

      await store.fetchMyBookings()

      expect(store.bookings).toHaveLength(1)
      expect(store.error).toBeNull()
    })

    it('throws and sets error on failure', async () => {
      const auth = useAuthStore()
      vi.spyOn(auth, 'apiFetch').mockImplementation(mockApiFetch({ detail: 'Unauthorized' }, 401))
      const store = useBookingStore()

      await expect(store.fetchMyBookings()).rejects.toThrow()
      expect(store.error).toBe('Unauthorized')
    })

    it('handles non-array response gracefully', async () => {
      const auth = useAuthStore()
      vi.spyOn(auth, 'apiFetch').mockImplementation(mockApiFetch(null))
      const store = useBookingStore()

      await store.fetchMyBookings()

      expect(store.bookings).toEqual([])
    })
  })

  describe('createBooking', () => {
    it('returns booking and sets lastBooking on success', async () => {
      const booking = makeBooking()
      const auth = useAuthStore()
      vi.spyOn(auth, 'apiFetch').mockImplementation(mockApiFetch(booking))
      const store = useBookingStore()

      const result = await store.createBooking('l1', '2026-08-01', '2026-08-07', 2)

      expect(result.id).toBe('b1')
      expect(store.lastBooking?.id).toBe('b1')
    })

    it('throws and sets error on conflict', async () => {
      const auth = useAuthStore()
      vi.spyOn(auth, 'apiFetch').mockImplementation(
        mockApiFetch({ detail: 'Dates not available' }, 409),
      )
      const store = useBookingStore()

      await expect(store.createBooking('l1', '2026-08-01', '2026-08-07', 2)).rejects.toThrow(
        'Dates not available',
      )
      expect(store.error).toBe('Dates not available')
    })

    it('sets isSubmitting to false after completion', async () => {
      const auth = useAuthStore()
      vi.spyOn(auth, 'apiFetch').mockImplementation(mockApiFetch(makeBooking()))
      const store = useBookingStore()

      await store.createBooking('l1', '2026-08-01', '2026-08-07', 2)

      expect(store.isSubmitting).toBe(false)
    })
  })

  describe('cancelBooking', () => {
    it('sets booking status to Cancelled on success', async () => {
      const auth = useAuthStore()
      vi.spyOn(auth, 'apiFetch').mockImplementation(mockApiFetch(null, 204))
      const store = useBookingStore()
      store.bookings.push(makeBooking({ id: 'b1' }))

      await store.cancelBooking('b1')

      expect(store.bookings).toHaveLength(1)
      expect(store.bookings[0].status).toBe('Cancelled')
    })

    it('sets lastBooking status to Cancelled when the cancelled booking matches', async () => {
      const auth = useAuthStore()
      vi.spyOn(auth, 'apiFetch').mockImplementation(mockApiFetch(null, 204))
      const store = useBookingStore()
      const booking = makeBooking({ id: 'b1' })
      store.bookings.push(booking)
      store.lastBooking = booking

      await store.cancelBooking('b1')

      expect(store.lastBooking?.status).toBe('Cancelled')
    })

    it('throws and sets error on failure', async () => {
      const auth = useAuthStore()
      vi.spyOn(auth, 'apiFetch').mockImplementation(mockApiFetch({ detail: 'Not found' }, 404))
      const store = useBookingStore()

      await expect(store.cancelBooking('b-unknown')).rejects.toThrow()
      expect(store.error).toBeTruthy()
    })

    it('sets isCancelling to false after completion', async () => {
      const auth = useAuthStore()
      vi.spyOn(auth, 'apiFetch').mockImplementation(mockApiFetch(null, 204))
      const store = useBookingStore()
      store.bookings.push(makeBooking({ id: 'b1' }))

      await store.cancelBooking('b1')

      expect(store.isCancelling).toBe(false)
    })
  })

  describe('rateBooking', () => {
    it('refreshes booking list after successful review', async () => {
      // First call: POST /api/reviews (returns review), second call: GET /api/users/me/bookings
      const auth = useAuthStore()
      const apiFetchMock = vi
        .fn()
        .mockResolvedValueOnce({ ok: true, status: 201, json: () => Promise.resolve({}) })
        .mockResolvedValueOnce({ ok: true, status: 200, json: () => Promise.resolve([]) })
      vi.spyOn(auth, 'apiFetch').mockImplementation(apiFetchMock)
      const store = useBookingStore()

      await store.rateBooking('b1', 5, 'Great stay!')

      // apiFetch called for review POST then for bookings GET
      expect(apiFetchMock).toHaveBeenCalledTimes(2)
    })

    it('throws and sets error on failure', async () => {
      const auth = useAuthStore()
      vi.spyOn(auth, 'apiFetch').mockImplementation(
        mockApiFetch({ detail: 'Booking not completed' }, 422),
      )
      const store = useBookingStore()

      await expect(store.rateBooking('b1', 5, 'Great')).rejects.toThrow()
      expect(store.error).toBeTruthy()
    })

    it('sets isRating to false after completion', async () => {
      const auth = useAuthStore()
      vi.spyOn(auth, 'apiFetch').mockImplementation(mockApiFetch({}))
      const store = useBookingStore()
      vi.spyOn(store, 'fetchMyBookings').mockResolvedValue([])

      await store.rateBooking('b1', 4, 'Nice')

      expect(store.isRating).toBe(false)
    })
  })
})
