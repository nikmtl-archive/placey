<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore, useMessagingStore } from '@/stores'
import { useClickOutside } from '@/composables/useClickOutside'

const router = useRouter()
const authStore = useAuthStore()
const messagingStore = useMessagingStore()
const scrolled = ref(false)
const dropdownOpen = ref(false)
const dropdownRef = ref<HTMLElement | null>(null)

useClickOutside(dropdownRef, () => {
  dropdownOpen.value = false
})

function onScroll() {
  scrolled.value = window.scrollY > 8
}

onMounted(() => {
  window.addEventListener('scroll', onScroll, { passive: true })
  if (authStore.isLoggedIn) messagingStore.fetchUnreadCount()
})
onUnmounted(() => window.removeEventListener('scroll', onScroll))

const initials = computed(() => {
  const name = authStore.user?.name
  if (!name) return '?'
  return name
    .split(' ')
    .map((w: string) => w[0])
    .join('')
    .toUpperCase()
    .slice(0, 2)
})

async function handleLogout() {
  dropdownOpen.value = false
  await authStore.logout()
  router.push({ name: 'home' })
}
</script>

<template>
  <header
    class="sticky top-0 z-50 bg-white transition-shadow duration-200"
    :class="scrolled ? 'shadow-[0_1px_12px_rgba(0,0,0,0.08)]' : 'border-b border-divider-light'"
  >
    <div class="max-w-7xl mx-auto px-10 h-16 flex items-center justify-between gap-6">
      <RouterLink
        to="/"
        class="font-serif text-2xl font-semibold text-ink tracking-tight shrink-0 select-none"
      >
        placey
      </RouterLink>

      <nav class="flex items-center gap-2 shrink-0">
        <RouterLink
          v-if="!authStore.isLoggedIn"
          to="/become-a-host"
          class="text-sm font-medium text-ink px-3.5 py-2 rounded-lg hover:bg-black/5 transition-colors"
        >
          Become a Host
        </RouterLink>

        <!-- Not logged in -->
        <RouterLink
          v-if="!authStore.isLoggedIn"
          to="/login"
          class="text-sm font-medium text-white bg-dark px-5 py-2 rounded-full hover:bg-black transition-colors"
        >
          Log in
        </RouterLink>

        <!-- Logged in -->
        <div v-else ref="dropdownRef" class="relative">
          <button
            class="flex items-center gap-2.5 border border-divider rounded-full pl-3 pr-1 py-1 hover:shadow-md transition-shadow cursor-pointer"
            @click="dropdownOpen = !dropdownOpen"
            aria-haspopup="true"
            :aria-expanded="dropdownOpen"
          >
            <!-- Hamburger lines -->
            <svg
              width="16"
              height="16"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              stroke-width="2"
              stroke-linecap="round"
              class="text-ink/60"
            >
              <line x1="3" y1="6" x2="21" y2="6" />
              <line x1="3" y1="12" x2="21" y2="12" />
              <line x1="3" y1="18" x2="21" y2="18" />
            </svg>

            <!-- Avatar or initials -->
            <div
              class="w-8 h-8 rounded-full bg-[#e8e3dc] border border-divider-light overflow-hidden flex items-center justify-center shrink-0"
            >
              <img
                v-if="authStore.user?.avatarUrl"
                :src="authStore.user.avatarUrl"
                :alt="authStore.user.name"
                class="w-full h-full object-cover"
              />
              <span v-else class="text-xs font-semibold text-ink/60 select-none">{{
                initials
              }}</span>
            </div>
          </button>

          <!-- Dropdown -->
          <Transition name="dropdown">
            <div
              v-if="dropdownOpen"
              class="absolute right-0 top-[calc(100%+8px)] w-56 bg-white rounded-2xl border border-divider shadow-[0_8px_32px_rgba(0,0,0,0.12)] overflow-hidden z-50 py-1.5"
              role="menu"
            >
              <!-- User info -->
              <div class="px-4 py-3 border-b border-divider-light">
                <p class="text-sm font-semibold text-ink truncate">{{ authStore.user?.name }}</p>
                <p class="text-xs text-muted truncate">{{ authStore.user?.email }}</p>
              </div>

              <!-- Menu items -->
              <RouterLink
                to="/messages"
                class="flex items-center justify-between px-4 py-2.5 text-sm text-ink hover:bg-black/5 transition-colors cursor-pointer"
                role="menuitem"
                @click="dropdownOpen = false"
              >
                Messages
                <span
                  v-if="messagingStore.unreadCount > 0"
                  class="bg-ink text-white text-[10px] font-semibold rounded-full px-1.5 py-0.5 leading-none"
                >
                  {{ messagingStore.unreadCount }}
                </span>
              </RouterLink>
              <RouterLink
                to="/favorites"
                class="flex items-center px-4 py-2.5 text-sm text-ink hover:bg-black/5 transition-colors cursor-pointer"
                role="menuitem"
                @click="dropdownOpen = false"
              >
                Favorites
              </RouterLink>
              <RouterLink
                to="/become-a-host"
                class="flex items-center px-4 py-2.5 text-sm text-ink hover:bg-black/5 transition-colors cursor-pointer"
                role="menuitem"
                @click="dropdownOpen = false"
              >
                Become a Host
              </RouterLink>

              <div class="border-t border-divider-light mt-1 pt-1">
                <button
                  class="flex items-center w-full px-4 py-2.5 text-sm text-rose-600 hover:bg-rose-50 transition-colors cursor-pointer"
                  role="menuitem"
                  @click="handleLogout"
                >
                  Log out
                </button>
              </div>
            </div>
          </Transition>
        </div>
      </nav>
    </div>
  </header>
</template>

<style scoped>
.dropdown-enter-active,
.dropdown-leave-active {
  transition:
    opacity 120ms ease,
    transform 120ms ease;
}
.dropdown-enter-from,
.dropdown-leave-to {
  opacity: 0;
  transform: translateY(-6px);
}
</style>
