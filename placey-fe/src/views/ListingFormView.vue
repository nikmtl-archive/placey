<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '@/stores'

const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()

const CATEGORIES = ['Beach', 'Mountains', 'Urban', 'Luxe', 'Countryside', 'Island', 'Lake']
const PROPERTY_TYPES = ['Apartment', 'House']

// Mode: 'create' for /listing/new, 'edit' for /listing/:id/edit
const listingId = route.params.id as string | undefined
const isEdit = !!listingId

// Form

const form = ref({
  title: '',
  description: '',
  city: '',
  country: '',
  street: '',
  pricePerNight: 100,
  maxGuests: 2,
  bedrooms: 1,
  bathrooms: 1,
  category: 'Urban',
  propertyType: 'Apartment',
  isInstantBook: false,
  amenityInput: '',
  amenities: [] as string[],
})

// Load existing listing (edit mode)

const loading = ref(false)
const loadError = ref<string | null>(null)

async function fetchListing() {
  loading.value = true
  loadError.value = null
  try {
    const res = await authStore.apiFetch(`/api/listings/${listingId}`)
    if (!res.ok) throw new Error(`HTTP ${res.status}`)
    const data = await res.json()
    form.value = {
      title: data.title ?? '',
      description: data.description ?? '',
      city: data.city ?? '',
      country: data.country ?? '',
      street: data.street ?? '',
      pricePerNight: data.pricePerNight ?? 100,
      maxGuests: data.maxGuests ?? 2,
      bedrooms: data.bedrooms ?? 1,
      bathrooms: data.bathrooms ?? 1,
      category: data.category ?? 'Urban',
      propertyType: data.propertyType ?? 'Apartment',
      isInstantBook: data.isInstantBook ?? false,
      amenityInput: '',
      amenities: data.amenities ?? [],
    }
  } catch (e: unknown) {
    loadError.value = e instanceof Error ? e.message : 'Failed to load listing'
  } finally {
    loading.value = false
  }
}

// Photos (edit mode)

interface Photo {
  id: string
  url: string
  order: number
}
const existingPhotos = ref<Photo[]>([])
const photosLoading = ref(false)
const deletingPhotoUrl = ref<string | null>(null)
const photoError = ref<string | null>(null)
const photoInput = ref<HTMLInputElement | null>(null)
const photoUploading = ref(false)

async function fetchPhotos() {
  photosLoading.value = true
  try {
    const res = await authStore.apiFetch(`/api/listings/${listingId}/photos`)
    if (!res.ok) return
    existingPhotos.value = await res.json()
  } finally {
    photosLoading.value = false
  }
}

async function uploadExistingPhoto(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  if (!file) return
  photoUploading.value = true
  photoError.value = null
  try {
    const fd = new FormData()
    fd.append('file', file)
    const res = await authStore.apiFetch(`/api/listings/${listingId}/photos`, {
      method: 'POST',
      body: fd,
    })
    if (!res.ok) {
      const err = await res.json().catch(() => ({}))
      throw new Error(err.detail ?? err.message ?? `HTTP ${res.status}`)
    }
    const photo: Photo = await res.json()
    existingPhotos.value.push(photo)
  } catch (e: unknown) {
    photoError.value = e instanceof Error ? e.message : 'Upload failed'
  } finally {
    photoUploading.value = false
    if (photoInput.value) photoInput.value.value = ''
  }
}

async function deleteExistingPhoto(photo: Photo) {
  if (!photo.id) return
  deletingPhotoUrl.value = photo.url
  photoError.value = null
  try {
    const res = await authStore.apiFetch(`/api/listings/${listingId}/photos/${photo.id}`, {
      method: 'DELETE',
    })
    if (!res.ok) {
      const err = await res.json().catch(() => ({}))
      throw new Error(err.detail ?? err.message ?? `HTTP ${res.status}`)
    }
    existingPhotos.value = existingPhotos.value.filter((p) => p.id !== photo.id)
  } catch (e: unknown) {
    photoError.value = e instanceof Error ? e.message : 'Delete failed'
  } finally {
    deletingPhotoUrl.value = null
  }
}

// Photos (create mode)

