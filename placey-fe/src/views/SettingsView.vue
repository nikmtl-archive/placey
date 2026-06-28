<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useAuthStore, parseErrorMessage } from '@/stores/useAuthStore'
import { useRouter } from 'vue-router'

const authStore = useAuthStore()
const router = useRouter()

const name = ref('')
const bio = ref('')
const isSaving = ref(false)
const saveError = ref<string | null>(null)
const saveSuccess = ref(false)

const isUploadingAvatar = ref(false)
const avatarError = ref<string | null>(null)
const avatarInput = ref<HTMLInputElement | null>(null)

const ALLOWED_IMAGE_TYPES = ['image/jpeg', 'image/png', 'image/webp']
const MAX_AVATAR_BYTES = 5 * 1024 * 1024

onMounted(() => {
  name.value = authStore.user?.name ?? ''
  bio.value = authStore.user?.bio ?? ''
})

async function saveProfile() {
  const trimmedName = name.value.trim()
  if (!trimmedName) {
    saveError.value = 'Name is required.'
    return
  }
  if (trimmedName.length > 100) {
    saveError.value = 'Name must be 100 characters or fewer.'
    return
  }
  if (bio.value.length > 500) {
    saveError.value = 'Bio must be 500 characters or fewer.'
    return
  }

  isSaving.value = true
  saveError.value = null
  saveSuccess.value = false

  try {
    const res = await authStore.apiFetch('/api/users/me', {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ name: trimmedName, bio: bio.value.trim() || null }),
    })
    if (!res.ok) throw new Error(await parseErrorMessage(res))
    const updated = await res.json()
    authStore.patchUser({ name: updated.name, bio: updated.bio ?? null })
    saveSuccess.value = true
    setTimeout(() => {
      saveSuccess.value = false
    }, 3000)
  } catch (e: unknown) {
    saveError.value = e instanceof Error ? e.message : 'Failed to save changes'
  } finally {
    isSaving.value = false
  }
}

async function uploadAvatar(event: Event) {
  const file = (event.target as HTMLInputElement).files?.[0]
  if (!file) return

  avatarError.value = null

  if (!ALLOWED_IMAGE_TYPES.includes(file.type)) {
    avatarError.value = 'Only JPEG, PNG, and WebP images are allowed.'
    if (avatarInput.value) avatarInput.value.value = ''
    return
  }
  if (file.size > MAX_AVATAR_BYTES) {
    avatarError.value = 'Image must be under 5 MB.'
    if (avatarInput.value) avatarInput.value.value = ''
    return
  }

  isUploadingAvatar.value = true

  try {
    const formData = new FormData()
    formData.append('file', file)

    const res = await authStore.apiFetch('/api/users/me/avatar', {
      method: 'POST',
      body: formData,
    })
    if (!res.ok) throw new Error(await parseErrorMessage(res))
    const updated = await res.json()
    authStore.patchUser({ avatarUrl: updated.url })
  } catch (e: unknown) {
    avatarError.value = e instanceof Error ? e.message : 'Failed to upload photo'
  } finally {
    isUploadingAvatar.value = false
    if (avatarInput.value) avatarInput.value.value = ''
  }
}

const initials = () => {
  const n = authStore.user?.name
  if (!n) return '?'
  return n
    .split(' ')
    .map((w: string) => w[0])
    .join('')
    .toUpperCase()
    .slice(0, 2)
}

const showDeleteConfirm = ref(false)
const isDeleting = ref(false)
const deleteError = ref<string | null>(null)

async function deleteAccount() {
  isDeleting.value = true
  deleteError.value = null
  try {
    const res = await authStore.apiFetch('/api/users/me', { method: 'DELETE' })
    if (!res.ok) throw new Error(await parseErrorMessage(res))
    await authStore.logout()
    router.push('/')
  } catch (e: unknown) {
    deleteError.value = e instanceof Error ? e.message : 'Failed to delete account'
    isDeleting.value = false
  }
}
</script>

