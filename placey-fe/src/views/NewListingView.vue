<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores'

const router = useRouter()
const authStore = useAuthStore()

const CATEGORIES = ['Beach', 'Mountains', 'Urban', 'Luxe', 'Countryside', 'Island', 'Lake']
const PROPERTY_TYPES = ['Apartment', 'House']

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

// Photos 

interface StagedPhoto {
  file: File
  previewUrl: string
}

const stagedPhotos = ref<StagedPhoto[]>([])
const photoInput = ref<HTMLInputElement | null>(null)
const isDragging = ref(false)

function addFiles(files: FileList | null) {
  if (!files) return
  for (const file of Array.from(files)) {
    if (!file.type.startsWith('image/')) continue
    if (stagedPhotos.value.some(p => p.file.name === file.name && p.file.size === file.size)) continue
    stagedPhotos.value.push({ file, previewUrl: URL.createObjectURL(file) })
  }
}

function onFileInput(e: Event) {
  addFiles((e.target as HTMLInputElement).files)
  if (photoInput.value) photoInput.value.value = ''
}

function onDrop(e: DragEvent) {
  isDragging.value = false
  addFiles(e.dataTransfer?.files ?? null)
}

function removePhoto(index: number) {
  const photo = stagedPhotos.value[index]
  if (photo) URL.revokeObjectURL(photo.previewUrl)
  stagedPhotos.value.splice(index, 1)
}

// Amenities

function addAmenity() {
  const val = form.value.amenityInput.trim()
  if (val && !form.value.amenities.includes(val)) {
    form.value.amenities.push(val)
  }
  form.value.amenityInput = ''
}

function removeAmenity(a: string) {
  form.value.amenities = form.value.amenities.filter(x => x !== a)
}

// Submit

const isSubmitting = ref(false)
const uploadProgress = ref<{ done: number; total: number } | null>(null)
const error = ref<string | null>(null)

const canSubmit = computed(() =>
  form.value.title.trim() &&
  form.value.description.trim() &&
  form.value.city.trim() &&
  form.value.country.trim() &&
  form.value.pricePerNight > 0
)

const submitLabel = computed(() => {
  if (!isSubmitting.value) return 'Create listing'
  if (uploadProgress.value) {
    return `Uploading photos ${uploadProgress.value.done}/${uploadProgress.value.total}…`
  }
  return 'Creating listing…'
})

async function submit() {
  if (!canSubmit.value) return
  isSubmitting.value = true
  error.value = null
  try {
    // 1. Create the listing
    const res = await authStore.apiFetch('/api/listings', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
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
      }),
    })
    if (!res.ok) {
      const err = await res.json().catch(() => ({}))
      throw new Error(err.detail ?? err.message ?? `HTTP ${res.status}`)
    }
    const created = await res.json()

    // 2. Upload photos sequentially
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
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : 'Something went wrong'
  } finally {
    isSubmitting.value = false
    uploadProgress.value = null
  }
}
</script>

