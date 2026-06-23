<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useAuthStore, useMessagingStore } from '@/stores'
import { useClickOutside } from '@/composables/useClickOutside'
import { MessageCircle, CalendarDays, House, Heart, Plus, LogOut, AlignJustify } from '@lucide/vue'

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
  if (authStore.isLoggedIn) {
    messagingStore.fetchUnreadCount()
    authStore.checkIsHost()
  }
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

      <nav class="flex items-center gap-1 shrink-0">
        <!-- Desktop shortcuts — only when logged in -->
        <template v-if="authStore.isLoggedIn">
          <!-- Messages -->
          <RouterLink
            to="/messages"
            class="relative flex items-center justify-center w-10 h-10 rounded-full transition-colors text-ink/60 hover:text-ink"
            title="Messages"
          >
            <MessageCircle :size="20" :stroke-width="1.75" />
            <span
              v-if="messagingStore.unreadCount > 0"
              class="absolute top-1 right-1 min-w-4 h-4 bg-ink text-white text-[9px] font-bold rounded-full flex items-center justify-center px-1 leading-none"
            >
              {{ messagingStore.unreadCount }}
            </span>
          </RouterLink>

          <!-- My Bookings (guests only) -->
          <RouterLink
            v-if="!authStore.isHost"
            to="/bookings"
            class="text-sm font-medium text-ink/70 hover:text-ink transition-colors px-1"
          >
            My Bookings
          </RouterLink>

          <!-- Host Dashboard (hosts only) -->
          <RouterLink
            v-if="authStore.isHost"
            to="/host-dashboard"
            class="text-sm font-medium text-ink/70 hover:text-ink transition-colors px-1"
          >
            Host Dashboard
          </RouterLink>
        </template>

        <RouterLink
          v-if="!authStore.isLoggedIn"
          to="/become-a-host"
          class="text-sm font-medium text-ink px-3.5 py-2 rounded-full transition-colors"
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
        <div v-else ref="dropdownRef" class="relative ml-1">
          <button
            class="flex items-center gap-2.5 border border-divider rounded-full pl-3 pr-1 py-1 cursor-pointer hover:border-ink/30 hover:shadow-sm transition-all"
            @click="dropdownOpen = !dropdownOpen"
            aria-haspopup="true"
            :aria-expanded="dropdownOpen"
          >
            <AlignJustify :size="16" :stroke-width="2" class="text-ink/60" />

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
                class="flex items-center gap-3 justify-between px-4 py-2.5 text-sm text-ink hover:bg-black/5 transition-colors cursor-pointer"
                role="menuitem"
                @click="dropdownOpen = false"
              >
                <span class="flex items-center gap-3">
                  <MessageCircle :size="15" :stroke-width="1.75" class="text-ink/50 shrink-0" />
                  Messages
                </span>
                <span
                  v-if="messagingStore.unreadCount > 0"
                  class="bg-ink text-white text-[10px] font-semibold rounded-full px-1.5 py-0.5 leading-none"
                >
                  {{ messagingStore.unreadCount }}
                </span>
              </RouterLink>

              <RouterLink
                to="/bookings"
                class="flex items-center gap-3 px-4 py-2.5 text-sm text-ink hover:bg-black/5 transition-colors cursor-pointer"
                role="menuitem"
                @click="dropdownOpen = false"
              >
                <CalendarDays :size="15" :stroke-width="1.75" class="text-ink/50 shrink-0" />
                My Bookings
              </RouterLink>

              <RouterLink
                to="/favorites"
                class="flex items-center gap-3 px-4 py-2.5 text-sm text-ink hover:bg-black/5 transition-colors cursor-pointer"
                role="menuitem"
                @click="dropdownOpen = false"
              >
                <Heart :size="15" :stroke-width="1.75" class="text-ink/50 shrink-0" />
                Favorites
              </RouterLink>

              <!-- Host section -->
              <div v-if="authStore.isHost" class="border-t border-divider-light mt-1 pt-1">
                <RouterLink
                  to="/host-dashboard"
                  class="flex items-center gap-3 px-4 py-2.5 text-sm text-ink hover:bg-black/5 transition-colors cursor-pointer"
                  role="menuitem"
                  @click="dropdownOpen = false"
                >
                  <House :size="15" :stroke-width="1.75" class="text-ink/50 shrink-0" />
                  Host Dashboard
                </RouterLink>
              </div>

              <!-- Become a host (non-hosts) -->
              <div v-else class="border-t border-divider-light mt-1 pt-1">
                <RouterLink
                  to="/become-a-host"
                  class="flex items-center gap-3 px-4 py-2.5 text-sm text-ink hover:bg-black/5 transition-colors cursor-pointer"
                  role="menuitem"
                  @click="dropdownOpen = false"
                >
                  <Plus :size="15" :stroke-width="1.75" class="text-ink/50 shrink-0" />
                  Become a Host
                </RouterLink>
              </div>

              <div class="border-t border-divider-light mt-1 pt-1">
                <button
                  class="flex items-center gap-3 w-full px-4 py-2.5 text-sm text-rose-600 hover:bg-rose-50 transition-colors cursor-pointer"
                  role="menuitem"
                  @click="handleLogout"
                >
                  <LogOut :size="15" :stroke-width="1.75" class="shrink-0" />
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
