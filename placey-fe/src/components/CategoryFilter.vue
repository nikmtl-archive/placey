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
  <div class="border-b border-divider-light bg-white">
    <div class="max-w-7xl mx-auto px-2 sm:px-6 lg:px-10 overflow-x-auto scrollbar-hide">
      <div class="flex gap-0.5 py-2 w-max min-w-full justify-center">
        <button
          v-for="cat in categories"
          :key="cat.id"
          class="relative flex flex-col items-center gap-1.5 px-3 sm:px-5 py-2.5 rounded-xl transition-all duration-150 cursor-pointer"
          :class="
            modelValue === cat.id ? 'text-ink' : 'text-ink/40 hover:text-ink/70 hover:bg-black/3'
          "
          @click="select(cat.id)"
        >
          <component :is="cat.icon" class="w-5 h-5" :stroke-width="1.75" />
          <span class="text-[9.5px] font-bold tracking-widest whitespace-nowrap uppercase">
            {{ cat.label }}
          </span>
          <!-- Active underline indicator -->
          <span
            v-if="modelValue === cat.id"
            class="absolute bottom-0 left-3 right-3 h-0.5 bg-ink rounded-full"
          />
        </button>
      </div>
    </div>
  </div>
</template>
