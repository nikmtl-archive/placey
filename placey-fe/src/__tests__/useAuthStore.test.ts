import { describe, it, expect, vi, beforeEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useAuthStore } from '@/stores/useAuthStore'

const mockSession = {
  user: { id: 'u1', email: 'alice@example.com', name: 'Alice', bio: null, avatarUrl: null },
  accessToken: 'access-token',
  refreshToken: 'refresh-token',
}

function mockFetch(body: unknown, status = 200) {
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

describe('useAuthStore', () => {
  describe('initial state', () => {
    it('starts logged out when localStorage is empty', () => {
      const store = useAuthStore()
      expect(store.isLoggedIn).toBe(false)
      expect(store.user).toBeNull()
    })

    it('restores session from localStorage', () => {
      localStorage.setItem('placey_session', JSON.stringify(mockSession))
      const store = useAuthStore()
      expect(store.isLoggedIn).toBe(true)
      expect(store.user?.email).toBe('alice@example.com')
      expect(store.accessToken).toBe('access-token')
    })

    it('ignores corrupt localStorage data', () => {
      localStorage.setItem('placey_session', 'not-json}}}')
      const store = useAuthStore()
      expect(store.isLoggedIn).toBe(false)
    })
  })

  describe('login', () => {
    it('sets session on success', async () => {
      vi.stubGlobal('fetch', mockFetch(mockSession))
      const store = useAuthStore()
      // stub side-effect calls so they don't throw
      vi.spyOn(store, 'checkIsHost').mockResolvedValue()
      vi.spyOn(store, 'fetchPendingReservationsCount').mockResolvedValue()

      await store.login('alice@example.com', 'password123')

      expect(store.isLoggedIn).toBe(true)
      expect(store.user?.email).toBe('alice@example.com')
      expect(store.error).toBeNull()
    })

    it('sets error on HTTP failure', async () => {
      vi.stubGlobal('fetch', mockFetch({ detail: 'Invalid credentials' }, 401))
      const store = useAuthStore()

      await store.login('alice@example.com', 'wrong')

      expect(store.isLoggedIn).toBe(false)
      expect(store.error).toBe('Invalid credentials')
    })

    it('sets isLoading to false after completion', async () => {
      vi.stubGlobal('fetch', mockFetch(mockSession))
      const store = useAuthStore()
      vi.spyOn(store, 'checkIsHost').mockResolvedValue()
      vi.spyOn(store, 'fetchPendingReservationsCount').mockResolvedValue()

      await store.login('alice@example.com', 'password123')

      expect(store.isLoading).toBe(false)
    })

    it('persists session to localStorage', async () => {
      vi.stubGlobal('fetch', mockFetch(mockSession))
      const store = useAuthStore()
      vi.spyOn(store, 'checkIsHost').mockResolvedValue()
      vi.spyOn(store, 'fetchPendingReservationsCount').mockResolvedValue()

      await store.login('alice@example.com', 'password123')

      const stored = JSON.parse(localStorage.getItem('placey_session')!)
      expect(stored.accessToken).toBe('access-token')
    })
  })

  describe('register', () => {
    it('sets session on success', async () => {
      vi.stubGlobal('fetch', mockFetch(mockSession))
      const store = useAuthStore()
      vi.spyOn(store, 'checkIsHost').mockResolvedValue()
      vi.spyOn(store, 'fetchPendingReservationsCount').mockResolvedValue()

      await store.register('alice@example.com', 'password123', 'Alice')

      expect(store.isLoggedIn).toBe(true)
    })

    it('sets error on duplicate email', async () => {
      vi.stubGlobal('fetch', mockFetch({ detail: 'Email already in use' }, 409))
      const store = useAuthStore()

      await store.register('alice@example.com', 'password123', 'Alice')

      expect(store.error).toBe('Email already in use')
    })
  })

  describe('logout', () => {
    it('clears session and localStorage', async () => {
      localStorage.setItem('placey_session', JSON.stringify(mockSession))
      vi.stubGlobal('fetch', mockFetch(null, 204))
      const store = useAuthStore()

      await store.logout()

      expect(store.isLoggedIn).toBe(false)
      expect(store.user).toBeNull()
      expect(localStorage.getItem('placey_session')).toBeNull()
    })

    it('clears session even if the API call throws', async () => {
      localStorage.setItem('placey_session', JSON.stringify(mockSession))
      vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new Error('Network error')))
      const store = useAuthStore()

      // logout propagates the network error but the finally block still clears state
      await store.logout().catch(() => {})

      expect(store.isLoggedIn).toBe(false)
    })
  })

  describe('patchUser', () => {
    it('merges fields and persists to localStorage', () => {
      localStorage.setItem('placey_session', JSON.stringify(mockSession))
      const store = useAuthStore()

      store.patchUser({ name: 'Alicia', bio: 'Traveler' })

      expect(store.user?.name).toBe('Alicia')
      expect(store.user?.bio).toBe('Traveler')
      const stored = JSON.parse(localStorage.getItem('placey_session')!)
      expect(stored.user.name).toBe('Alicia')
    })

    it('does nothing when not logged in', () => {
      const store = useAuthStore()
      store.patchUser({ name: 'Ghost' })
      expect(store.user).toBeNull()
    })
  })

  describe('refreshSession', () => {
    it('updates tokens on success', async () => {
      const newSession = { ...mockSession, accessToken: 'new-access', refreshToken: 'new-refresh' }
      vi.stubGlobal('fetch', mockFetch(newSession))
      localStorage.setItem('placey_session', JSON.stringify(mockSession))
      const store = useAuthStore()

      const ok = await store.refreshSession()

      expect(ok).toBe(true)
      expect(store.accessToken).toBe('new-access')
    })

    it('returns false when no refresh token', async () => {
      const store = useAuthStore()

      const ok = await store.refreshSession()

      expect(ok).toBe(false)
    })

    it('deduplicates concurrent refresh calls', async () => {
      const newSession = { ...mockSession, accessToken: 'new-access', refreshToken: 'new-refresh' }
      const fetchMock = mockFetch(newSession)
      vi.stubGlobal('fetch', fetchMock)
      localStorage.setItem('placey_session', JSON.stringify(mockSession))
      const store = useAuthStore()

      await Promise.all([store.refreshSession(), store.refreshSession()])

      expect(fetchMock).toHaveBeenCalledTimes(1)
    })
  })

  describe('authHeaders', () => {
    it('returns empty object when not logged in', () => {
      const store = useAuthStore()
      expect(store.authHeaders()).toEqual({})
    })

    it('returns Authorization header when logged in', () => {
      localStorage.setItem('placey_session', JSON.stringify(mockSession))
      const store = useAuthStore()
      expect(store.authHeaders()).toEqual({ Authorization: 'Bearer access-token' })
    })
  })

  describe('apiFetch', () => {
    it('retries with refreshed token on 401', async () => {
      const newSession = { ...mockSession, accessToken: 'new-access', refreshToken: 'new-refresh' }
      const fetchMock = vi
        .fn()
        .mockResolvedValueOnce({ ok: false, status: 401, json: () => Promise.resolve({}) })
        .mockResolvedValueOnce({ ok: true, status: 200, json: () => Promise.resolve(newSession) }) // refresh call
        .mockResolvedValueOnce({
          ok: true,
          status: 200,
          json: () => Promise.resolve({ data: 'ok' }),
        })
      vi.stubGlobal('fetch', fetchMock)
      localStorage.setItem('placey_session', JSON.stringify(mockSession))
      const store = useAuthStore()

      const res = await store.apiFetch('/api/something')

      expect(res.ok).toBe(true)
      expect(fetchMock).toHaveBeenCalledTimes(3)
    })

    it('clears session when refresh fails on 401', async () => {
      const fetchMock = vi
        .fn()
        .mockResolvedValueOnce({ ok: false, status: 401, json: () => Promise.resolve({}) })
        .mockResolvedValueOnce({ ok: false, status: 401, json: () => Promise.resolve({}) }) // refresh fails
      vi.stubGlobal('fetch', fetchMock)
      localStorage.setItem('placey_session', JSON.stringify(mockSession))
      const store = useAuthStore()

      await store.apiFetch('/api/something')

      expect(store.isLoggedIn).toBe(false)
    })
  })
})
