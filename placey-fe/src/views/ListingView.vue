<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useListing } from '@/composables/useListing'
import type { Review } from '@/types'
import {
  ChevronLeft,
  ChevronRight,
  X,
  Star,
  MapPin,
  Users,
  BedDouble,
  Bath,
  Zap,
  Share2,
  Heart,
  Wifi,
  Car,
  UtensilsCrossed,
  Wind,
  Waves,
  Tv,
  Dumbbell,
  Dog,
  Flame,
  Coffee,
  Snowflake,
  WashingMachine,
  Lock,
  Camera,
  Sparkles,
} from '@lucide/vue'

const route  = useRoute()
const router = useRouter()
const { listing, isLoading, error, fetchListing } = useListing()

// Reviews
const reviews        = ref<Review[]>([])
const reviewsLoading = ref(false)

async function fetchReviews(id: string) {
  reviewsLoading.value = true
  try {
    const res = await fetch(`/api/listings/${id}/reviews`)
    if (!res.ok) throw new Error()
    reviews.value = await res.json()
  } catch {
    reviews.value = []
  } finally {
    reviewsLoading.value = false
  }
}

onMounted(async () => {
  const id = String(route.params.id)
  await fetchListing(id)
  fetchReviews(id)
})

function scrollToReviews() {
  const el = document.getElementById('reviews')
  if (el) el.scrollIntoView({ behavior: 'smooth' })
}

function scrollToBooking() {
  const el = document.getElementById('booking')
  if (el) el.scrollIntoView({ behavior: 'smooth' })
}

// Image gallery 
const lightboxOpen  = ref(false)
const lightboxIndex = ref(0)
const saved         = ref(false)

function openLightbox(i: number) {
  lightboxIndex.value = i
  lightboxOpen.value = true
}

function closeLightbox() {
  lightboxOpen.value = false
}

function lightboxPrev() {
  if (!listing.value) return
  lightboxIndex.value = (lightboxIndex.value - 1 + listing.value.photoUrls.length) % listing.value.photoUrls.length
}

function lightboxNext() {
  if (!listing.value) return
  lightboxIndex.value = (lightboxIndex.value + 1) % listing.value.photoUrls.length
}

function handleLightboxKey(e: KeyboardEvent) {
  if (e.key === 'ArrowLeft')  lightboxPrev()
  if (e.key === 'ArrowRight') lightboxNext()
  if (e.key === 'Escape')     closeLightbox()
}

// Share
async function share() {
  const url   = window.location.href
  const title = listing.value?.title ?? 'Check out this place on Placey'
  if (navigator.share) {
    try { await navigator.share({ title, url }) } catch { /* user cancelled */ }
  } else {
    await navigator.clipboard.writeText(url)
    shareCopied.value = true
    setTimeout(() => { shareCopied.value = false }, 2000)
  }
}
const shareCopied = ref(false)

// Amenity icon map
const amenityIcons: Record<string, object> = {
  'WiFi': Wifi,
  'Parking': Car,
  'Kitchen': UtensilsCrossed,
  'Air conditioning': Wind,
  'Pool': Waves,
  'TV': Tv,
  'Gym': Dumbbell,
  'Pets allowed': Dog,
  'Fireplace': Flame,
  'Coffee maker': Coffee,
  'Heating': Snowflake,
  'Washer': WashingMachine,
  'Dryer': WashingMachine,
  'Safe': Lock,
  'Security cameras': Camera,
}

function amenityIcon(name: string): object {
  return amenityIcons[name] ?? Sparkles
}

// Booking panel
const checkIn  = ref('')
const checkOut = ref('')
const guests   = ref(1)

const nights = computed(() => {
  if (!checkIn.value || !checkOut.value) return 0
  const diff = new Date(checkOut.value).getTime() - new Date(checkIn.value).getTime()
  return Math.max(0, Math.floor(diff / 86_400_000))
})

const subtotal   = computed(() => listing.value ? listing.value.pricePerNight * nights.value : 0)
const serviceFee = computed(() => Math.round(subtotal.value * 0.12))
const total      = computed(() => subtotal.value + serviceFee.value)

// Review helpers
function reviewDate(iso: string) {
  return new Date(iso).toLocaleDateString('en-US', { month: 'long', year: 'numeric' })
}
</script>

