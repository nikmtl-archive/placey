<script setup lang="ts">
import { ref, watch, computed } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '@/stores'

const authStore = useAuthStore()
const router = useRouter()
const route = useRoute()

const tab = ref<'login' | 'register'>(route.name === 'register' ? 'register' : 'login')

watch(
  () => route.name,
  (name) => {
    if (name === 'login' || name === 'register') tab.value = name
  },
)

watch(tab, () => {
  authStore.error = null
  resetValidation()
})

// Field values
const name = ref('')
const email = ref('')
const password = ref('')

// Touched state
const touched = ref({ name: false, email: false, password: false })

function touch(field: keyof typeof touched.value) {
  touched.value[field] = true
}

function resetValidation() {
  touched.value = { name: false, email: false, password: false }
  name.value = email.value = password.value = ''
}

// Validation rules
const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

const fieldErrors = computed(() => ({
  name: tab.value === 'register' && !name.value.trim() ? 'Full name is required' : null,
  email: !email.value.trim()
    ? 'Email is required'
    : !EMAIL_RE.test(email.value)
      ? 'Enter a valid email address'
      : null,
  password: !password.value
    ? 'Password is required'
    : tab.value === 'register' && password.value.length < 8
      ? 'Password must be at least 8 characters'
      : null,
}))

const isFormValid = computed(
  () =>
    !fieldErrors.value.email &&
    !fieldErrors.value.password &&
    (tab.value === 'login' || !fieldErrors.value.name),
)

// Server error → field highlight mapping
const serverEmailError = computed(() => {
  const e = authStore.error?.toLowerCase() ?? ''
  return e.includes('email') || e.includes('credential') || e.includes('invalid')
})
const serverPasswordError = computed(() => {
  const e = authStore.error?.toLowerCase() ?? ''
  return e.includes('password') || e.includes('credential') || e.includes('invalid')
})
const serverNameError = computed(() => {
  const e = authStore.error?.toLowerCase() ?? ''
  return e.includes('name')
})

// Clear server error when user starts typing
watch([email, password, name], () => {
  authStore.error = null
})

// Combined error state per field (shown in UI)
const shown = computed(() => ({
  name:
    (touched.value.name && fieldErrors.value.name) ||
    (serverNameError.value ? authStore.error : null),
  email:
    (touched.value.email && fieldErrors.value.email) ||
    (serverEmailError.value ? authStore.error : null),
  password:
    (touched.value.password && fieldErrors.value.password) ||
    (serverPasswordError.value ? authStore.error : null),
}))

// Submit
async function submit() {
  // Mark all relevant fields as touched so errors show immediately on submit
  touched.value.email = true
  touched.value.password = true
  if (tab.value === 'register') touched.value.name = true

  if (!isFormValid.value) return

  if (tab.value === 'login') {
    await authStore.login(email.value, password.value)
    if (!authStore.error) {
      const redirect = route.query.redirect as string | undefined
      router.push(redirect ?? '/')
    }
  } else {
    await authStore.register(email.value, password.value, name.value)
    if (!authStore.error) {
      router.push('/')
    }
  }
}
</script>

