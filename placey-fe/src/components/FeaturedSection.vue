<script setup lang="ts">
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'
import ListingCard from '@/components/ListingCard.vue'
import { useListings } from '@/composables/useListings'
import type { ListingCategory } from '@/types'

const props = defineProps<{
  category: ListingCategory
  label: string
}>()

const emit = defineEmits<{ empty: [] }>()

const router = useRouter()
const { listings, isLoading, fetchListings } = useListings()

onMounted(async () => {
  await fetchListings({ category: props.category, limit: 5 })
  if (!listings.value.length) emit('empty')
})

function viewAll() {
  router.push({ name: 'search', query: { category: props.category } })
}
</script>

<template>
  <section v-if="isLoading || listings.length" class="py-8">
    <div class="max-w-7xl mx-auto px-10">
      <!-- Section header -->
      <div class="flex items-center justify-between mb-4">
        <h2 class="font-serif text-lg font-medium text-ink tracking-tight">{{ label }}</h2>
        <button
          class="text-[13px] font-semibold text-ink underline underline-offset-4 decoration-divider hover:decoration-ink transition-colors shrink-0 cursor-pointer"
          @click="viewAll"
        >
          View all
        </button>
      </div>

      <!-- Skeleton -->
      <div v-if="isLoading" class="grid grid-cols-2 lg:grid-cols-3 gap-6">
        <div v-for="n in 3" :key="n" class="animate-pulse">
          <div class="aspect-4/3 rounded-2xl bg-divider-light mb-3" />
          <div class="h-3.5 bg-divider-light rounded w-3/4 mb-2" />
          <div class="h-3 bg-divider-light rounded w-1/2" />
        </div>
      </div>

      <!-- Card grid -->
      <div
        v-else
        class="grid grid-cols-2 lg:grid-cols-4 gap-6"
      >
        <ListingCard
          v-for="listing in listings"
          :key="listing.id"
          v-bind="listing"
        />
      </div>
    </div>
  </section>
</template>
