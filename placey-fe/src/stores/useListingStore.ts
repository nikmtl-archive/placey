import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { Listing, Review, AvailabilityRange } from '@/types'

export const useListingStore = defineStore('listing', () => {
  const listing     = ref<Listing | null>(null)
  const reviews     = ref<Review[]>([])
  const availability = ref<AvailabilityRange[]>([])

  const isLoading          = ref(false)
  const reviewsLoading     = ref(false)
  const availabilityLoading = ref(false)

  const error = ref<string | null>(null)

  async function fetchListing(id: string) {
    if (listing.value?.id === id) return
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

  async function fetchReviews(id: string) {
    reviewsLoading.value = true
    try {
      const res = await fetch(`/api/listings/${id}/reviews`)
      if (!res.ok) throw new Error()
      reviews.value = await res.json()
    } catch {
      reviews.value = []
    } finally {
      reviewsLoading.value = false
    }
  }

  async function fetchAvailability(id: string) {
    availabilityLoading.value = true
    try {
      const res = await fetch(`/api/listings/${id}/availability`)
      if (!res.ok) throw new Error()
      availability.value = await res.json()
    } catch {
      availability.value = []
    } finally {
      availabilityLoading.value = false
    }
  }

  function reset() {
    listing.value = null
    reviews.value = []
    availability.value = []
    error.value = null
  }

  return {
    listing, reviews, availability,
    isLoading, reviewsLoading, availabilityLoading,
    error,
    fetchListing, fetchReviews, fetchAvailability, reset,
  }
})
