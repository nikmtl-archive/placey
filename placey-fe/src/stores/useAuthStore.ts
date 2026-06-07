import { defineStore } from 'pinia'
import { ref, computed } from 'vue'

export interface AuthUser {
  id: string
  email: string
  name: string
  bio: string | null
  avatarUrl: string | null
}

export const useAuthStore = defineStore('auth', () => {
  const user         = ref<AuthUser | null>(null)
  const accessToken  = ref<string | null>(null)
  const refreshToken = ref<string | null>(null)
  const isLoading    = ref(false)
  const error        = ref<string | null>(null)

  const isLoggedIn = computed(() => !!accessToken.value)

  function setSession(data: { user: AuthUser; accessToken: string; refreshToken: string }) {
    user.value         = data.user
    accessToken.value  = data.accessToken
    refreshToken.value = data.refreshToken
    error.value        = null
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
        throw new Error(body.message ?? `HTTP ${res.status}`)
      }
      setSession(await res.json())
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
        throw new Error(body.message ?? `HTTP ${res.status}`)
      }
      setSession(await res.json())
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
            'Authorization': `Bearer ${accessToken.value}`,
          },
          body: JSON.stringify({ refreshToken: refreshToken.value }),
        })
      }
    } finally {
      user.value         = null
      accessToken.value  = null
      refreshToken.value = null
    }
  }

  function authHeaders(): Record<string, string> {
    if (!accessToken.value) return {}
    return { Authorization: `Bearer ${accessToken.value}` }
  }

  return {
    user, accessToken, refreshToken, isLoading, error,
    isLoggedIn,
    login, register, logout, authHeaders,
  }
})
