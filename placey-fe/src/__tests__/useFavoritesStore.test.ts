import { describe, it, expect, vi, beforeEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useFavoritesStore } from '@/stores/useFavoritesStore'
import { useAuthStore } from '@/stores/useAuthStore'

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

describe('useFavoritesStore', () => {
  describe('isFavorited', () => {
    it('returns false when list is empty', () => {
      const store = useFavoritesStore()
      expect(store.isFavorited('listing-1')).toBe(false)
    })

    it('returns true when id is in list', () => {
      const store = useFavoritesStore()
      store.favoriteIds.push('listing-1')
      expect(store.isFavorited('listing-1')).toBe(true)
    })
  })

  describe('hydrate', () => {
    it('populates favoriteIds from API', async () => {
      const auth = useAuthStore()
      vi.spyOn(auth, 'apiFetch').mockImplementation(mockApiFetch([{ id: 'l1' }, { id: 'l2' }]))
      const store = useFavoritesStore()

      await store.hydrate()

      expect(store.favoriteIds).toEqual(['l1', 'l2'])
      expect(store.isLoading).toBe(false)
    })

    it('sets error on API failure', async () => {
      const auth = useAuthStore()
      vi.spyOn(auth, 'apiFetch').mockImplementation(mockApiFetch({}, 500))
      const store = useFavoritesStore()

      await store.hydrate()

      expect(store.error).toBeTruthy()
      expect(store.isLoading).toBe(false)
    })
  })

  describe('toggleFavorite', () => {
    it('adds listing optimistically', async () => {
      const auth = useAuthStore()
      vi.spyOn(auth, 'apiFetch').mockImplementation(mockApiFetch(null, 200))
      const store = useFavoritesStore()

      const toggle = store.toggleFavorite('listing-1')
      expect(store.favoriteIds).toContain('listing-1')
      await toggle
      expect(store.favoriteIds).toContain('listing-1')
    })

    it('removes listing optimistically', async () => {
      const auth = useAuthStore()
      vi.spyOn(auth, 'apiFetch').mockImplementation(mockApiFetch(null, 200))
      const store = useFavoritesStore()
      store.favoriteIds.push('listing-1')

      const toggle = store.toggleFavorite('listing-1')
      expect(store.favoriteIds).not.toContain('listing-1')
      await toggle
      expect(store.favoriteIds).not.toContain('listing-1')
    })

    it('rolls back add on API error', async () => {
      const auth = useAuthStore()
      vi.spyOn(auth, 'apiFetch').mockImplementation(mockApiFetch({}, 500))
      const store = useFavoritesStore()

      await store.toggleFavorite('listing-1')

      expect(store.favoriteIds).not.toContain('listing-1')
      expect(store.error).toBeTruthy()
    })

    it('rolls back remove on API error', async () => {
      const auth = useAuthStore()
      vi.spyOn(auth, 'apiFetch').mockImplementation(mockApiFetch({}, 500))
      const store = useFavoritesStore()
      store.favoriteIds.push('listing-1')

      await store.toggleFavorite('listing-1')

      expect(store.favoriteIds).toContain('listing-1')
    })
  })

  describe('reset', () => {
    it('clears favoriteIds and error', () => {
      const store = useFavoritesStore()
      store.favoriteIds.push('l1')
      store.error = 'some error'

      store.reset()

      expect(store.favoriteIds).toEqual([])
      expect(store.error).toBeNull()
    })
  })
})
