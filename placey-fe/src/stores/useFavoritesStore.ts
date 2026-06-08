import { defineStore } from 'pinia'
import { ref } from 'vue'
import { useAuthStore } from './useAuthStore'

export const useFavoritesStore = defineStore('favorites', () => {
  const favoriteIds = ref<string[]>([])
  const isLoading = ref(false)
  const error = ref<string | null>(null)

  function isFavorited(listingId: string): boolean {
    return favoriteIds.value.includes(listingId)
  }

  async function hydrate() {
    const authStore = useAuthStore()
    isLoading.value = true
    error.value = null
    try {
      const res = await authStore.apiFetch('/api/favorites')
      if (!res.ok) throw new Error(`HTTP ${res.status}`)
      const data = await res.json()
      favoriteIds.value = data.map((l: { id: string }) => l.id)
    } catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to load favorites'
    } finally {
      isLoading.value = false
    }
  }

  async function toggleFavorite(listingId: string) {
    const authStore = useAuthStore()
    const wasFavorited = isFavorited(listingId)

    // Optimistic update
    if (wasFavorited) {
      favoriteIds.value = favoriteIds.value.filter((id) => id !== listingId)
    } else {
      favoriteIds.value = [...favoriteIds.value, listingId]
    }

    try {
      const res = await authStore.apiFetch(`/api/favorites/${listingId}`, {
        method: wasFavorited ? 'DELETE' : 'POST',
      })
      if (!res.ok) throw new Error(`HTTP ${res.status}`)
    } catch (e: unknown) {
      // Revert optimistic update on failure
      if (wasFavorited) {
        favoriteIds.value = [...favoriteIds.value, listingId]
      } else {
        favoriteIds.value = favoriteIds.value.filter((id) => id !== listingId)
      }
      error.value = e instanceof Error ? e.message : 'Failed to update favorite'
    }
  }

  function reset() {
    favoriteIds.value = []
    error.value = null
  }

  return { favoriteIds, isLoading, error, isFavorited, hydrate, toggleFavorite, reset }
})
