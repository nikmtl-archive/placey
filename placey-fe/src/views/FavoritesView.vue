<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useAuthStore } from '@/stores'
import ListingCard from '@/components/ListingCard.vue'
import type { Listing } from '@/types'

const authStore = useAuthStore()

const listings = ref<Listing[]>([])
const isLoading = ref(false)
const error = ref<string | null>(null)

async function fetchFavorites() {
  isLoading.value = true
  error.value = null
  try {
    const res = await fetch('/api/favorites', {
      headers: authStore.authHeaders(),
    })
    if (!res.ok) throw new Error(`HTTP ${res.status}`)
    listings.value = await res.json()
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : 'Failed to load favorites'
  } finally {
    isLoading.value = false
  }
}

onMounted(fetchFavorites)
</script>

<template>
  <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-10 py-10 lg:py-14">
    <div class="mb-8">
      <h1 class="font-serif text-3xl font-semibold text-ink mb-1">Your favorites</h1>
      <p class="text-sm text-muted">Places you've saved</p>
    </div>

    <!-- Skeleton -->
    <div
      v-if="isLoading"
      class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-5"
    >
      <div v-for="n in 8" :key="n" class="animate-pulse">
        <div class="aspect-4/3 rounded-3xl bg-divider-light mb-3" />
        <div class="h-3.5 bg-divider-light rounded w-3/4 mb-2" />
        <div class="h-3 bg-divider-light rounded w-1/2" />
      </div>
    </div>

    <!-- Error -->
    <div v-else-if="error" class="text-center py-24">
      <p class="text-sm text-muted mb-4">{{ error }}</p>
      <button class="text-sm font-medium text-ink underline cursor-pointer" @click="fetchFavorites">
        Try again
      </button>
    </div>

    <!-- Empty -->
    <div
      v-else-if="listings.length === 0"
      class="flex flex-col items-center justify-center py-28 text-center"
    >
      <svg
        class="mb-5 text-ink/20"
        width="48"
        height="48"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        stroke-width="1.5"
        stroke-linecap="round"
        stroke-linejoin="round"
      >
        <path
          d="M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z"
        />
      </svg>
      <p class="font-serif text-2xl text-ink mb-2">No favorites yet</p>
      <p class="text-sm text-muted mb-6 max-w-xs">
        Tap the heart on any listing to save it here for later.
      </p>
      <RouterLink
        to="/search"
        class="text-sm font-medium text-white bg-ink px-6 py-2.5 rounded-full hover:bg-black transition-colors"
        >Browse places</RouterLink
      >
    </div>

    <!-- Grid -->
    <div v-else class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-5">
      <ListingCard v-for="listing in listings" :key="listing.id" v-bind="listing" />
    </div>
  </div>
</template>
