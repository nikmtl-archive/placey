<script setup lang="ts">
import { useCategories } from '@/composables/useCategories'
import type { ListingCategory } from '@/types'

const props = defineProps<{ modelValue: ListingCategory | undefined }>()
const emit = defineEmits<{ 'update:modelValue': [value: ListingCategory | undefined] }>()

const { categories } = useCategories()

function select(id: string) {
  const cat = id as ListingCategory
  emit('update:modelValue', props.modelValue === cat ? undefined : cat)
}
</script>

<template>
  <div class="border-y border-divider-light">
    <div class=" max-w-7xl mx-auto px-10 overflow-x-auto scrollbar-hide flex items-center justify-center">
      <div class="flex gap-1 py-3 w-max">
        <button
          v-for="cat in categories"
          :key="cat.id"
          class="flex flex-col items-center gap-1.5 px-4 py-2 border-b-2 transition-all cursor-pointer"
          :class="
            modelValue === cat.id
              ? 'opacity-100 border-ink'
              : 'opacity-55 border-transparent hover:opacity-85 hover:bg-black/3 rounded-sm'
          "
          @click="select(cat.id)"
        >
          <span class="text-[22px] leading-none">{{ cat.icon }}</span>
          <span class="text-[10px] font-semibold tracking-widest text-ink whitespace-nowrap">
            {{ cat.label }}
          </span>
        </button>
      </div>
    </div>
  </div>
</template>