<template>
  <div class="max-w-2xl mx-auto px-4 sm:px-6 lg:px-10 py-10 lg:py-14">
    <div class="mb-10">
      <h1 class="font-serif text-3xl font-semibold text-ink mb-1">Account settings</h1>
      <p class="text-sm text-muted">Manage your profile and account</p>
    </div>

    <!-- Avatar -->
    <section class="mb-10">
      <h2 class="text-sm font-semibold text-ink mb-4">Profile photo</h2>
      <div class="flex items-center gap-5">
        <div
          class="w-20 h-20 rounded-full bg-[#e8e3dc] border border-divider-light overflow-hidden flex items-center justify-center shrink-0"
        >
          <img
            v-if="authStore.user?.avatarUrl"
            :src="authStore.user.avatarUrl"
            :alt="authStore.user?.name"
            class="w-full h-full object-cover"
          />
          <span v-else class="text-xl font-semibold text-ink/60 select-none">{{ initials() }}</span>
        </div>

        <div>
          <input
            ref="avatarInput"
            type="file"
            accept="image/jpeg,image/png,image/webp"
            class="hidden"
            @change="uploadAvatar"
          />
          <button
            class="text-sm font-medium text-ink border border-divider rounded-full px-4 py-2 hover:border-ink/30 hover:shadow-sm transition-all disabled:opacity-50 cursor-pointer"
            :disabled="isUploadingAvatar"
            @click="avatarInput?.click()"
          >
            {{ isUploadingAvatar ? 'Uploading…' : 'Change photo' }}
          </button>
          <p v-if="avatarError" class="text-xs text-rose-600 mt-2">{{ avatarError }}</p>
          <p class="text-xs text-muted mt-1.5">JPG, PNG or WebP · max 5 MB</p>
        </div>
      </div>
    </section>

    <!-- Profile info -->
    <section class="mb-10">
      <h2 class="text-sm font-semibold text-ink mb-4">Profile info</h2>
      <form @submit.prevent="saveProfile" class="space-y-5">
        <div>
          <label for="settings-name" class="block text-sm text-muted mb-1.5">Name</label>
          <input
            id="settings-name"
            v-model="name"
            type="text"
            required
            maxlength="100"
            autocomplete="name"
            placeholder="Your name"
            class="w-full border border-divider rounded-xl px-4 py-2.5 text-sm text-ink placeholder:text-muted/60 focus:outline-none focus:border-ink/40 transition-colors"
          />
        </div>

        <div>
          <label for="settings-bio" class="block text-sm text-muted mb-1.5">Bio</label>
          <textarea
            id="settings-bio"
            v-model="bio"
            rows="4"
            maxlength="500"
            placeholder="Tell hosts and guests a little about yourself…"
            class="w-full border border-divider rounded-xl px-4 py-2.5 text-sm text-ink placeholder:text-muted/60 focus:outline-none focus:border-ink/40 transition-colors resize-none"
          />
        </div>

        <div>
          <label class="block text-sm text-muted mb-1.5">Email</label>
          <p class="text-sm text-ink px-4 py-2.5 bg-black/[0.03] border border-divider rounded-xl">
            {{ authStore.user?.email }}
          </p>
        </div>

        <div class="flex items-center gap-4 pt-1">
          <button
            type="submit"
            class="text-sm font-medium text-white bg-ink px-6 py-2.5 rounded-full hover:bg-black transition-colors disabled:opacity-50 cursor-pointer"
            :disabled="isSaving || !name.trim()"
          >
            {{ isSaving ? 'Saving…' : 'Save changes' }}
          </button>

          <Transition name="fade">
            <p v-if="saveSuccess" class="text-sm text-emerald-600 font-medium">Saved!</p>
          </Transition>

          <p v-if="saveError" class="text-sm text-rose-600">{{ saveError }}</p>
        </div>
      </form>
    </section>

    <!-- Danger zone -->
    <section class="mb-10 border border-rose-200 rounded-3xl p-6">
      <h2 class="text-sm font-semibold text-rose-600 mb-1">Danger zone</h2>
      <p class="text-sm text-muted mb-4">
        Permanently delete your account and all associated data. This cannot be undone.
      </p>

      <div v-if="!showDeleteConfirm">
        <button
          class="text-sm font-medium text-rose-600 border border-rose-200 rounded-full px-4 py-2 hover:bg-rose-50 transition-colors cursor-pointer"
          @click="showDeleteConfirm = true"
        >
          Delete account
        </button>
      </div>

      <div v-else class="space-y-3">
        <p class="text-sm font-medium text-ink">
          Are you sure? This will delete all your data including bookings, messages, and listings.
        </p>
        <div class="flex items-center gap-3">
          <button
            class="text-sm font-medium text-white bg-rose-600 px-5 py-2 rounded-full hover:bg-rose-700 transition-colors disabled:opacity-50 cursor-pointer"
            :disabled="isDeleting"
            @click="deleteAccount"
          >
            {{ isDeleting ? 'Deleting…' : 'Yes, delete my account' }}
          </button>
          <button
            class="text-sm text-muted hover:text-ink transition-colors cursor-pointer"
            :disabled="isDeleting"
            @click="showDeleteConfirm = false"
          >
            Cancel
          </button>
        </div>
        <p v-if="deleteError" class="text-xs text-rose-600">{{ deleteError }}</p>
      </div>
    </section>
  </div>
</template>

<style scoped>
.fade-enter-active,
.fade-leave-active {
  transition: opacity 300ms ease;
}
.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>
