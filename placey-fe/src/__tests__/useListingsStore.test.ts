import { describe, it, expect, vi, beforeEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useListingsStore } from '@/stores/useListingsStore'
import type { Listing } from '@/types'

const makeListing = (id: string): Listing => ({
  id,
  title: 'Test Listing',
  description: 'A place',
  city: 'Berlin',
  country: 'Germany',
  location: 'Berlin, Germany',
  pricePerNight: 100,
  bedrooms: 2,
  bathrooms: 1,
  maxGuests: 4,
  isInstantBook: true,
  amenities: [],
  category: 'Urban',
  propertyType: 'Apartment',
  photoUrls: [],
  averageRating: null,
  reviewCount: 0,
  hostId: 'host-1',
  hostName: 'Host',
  hostAvatarUrl: null,
  createdAt: new Date().toISOString(),
})

beforeEach(() => {
  setActivePinia(createPinia())
  vi.restoreAllMocks()
})

describe('useListingsStore', () => {
  describe('fetchListings', () => {
    it('populates listings and total on success', async () => {
      const items = [makeListing('l1'), makeListing('l2')]
      vi.stubGlobal(
        'fetch',
        vi.fn().mockResolvedValue({
          ok: true,
          json: () => Promise.resolve({ items, total: 2 }),
        }),
      )
      const store = useListingsStore()

      await store.fetchListings()

      expect(store.listings).toHaveLength(2)
      expect(store.total).toBe(2)
      expect(store.error).toBeNull()
    })

    it('sets error on HTTP failure', async () => {
      vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: false, status: 500 }))
      const store = useListingsStore()

      await store.fetchListings()

      expect(store.error).toBe('HTTP 500')
      expect(store.listings).toHaveLength(0)
    })

    it('builds correct query string for location and guests', async () => {
      const fetchMock = vi.fn().mockResolvedValue({
        ok: true,
        json: () => Promise.resolve({ items: [], total: 0 }),
      })
      vi.stubGlobal('fetch', fetchMock)
      const store = useListingsStore()

      await store.fetchListings({ location: 'Berlin', guests: 3, page: 2, limit: 10 })

      const url: string = fetchMock.mock.calls[0][0]
      expect(url).toContain('location=Berlin')
      expect(url).toContain('guests=3')
      expect(url).toContain('page=2')
      expect(url).toContain('limit=10')
    })

    it('builds correct query string for optional filters', async () => {
      const fetchMock = vi.fn().mockResolvedValue({
        ok: true,
        json: () => Promise.resolve({ items: [], total: 0 }),
      })
      vi.stubGlobal('fetch', fetchMock)
      const store = useListingsStore()

      await store.fetchListings({
        category: 'Beach',
        propertyType: 'House',
        minBedrooms: 3,
        minPrice: 100,
        maxPrice: 500,
        checkIn: '2026-07-01',
        checkOut: '2026-07-07',
      })

      const url: string = fetchMock.mock.calls[0][0]
      expect(url).toContain('category=Beach')
      expect(url).toContain('propertyType=House')
      expect(url).toContain('minBedrooms=3')
      expect(url).toContain('minPrice=100')
      expect(url).toContain('maxPrice=500')
      expect(url).toContain('checkIn=2026-07-01')
      expect(url).toContain('checkOut=2026-07-07')
    })

    it('omits absent optional params from query string', async () => {
      const fetchMock = vi.fn().mockResolvedValue({
        ok: true,
        json: () => Promise.resolve({ items: [], total: 0 }),
      })
      vi.stubGlobal('fetch', fetchMock)
      const store = useListingsStore()

      await store.fetchListings({})

      const url: string = fetchMock.mock.calls[0][0]
      expect(url).not.toContain('location=')
      expect(url).not.toContain('guests=')
      expect(url).not.toContain('category=')
    })

    it('ignores AbortError and does not set error', async () => {
      const abortError = new DOMException('Aborted', 'AbortError')
      vi.stubGlobal('fetch', vi.fn().mockRejectedValue(abortError))
      const store = useListingsStore()

      await store.fetchListings()

      expect(store.error).toBeNull()
    })

    it('sets isLoading to false after completion', async () => {
      vi.stubGlobal(
        'fetch',
        vi.fn().mockResolvedValue({
          ok: true,
          json: () => Promise.resolve({ items: [], total: 0 }),
        }),
      )
      const store = useListingsStore()

      await store.fetchListings()

      expect(store.isLoading).toBe(false)
    })
  })
})