interface StagedPhoto {
  file: File
  previewUrl: string
}
const stagedPhotos = ref<StagedPhoto[]>([])
const newPhotoInput = ref<HTMLInputElement | null>(null)
const isDragging = ref(false)

function addFiles(files: FileList | null) {
  if (!files) return
  for (const file of Array.from(files)) {
    if (!file.type.startsWith('image/')) continue
    if (stagedPhotos.value.some((p) => p.file.name === file.name && p.file.size === file.size))
      continue
    stagedPhotos.value.push({ file, previewUrl: URL.createObjectURL(file) })
  }
}

function onFileInput(e: Event) {
  addFiles((e.target as HTMLInputElement).files)
  if (newPhotoInput.value) newPhotoInput.value.value = ''
}

function onDrop(e: DragEvent) {
  isDragging.value = false
  addFiles(e.dataTransfer?.files ?? null)
}

function removeStagedPhoto(index: number) {
  const photo = stagedPhotos.value[index]
  if (photo) URL.revokeObjectURL(photo.previewUrl)
  stagedPhotos.value.splice(index, 1)
}

onUnmounted(() => {
  for (const p of stagedPhotos.value) URL.revokeObjectURL(p.previewUrl)
})

// Blockings (edit mode)

interface BlockingItem {
  id: string
  startDate: string
  endDate: string
}
const blockings = ref<BlockingItem[]>([])
const blockingsLoading = ref(false)
const blockingsError = ref<string | null>(null)
const blockingForm = ref({ startDate: '', endDate: '' })
const isSavingBlocking = ref(false)
const blockingSaved = ref(false)
const blockingFormError = ref<string | null>(null)

async function fetchBlockings() {
  blockingsLoading.value = true
  blockingsError.value = null
  try {
    const res = await authStore.apiFetch(`/api/listings/${listingId}/blockings`)
    if (!res.ok) throw new Error(`HTTP ${res.status}`)
    blockings.value = await res.json()
  } catch (e: unknown) {
    blockingsError.value = e instanceof Error ? e.message : 'Failed to load blockings'
  } finally {
    blockingsLoading.value = false
  }
}

async function addBlocking() {
  if (!blockingForm.value.startDate || !blockingForm.value.endDate) {
    blockingFormError.value = 'Please select start and end dates'
    return
  }
  if (blockingForm.value.startDate > blockingForm.value.endDate) {
    blockingFormError.value = 'Start date must be before end date'
    return
  }
  isSavingBlocking.value = true
  blockingSaved.value = false
  blockingFormError.value = null
  try {
    const res = await authStore.apiFetch(`/api/listings/${listingId}/blockings`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(blockingForm.value),
    })
    if (!res.ok) {
      const err = await res.json().catch(() => ({}))
      throw new Error(err.detail ?? err.message ?? `HTTP ${res.status}`)
    }
    const block: BlockingItem = await res.json()
    blockings.value.push(block)
    blockingSaved.value = true
    blockingForm.value = { startDate: '', endDate: '' }
  } catch (e: unknown) {
    blockingFormError.value = e instanceof Error ? e.message : 'Failed to save'
  } finally {
    isSavingBlocking.value = false
  }
}

async function removeBlocking(blockingId: string) {
  try {
    const res = await authStore.apiFetch(`/api/listings/${listingId}/blockings/${blockingId}`, {
      method: 'DELETE',
    })
    if (!res.ok) throw new Error(`HTTP ${res.status}`)
    blockings.value = blockings.value.filter((b) => b.id !== blockingId)
  } catch {
    // ignore
  }
}

// Amenities

function addAmenity() {
  const val = form.value.amenityInput.trim()
  if (val && !form.value.amenities.includes(val)) form.value.amenities.push(val)
  form.value.amenityInput = ''
}

function removeAmenity(a: string) {
  form.value.amenities = form.value.amenities.filter((x) => x !== a)
}

// Delete listing

const confirmDeleteOpen = ref(false)
const isDeleting = ref(false)
const deleteError = ref<string | null>(null)

