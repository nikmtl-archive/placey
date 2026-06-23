<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore, useMessagingStore } from '@/stores'
import type { HostProfile, Listing, Review } from '@/types'
import { ChevronLeft, Star, MapPin, MessageCircle, CalendarDays } from '@lucide/vue'
import ListingCard from '@/components/ListingCard.vue'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const messagingStore = useMessagingStore()

const isStartingConversation = ref(false)

const host = ref<HostProfile | null>(null)
const listings = ref<Listing[]>([])
const reviews = ref<Review[]>([])
const isLoading = ref(true)
const error = ref<string | null>(null)

async function fetchAll() {
  isLoading.value = true
  error.value = null
  const id = String(route.params.id)

  try {
    const [hostRes, listingsRes, reviewsRes] = await Promise.all([
      fetch(`/api/users/${id}`),
      fetch(`/api/users/${id}/listings`),
      fetch(`/api/users/${id}/reviews`),
    ])

    if (!hostRes.ok) throw new Error('Host not found')

    host.value = await hostRes.json()
    listings.value = listingsRes.ok ? await listingsRes.json() : []
    reviews.value = reviewsRes.ok ? await reviewsRes.json() : []

    if (listings.value.length === 0) throw new Error('This user is not a host')
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : 'Failed to load host profile'
  } finally {
    isLoading.value = false
  }
}

onMounted(fetchAll)

const averageRating = computed(() => {
  if (reviews.value.length === 0) return null
  return reviews.value.reduce((sum, r) => sum + r.rating, 0) / reviews.value.length
})

const joinedDate = computed(() => {
  if (!host.value) return ''
  return new Date(host.value.createdAt).toLocaleDateString('en-US', {
    month: 'long',
    year: 'numeric',
  })
})

function reviewDate(iso: string) {
  return new Date(iso).toLocaleDateString('en-US', { month: 'long', year: 'numeric' })
}

async function goToMessages() {
  if (!authStore.isLoggedIn) {
    router.push({ path: '/login', query: { redirect: route.fullPath } })
    return
  }
  isStartingConversation.value = true
  try {
    const convId = await messagingStore.getOrCreateConversation(String(route.params.id))
    router.push({ name: 'messages', params: { id: convId } })
  } finally {
    isStartingConversation.value = false
  }
}
</script>

