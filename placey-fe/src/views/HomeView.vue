<script setup lang="ts">
import { ref } from 'vue'
import HeroSection from '@/components/HeroSection.vue'
import FeaturedSection from '@/components/FeaturedSection.vue'
import type { ListingCategory } from '@/types'

const sections = [
  { category: 'Beach' as ListingCategory, label: 'Beach escapes' },
  { category: 'Mountains' as ListingCategory, label: 'Mountain retreats' },
  { category: 'Urban' as ListingCategory, label: 'City stays' },
  { category: 'Luxe' as ListingCategory, label: 'Luxe properties' },
  { category: 'Countryside' as ListingCategory, label: 'Countryside hideaways' },
  { category: 'Island' as ListingCategory, label: 'Island getaways' },
  { category: 'Lake' as ListingCategory, label: 'Lakeside stays' },
]

const emptyLabels = ref<string[]>([])

function onEmpty(label: string) {
  emptyLabels.value.push(label)
}
</script>

<template>
  <HeroSection />

  <div class="divide-y divide-divider-light pb-16">
    <FeaturedSection
      v-for="s in sections"
      :key="s.category"
      :category="s.category"
      :label="s.label"
      @empty="onEmpty(s.label)"
    />
  </div>

  <p v-if="emptyLabels.length" class="max-w-7xl mx-auto px-10 pb-10 text-[12px] text-muted/60">
    No listings yet for: {{ emptyLabels.join(', ') }}.
  </p>
</template>