async function confirmDelete() {
  isDeleting.value = true
  deleteError.value = null
  try {
    const res = await authStore.apiFetch(`/api/listings/${listingId}`, { method: 'DELETE' })
    if (!res.ok) {
      const err = await res.json().catch(() => ({}))
      throw new Error(err.detail ?? err.message ?? `HTTP ${res.status}`)
    }
    router.push('/host-dashboard')
  } catch (e: unknown) {
    deleteError.value = e instanceof Error ? e.message : 'Failed to delete listing'
  } finally {
    isDeleting.value = false
  }
}

// Submit / save

const isSubmitting = ref(false)
const uploadProgress = ref<{ done: number; total: number } | null>(null)
const submitError = ref<string | null>(null)
const saved = ref(false)

const canSubmit = computed(
  () =>
    form.value.title.trim() &&
    form.value.description.trim() &&
    form.value.city.trim() &&
    form.value.country.trim() &&
    form.value.pricePerNight > 0,
)

async function submit() {
  if (!canSubmit.value) return
  isSubmitting.value = true
  submitError.value = null
  saved.value = false

  const body = {
    title: form.value.title.trim(),
    description: form.value.description.trim(),
    city: form.value.city.trim(),
    country: form.value.country.trim(),
    street: form.value.street.trim() || null,
    pricePerNight: form.value.pricePerNight,
    maxGuests: form.value.maxGuests,
    bedrooms: form.value.bedrooms,
    bathrooms: form.value.bathrooms,
    category: form.value.category,
    propertyType: form.value.propertyType,
    isInstantBook: form.value.isInstantBook,
    amenities: form.value.amenities,
  }

  try {
    if (isEdit) {
      const res = await authStore.apiFetch(`/api/listings/${listingId}`, {
        method: 'PATCH',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body),
      })
      if (!res.ok) {
        const err = await res.json().catch(() => ({}))
        throw new Error(err.detail ?? err.message ?? `HTTP ${res.status}`)
      }
      saved.value = true
    } else {
      const res = await authStore.apiFetch('/api/listings', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body),
      })
      if (!res.ok) {
        const err = await res.json().catch(() => ({}))
        throw new Error(err.detail ?? err.message ?? `HTTP ${res.status}`)
      }
      const created = await res.json()
      if (stagedPhotos.value.length > 0) {
        uploadProgress.value = { done: 0, total: stagedPhotos.value.length }
        for (const staged of stagedPhotos.value) {
          const fd = new FormData()
          fd.append('file', staged.file)
          await authStore.apiFetch(`/api/listings/${created.id}/photos`, {
            method: 'POST',
            body: fd,
          })
          uploadProgress.value.done++
        }
      }
      await authStore.checkIsHost()
      router.push(`/listing/${created.id}`)
    }
  } catch (e: unknown) {
    submitError.value = e instanceof Error ? e.message : 'Something went wrong'
  } finally {
    isSubmitting.value = false
    uploadProgress.value = null
  }
}

// Helpers

function formatDate(d: string) {
  const date = d.length === 10 ? new Date(`${d}T00:00:00`) : new Date(d)
  return date.toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' })
}

// Init

onMounted(async () => {
  if (isEdit) {
    await fetchListing()
    await Promise.all([fetchPhotos(), fetchBlockings()])
  }
})
</script>