<template>
  <div class="min-h-[70vh] flex flex-col items-center justify-center px-6 py-12">
    <RouterLink
      to="/"
      class="font-serif text-3xl font-semibold text-ink tracking-tight select-none mb-6"
    >
      placey
    </RouterLink>

    <!-- Tab toggle -->
    <div class="flex gap-1 bg-black/6 rounded-full p-1 mb-6">
      <button
        class="text-sm font-medium px-5 py-2 rounded-full transition-colors cursor-pointer"
        :class="tab === 'login' ? 'bg-white text-ink shadow-sm' : 'text-muted hover:text-ink'"
        @click="tab = 'login'"
      >
        Log in
      </button>
      <button
        class="text-sm font-medium px-5 py-2 rounded-full transition-colors cursor-pointer"
        :class="tab === 'register' ? 'bg-white text-ink shadow-sm' : 'text-muted hover:text-ink'"
        @click="tab = 'register'"
      >
        Sign up
      </button>
    </div>

    <div
      class="w-full max-w-sm bg-white border border-divider rounded-3xl shadow-[0_4px_32px_rgba(0,0,0,0.08)] p-6"
    >
      <!-- Server error banner (only for errors not mapped to a specific field) -->
      <Transition name="error-slide">
        <div
          v-if="authStore.error && !serverEmailError && !serverPasswordError && !serverNameError"
          class="mb-5 flex items-start gap-3 px-4 py-3 bg-rose-50 border border-rose-200 rounded-xl"
        >
          <svg
            class="shrink-0 mt-0.5 text-rose-500"
            width="15"
            height="15"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="2"
            stroke-linecap="round"
            stroke-linejoin="round"
          >
            <circle cx="12" cy="12" r="10" />
            <line x1="12" y1="8" x2="12" y2="12" />
            <line x1="12" y1="16" x2="12.01" y2="16" />
          </svg>
          <p class="text-sm text-rose-700 leading-snug">{{ authStore.error }}</p>
        </div>
      </Transition>

      <form @submit.prevent="submit" novalidate class="space-y-4">
        <!-- Name — register only -->
        <div v-if="tab === 'register'">
          <label
            for="name"
            class="block text-sm font-medium mb-1.5"
            :class="shown.name ? 'text-rose-600' : 'text-ink'"
          >
            Full name
          </label>
          <input
            id="name"
            v-model="name"
            type="text"
            autocomplete="name"
            placeholder="Jane Doe"
            class="w-full border rounded-xl px-3.5 py-2.5 text-sm text-ink placeholder:text-muted/50 focus:outline-none focus:ring-2 transition-colors"
            :class="
              shown.name
                ? 'border-rose-300 bg-rose-50/40 focus:ring-rose-200 focus:border-rose-400'
                : 'border-divider focus:ring-ink/10 focus:border-ink/40'
            "
            @blur="touch('name')"
          />
          <Transition name="field-error">
            <p v-if="shown.name" class="mt-1.5 text-xs text-rose-600 flex items-center gap-1">
              <svg
                width="11"
                height="11"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                stroke-width="2.5"
                stroke-linecap="round"
                stroke-linejoin="round"
              >
                <circle cx="12" cy="12" r="10" />
                <line x1="12" y1="8" x2="12" y2="12" />
                <line x1="12" y1="16" x2="12.01" y2="16" />
              </svg>
              {{ shown.name }}
            </p>
          </Transition>
        </div>

        <!-- Email -->
        <div>
          <label
            for="email"
            class="block text-sm font-medium mb-1.5"
            :class="shown.email ? 'text-rose-600' : 'text-ink'"
          >
            Email
          </label>
          <input
            id="email"
            v-model="email"
            type="text"
            autocomplete="email"
            placeholder="you@example.com"
            class="w-full border rounded-xl px-3.5 py-2.5 text-sm text-ink placeholder:text-muted/50 focus:outline-none focus:ring-2 transition-colors"
            :class="
              shown.email
                ? 'border-rose-300 bg-rose-50/40 focus:ring-rose-200 focus:border-rose-400'
                : 'border-divider focus:ring-ink/10 focus:border-ink/40'
            "
            @blur="touch('email')"
          />
          <Transition name="field-error">
            <p v-if="shown.email" class="mt-1.5 text-xs text-rose-600 flex items-center gap-1">
              <svg
                width="11"
                height="11"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                stroke-width="2.5"
                stroke-linecap="round"
                stroke-linejoin="round"
              >
                <circle cx="12" cy="12" r="10" />
                <line x1="12" y1="8" x2="12" y2="12" />
                <line x1="12" y1="16" x2="12.01" y2="16" />
              </svg>
              {{ shown.email }}
            </p>
          </Transition>
        </div>

        <!-- Password -->
        <div>
          <label
            for="password"
            class="block text-sm font-medium mb-1.5"
            :class="shown.password ? 'text-rose-600' : 'text-ink'"
          >
            Password
          </label>
          <input
            id="password"
            v-model="password"
            type="password"
            :autocomplete="tab === 'login' ? 'current-password' : 'new-password'"
            :placeholder="tab === 'register' ? 'Min. 8 characters' : '••••••••'"
            class="w-full border rounded-xl px-3.5 py-2.5 text-sm text-ink placeholder:text-muted/50 focus:outline-none focus:ring-2 transition-colors"
            :class="
              shown.password
                ? 'border-rose-300 bg-rose-50/40 focus:ring-rose-200 focus:border-rose-400'
                : 'border-divider focus:ring-ink/10 focus:border-ink/40'
            "
            @blur="touch('password')"
          />
          <Transition name="field-error">
            <p v-if="shown.password" class="mt-1.5 text-xs text-rose-600 flex items-center gap-1">
              <svg
                width="11"
                height="11"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                stroke-width="2.5"
                stroke-linecap="round"
                stroke-linejoin="round"
              >
                <circle cx="12" cy="12" r="10" />
                <line x1="12" y1="8" x2="12" y2="12" />
                <line x1="12" y1="16" x2="12.01" y2="16" />
              </svg>
              {{ shown.password }}
            </p>
          </Transition>
        </div>

        <button
          type="submit"
          :disabled="authStore.isLoading"
          class="w-full bg-dark text-white text-sm font-medium py-2.5 rounded-xl hover:bg-black transition-colors disabled:opacity-60 disabled:cursor-not-allowed"
        >
          <span v-if="authStore.isLoading">{{
            tab === 'login' ? 'Signing in…' : 'Creating account…'
          }}</span>
          <span v-else>{{ tab === 'login' ? 'Log in' : 'Sign up' }}</span>
        </button>
      </form>
    </div>

    <RouterLink to="/" class="mt-5 text-sm text-muted hover:text-ink transition-colors">
      ← Back to home
    </RouterLink>
  </div>
</template>

<style scoped>
/* Server error banner slide */
.error-slide-enter-active {
  transition:
    opacity 200ms ease,
    transform 200ms ease;
}
.error-slide-leave-active {
  transition:
    opacity 150ms ease,
    transform 150ms ease;
}
.error-slide-enter-from,
.error-slide-leave-to {
  opacity: 0;
  transform: translateY(-6px);
}

/* Inline field error slide */
.field-error-enter-active {
  transition:
    opacity 180ms ease,
    transform 180ms ease;
}
.field-error-leave-active {
  transition:
    opacity 120ms ease,
    transform 120ms ease;
}
.field-error-enter-from,
.field-error-leave-to {
  opacity: 0;
  transform: translateY(-4px);
}
</style>
