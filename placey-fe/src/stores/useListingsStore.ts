import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { Listing, ListingSearchParams, ListingsResponse } from '@/types'

function buildQueryString(params: ListingSearchParams): string {
  const q = new URLSearchParams()
  if (params.location) q.set('location', params.location)
  if (params.checkIn) q.set('checkIn', params.checkIn)
  if (params.checkOut) q.set('checkOut', params.checkOut)
  if (params.guests) q.set('guests', String(params.guests))
  if (params.category) q.set('category', params.category)
  if (params.propertyType) q.set('propertyType', params.propertyType)
  if (params.minBedrooms) q.set('minBedrooms', String(params.minBedrooms))
  if (params.minPrice) q.set('minPrice', String(params.minPrice))
  if (params.maxPrice) q.set('maxPrice', String(params.maxPrice))
  q.set('page', String(params.page ?? 1))
  q.set('limit', String(params.limit ?? 20))
  return q.toString()
}

export const useListingsStore = defineStore('listings', () => {
  const listings = ref<Listing[]>([])
  const total = ref(0)
  const isLoading = ref(false)
  const error = ref<string | null>(null)

  let controller: AbortController | null = null

  async function fetchListings(params: ListingSearchParams = {}) {
    controller?.abort()
    controller = new AbortController()
    isLoading.value = true
    error.value = null
    try {
      const res = await fetch(`/api/listings?${buildQueryString(params)}`, {
        signal: controller.signal,
      })
      if (!res.ok) throw new Error(`HTTP ${res.status}`)
      const data: ListingsResponse = await res.json()
      listings.value = data.items
      total.value = data.total
    } catch (e: unknown) {
      if ((e as Error).name !== 'AbortError') {
        error.value = e instanceof Error ? e.message : 'Failed to load listings'
      }
    } finally {
      isLoading.value = false
    }
  }

  return { listings, total, isLoading, error, fetchListings }
})