<template>
  <div class="max-w-2xl mx-auto px-4 sm:px-6 lg:px-10 py-10 lg:py-14">
    <!-- Header -->
    <div class="mb-10">
      <button
        class="flex items-center gap-1.5 text-sm text-muted hover:text-ink transition-colors mb-6 cursor-pointer"
        @click="router.back()"
      >
        <svg
          width="16"
          height="16"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="2"
          stroke-linecap="round"
          stroke-linejoin="round"
        >
          <polyline points="15 18 9 12 15 6" />
        </svg>
        Back
      </button>
      <div class="flex items-start justify-between gap-4">
        <div>
          <h1 class="font-serif text-3xl font-semibold text-ink mb-1">
            {{ isEdit ? 'Edit listing' : 'Create a listing' }}
          </h1>
          <p class="text-sm text-muted">
            {{
              isEdit
                ? 'Update your listing details, photos and availability.'
                : 'Fill in the details below to publish your space.'
            }}
          </p>
        </div>
        <!-- Edit mode: view + delete actions -->
        <div v-if="isEdit" class="flex items-center gap-2 shrink-0 mt-1">
          <RouterLink
            :to="`/listing/${listingId}`"
            target="_blank"
            class="text-xs font-medium text-muted hover:text-ink transition-colors border border-divider rounded-full px-3 py-1.5"
            >View</RouterLink
          >
          <button
            class="text-xs font-medium text-rose-600 hover:text-rose-700 border border-rose-200 hover:border-rose-300 transition-colors rounded-full px-3 py-1.5 cursor-pointer"
            @click="((confirmDeleteOpen = true), (deleteError = null))"
          >
            Delete
          </button>
        </div>
      </div>
    </div>

    <!-- Loading skeleton (edit mode) -->
    <div v-if="loading" class="space-y-6">
      <div v-for="n in 5" :key="n" class="animate-pulse h-12 bg-divider-light rounded-xl" />
    </div>

    <!-- Load error -->
    <div v-else-if="loadError" class="text-center py-20">
      <p class="text-sm text-muted mb-4">{{ loadError }}</p>
      <button class="text-sm font-medium text-ink underline cursor-pointer" @click="fetchListing">
        Try again
      </button>
    </div>

    <div v-else class="space-y-8">
      <!-- Basic info -->
      <section>
        <h2 class="text-xs font-semibold text-muted uppercase tracking-widest mb-4">Basic info</h2>
        <div class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-ink mb-1.5"
              >Title <span class="text-rose-500">*</span></label
            >
            <input
              v-model="form.title"
              type="text"
              placeholder="Cosy studio in the heart of town"
              class="w-full border border-divider rounded-xl px-4 py-3 text-sm text-ink placeholder:text-muted/60 focus:outline-none focus:border-ink/40 transition-colors"
            />
          </div>

          <div>
            <label class="block text-sm font-medium text-ink mb-1.5"
              >Description <span class="text-rose-500">*</span></label
            >
            <textarea
              v-model="form.description"
              rows="5"
              placeholder="Describe your space, what makes it special, nearby attractions…"
              class="w-full border border-divider rounded-xl px-4 py-3 text-sm text-ink placeholder:text-muted/60 focus:outline-none focus:border-ink/40 transition-colors resize-none"
            />
          </div>

          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-sm font-medium text-ink mb-1.5">Category</label>
              <select
                v-model="form.category"
                class="w-full border border-divider rounded-xl px-4 py-3 text-sm text-ink focus:outline-none focus:border-ink/40 transition-colors bg-white"
              >
                <option v-for="c in CATEGORIES" :key="c" :value="c">{{ c }}</option>
              </select>
            </div>
            <div>
              <label class="block text-sm font-medium text-ink mb-1.5">Property type</label>
              <select
                v-model="form.propertyType"
                class="w-full border border-divider rounded-xl px-4 py-3 text-sm text-ink focus:outline-none focus:border-ink/40 transition-colors bg-white"
              >
                <option v-for="p in PROPERTY_TYPES" :key="p" :value="p">{{ p }}</option>
              </select>
            </div>
          </div>
        </div>
      </section>

      <div class="border-t border-divider-light" />

      <!-- Location -->
      <section>
        <h2 class="text-xs font-semibold text-muted uppercase tracking-widest mb-4">Location</h2>
        <div class="space-y-4">
          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-sm font-medium text-ink mb-1.5"
                >City <span class="text-rose-500">*</span></label
              >
              <input
                v-model="form.city"
                type="text"
                placeholder="Paris"
                class="w-full border border-divider rounded-xl px-4 py-3 text-sm text-ink placeholder:text-muted/60 focus:outline-none focus:border-ink/40 transition-colors"
              />
            </div>
            <div>
              <label class="block text-sm font-medium text-ink mb-1.5"
                >Country <span class="text-rose-500">*</span></label
              >
              <input
                v-model="form.country"
                type="text"
                placeholder="France"
                class="w-full border border-divider rounded-xl px-4 py-3 text-sm text-ink placeholder:text-muted/60 focus:outline-none focus:border-ink/40 transition-colors"
              />
            </div>
          </div>
          <div>
            <label class="block text-sm font-medium text-ink mb-1.5">
              Street address
              <span class="font-normal text-muted ml-1">(optional)</span>
            </label>
            <input
              v-model="form.street"
              type="text"
              placeholder="12 Rue de Rivoli"
              class="w-full border border-divider rounded-xl px-4 py-3 text-sm text-ink placeholder:text-muted/60 focus:outline-none focus:border-ink/40 transition-colors"
            />
          </div>
        </div>
      </section>

      <div class="border-t border-divider-light" />

      <!-- Pricing & capacity -->
      <section>
        <h2 class="text-xs font-semibold text-muted uppercase tracking-widest mb-4">
          Pricing & capacity
        </h2>
        <div class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-ink mb-1.5"
              >Price per night <span class="text-rose-500">*</span></label
            >
            <div class="relative">
              <span class="absolute left-4 top-1/2 -translate-y-1/2 text-sm text-muted">$</span>
              <input
                v-model.number="form.pricePerNight"
                type="number"
                min="1"
                max="100000"
                class="w-full border border-divider rounded-xl pl-8 pr-4 py-3 text-sm text-ink focus:outline-none focus:border-ink/40 transition-colors"
              />
            </div>
          </div>

          <div class="grid grid-cols-3 gap-4">
            <div>
              <label class="block text-sm font-medium text-ink mb-1.5">Max guests</label>
              <input
                v-model.number="form.maxGuests"
                type="number"
                min="1"
                max="50"
                class="w-full border border-divider rounded-xl px-4 py-3 text-sm text-ink focus:outline-none focus:border-ink/40 transition-colors"
              />
            </div>
            <div>
              <label class="block text-sm font-medium text-ink mb-1.5">Bedrooms</label>
              <input
                v-model.number="form.bedrooms"
                type="number"
                min="0"
                max="20"
                class="w-full border border-divider rounded-xl px-4 py-3 text-sm text-ink focus:outline-none focus:border-ink/40 transition-colors"
              />
            </div>
            <div>
              <label class="block text-sm font-medium text-ink mb-1.5">Bathrooms</label>
              <input
                v-model.number="form.bathrooms"
                type="number"
                min="0"
                max="20"
                class="w-full border border-divider rounded-xl px-4 py-3 text-sm text-ink focus:outline-none focus:border-ink/40 transition-colors"
              />
            </div>
          </div>

          <label class="flex items-center gap-3 cursor-pointer pt-1">
            <div
              class="relative w-10 h-6 rounded-full transition-colors shrink-0"
              :class="form.isInstantBook ? 'bg-ink' : 'bg-divider'"
              @click="form.isInstantBook = !form.isInstantBook"
            >
              <div
                class="absolute top-1 left-1 w-4 h-4 bg-white rounded-full shadow transition-transform"
                :class="form.isInstantBook ? 'translate-x-4' : 'translate-x-0'"
              />
            </div>
            <div>
              <p class="text-sm font-medium text-ink">Instant book</p>
              <p class="text-xs text-muted">Guests can book without waiting for approval</p>
            </div>
          </label>
        </div>
      </section>

      <div class="border-t border-divider-light" />

      <!-- Amenities -->
      <section>
        <h2 class="text-xs font-semibold text-muted uppercase tracking-widest mb-4">Amenities</h2>
        <div class="flex flex-wrap gap-2 mb-3">
          <span
            v-for="a in form.amenities"
            :key="a"
            class="flex items-center gap-1.5 text-sm bg-cream text-ink px-3 py-1.5 rounded-full"
          >
            {{ a }}
            <button
              class="text-ink/40 hover:text-rose-500 transition-colors cursor-pointer"
              @click="removeAmenity(a)"
            >
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
                <line x1="18" y1="6" x2="6" y2="18" />
                <line x1="6" y1="6" x2="18" y2="18" />
              </svg>
            </button>
          </span>
          <span v-if="form.amenities.length === 0" class="text-sm text-muted"
            >No amenities added yet.</span
          >
        </div>
        <div class="flex gap-2">
          <input
            v-model="form.amenityInput"
            type="text"
            placeholder="e.g. WiFi, Pool, Air conditioning…"
            class="flex-1 border border-divider rounded-xl px-4 py-2.5 text-sm text-ink placeholder:text-muted/60 focus:outline-none focus:border-ink/40 transition-colors"
            @keydown.enter.prevent="addAmenity"
          />
          <button
            class="text-sm font-medium border border-divider text-ink px-4 py-2.5 rounded-xl hover:border-ink/40 transition-colors cursor-pointer"
            @click="addAmenity"
          >
            Add
          </button>
        </div>
      </section>

      <div class="border-t border-divider-light" />

      <!-- Photos -->
      <section>
        <h2 class="text-xs font-semibold text-muted uppercase tracking-widest mb-1">Photos</h2>
        <p class="text-xs text-muted mb-4">
          {{
            isEdit
              ? 'Upload new photos or remove existing ones.'
              : 'Add photos now or upload them later from your dashboard.'
          }}
        </p>

        <p v-if="photoError" class="text-xs text-rose-600 mb-3">{{ photoError }}</p>

        <!-- Edit mode: existing photos grid -->
        <div v-if="isEdit">
          <div v-if="photosLoading" class="grid grid-cols-3 sm:grid-cols-4 gap-3 mb-4">
            <div
              v-for="n in 4"
              :key="n"
              class="animate-pulse aspect-square rounded-xl bg-divider-light"
            />
          </div>
          <div
            v-else-if="existingPhotos.length > 0"
            class="grid grid-cols-3 sm:grid-cols-4 gap-3 mb-4"
          >
            <div
              v-for="photo in existingPhotos"
              :key="photo.url"
              class="relative aspect-square rounded-xl overflow-hidden group bg-divider-light"
            >
              <img :src="photo.url" alt="Listing photo" class="w-full h-full object-cover" />
              <div
                class="absolute inset-0 bg-black/0 group-hover:bg-black/30 transition-colors flex items-center justify-center"
              >
                <button
                  v-if="photo.id"
                  class="opacity-0 group-hover:opacity-100 transition-opacity bg-white rounded-full p-2 shadow hover:bg-rose-50 cursor-pointer"
                  :disabled="deletingPhotoUrl === photo.url"
                  @click="deleteExistingPhoto(photo)"
                >
                  <svg
                    :class="deletingPhotoUrl === photo.url ? 'text-rose-300' : 'text-rose-500'"
                    width="14"
                    height="14"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    stroke-width="2"
                    stroke-linecap="round"
                    stroke-linejoin="round"
                  >
                    <polyline points="3 6 5 6 21 6" />
                    <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6" />
                    <path d="M10 11v6" />
                    <path d="M14 11v6" />
                    <path d="M9 6V4h6v2" />
                  </svg>
                </button>
              </div>
              <div
                v-if="photo.order === 0"
                class="absolute top-1.5 left-1.5 bg-ink text-white text-[10px] font-medium px-2 py-0.5 rounded-full"
              >
                Cover
              </div>
            </div>
          </div>

          <!-- Upload for existing listing -->
          <label
            class="flex flex-col items-center justify-center gap-3 border-2 border-dashed rounded-2xl p-6 cursor-pointer transition-colors"
            :class="
              photoUploading ? 'border-divider opacity-60' : 'border-divider hover:border-ink/30'
            "
          >
            <div class="w-10 h-10 rounded-full bg-cream flex items-center justify-center">
              <svg
                width="18"
                height="18"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                stroke-width="2"
                stroke-linecap="round"
                stroke-linejoin="round"
                class="text-ink/50"
              >
                <polyline points="16 16 12 12 8 16" />
                <line x1="12" y1="12" x2="12" y2="21" />
                <path d="M20.39 18.39A5 5 0 0 0 18 9h-1.26A8 8 0 1 0 3 16.3" />
              </svg>
            </div>
            <div class="text-center">
              <p class="text-sm font-medium text-ink">
                {{ photoUploading ? 'Uploading…' : 'Upload a photo' }}
              </p>
              <p class="text-xs text-muted mt-0.5">JPG, PNG, WebP up to 10 MB</p>
            </div>
            <input
              ref="photoInput"
              type="file"
              accept="image/*"
              class="sr-only"
              :disabled="photoUploading"
              @change="uploadExistingPhoto"
            />
          </label>
        </div>

        <!-- Create mode: staged photos with drag-and-drop -->
        <div v-else>
          <label
            class="flex flex-col items-center justify-center gap-3 border-2 border-dashed rounded-2xl p-8 cursor-pointer transition-colors mb-4"
            :class="isDragging ? 'border-ink bg-ink/5' : 'border-divider hover:border-ink/30'"
            @dragover.prevent="isDragging = true"
            @dragleave.prevent="isDragging = false"
            @drop.prevent="onDrop"
          >
            <div class="w-10 h-10 rounded-full bg-cream flex items-center justify-center">
              <svg
                width="18"
                height="18"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                stroke-width="2"
                stroke-linecap="round"
                stroke-linejoin="round"
                class="text-ink/50"
              >
                <polyline points="16 16 12 12 8 16" />
                <line x1="12" y1="12" x2="12" y2="21" />
                <path d="M20.39 18.39A5 5 0 0 0 18 9h-1.26A8 8 0 1 0 3 16.3" />
              </svg>
            </div>
            <div class="text-center">
              <p class="text-sm font-medium text-ink">Click or drag photos here</p>
              <p class="text-xs text-muted mt-0.5">JPG, PNG, WebP — any number</p>
            </div>
            <input
              ref="newPhotoInput"
              type="file"
              accept="image/*"
              multiple
              class="sr-only"
              @change="onFileInput"
            />
          </label>

          <div v-if="stagedPhotos.length > 0" class="grid grid-cols-3 sm:grid-cols-4 gap-3">
            <div
              v-for="(photo, i) in stagedPhotos"
              :key="photo.previewUrl"
              class="relative aspect-square rounded-xl overflow-hidden group bg-divider-light"
            >
              <img
                :src="photo.previewUrl"
                :alt="photo.file.name"
                class="w-full h-full object-cover"
              />
              <div
                class="absolute inset-0 bg-black/0 group-hover:bg-black/30 transition-colors flex items-center justify-center"
              >
                <button
                  type="button"
                  class="opacity-0 group-hover:opacity-100 transition-opacity bg-white rounded-full p-2 shadow hover:bg-rose-50 cursor-pointer"
                  @click.prevent="removeStagedPhoto(i)"
                >
                  <svg
                    class="text-rose-500"
                    width="14"
                    height="14"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    stroke-width="2"
                    stroke-linecap="round"
                    stroke-linejoin="round"
                  >
                    <polyline points="3 6 5 6 21 6" />
                    <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6" />
                    <path d="M10 11v6" />
                    <path d="M14 11v6" />
                    <path d="M9 6V4h6v2" />
                  </svg>
                </button>
              </div>
              <div
                v-if="i === 0"
                class="absolute top-1.5 left-1.5 bg-ink text-white text-[10px] font-medium px-2 py-0.5 rounded-full"
              >
                Cover
              </div>
            </div>
          </div>
        </div>
      </section>

      <!-- Blockings (edit mode only) -->
      <template v-if="isEdit">
        <div class="border-t border-divider-light" />

        <section>
          <h2 class="text-xs font-semibold text-muted uppercase tracking-widest mb-4">
            Blocked dates
          </h2>

          <div class="mb-5">
            <div class="grid grid-cols-2 gap-3 mb-4">
              <div>
                <label class="block text-xs text-muted mb-1">Start date</label>
                <input
                  v-model="blockingForm.startDate"
                  type="date"
                  class="w-full border border-divider bg-white rounded-xl px-3 py-2 text-sm text-ink focus:outline-none focus:border-ink/40 transition-colors"
                />
              </div>
              <div>
                <label class="block text-xs text-muted mb-1">End date</label>
                <input
                  v-model="blockingForm.endDate"
                  type="date"
                  class="w-full border border-divider bg-white rounded-xl px-3 py-2 text-sm text-ink focus:outline-none focus:border-ink/40 transition-colors"
                />
              </div>
            </div>

            <p v-if="blockingFormError" class="text-xs text-rose-600 mb-3">
              {{ blockingFormError }}
            </p>
            <p v-if="blockingSaved" class="text-xs text-emerald-600 mb-3">Blocking added</p>
            <button
              class="w-full text-sm font-medium text-white bg-ink py-2.5 rounded-xl hover:bg-black transition-colors disabled:opacity-50 cursor-pointer"
              :disabled="isSavingBlocking"
              @click="addBlocking"
            >
              {{ isSavingBlocking ? 'Saving…' : 'Block dates' }}
            </button>
          </div>

          <div v-if="blockingsLoading" class="space-y-2">
            <div v-for="n in 3" :key="n" class="h-12 animate-pulse bg-divider-light rounded-xl" />
          </div>
          <div v-else-if="blockingsError" class="text-xs text-rose-600">{{ blockingsError }}</div>
          <div v-else-if="blockings.length === 0" class="text-sm text-muted">No dates blocked.</div>
          <div v-else class="space-y-2">
            <div
              v-for="block in blockings"
              :key="block.id"
              class="flex items-center justify-between gap-4 border border-divider-light rounded-xl px-4 py-3"
            >
              <p class="text-sm text-ink">
                {{ formatDate(block.startDate) }} – {{ formatDate(block.endDate) }}
              </p>
              <button
                class="text-xs text-rose-600 hover:text-rose-700 transition-colors cursor-pointer"
                @click="removeBlocking(block.id)"
              >
                Remove
              </button>
            </div>
          </div>
        </section>
      </template>

      <!-- Submit -->
      <div class="pt-2 pb-8">
        <p v-if="submitError" class="text-sm text-rose-600 mb-4">{{ submitError }}</p>
        <p v-if="saved" class="text-sm text-emerald-600 mb-4">Changes saved.</p>

        <div v-if="uploadProgress" class="mb-4">
          <div class="flex justify-between text-xs text-muted mb-1.5">
            <span>Uploading photos</span>
            <span>{{ uploadProgress.done }}/{{ uploadProgress.total }}</span>
          </div>
          <div class="h-1.5 bg-divider-light rounded-full overflow-hidden">
            <div
              class="h-full bg-ink rounded-full transition-all duration-300"
              :style="{ width: `${(uploadProgress.done / uploadProgress.total) * 100}%` }"
            />
          </div>
        </div>

        <button
          class="w-full text-sm font-medium text-white bg-ink py-3.5 rounded-xl hover:bg-black transition-colors disabled:opacity-50 cursor-pointer"
          :disabled="isSubmitting || !canSubmit"
          @click="submit"
        >
          {{
            isSubmitting
              ? isEdit
                ? 'Saving…'
                : uploadProgress
                  ? `Uploading photos ${uploadProgress.done}/${uploadProgress.total}…`
                  : 'Creating…'
              : isEdit
                ? 'Save changes'
                : 'Create listing'
          }}
        </button>
      </div>
    </div>
  </div>

  <!-- Delete confirm dialog -->
  <Teleport to="body">
    <div
      v-if="confirmDeleteOpen"
      class="fixed inset-0 z-50 flex items-center justify-center p-4"
      @click.self="confirmDeleteOpen = false"
    >
      <div
        class="absolute inset-0 bg-black/40 backdrop-blur-sm"
        @click="confirmDeleteOpen = false"
      />
      <div class="relative bg-white rounded-2xl shadow-xl max-w-sm w-full p-6">
        <h2 class="font-serif text-xl font-semibold text-ink mb-2">Delete listing?</h2>
        <p class="text-sm text-muted mb-6">
          This cannot be undone. All photos, availability rules and associated data will be
          permanently removed.
        </p>
        <p v-if="deleteError" class="text-xs text-rose-600 mb-3">{{ deleteError }}</p>
        <div class="flex gap-3">
          <button
            class="flex-1 text-sm font-medium text-white bg-rose-600 rounded-full py-2.5 hover:bg-rose-700 transition-colors disabled:opacity-50 cursor-pointer"
            :disabled="isDeleting"
            @click="confirmDelete"
          >
            {{ isDeleting ? 'Deleting…' : 'Yes, delete' }}
          </button>
          <button
            class="flex-1 text-sm font-medium text-ink border border-divider rounded-full py-2.5 hover:border-ink/40 transition-colors cursor-pointer"
            :disabled="isDeleting"
            @click="confirmDeleteOpen = false"
          >
            Cancel
          </button>
        </div>
      </div>
    </div>
  </Teleport>
</template>