<template>
  <div class="min-h-screen bg-page-bg">
    <!-- Loading skeleton -->
    <div v-if="isLoading" class="max-w-5xl mx-auto px-6 md:px-10 py-10 animate-pulse">
      <div class="h-5 w-24 bg-divider rounded mb-8" />
      <div class="flex gap-6 mb-10">
        <div class="w-24 h-24 rounded-full bg-divider shrink-0" />
        <div class="flex-1 space-y-3 pt-2">
          <div class="h-7 w-48 bg-divider rounded" />
          <div class="h-4 w-32 bg-divider rounded" />
          <div class="h-4 w-64 bg-divider rounded" />
        </div>
      </div>
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-5">
        <div v-for="i in 3" :key="i" class="h-56 bg-divider rounded-2xl" />
      </div>
    </div>

    <!-- Error -->
    <div v-else-if="error" class="max-w-5xl mx-auto px-6 md:px-10 py-24 text-center">
      <p class="text-muted mb-4">{{ error }}</p>
      <button
        class="text-sm font-medium underline underline-offset-2 cursor-pointer"
        @click="router.back()"
      >
        Go back
      </button>
    </div>

    <!-- Content -->
    <template v-else-if="host">
      <!-- Back nav -->
      <div class="max-w-5xl mx-auto px-6 md:px-10 pt-6 pb-2">
        <button
          class="inline-flex items-center gap-1.5 text-sm font-medium text-muted hover:text-ink transition-colors cursor-pointer"
          @click="router.back()"
        >
          <ChevronLeft :size="16" />
          Back
        </button>
      </div>

      <!-- Profile header -->
      <div class="max-w-5xl mx-auto px-6 md:px-10 py-8">
        <div class="flex flex-col sm:flex-row items-start sm:items-center gap-6">
          <!-- Avatar -->
          <img
            v-if="host.avatarUrl"
            :src="host.avatarUrl"
            :alt="host.name"
            class="w-24 h-24 rounded-full object-cover border-2 border-divider-light shrink-0"
          />
          <div
            v-else
            class="w-24 h-24 rounded-full bg-cream border-2 border-divider-light flex items-center justify-center text-3xl font-serif font-semibold text-ink/50 select-none shrink-0"
          >
            {{ host.name.charAt(0).toUpperCase() }}
          </div>

          <!-- Info -->
          <div class="flex-1 min-w-0">
            <h1 class="font-serif text-3xl font-semibold text-ink mb-1">{{ host.name }}</h1>
            <div class="flex flex-wrap items-center gap-x-4 gap-y-1 text-sm text-muted mb-3">
              <span class="flex items-center gap-1.5">
                <CalendarDays :size="13" />
                Joined {{ joinedDate }}
              </span>
              <span v-if="averageRating" class="flex items-center gap-1.5 text-ink font-medium">
                <Star :size="13" class="fill-ink" />
                {{ averageRating.toFixed(1) }}
                <span class="font-normal text-muted">· {{ reviews.length }} reviews</span>
              </span>
              <span class="flex items-center gap-1.5">
                <MapPin :size="13" />
                {{ listings.length }} {{ listings.length === 1 ? 'listing' : 'listings' }}
              </span>
            </div>
            <p v-if="host.bio" class="text-[15px] text-ink/75 leading-relaxed max-w-xl">
              {{ host.bio }}
            </p>
            <p v-else class="text-sm text-muted italic">No bio provided.</p>
          </div>

          <!-- Message button (hidden when viewing own profile) -->
          <button
            v-if="authStore.user?.id !== host.id"
            class="inline-flex items-center gap-2 bg-ink text-white text-sm font-semibold px-5 py-2.5 rounded-xl hover:bg-black transition-colors shrink-0 disabled:opacity-50 disabled:cursor-not-allowed cursor-pointer"
            :disabled="isStartingConversation"
            @click="goToMessages"
          >
            <MessageCircle :size="16" />
            {{ isStartingConversation ? 'Opening…' : 'Message host' }}
          </button>
        </div>
      </div>

      <!-- Divider -->
      <div class="max-w-5xl mx-auto px-6 md:px-10">
        <div class="border-t border-divider-light" />
      </div>

      <!-- Listings section -->
      <div class="max-w-5xl mx-auto px-6 md:px-10 py-10">
        <h2 class="text-xl font-semibold text-ink mb-6">
          {{ host.name.split(' ')[0] }}'s listings
        </h2>

        <div v-if="listings.length === 0" class="text-sm text-muted">
          This host has no active listings.
        </div>

        <div v-else class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-5">
          <ListingCard
            v-for="listing in listings"
            :key="listing.id"
            v-bind="listing"
          />
        </div>
      </div>

      <!-- Divider -->
      <div class="max-w-5xl mx-auto px-6 md:px-10">
        <div class="border-t border-divider-light" />
      </div>

      <!-- Reviews section -->
      <div class="max-w-5xl mx-auto px-6 md:px-10 py-10 pb-24">
        <div class="flex items-center gap-3 mb-6">
          <h2 class="text-xl font-semibold text-ink">
            Reviews about {{ host.name.split(' ')[0] }}
          </h2>
          <span v-if="averageRating" class="flex items-center gap-1.5 text-sm font-semibold">
            <Star :size="14" class="fill-ink" />
            {{ averageRating.toFixed(1) }}
            <span class="font-normal text-muted"
              >· {{ reviews.length }} {{ reviews.length === 1 ? 'review' : 'reviews' }}</span
            >
          </span>
        </div>

        <div v-if="reviews.length === 0" class="text-sm text-muted">No reviews yet.</div>

        <div v-else class="grid grid-cols-1 sm:grid-cols-2 gap-6">
          <div v-for="review in reviews" :key="review.id" class="space-y-3">
            <div class="flex items-center justify-between gap-2">
              <div class="flex items-center gap-2.5">
                <img
                  v-if="review.authorAvatarUrl"
                  :src="review.authorAvatarUrl"
                  :alt="review.authorName"
                  class="w-9 h-9 rounded-full object-cover border border-divider-light shrink-0"
                />
                <div
                  v-else
                  class="w-9 h-9 rounded-full bg-cream border border-divider-light flex items-center justify-center text-sm font-semibold text-ink/60 select-none shrink-0"
                >
                  {{ review.authorName.charAt(0).toUpperCase() }}
                </div>
                <div>
                  <p class="text-sm font-semibold text-ink leading-none mb-0.5">
                    {{ review.authorName }}
                  </p>
                  <RouterLink
                    :to="`/listing/${review.listingId}`"
                    class="text-xs text-muted hover:text-ink hover:underline underline-offset-2 transition-colors"
                  >
                    {{ reviewDate(review.createdAt) }}
                  </RouterLink>
                </div>
              </div>
              <div class="flex items-center gap-0.5 shrink-0">
                <Star
                  v-for="s in 5"
                  :key="s"
                  :size="11"
                  :class="s <= review.rating ? 'fill-ink text-ink' : 'text-divider'"
                />
              </div>
            </div>
            <p class="text-sm text-ink/75 leading-relaxed">{{ review.comment }}</p>
          </div>
        </div>
      </div>
    </template>
  </div>
</template>
