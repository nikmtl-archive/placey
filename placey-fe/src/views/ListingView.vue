<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import {
  useListingStore,
  useAuthStore,
  useFavoritesStore,
  useBookingStore,
  useMessagingStore,
} from '@/stores'
import { storeToRefs } from 'pinia'
import BookingCard from '@/components/BookingCard.vue'
import ListingPhotoGrid from '@/components/ListingPhotoGrid.vue'
import ListingLightbox from '@/components/ListingLightbox.vue'
import {
  ChevronLeft,
  X,
  Star,
  MapPin,
  Users,
  BedDouble,
  Bath,
  Zap,
  Share,
  Heart,
} from '@lucide/vue'
import { useAmenityIcons } from '@/composables/useAmenityIcons'

const route = useRoute()
const router = useRouter()

const authStore = useAuthStore()
const favoritesStore = useFavoritesStore()
const listingStore = useListingStore()
const bookingStore = useBookingStore()
const messagingStore = useMessagingStore()
const { listing, isLoading, error, isDeleted, reviews, reviewsLoading, blockings } =
  storeToRefs(listingStore)

const isStartingConversation = ref(false)

async function messageHost() {
  if (!authStore.isLoggedIn) {
    router.push({ path: '/login', query: { redirect: route.fullPath } })
    return
  }
  if (!listing.value) return
  isStartingConversation.value = true
  try {
    const convId = await messagingStore.getOrCreateConversation(listing.value.hostId)
    router.push({ name: 'messages', params: { id: convId } })
  } finally {
    isStartingConversation.value = false
  }
}

const isRangeBlocked = computed(() => {
  if (!checkIn.value || !checkOut.value || nights.value <= 0) return false
  const start = new Date(checkIn.value)
  const end = new Date(checkOut.value)
  return blockings.value.some((b) => start <= new Date(b.endDate) && end >= new Date(b.startDate))
})

onMounted(async () => {
  const id = String(route.params.id)
  listingStore.reset()
  await listingStore.fetchListing(id)
  listingStore.fetchReviews(id)
  listingStore.fetchBlockings(id)
})

function scrollToReviews() {
  document.getElementById('reviews')?.scrollIntoView({ behavior: 'smooth' })
}

function scrollToBooking() {
  document.getElementById('booking')?.scrollIntoView({ behavior: 'smooth' })
}

// Image gallery / lightbox
const lightboxOpen = ref(false)
const lightboxIndex = ref(0)

function openLightbox(i: number) {
  lightboxIndex.value = i
  lightboxOpen.value = true
}

// Favorites / save
const showSaveHint = ref(false)
let saveHintTimer: ReturnType<typeof setTimeout> | null = null

function handleSave() {
  if (authStore.isLoggedIn) {
    if (listing.value) favoritesStore.toggleFavorite(listing.value.id)
  } else {
    showSaveHint.value = true
    if (saveHintTimer) clearTimeout(saveHintTimer)
    saveHintTimer = setTimeout(() => {
      showSaveHint.value = false
    }, 3000)
  }
}

onUnmounted(() => {
  if (saveHintTimer) clearTimeout(saveHintTimer)
})

// Share
const shareCopied = ref(false)

async function share() {
  const url = window.location.href
  const title = listing.value?.title ?? 'Check out this place on Placey'
  if (navigator.share) {
    try {
      await navigator.share({ title, url })
    } catch {
      /* user cancelled */
    }
  } else {
    await navigator.clipboard.writeText(url)
    shareCopied.value = true
    setTimeout(() => {
      shareCopied.value = false
    }, 2000)
  }
}

// Amenity icon map
const { amenityIcon } = useAmenityIcons()

// Booking panel — pre-fill from search query params
const checkIn = ref(route.query.checkIn ? String(route.query.checkIn) : '')
const checkOut = ref(route.query.checkOut ? String(route.query.checkOut) : '')
const guests = ref(route.query.guests ? Number(route.query.guests) : 1)

const nights = computed(() => {
  if (!checkIn.value || !checkOut.value) return 0
  const diff = new Date(checkOut.value).getTime() - new Date(checkIn.value).getTime()
  return Math.max(0, Math.floor(diff / 86_400_000))
})