<template>
  <div class="min-h-screen bg-page-bg">

    <!-- Loading skeleton -->
    <div v-if="isLoading" class="max-w-7xl mx-auto px-6 md:px-10 py-10 animate-pulse">
      <div class="h-6 w-40 bg-divider rounded mb-6" />
      <div class="h-8 w-2/3 bg-divider rounded mb-3" />
      <div class="h-4 w-1/3 bg-divider rounded mb-8" />
      <div class="aspect-16/7 bg-divider rounded-3xl mb-10" />
      <div class="grid grid-cols-1 lg:grid-cols-[1fr_360px] gap-12">
        <div class="space-y-6">
          <div class="h-5 bg-divider rounded w-3/4" />
          <div class="h-5 bg-divider rounded w-1/2" />
          <div class="h-5 bg-divider rounded w-2/3" />
        </div>
        <div class="h-64 bg-divider rounded-2xl" />
      </div>
    </div>

    <!-- Error -->
    <div v-else-if="error" class="max-w-7xl mx-auto px-6 md:px-10 py-24 text-center">
      <p class="text-muted mb-4">{{ error }}</p>
      <button class="text-sm font-medium underline underline-offset-2" @click="router.back()">Go back</button>
    </div>

    <!-- Content -->
    <template v-else-if="listing">

      <!-- Back nav -->
      <div class="max-w-7xl mx-auto px-6 md:px-10 pt-6 pb-2">
        <button
          class="inline-flex items-center gap-1.5 text-sm font-medium text-muted hover:text-ink transition-colors cursor-pointer"
          @click="router.back()"
        >
          <ChevronLeft :size="16" />
          Back
        </button>
      </div>

      <!-- Title block -->
      <div class="max-w-7xl mx-auto px-6 md:px-10 pt-2 pb-5 flex items-start justify-between gap-4 flex-wrap">
        <div>
          <h1 class="font-serif text-3xl md:text-4xl font-semibold text-ink leading-tight mb-1.5">
            {{ listing.title }}
          </h1>
          <div class="flex items-center flex-wrap gap-x-3 gap-y-1 text-sm text-muted">
            <span v-if="listing.averageRating" class="flex items-center gap-1 text-ink font-semibold">
              <Star :size="13" class="fill-ink" />
              {{ listing.averageRating.toFixed(1) }}
              <span class="font-normal text-muted">·</span>
              <a class="font-normal text-muted underline underline-offset-2 cursor-pointer" @click="scrollToReviews">
                {{ listing.reviewCount }} reviews
              </a>
            </span>
            <span v-else class="text-xs font-medium bg-gray-100 text-muted px-2 py-0.5 rounded-full">New listing</span>
            <span class="flex items-center gap-1">
              <MapPin :size="13" />
              {{ listing.city }}, {{ listing.country }}
            </span>
          </div>
        </div>
        <div class="flex items-center gap-3 shrink-0">
          <!-- Share -->
          <button
            class="relative flex items-center gap-1.5 text-sm font-medium text-ink/70 hover:text-ink px-3 py-2 rounded-lg hover:bg-black/5 transition-colors cursor-pointer"
            @click="share"
          >
            <Share2 :size="15" />
            {{ shareCopied ? 'Copied!' : 'Share' }}
          </button>
          <!-- Save -->
          <button
            class="flex items-center gap-1.5 text-sm font-medium px-3 py-2 rounded-lg hover:bg-black/5 transition-colors cursor-pointer"
            :class="saved ? 'text-rose-500' : 'text-ink/70 hover:text-ink'"
            @click="saved = !saved"
          >
            <Heart :size="15" :class="saved ? 'fill-rose-500' : ''" />
            {{ saved ? 'Saved' : 'Save' }}
          </button>
        </div>
      </div>

      <!-- Photo grid -->
      <div class="max-w-7xl mx-auto px-6 md:px-10 mb-10">
        <div
          v-if="listing.photoUrls.length === 0"
          class="aspect-16/7 rounded-3xl bg-[#e8e3dc] flex flex-col items-center justify-center gap-3 text-[#b5afa8]"
        >
          <svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
            <rect x="3" y="3" width="18" height="18" rx="2" />
            <circle cx="8.5" cy="8.5" r="1.5" />
            <path d="M21 15l-5-5L5 21" />
          </svg>
          <span class="text-sm font-medium opacity-70">No photos yet</span>
        </div>

        <div
          v-else-if="listing.photoUrls.length === 1"
          class="aspect-16/7 rounded-3xl overflow-hidden bg-[#e8e3dc] cursor-pointer"
          @click="openLightbox(0)"
        >
          <img :src="listing.photoUrls[0]" :alt="listing.title" class="w-full h-full object-cover" />
        </div>

        <div
          v-else
          class="grid grid-cols-4 grid-rows-2 gap-2 rounded-3xl overflow-hidden aspect-16/7"
        >
          <div
            class="col-span-2 row-span-2 cursor-pointer overflow-hidden bg-[#e8e3dc]"
            @click="openLightbox(0)"
          >
            <img
              :src="listing.photoUrls[0]"
              :alt="listing.title"
              class="w-full h-full object-cover hover:scale-[1.03] transition-transform duration-500"
            />
          </div>
          <template v-for="(url, i) in listing.photoUrls.slice(1, 5)" :key="i">
            <div
              class="relative cursor-pointer overflow-hidden bg-[#e8e3dc]"
              @click="openLightbox(i + 1)"
            >
              <img
                :src="url"
                :alt="`${listing.title} photo ${i + 2}`"
                class="w-full h-full object-cover hover:scale-[1.04] transition-transform duration-500"
              />
              <div
                v-if="i === 3 && listing.photoUrls.length > 5"
                class="absolute inset-0 bg-black/40 flex items-center justify-center"
              >
                <span class="text-white text-sm font-semibold">+{{ listing.photoUrls.length - 5 }} more</span>
              </div>
            </div>
          </template>
          <template v-for="i in Math.max(0, 4 - Math.min(listing.photoUrls.length - 1, 4))" :key="`empty-${i}`">
            <div class="bg-[#e8e3dc]" />
          </template>
        </div>
      </div>

      <!-- Body -->
      <div class="max-w-7xl mx-auto px-6 md:px-10 pb-24">
        <div class="grid grid-cols-1 lg:grid-cols-[1fr_360px] gap-12 items-start">

          <!-- Left column -->
          <div class="space-y-10 min-w-0">

            <!-- Host + quick stats -->
            <div class="flex items-start justify-between gap-4 flex-wrap pb-8 border-b border-divider-light">
              <div>
                <h2 class="text-xl font-semibold text-ink mb-1">
                  {{ listing.propertyType }} hosted by {{ listing.hostName }}
                </h2>
                <div class="flex items-center flex-wrap gap-x-3 gap-y-0.5 text-sm text-muted">
                  <span class="flex items-center gap-1.5"><Users :size="14" /> {{ listing.maxGuests }} guests</span>
                  <span class="text-divider">·</span>
                  <span class="flex items-center gap-1.5"><BedDouble :size="14" /> {{ listing.bedrooms }} {{ listing.bedrooms === 1 ? 'bedroom' : 'bedrooms' }}</span>
                  <span class="text-divider">·</span>
                  <span class="flex items-center gap-1.5"><Bath :size="14" /> {{ listing.bathrooms }} {{ listing.bathrooms === 1 ? 'bath' : 'baths' }}</span>
                </div>
              </div>
              <div class="w-14 h-14 rounded-full bg-cream border-2 border-divider-light flex items-center justify-center text-xl font-serif font-semibold text-ink/50 shrink-0 select-none">
                {{ listing.hostName.charAt(0).toUpperCase() }}
              </div>
            </div>

            <!-- ── Mobile: compact price + CTA that scrolls to booking form ── -->
          <div class="lg:hidden flex items-center justify-between gap-4 py-1">
            <div class="flex items-baseline gap-1">
              <span class="text-xl font-bold text-ink">${{ listing.pricePerNight }}</span>
              <span class="text-sm text-muted font-normal">/ night</span>
            </div>
            <button
              class="bg-ink text-white text-sm font-semibold px-6 py-3 rounded-xl hover:bg-black transition-colors cursor-pointer shrink-0"
              @click="scrollToBooking"
            >
              Check availability
            </button>
          </div>

            <!-- Description -->
            <div class="pb-8 border-b border-divider-light">
              <div class="flex items-center gap-2 mb-1">
                <h3 class="text-xl font-semibold text-ink">About this place</h3>
              </div>
              <p class="text-[13px] text-muted mb-3">{{ listing.category }} · {{ listing.propertyType }}</p>
              <p class="text-[15px] text-ink/80 leading-relaxed whitespace-pre-line">{{ listing.description }}</p>
            </div>

            <!-- Amenities -->
            <div class="pb-8 border-b border-divider-light">
              <h3 class="text-xl font-semibold text-ink mb-6">What this place offers</h3>
              <div v-if="listing.amenities.length > 0" class="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div
                  v-for="amenity in listing.amenities"
                  :key="amenity"
                  class="flex items-center gap-3 text-sm text-ink"
                >
                  <component :is="amenityIcon(amenity)" :size="20" class="text-ink/70 shrink-0" />
                  {{ amenity }}
                </div>
              </div>
              <p v-else class="text-sm text-muted">No amenities listed.</p>
            </div>

            <!-- Instant Book (moved here) -->
            <div v-if="listing.isInstantBook" class="pb-8 border-b border-divider-light flex items-start gap-3">
              <div class="w-9 h-9 rounded-full bg-ink flex items-center justify-center shrink-0 mt-0.5">
                <Zap :size="16" class="text-white fill-white" />
              </div>
              <div>
                <p class="text-sm font-semibold text-ink mb-0.5">Instant Book</p>
                <p class="text-sm text-muted leading-relaxed">Book without waiting for host approval. Your reservation will be confirmed immediately.</p>
              </div>
            </div>

            <!-- Booking form (mobile only, full card) -->
            <div id="booking" class="lg:hidden pb-10 border-b border-divider-light">
              <h3 class="text-xl font-semibold text-ink mb-5">Check availability</h3>
              <div class="rounded-2xl border border-divider shadow-[0_4px_32px_rgba(0,0,0,0.08)] p-6 bg-white">
                <div class="grid grid-cols-2 border border-divider rounded-xl overflow-hidden mb-3 text-sm">
                  <div class="px-3 py-2.5 border-r border-divider">
                    <p class="text-[10px] font-semibold text-ink uppercase tracking-wider mb-1">Check-in</p>
                    <input v-model="checkIn" type="date" class="w-full bg-transparent text-ink text-xs outline-none cursor-pointer" :min="new Date().toISOString().split('T')[0]" />
                  </div>
                  <div class="px-3 py-2.5">
                    <p class="text-[10px] font-semibold text-ink uppercase tracking-wider mb-1">Check-out</p>
                    <input v-model="checkOut" type="date" class="w-full bg-transparent text-ink text-xs outline-none cursor-pointer" :min="checkIn || new Date().toISOString().split('T')[0]" />
                  </div>
                </div>
                <div class="border border-divider rounded-xl px-3 py-2.5 mb-4 text-sm">
                  <p class="text-[10px] font-semibold text-ink uppercase tracking-wider mb-1">Guests</p>
                  <div class="flex items-center justify-between">
                    <span class="text-xs text-ink">{{ guests }} {{ guests === 1 ? 'guest' : 'guests' }}</span>
                    <div class="flex items-center gap-2">
                      <button class="w-6 h-6 rounded-full border border-divider flex items-center justify-center text-muted hover:border-ink hover:text-ink transition-colors cursor-pointer disabled:opacity-30" :disabled="guests <= 1" @click="guests = Math.max(1, guests - 1)"><span class="text-base leading-none mb-0.5">−</span></button>
                      <button class="w-6 h-6 rounded-full border border-divider flex items-center justify-center text-muted hover:border-ink hover:text-ink transition-colors cursor-pointer disabled:opacity-30" :disabled="guests >= listing.maxGuests" @click="guests = Math.min(listing.maxGuests, guests + 1)"><span class="text-base leading-none mb-0.5">+</span></button>
                    </div>
                  </div>
                </div>
                <button class="w-full bg-ink text-white text-sm font-semibold py-3.5 rounded-xl hover:bg-black transition-colors cursor-pointer">
                  {{ checkIn && checkOut && nights > 0 ? 'Reserve' : 'Check availability' }}
                </button>
                <div v-if="nights > 0" class="mt-5 pt-5 border-t border-divider-light space-y-2.5 text-sm">
                  <div class="flex justify-between text-muted"><span>${{ listing.pricePerNight }} × {{ nights }} {{ nights === 1 ? 'night' : 'nights' }}</span><span class="text-ink">${{ subtotal }}</span></div>
                  <div class="flex justify-between text-muted"><span>Service fee</span><span class="text-ink">${{ serviceFee }}</span></div>
                  <div class="flex justify-between font-semibold text-ink pt-2.5 border-t border-divider-light"><span>Total</span><span>${{ total }}</span></div>
                </div>
                <p class="text-xs text-muted text-center mt-3">You won't be charged yet</p>
              </div>
            </div>

            <!-- Reviews -->
            <div id="reviews">
              <div class="flex items-center gap-3 mb-6">
                <h3 class="text-xl font-semibold text-ink">Reviews</h3>
                <span v-if="listing.averageRating" class="flex items-center gap-1.5 text-sm font-semibold">
                  <Star :size="14" class="fill-ink" />
                  {{ listing.averageRating.toFixed(1) }}
                  <span class="font-normal text-muted">· {{ listing.reviewCount }} {{ listing.reviewCount === 1 ? 'review' : 'reviews' }}</span>
                </span>
              </div>

              <div v-if="reviewsLoading" class="space-y-4 animate-pulse">
                <div v-for="i in 3" :key="i" class="h-20 bg-divider-light rounded-xl" />
              </div>

              <div v-else-if="reviews.length > 0" class="grid grid-cols-1 sm:grid-cols-2 gap-6">
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
                        <p class="text-sm font-semibold text-ink leading-none mb-0.5">{{ review.authorName }}</p>
                        <p class="text-xs text-muted">{{ reviewDate(review.createdAt) }}</p>
                      </div>
                    </div>
                    <div class="flex items-center gap-0.5 shrink-0">
                      <Star v-for="s in 5" :key="s" :size="11" :class="s <= review.rating ? 'fill-ink text-ink' : 'text-divider'" />
                    </div>
                  </div>
                  <p class="text-sm text-ink/75 leading-relaxed">{{ review.comment }}</p>
                </div>
              </div>

              <div v-else class="text-sm text-muted">
                No reviews yet — be the first to stay here.
              </div>
            </div>

          </div>

          <!-- Right column: Booking card (desktop only) -->
          <div class="hidden lg:block lg:sticky lg:top-24">
            <div class="rounded-2xl border border-divider shadow-[0_4px_32px_rgba(0,0,0,0.08)] p-6 bg-white">
              <div class="flex items-baseline gap-1 mb-5">
                <span class="text-2xl font-bold text-ink">${{ listing.pricePerNight }}</span>
                <span class="text-sm text-muted font-normal">/ night</span>
              </div>
              <div class="grid grid-cols-2 border border-divider rounded-xl overflow-hidden mb-3 text-sm">
                <div class="px-3 py-2.5 border-r border-divider">
                  <p class="text-[10px] font-semibold text-ink uppercase tracking-wider mb-1">Check-in</p>
                  <input v-model="checkIn" type="date" class="w-full bg-transparent text-ink text-xs outline-none cursor-pointer" :min="new Date().toISOString().split('T')[0]" />
                </div>
                <div class="px-3 py-2.5">
                  <p class="text-[10px] font-semibold text-ink uppercase tracking-wider mb-1">Check-out</p>
                  <input v-model="checkOut" type="date" class="w-full bg-transparent text-ink text-xs outline-none cursor-pointer" :min="checkIn || new Date().toISOString().split('T')[0]" />
                </div>
              </div>
              <div class="border border-divider rounded-xl px-3 py-2.5 mb-4 text-sm">
                <p class="text-[10px] font-semibold text-ink uppercase tracking-wider mb-1">Guests</p>
                <div class="flex items-center justify-between">
                  <span class="text-xs text-ink">{{ guests }} {{ guests === 1 ? 'guest' : 'guests' }}</span>
                  <div class="flex items-center gap-2">
                    <button class="w-6 h-6 rounded-full border border-divider flex items-center justify-center text-muted hover:border-ink hover:text-ink transition-colors cursor-pointer disabled:opacity-30" :disabled="guests <= 1" @click="guests = Math.max(1, guests - 1)"><span class="text-base leading-none mb-0.5">−</span></button>
                    <button class="w-6 h-6 rounded-full border border-divider flex items-center justify-center text-muted hover:border-ink hover:text-ink transition-colors cursor-pointer disabled:opacity-30" :disabled="guests >= listing.maxGuests" @click="guests = Math.min(listing.maxGuests, guests + 1)"><span class="text-base leading-none mb-0.5">+</span></button>
                  </div>
                </div>
              </div>
              <button class="w-full bg-ink text-white text-sm font-semibold py-3.5 rounded-xl hover:bg-black transition-colors cursor-pointer">
                {{ checkIn && checkOut && nights > 0 ? 'Reserve' : 'Check availability' }}
              </button>
              <div v-if="nights > 0" class="mt-5 pt-5 border-t border-divider-light space-y-2.5 text-sm">
                <div class="flex justify-between text-muted"><span>${{ listing.pricePerNight }} × {{ nights }} {{ nights === 1 ? 'night' : 'nights' }}</span><span class="text-ink">${{ subtotal }}</span></div>
                <div class="flex justify-between text-muted"><span>Service fee</span><span class="text-ink">${{ serviceFee }}</span></div>
                <div class="flex justify-between font-semibold text-ink pt-2.5 border-t border-divider-light"><span>Total</span><span>${{ total }}</span></div>
              </div>
              <p class="text-xs text-muted text-center mt-3">You won't be charged yet</p>
            </div>
          </div>

        </div>
      </div>

    </template>

    <!-- Lightbox -->
    <Teleport to="body">
      <div
        v-if="lightboxOpen && listing"
        class="fixed inset-0 z-200 bg-black/95 flex items-center justify-center"
        @keydown="handleLightboxKey"
        tabindex="0"
      >
        <button class="absolute top-4 left-4 w-10 h-10 rounded-full bg-white/10 hover:bg-white/20 flex items-center justify-center text-white transition-colors cursor-pointer" @click="closeLightbox">
          <X :size="18" />
        </button>

        <span class="absolute top-4 left-1/2 -translate-x-1/2 text-white/60 text-sm tabular-nums">
          {{ lightboxIndex + 1 }} / {{ listing.photoUrls.length }}
        </span>

        <button
          v-if="listing.photoUrls.length > 1"
          class="absolute left-4 top-1/2 -translate-y-1/2 w-10 h-10 rounded-full bg-white/10 hover:bg-white/20 flex items-center justify-center text-white transition-colors cursor-pointer"
          @click="lightboxPrev"
        >
          <ChevronLeft :size="20" />
        </button>

        <img
          :src="listing.photoUrls[lightboxIndex]"
          :alt="`${listing.title} – photo ${lightboxIndex + 1}`"
          class="max-h-[90vh] max-w-[90vw] object-contain rounded-lg"
        />

        <button
          v-if="listing.photoUrls.length > 1"
          class="absolute right-4 top-1/2 -translate-y-1/2 w-10 h-10 rounded-full bg-white/10 hover:bg-white/20 flex items-center justify-center text-white transition-colors cursor-pointer"
          @click="lightboxNext"
        >
          <ChevronRight :size="20" />
        </button>

        <div
          v-if="listing.photoUrls.length > 1"
          class="absolute bottom-4 left-1/2 -translate-x-1/2 flex gap-2 max-w-[90vw] overflow-x-auto scrollbar-hide px-2"
        >
          <button
            v-for="(url, i) in listing.photoUrls"
            :key="i"
            class="w-12 h-9 rounded-md overflow-hidden shrink-0 border-2 transition-all cursor-pointer"
            :class="i === lightboxIndex ? 'border-white opacity-100' : 'border-transparent opacity-50 hover:opacity-75'"
            @click="lightboxIndex = i"
          >
            <img :src="url" :alt="`thumb ${i + 1}`" class="w-full h-full object-cover" />
          </button>
        </div>
      </div>
    </Teleport>

  </div>
</template>
