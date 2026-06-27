import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { Listing, Review, ListingBlock } from '@/types'

export const useListingStore = defineStore('listing', () => {
  const listing = ref<Listing | null>(null)
  const reviews = ref<Review[]>([])
  const blockings = ref<ListingBlock[]>([])

  const isLoading = ref(false)
  const reviewsLoading = ref(false)
  const blockingsLoading = ref(false)

  const error = ref<string | null>(null)
  const isDeleted = ref(false)

  async function fetchListing(id: string) {
    if (listing.value?.id === id) return
    isLoading.value = true
    error.value = null
    isDeleted.value = false
    listing.value = null
    try {
      const res = await fetch(`/api/listings/${id}`)
      if (res.status === 410) {
        isDeleted.value = true
        return
      }
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

  async function fetchBlockings(id: string) {
    blockingsLoading.value = true
    try {
      const res = await fetch(`/api/listings/${id}/blockings`)
      if (!res.ok) throw new Error()
      blockings.value = await res.json()
    } catch {
      blockings.value = []
    } finally {
      blockingsLoading.value = false
    }
  }

  function reset() {
    listing.value = null
    reviews.value = []
    blockings.value = []
    error.value = null
    isDeleted.value = false
  }

  return {
    listing,
    reviews,
    blockings,
    isLoading,
    reviewsLoading,
    blockingsLoading,
    error,
    isDeleted,
    fetchListing,
    fetchReviews,
    fetchBlockings,
    reset,
  }
})