const datesReady = computed(() => !!checkIn.value && !!checkOut.value && nights.value > 0)
const total = computed(() => (listing.value ? listing.value.pricePerNight * nights.value : 0))

// Booking
const bookingConfirmed = ref(false)
const bookingModalOpen = ref(false)
const bookingStatus = ref<string | null>(null)
const bookingError = ref<string | null>(null)

async function reserve() {
  if (!authStore.isLoggedIn) {
    router.push({ path: '/login', query: { redirect: route.fullPath } })
    return
  }
  if (!listing.value || !checkIn.value || !checkOut.value || nights.value <= 0) return
  bookingError.value = null
  try {
    const booking = await bookingStore.createBooking(
      listing.value.id,
      checkIn.value,
      checkOut.value,
      guests.value,
    )
    bookingConfirmed.value = true
    bookingStatus.value = booking.status
    bookingModalOpen.value = true
  } catch (e: unknown) {
    bookingError.value = e instanceof Error ? e.message : 'Something went wrong'
  }
}

function goToBookings() {
  bookingModalOpen.value = false
  router.push({ name: 'bookings' })
}

// Review helpers
function reviewDate(iso: string) {
  return new Date(iso).toLocaleDateString('en-US', { month: 'long', year: 'numeric' })
}
</script>

<template>
  <div class="min-h-screen bg-page-bg">
    <!-- Loading skeleton -->
    <div v-if="isLoading" class="max-w-7xl mx-auto px-4 sm:px-6 md:px-10 py-10 animate-pulse">
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

    <!-- Deleted listing -->
    <div v-else-if="isDeleted" class="max-w-7xl mx-auto px-4 sm:px-6 md:px-10 py-24 text-center">
      <div
        class="mx-auto mb-6 w-16 h-16 rounded-full bg-[#e8e3dc] flex items-center justify-center"
      >
        <svg
          width="28"
          height="28"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="1.5"
          stroke-linecap="round"
          stroke-linejoin="round"
          class="text-muted"
        >
          <path d="M3 6h18M8 6V4h8v2M19 6l-1 14H6L5 6" />
        </svg>
      </div>
      <h2 class="text-xl font-semibold text-ink mb-2">This listing is no longer available</h2>
      <p class="text-sm text-muted mb-6">
        The host has removed this listing. Any existing bookings have been cancelled.
      </p>
      <button
        class="text-sm font-medium underline underline-offset-2 text-muted hover:text-ink transition-colors"
        @click="router.back()"
      >
        Go back
      </button>
    </div>

    <!-- Error -->
    <div v-else-if="error" class="max-w-7xl mx-auto px-4 sm:px-6 md:px-10 py-24 text-center">
      <p class="text-muted mb-4">{{ error }}</p>
      <button class="text-sm font-medium underline underline-offset-2" @click="router.back()">
        Go back
      </button>
    </div>

    <!-- Content -->
    <template v-else-if="listing">
      <!-- Back nav -->
      <div class="max-w-7xl mx-auto px-4 sm:px-6 md:px-10 pt-6 pb-2">
        <button
          class="inline-flex items-center gap-1.5 text-sm font-medium text-muted hover:text-ink transition-colors cursor-pointer"
          @click="router.back()"
        >
          <ChevronLeft :size="16" />
          Back
        </button>
      </div>

      <!-- Title block -->
      <div
        class="max-w-7xl mx-auto px-4 sm:px-6 md:px-10 pt-2 pb-5 flex items-start justify-between gap-4 flex-wrap"
      >
        <div>
          <h1 class="font-serif text-3xl md:text-4xl font-semibold text-ink leading-tight mb-1.5">
            {{ listing.title }}
          </h1>
          <div class="flex items-center flex-wrap gap-x-3 gap-y-1 text-sm text-muted">
            <span
              v-if="listing.averageRating"
              class="flex items-center gap-1 text-ink font-semibold"
            >
              <Star :size="13" class="fill-ink" />
              {{ listing.averageRating.toFixed(1) }}
              <span class="font-normal text-muted">·</span>
              <a
                class="font-normal text-muted underline underline-offset-2 cursor-pointer"
                @click="scrollToReviews"
              >
                {{ listing.reviewCount }} reviews
              </a>
            </span>
            <span v-else class="text-xs font-medium bg-gray-100 text-muted px-2 py-0.5 rounded-full"
              >New listing</span
            >
            <span class="flex items-center gap-1">
              <MapPin :size="13" />
              {{ listing.city }}, {{ listing.country }}
            </span>
          </div>
        </div>
        <div class="flex items-center gap-3 shrink-0">
          <!-- Share -->
          <button
            class="relative flex items-center gap-1.5 text-sm font-medium text-ink/70 hover:text-ink px-3 py-2 rounded-full hover:bg-black/5 transition-colors cursor-pointer"
            @click="share"
          >
            <Share :size="15" />
            {{ shareCopied ? 'Copied!' : 'Share' }}
          </button>
          <!-- Save -->
          <div class="relative">
            <button
              class="flex items-center gap-1.5 text-sm font-medium px-3 py-2 rounded-full hover:bg-black/5 transition-colors cursor-pointer"
              :class="
                authStore.isLoggedIn && listing && favoritesStore.isFavorited(listing.id)
                  ? 'text-rose-500'
                  : 'text-ink/70 hover:text-ink'
              "
              @click="handleSave"
            >
              <Heart
                :size="15"
                :class="
                  authStore.isLoggedIn && listing && favoritesStore.isFavorited(listing.id)
                    ? 'fill-rose-500'
                    : ''
                "
              />
              {{
                authStore.isLoggedIn && listing && favoritesStore.isFavorited(listing.id)
                  ? 'Saved'
                  : 'Save'
              }}
            </button>

            <!-- Login hint -->
            <Transition name="fade">
              <div
                v-if="showSaveHint"
                class="absolute right-0 top-[calc(100%+6px)] bg-white border border-divider rounded-xl shadow-[0_4px_16px_rgba(0,0,0,0.12)] px-3.5 py-2.5 text-xs whitespace-nowrap z-10 flex items-center gap-2"
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
                  @click.stop="showSaveHint = false"
                >
                  <svg
                    width="11"
                    height="11"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    stroke-width="2.5"
                    stroke-linecap="round"
                  >
                    <path d="M18 6 6 18M6 6l12 12" />
                  </svg>
                </button>
              </div>
            </Transition>
          </div>
        </div>
      </div>

      <!-- Photo grid -->
      <div class="max-w-7xl mx-auto px-4 sm:px-6 md:px-10 mb-10">
        <ListingPhotoGrid
          :photo-urls="listing.photoUrls"
          :title="listing.title"
          @open="openLightbox"
        />
      </div>

      <!-- Body -->
      <div class="max-w-7xl mx-auto px-4 sm:px-6 md:px-10 pb-24">
        <div class="grid grid-cols-1 lg:grid-cols-[1fr_360px] gap-12 items-start">
          <!-- Left column -->
          <div class="space-y-10 min-w-0">
            <!-- Host + quick stats -->
            <div
              class="flex items-start justify-between gap-4 flex-wrap pb-8 border-b border-divider-light"
            >
              <div>
                <h2 class="text-xl font-semibold text-ink mb-1">
                  {{ listing.propertyType }} hosted by
                  <RouterLink
                    :to="`/host/${listing.hostId}`"
                    class="hover:underline underline-offset-2 transition-colors"
                    >{{ listing.hostName }}</RouterLink
                  >
                </h2>
                <div class="flex items-center flex-wrap gap-x-3 gap-y-0.5 text-sm text-muted">
                  <span class="flex items-center gap-1.5"
                    ><Users :size="14" /> {{ listing.maxGuests }} guests</span
                  >
                  <span class="text-divider">·</span>
                  <span class="flex items-center gap-1.5"
                    ><BedDouble :size="14" /> {{ listing.bedrooms }}
                    {{ listing.bedrooms === 1 ? 'bedroom' : 'bedrooms' }}</span
                  >
                  <span class="text-divider">·</span>
                  <span class="flex items-center gap-1.5"
                    ><Bath :size="14" /> {{ listing.bathrooms }}
                    {{ listing.bathrooms === 1 ? 'bath' : 'baths' }}</span
                  >
                </div>
              </div>
              <div class="flex items-center gap-2 shrink-0">
                <button
                  v-if="authStore.user?.id !== listing.hostId"
                  class="text-xs font-medium text-ink border border-divider rounded-full px-3 py-1.5 hover:bg-black/5 transition-colors disabled:opacity-50 cursor-pointer disabled:cursor-not-allowed whitespace-nowrap"
                  :disabled="isStartingConversation"
                  @click="messageHost"
                >
                  {{ isStartingConversation ? 'Opening…' : 'Message Host' }}
                </button>
                <RouterLink :to="`/host/${listing.hostId}`" class="shrink-0">
                  <img
                    v-if="listing.hostAvatarUrl"
                    :src="listing.hostAvatarUrl"
                    :alt="listing.hostName"
                    class="w-14 h-14 rounded-full object-cover border-2 border-divider-light hover:opacity-80 transition-opacity"
                  />
                  <div
                    v-else
                    class="w-14 h-14 rounded-full bg-cream border-2 border-divider-light flex items-center justify-center text-xl font-serif font-semibold text-ink/50 select-none hover:opacity-80 transition-opacity"
                  >
                    {{ listing.hostName.charAt(0).toUpperCase() }}
                  </div>
                </RouterLink>
              </div>
            </div>

            <!-- Mobile: compact price + CTA -->
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
              <p class="text-[13px] text-muted mb-3">
                {{ listing.category }} · {{ listing.propertyType }}
              </p>
              <p class="text-[15px] text-ink/80 leading-relaxed whitespace-pre-line">
                {{ listing.description }}
              </p>
            </div>

            <!-- Amenities -->
            <div class="pb-8 border-b border-divider-light">
              <h3 class="text-xl font-semibold text-ink mb-6">What this place offers</h3>
              <div
                v-if="listing.amenities.length > 0"
                class="grid grid-cols-1 sm:grid-cols-2 gap-4"
              >
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

            <!-- Instant Book -->
            <div
              v-if="listing.isInstantBook"
              class="pb-8 border-b border-divider-light flex items-start gap-3"
            >
              <div
                class="w-9 h-9 rounded-full bg-ink flex items-center justify-center shrink-0 mt-0.5"
              >
                <Zap :size="16" class="text-white fill-white" />
              </div>
              <div>
                <p class="text-sm font-semibold text-ink mb-0.5">Instant Book</p>
                <p class="text-sm text-muted leading-relaxed">
                  Book without waiting for host approval. Your reservation will be confirmed
                  immediately.
                </p>
              </div>
            </div>

            <!-- Booking form (mobile) -->
            <div id="booking" class="lg:hidden pb-10 border-b border-divider-light">
              <h3 class="text-xl font-semibold text-ink mb-5">Check availability</h3>
              <BookingCard
                :price-per-night="listing.pricePerNight"
                :max-guests="listing.maxGuests"
                :check-in="checkIn"
                :check-out="checkOut"
                :guests="guests"
                :nights="nights"
                :total="total"
                :dates-ready="datesReady"
                :is-range-blocked="isRangeBlocked"
                :booking-confirmed="bookingConfirmed"
                :is-submitting="bookingStore.isSubmitting"
                :booking-error="bookingError"
                :is-logged-in="authStore.isLoggedIn"
                :blockings="blockings"
                variant="mobile"
                @update:check-in="checkIn = $event"
                @update:check-out="checkOut = $event"
                @update:guests="guests = $event"
                @reserve="reserve"
              />
            </div>

            <!-- Reviews -->
            <div id="reviews">
              <div class="flex items-center gap-3 mb-6">
                <h3 class="text-xl font-semibold text-ink">Reviews</h3>
                <span
                  v-if="listing.averageRating"
                  class="flex items-center gap-1.5 text-sm font-semibold"
                >
                  <Star :size="14" class="fill-ink" />
                  {{ listing.averageRating.toFixed(1) }}
                  <span class="font-normal text-muted"
                    >· {{ listing.reviewCount }}
                    {{ listing.reviewCount === 1 ? 'review' : 'reviews' }}</span
                  >
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
                        <p class="text-sm font-semibold text-ink leading-none mb-0.5">
                          {{ review.authorName }}
                        </p>
                        <p class="text-xs text-muted">{{ reviewDate(review.createdAt) }}</p>
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

              <div v-else class="text-sm text-muted">
                No reviews yet. Be the first to leave a review after your stay!
              </div>
            </div>
          </div>

          <!-- Right column: Booking card (desktop) -->
          <div class="hidden lg:block lg:sticky lg:top-24">
            <BookingCard
              :price-per-night="listing.pricePerNight"
              :max-guests="listing.maxGuests"
              :check-in="checkIn"
              :check-out="checkOut"
              :guests="guests"
              :nights="nights"
              :total="total"
              :dates-ready="datesReady"
              :is-range-blocked="isRangeBlocked"
              :booking-confirmed="bookingConfirmed"
              :is-submitting="bookingStore.isSubmitting"
              :booking-error="bookingError"
              :is-logged-in="authStore.isLoggedIn"
              :blockings="blockings"
              variant="desktop"
              @update:check-in="checkIn = $event"
              @update:check-out="checkOut = $event"
              @update:guests="guests = $event"
              @reserve="reserve"
            />
          </div>
        </div>
      </div>
    </template>

    <!-- Booking Confirmation Modal -->
    <Teleport to="body">
      <Transition name="modal">
        <div
          v-if="bookingModalOpen"
          class="fixed inset-0 z-300 flex items-center justify-center px-4"
          @click.self="bookingModalOpen = false"
        >
          <div
            class="absolute inset-0 bg-black/40 backdrop-blur-sm"
            @click="bookingModalOpen = false"
          />
          <div class="relative bg-white rounded-4xl shadow-2xl w-full max-w-sm p-8 text-center">
            <button
              class="absolute top-4 right-4 w-8 h-8 rounded-full flex items-center justify-center text-gray-400 hover:text-gray-600 hover:bg-gray-100 transition-colors cursor-pointer"
              @click="bookingModalOpen = false"
            >
              <X :size="16" />
            </button>
            <div
              class="mx-auto mb-5 w-16 h-16 rounded-full flex items-center justify-center"
              :class="bookingStatus === 'Confirmed' ? 'bg-emerald-50' : 'bg-amber-50'"
            >
              <svg
                v-if="bookingStatus === 'Confirmed'"
                class="w-8 h-8 text-emerald-500"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
                stroke-width="2"
              >
                <path stroke-linecap="round" stroke-linejoin="round" d="M5 13l4 4L19 7" />
              </svg>
              <svg
                v-else
                class="w-8 h-8 text-amber-500"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
                stroke-width="2"
              >
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"
                />
              </svg>
            </div>
            <h2 class="text-xl font-bold text-ink mb-2">
              {{ bookingStatus === 'Confirmed' ? "You're booked!" : 'Request sent!' }}
            </h2>
            <p class="text-sm text-muted mb-6">
              {{
                bookingStatus === 'Confirmed'
                  ? 'Your reservation is confirmed. You can manage it from your bookings page.'
                  : "Your request is awaiting host approval. You'll be notified once it's confirmed."
              }}
            </p>
            <button
              class="w-full bg-ink text-white text-sm font-semibold py-3 rounded-xl hover:bg-black transition-colors cursor-pointer"
              @click="goToBookings"
            >
              View My Bookings
            </button>
          </div>
        </div>
      </Transition>
    </Teleport>

    <!-- Lightbox -->
    <ListingLightbox
      v-if="lightboxOpen && listing"
      :photo-urls="listing.photoUrls"
      :index="lightboxIndex"
      :title="listing.title"
      @close="lightboxOpen = false"
      @update:index="lightboxIndex = $event"
    />
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

.modal-enter-active,
.modal-leave-active {
  transition: opacity 200ms ease;
}
.modal-enter-from,
.modal-leave-to {
  opacity: 0;
}
</style>
