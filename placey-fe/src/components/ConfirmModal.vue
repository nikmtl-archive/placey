<script setup lang="ts">
defineProps<{
  title: string
  message: string
  confirmLabel?: string
  cancelLabel?: string
  danger?: boolean
}>()

const emit = defineEmits<{
  confirm: []
  cancel: []
}>()

function onKey(e: KeyboardEvent) {
  if (e.key === 'Escape') emit('cancel')
}
</script>

<template>
  <Teleport to="body">
    <div
      class="fixed inset-0 z-50 flex items-center justify-center p-4"
      role="dialog"
      aria-modal="true"
      @keydown="onKey"
    >
      <!-- Backdrop -->
      <div class="absolute inset-0 bg-black/40 backdrop-blur-sm" @click="emit('cancel')" />

      <!-- Panel -->
      <div class="relative w-full max-w-sm rounded-3xl bg-white shadow-xl p-6">
        <h2 class="font-serif text-xl font-semibold text-ink mb-2">{{ title }}</h2>
        <p class="text-sm text-muted mb-6">{{ message }}</p>

        <div class="flex gap-3 justify-end">
          <button
            class="text-sm font-medium text-muted px-4 py-2 rounded-full border border-divider hover:border-ink/30 hover:text-ink transition-colors cursor-pointer"
            @click="emit('cancel')"
          >
            {{ cancelLabel ?? 'Never mind' }}
          </button>
          <button
            class="text-sm font-medium px-4 py-2 rounded-full transition-colors cursor-pointer"
            :class="
              danger
                ? 'bg-rose-600 text-white hover:bg-rose-700'
                : 'bg-ink text-white hover:bg-black'
            "
            @click="emit('confirm')"
          >
            {{ confirmLabel ?? 'Confirm' }}
          </button>
        </div>
      </div>
    </div>
  </Teleport>
</template>
