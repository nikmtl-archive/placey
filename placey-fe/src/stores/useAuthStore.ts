import { defineStore } from 'pinia'
import { ref, computed } from 'vue'

export interface AuthUser {
  id: string
  email: string
  name: string
  bio: string | null
  avatarUrl: string | null
  isHost?: boolean
}

const LS_KEY = 'placey_session'

export const useAuthStore = defineStore('auth', () => {
  const user = ref<AuthUser | null>(null)
  const accessToken = ref<string | null>(null)
  const refreshToken = ref<string | null>(null)
  const isLoading = ref(false)
  const error = ref<string | null>(null)

  // Restore session from localStorage synchronously on store creation
  const stored = localStorage.getItem(LS_KEY)
  if (stored) {
    try {
      const parsed = JSON.parse(stored)
      user.value = parsed.user
      accessToken.value = parsed.accessToken
      refreshToken.value = parsed.refreshToken
    } catch {
      /* corrupt storage — ignore */
    }
  }

  const isLoggedIn = computed(() => !!accessToken.value)
  const isHost = ref(false)
  const pendingReservationsCount = ref(0)

  async function fetchPendingReservationsCount() {
    if (!accessToken.value) return
    try {
      const res = await apiFetch('/api/users/me/reservations')
      if (!res.ok) return
      const data: { status: string }[] = await res.json()
      pendingReservationsCount.value = data.filter((r) => r.status === 'Pending').length
    } catch {
      // silently ignore
    }
  }

  async function checkIsHost() {
    if (!accessToken.value) {
      isHost.value = false
      return
    }
    try {
      const res = await apiFetch('/api/users/me/listings')
      if (!res.ok) {
        isHost.value = false
        return
      }
      const data = await res.json()
      isHost.value = Array.isArray(data) ? data.length > 0 : (data.total ?? 0) > 0
    } catch {
      isHost.value = false
    }
  }

  // Serialises concurrent refresh attempts so only one request goes out
  let _refreshPromise: Promise<boolean> | null = null

  async function refreshSession(): Promise<boolean> {
    if (_refreshPromise) return _refreshPromise
    _refreshPromise = (async () => {
      if (!refreshToken.value) return false
      try {
        const res = await fetch('/api/auth/refresh', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ refreshToken: refreshToken.value }),
        })
        if (!res.ok) return false
        setSession(await res.json())
        return true
      } catch {
        return false
      } finally {
        _refreshPromise = null
      }
    })()
    return _refreshPromise
  }

  // Drop-in fetch wrapper: retries once after a transparent token refresh on 401
  async function apiFetch(input: RequestInfo, init: RequestInit = {}): Promise<Response> {
    const makeHeaders = () => ({
      ...(init.headers as Record<string, string> | undefined),
      ...authHeaders(),
    })

    let res = await fetch(input, { ...init, headers: makeHeaders() })

    if (res.status === 401 && refreshToken.value) {
      const refreshed = await refreshSession()
      if (refreshed) {
        res = await fetch(input, { ...init, headers: makeHeaders() })
      } else {
        // Refresh failed — clear stale session so UI reflects logged-out state
        user.value = null
        accessToken.value = null
        refreshToken.value = null
        localStorage.removeItem(LS_KEY)
      }
    }

    return res
  }

  function setSession(data: { user: AuthUser; accessToken: string; refreshToken: string }) {
    user.value = data.user
    accessToken.value = data.accessToken
    refreshToken.value = data.refreshToken
    error.value = null
    localStorage.setItem(LS_KEY, JSON.stringify(data))
  }

  async function login(email: string, password: string) {
    isLoading.value = true
    error.value = null
    try {
      const res = await fetch('/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password }),
      })
      if (!res.ok) {
        const body = await res.json().catch(() => ({}))
        throw new Error(body.detail ?? body.message ?? `HTTP ${res.status}`)
      }
      setSession(await res.json())
      checkIsHost()
      fetchPendingReservationsCount()
    } catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Login failed'
    } finally {
      isLoading.value = false
    }
  }

  async function register(email: string, password: string, name: string) {
    isLoading.value = true
    error.value = null
    try {
      const res = await fetch('/api/auth/register', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password, name }),
      })
      if (!res.ok) {
        const body = await res.json().catch(() => ({}))
        throw new Error(body.detail ?? body.message ?? `HTTP ${res.status}`)
      }
      setSession(await res.json())
      checkIsHost()
      fetchPendingReservationsCount()
    } catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Registration failed'
    } finally {
      isLoading.value = false
    }
  }

  async function logout() {
    try {
      if (accessToken.value) {
        await fetch('/api/auth/logout', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            Authorization: `Bearer ${accessToken.value}`,
          },
          body: JSON.stringify({ refreshToken: refreshToken.value }),
        })
      }
    } finally {
      user.value = null
      accessToken.value = null
      refreshToken.value = null
      localStorage.removeItem(LS_KEY)
    }
  }

  function authHeaders(): Record<string, string> {
    if (!accessToken.value) return {}
    return { Authorization: `Bearer ${accessToken.value}` }
  }

  function patchUser(partial: Partial<AuthUser>) {
    if (!user.value) return
    user.value = { ...user.value, ...partial }
    localStorage.setItem(
      LS_KEY,
      JSON.stringify({
        user: user.value,
        accessToken: accessToken.value,
        refreshToken: refreshToken.value,
      }),
    )
  }

  return {
    user,
    accessToken,
    refreshToken,
    isLoading,
    error,
    isLoggedIn,
    isHost,
    pendingReservationsCount,
    checkIsHost,
    fetchPendingReservationsCount,
    login,
    register,
    logout,
    authHeaders,
    refreshSession,
    apiFetch,
    patchUser,
  }
})
