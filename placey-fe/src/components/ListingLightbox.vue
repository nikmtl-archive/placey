<script setup lang="ts">
import { ChevronLeft, ChevronRight, X } from '@lucide/vue'

const props = defineProps<{
  photoUrls: string[]
  index: number
  title: string
}>()

const emit = defineEmits<{
  close: []
  'update:index': [value: number]
}>()

function prev() {
  emit('update:index', (props.index - 1 + props.photoUrls.length) % props.photoUrls.length)
}

function next() {
  emit('update:index', (props.index + 1) % props.photoUrls.length)
}

function onKey(e: KeyboardEvent) {
  if (e.key === 'ArrowLeft') prev()
  if (e.key === 'ArrowRight') next()
  if (e.key === 'Escape') emit('close')
}
</script>

<template>
  <Teleport to="body">
    <div
      class="fixed inset-0 z-200 bg-black/95 flex items-center justify-center"
      tabindex="0"
      @keydown="onKey"
    >
      <button
        class="absolute top-4 left-4 w-10 h-10 rounded-full bg-white/10 hover:bg-white/20 flex items-center justify-center text-white transition-colors cursor-pointer"
        @click="emit('close')"
      >
        <X :size="18" />
      </button>

      <span class="absolute top-4 left-1/2 -translate-x-1/2 text-white/60 text-sm tabular-nums">
        {{ index + 1 }} / {{ photoUrls.length }}
      </span>

      <button
        v-if="photoUrls.length > 1"
        class="absolute left-4 top-1/2 -translate-y-1/2 w-10 h-10 rounded-full bg-white/10 hover:bg-white/20 flex items-center justify-center text-white transition-colors cursor-pointer"
        @click="prev"
      >
        <ChevronLeft :size="20" />
      </button>

      <img
        :src="photoUrls[index]"
        :alt="`${title} – photo ${index + 1}`"
        class="max-h-[90vh] max-w-[90vw] object-contain rounded-lg"
      />

      <button
        v-if="photoUrls.length > 1"
        class="absolute right-4 top-1/2 -translate-y-1/2 w-10 h-10 rounded-full bg-white/10 hover:bg-white/20 flex items-center justify-center text-white transition-colors cursor-pointer"
        @click="next"
      >
        <ChevronRight :size="20" />
      </button>

      <div
        v-if="photoUrls.length > 1"
        class="absolute bottom-4 left-1/2 -translate-x-1/2 flex gap-2 max-w-[90vw] overflow-x-auto scrollbar-hide px-2"
      >
        <button
          v-for="(url, i) in photoUrls"
          :key="i"
          class="w-12 h-9 rounded-md overflow-hidden shrink-0 border-2 transition-all cursor-pointer"
          :class="
            i === index
              ? 'border-white opacity-100'
              : 'border-transparent opacity-50 hover:opacity-75'
          "
          @click="emit('update:index', i)"
        >
          <img :src="url" :alt="`thumb ${i + 1}`" class="w-full h-full object-cover" />
        </button>
      </div>
    </div>
  </Teleport>
</template>
