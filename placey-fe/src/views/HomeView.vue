<script setup lang="ts">
import { ref, onMounted } from 'vue'
import HeroSection from '@/components/HeroSection.vue'
import CategoryFilter from '@/components/CategoryFilter.vue'
import ListingCard from '@/components/ListingCard.vue'
import { useListings } from '@/composables/useListings'
import type { ListingCategory } from '@/types'

const { listings, isLoading, fetchListings } = useListings()
const activeCategory = ref<ListingCategory | undefined>(undefined)

function onCategoryChange(cat: ListingCategory | undefined) {
  activeCategory.value = cat
  fetchListings({ category: cat })
}

onMounted(() => fetchListings())
</script>

<template>
  <HeroSection />
  <CategoryFilter :model-value="activeCategory" @update:model-value="onCategoryChange" />

  <section class="py-10 pb-20">
    <div class="max-w-7xl mx-auto px-10">
      <div v-if="isLoading" class="grid grid-cols-4 gap-7 gap-x-6">
        <div v-for="n in 8" :key="n" class="animate-pulse">
          <div class="aspect-square rounded-xl bg-divider-light mb-3" />
          <div class="h-3.5 bg-divider-light rounded w-3/4 mb-2" />
          <div class="h-3 bg-divider-light rounded w-1/2" />
        </div>
      </div>

      <div v-else class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-7 gap-x-6">
        <ListingCard
          v-for="listing in listings"
          :key="listing.id"
          v-bind="listing"
        />
      </div>
    </div>
  </section>
</template>