<template>
  <div class="max-w-2xl mx-auto px-4 sm:px-6 lg:px-10 py-10 lg:py-14">
    <!-- Header -->
    <div class="mb-10">
      <button
        class="flex items-center gap-1.5 text-sm text-muted hover:text-ink transition-colors mb-6 cursor-pointer"
        @click="router.back()"
      >
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="15 18 9 12 15 6"/></svg>
        Back
      </button>
      <h1 class="font-serif text-3xl font-semibold text-ink mb-1">Create a listing</h1>
      <p class="text-sm text-muted">Fill in the details below to publish your space.</p>
    </div>

    <div class="space-y-8">
      <!-- Basic info -->
      <section>
        <h2 class="text-xs font-semibold text-muted uppercase tracking-widest mb-4">Basic info</h2>
        <div class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-ink mb-1.5">Title <span class="text-rose-500">*</span></label>
            <input
              v-model="form.title"
              type="text"
              placeholder="Cosy studio in the heart of town"
              class="w-full border border-divider rounded-xl px-4 py-3 text-sm text-ink placeholder:text-muted/60 focus:outline-none focus:border-ink/40 transition-colors"
            />
          </div>

          <div>
            <label class="block text-sm font-medium text-ink mb-1.5">Description <span class="text-rose-500">*</span></label>
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
              <label class="block text-sm font-medium text-ink mb-1.5">City <span class="text-rose-500">*</span></label>
              <input
                v-model="form.city"
                type="text"
                placeholder="Paris"
                class="w-full border border-divider rounded-xl px-4 py-3 text-sm text-ink placeholder:text-muted/60 focus:outline-none focus:border-ink/40 transition-colors"
              />
            </div>
            <div>
              <label class="block text-sm font-medium text-ink mb-1.5">Country <span class="text-rose-500">*</span></label>
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
        <h2 class="text-xs font-semibold text-muted uppercase tracking-widest mb-4">Pricing & capacity</h2>
        <div class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-ink mb-1.5">Price per night <span class="text-rose-500">*</span></label>
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
              <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>
            </button>
          </span>
          <span v-if="form.amenities.length === 0" class="text-sm text-muted">No amenities added yet.</span>
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
        <p class="text-xs text-muted mb-4">Add photos now or upload them later from your dashboard.</p>

        <!-- Drop zone -->
        <label
          class="flex flex-col items-center justify-center gap-3 border-2 border-dashed rounded-2xl p-8 cursor-pointer transition-colors mb-4"
          :class="isDragging ? 'border-ink bg-ink/5' : 'border-divider hover:border-ink/30'"
          @dragover.prevent="isDragging = true"
          @dragleave.prevent="isDragging = false"
          @drop.prevent="onDrop"
        >
          <div class="w-10 h-10 rounded-full bg-cream flex items-center justify-center">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="text-ink/50">
              <polyline points="16 16 12 12 8 16"/><line x1="12" y1="12" x2="12" y2="21"/>
              <path d="M20.39 18.39A5 5 0 0 0 18 9h-1.26A8 8 0 1 0 3 16.3"/>
            </svg>
          </div>
          <div class="text-center">
            <p class="text-sm font-medium text-ink">Click or drag photos here</p>
            <p class="text-xs text-muted mt-0.5">JPG, PNG, WebP — any number</p>
          </div>
          <input
            ref="photoInput"
            type="file"
            accept="image/*"
            multiple
            class="sr-only"
            @change="onFileInput"
          />
        </label>

        <!-- Preview grid -->
        <div v-if="stagedPhotos.length > 0" class="grid grid-cols-3 sm:grid-cols-4 gap-3">
          <div
            v-for="(photo, i) in stagedPhotos"
            :key="photo.previewUrl"
            class="relative aspect-square rounded-xl overflow-hidden group bg-divider-light"
          >
            <img :src="photo.previewUrl" :alt="photo.file.name" class="w-full h-full object-cover" />
            <div class="absolute inset-0 bg-black/0 group-hover:bg-black/30 transition-colors flex items-center justify-center">
              <button
                type="button"
                class="opacity-0 group-hover:opacity-100 transition-opacity bg-white rounded-full p-2 shadow hover:bg-rose-50 cursor-pointer"
                @click.prevent="removePhoto(i)"
              >
                <svg class="text-rose-500" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                  <polyline points="3 6 5 6 21 6"/>
                  <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"/>
                  <path d="M10 11v6"/><path d="M14 11v6"/><path d="M9 6V4h6v2"/>
                </svg>
              </button>
            </div>
            <div v-if="i === 0" class="absolute top-1.5 left-1.5 bg-ink text-white text-[10px] font-medium px-2 py-0.5 rounded-full">Cover</div>
          </div>
        </div>
      </section>

      <!-- Submit -->
      <div class="pt-2 pb-8">
        <p v-if="error" class="text-sm text-rose-600 mb-4">{{ error }}</p>

        <!-- Upload progress bar -->
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
          {{ submitLabel }}
        </button>
      </div>
    </div>
  </div>
</template>
