<script setup lang="ts">
defineProps<{
  photoUrls: string[]
  title: string
}>()

const emit = defineEmits<{
  open: [index: number]
}>()
</script>

<template>
  <!-- No photos -->
  <div
    v-if="photoUrls.length === 0"
    class="aspect-16/7 rounded-3xl bg-[#e8e3dc] flex flex-col items-center justify-center gap-3 text-[#b5afa8]"
  >
    <svg
      width="40"
      height="40"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      stroke-width="1.5"
      stroke-linecap="round"
      stroke-linejoin="round"
    >
      <rect x="3" y="3" width="18" height="18" rx="2" />
      <circle cx="8.5" cy="8.5" r="1.5" />
      <path d="M21 15l-5-5L5 21" />
    </svg>
    <span class="text-sm font-medium opacity-70">No photos yet</span>
  </div>

  <!-- Single photo -->
  <div
    v-else-if="photoUrls.length === 1"
    class="aspect-16/7 rounded-3xl overflow-hidden bg-[#e8e3dc] cursor-pointer"
    @click="emit('open', 0)"
  >
    <img :src="photoUrls[0]" :alt="title" class="w-full h-full object-cover" />
  </div>

  <!-- Multi-photo grid -->
  <div v-else class="grid grid-cols-4 grid-rows-2 gap-2 rounded-3xl overflow-hidden aspect-16/7">
    <div
      class="col-span-2 row-span-2 cursor-pointer overflow-hidden bg-[#e8e3dc]"
      @click="emit('open', 0)"
    >
      <img
        :src="photoUrls[0]"
        :alt="title"
        class="w-full h-full object-cover hover:scale-[1.03] transition-transform duration-500"
      />
    </div>
    <template v-for="(url, i) in photoUrls.slice(1, 5)" :key="i">
      <div
        class="relative cursor-pointer overflow-hidden bg-[#e8e3dc]"
        @click="emit('open', i + 1)"
      >
        <img
          :src="url"
          :alt="`${title} photo ${i + 2}`"
          class="w-full h-full object-cover hover:scale-[1.04] transition-transform duration-500"
        />
        <div
          v-if="i === 3 && photoUrls.length > 5"
          class="absolute inset-0 bg-black/40 flex items-center justify-center"
        >
          <span class="text-white text-sm font-semibold">+{{ photoUrls.length - 5 }} more</span>
        </div>
      </div>
    </template>
    <template v-for="i in Math.max(0, 4 - Math.min(photoUrls.length - 1, 4))" :key="`empty-${i}`">
      <div class="bg-[#e8e3dc]" />
    </template>
  </div>
</template>
