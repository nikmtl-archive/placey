import { ref } from 'vue'
import type { Listing } from '@/types'

export function useListing() {
  const listing = ref<Listing | null>(null)
  const isLoading = ref(false)
  const error = ref<string | null>(null)

  async function fetchListing(id: string) {
    isLoading.value = true
    error.value = null
    listing.value = null
    try {
      const res = await fetch(`/api/listings/${id}`)
      if (!res.ok) throw new Error(`HTTP ${res.status}`)
      listing.value = await res.json()
    } catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to load listing'
    } finally {
      isLoading.value = false
    }
  }

  return { listing, isLoading, error, fetchListing }
}
