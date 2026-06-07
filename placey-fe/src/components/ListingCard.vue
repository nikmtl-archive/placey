<script setup lang="ts">
import { ref, onUnmounted } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore, useFavoritesStore } from '@/stores'
import type { Listing } from '@/types'

const props = defineProps<Listing>()

const route          = useRoute()
const authStore      = useAuthStore()
const favoritesStore = useFavoritesStore()

const showLoginHint = ref(false)
let hintTimer: ReturnType<typeof setTimeout> | null = null

function listingTo(id: string) {
  const q: Record<string, string> = {}
  if (route.query.checkIn)  q.checkIn  = String(route.query.checkIn)
  if (route.query.checkOut) q.checkOut = String(route.query.checkOut)
  if (route.query.guests)   q.guests   = String(route.query.guests)
  return { path: `/listing/${id}`, query: q }
}

function handleWishlist() {
  if (authStore.isLoggedIn) {
    favoritesStore.toggleFavorite(props.id)
  } else {
    showLoginHint.value = true
    if (hintTimer) clearTimeout(hintTimer)
    hintTimer = setTimeout(() => { showLoginHint.value = false }, 3000)
  }
}

onUnmounted(() => { if (hintTimer) clearTimeout(hintTimer) })
</script>

<template>
  <div class="group relative">
    <!-- Image area -->
    <RouterLink :to="listingTo(id)" class="block">
      <div class="relative aspect-4/3 rounded-3xl overflow-hidden bg-[#e8e3dc] mb-3">
        <img
          v-if="photoUrls[0]"
          :src="photoUrls[0]"
          :alt="title"
          loading="lazy"
          class="w-full h-full object-cover transition-transform duration-500 group-hover:scale-[1.04]"
        />
        <div
          v-else
          class="w-full h-full flex flex-col items-center justify-center gap-2 text-[#b5afa8]"
        >
          <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
            <rect x="3" y="3" width="18" height="18" rx="2" />
            <circle cx="8.5" cy="8.5" r="1.5" />
            <path d="M21 15l-5-5L5 21" />
          </svg>
          <span class="text-xs font-medium tracking-wide opacity-70">No photo yet</span>
        </div>

        <!-- Wishlist button -->
        <button
          class="absolute top-2.5 right-2.5 w-8 h-8 flex items-center justify-center rounded-full bg-white/80 backdrop-blur-sm shadow-sm transition-all duration-150 hover:scale-110 active:scale-95 cursor-pointer"
          :class="authStore.isLoggedIn && favoritesStore.isFavorited(id) ? 'text-rose-500' : 'text-ink/60 hover:text-ink'"
          aria-label="Save to wishlist"
          @click.prevent="handleWishlist"
        >
          <svg
            width="15" height="15" viewBox="0 0 24 24"
            :fill="authStore.isLoggedIn && favoritesStore.isFavorited(id) ? 'currentColor' : 'none'"
            stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"
          >
            <path d="M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z"/>
          </svg>
        </button>
      </div>
    </RouterLink>

    <!-- Login hint popup — anchored below the heart button (top-right) -->
    <Transition name="fade">
      <div
        v-if="showLoginHint"
        class="absolute top-12 right-2.5 bg-white border border-divider rounded-xl shadow-[0_4px_16px_rgba(0,0,0,0.12)] px-3.5 py-2.5 text-xs whitespace-nowrap z-10 flex items-center gap-2"
        @click.stop
      >
        <span class="text-muted">Log in to save favorites</span>
        <RouterLink
          to="/login"
          class="font-semibold text-ink underline underline-offset-2 hover:opacity-70 transition-opacity"
        >
          Log in
        </RouterLink>
        <button
          class="ml-0.5 text-muted/60 hover:text-muted transition-colors cursor-pointer"
          aria-label="Dismiss"
          @click.stop="showLoginHint = false"
        >
          <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round">
            <path d="M18 6 6 18M6 6l12 12"/>
          </svg>
        </button>
      </div>
    </Transition>

    <!-- Info -->
    <RouterLink :to="listingTo(id)" class="block px-0.5">
      <div class="flex items-start justify-between gap-2 mb-0.5">
        <span class="text-[13px] font-semibold text-ink truncate leading-snug">{{ city }}, {{ country }}</span>
        <span
          v-if="averageRating !== null"
          class="flex items-center gap-1 text-[12px] font-semibold text-ink shrink-0"
        >
          <svg width="10" height="10" viewBox="0 0 24 24" fill="currentColor">
            <path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z" />
          </svg>
          {{ averageRating.toFixed(1) }}
        </span>
        <span v-else class="text-[11px] font-medium text-muted/80 bg-gray-100 px-1.5 py-0.5 rounded-md shrink-0 leading-none">New</span>
      </div>
      <p class="text-[12px] text-muted truncate mb-1.5 leading-snug">{{ title }}</p>
      <p class="text-[13px] text-ink">
        <strong class="font-semibold">${{ pricePerNight }}</strong>
        <span class="text-muted font-normal"> / night</span>
        <span v-if="reviewCount > 0" class="text-muted"> · {{ reviewCount }} {{ reviewCount === 1 ? 'review' : 'reviews' }}</span>
      </p>
    </RouterLink>
  </div>
</template>

<style scoped>
.fade-enter-active,
.fade-leave-active {
  transition: opacity 150ms ease;
}
.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>
