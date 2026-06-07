<script setup lang="ts">
import type { Listing } from '@/types'

defineProps<Listing>()

const PLACEHOLDER = 'https://images.unsplash.com/photo-1518780664697-55e3ad937233?w=600&q=80'
</script>

<template>
  <RouterLink :to="`/listing/${id}`" class="group block">
    <div class="aspect-square rounded-xl overflow-hidden bg-[#e0dbd4] mb-3">
      <img
        :src="photoUrls[0] ?? PLACEHOLDER"
        :alt="title"
        loading="lazy"
        class="w-full h-full object-cover transition-transform duration-300 group-hover:scale-105"
      />
    </div>
    <div class="px-0.5">
      <div class="flex items-center justify-between mb-0.5">
        <span class="text-sm font-medium text-ink truncate">{{ location }}</span>
        <span
          v-if="averageRating !== null"
          class="flex items-center gap-1 text-[13px] font-medium text-ink shrink-0 ml-2"
        >
          <svg width="11" height="11" viewBox="0 0 24 24" fill="currentColor">
            <path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z" />
          </svg>
          {{ averageRating.toFixed(2) }}
        </span>
        <span v-else class="text-[13px] text-muted shrink-0 ml-2">New</span>
      </div>
      <p class="text-[13px] text-muted mb-1 truncate">{{ title }}</p>
      <p class="text-sm text-ink">
        <strong class="font-semibold">${{ pricePerNight }}</strong> night
        <span v-if="reviewCount > 0" class="text-muted"> · {{ reviewCount }} reviews</span>
      </p>
    </div>
  </RouterLink>
</template>
