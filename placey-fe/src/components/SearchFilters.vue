<script setup lang="ts">
import { useCategories } from '@/composables/useCategories'
import type { ListingCategory, PropertyType } from '@/types'

defineProps<{
  activeCategory: ListingCategory | undefined
  activePropertyType: PropertyType | undefined
  activeMinBedrooms: number | undefined
  minPriceInput: string
  maxPriceInput: string
  compact: boolean
}>()

const emit = defineEmits<{
  toggleCategory: [id: ListingCategory]
  togglePropertyType: [type: PropertyType]
  toggleBedrooms: [n: number]
  applyPrice: []
  'update:minPriceInput': [value: string]
  'update:maxPriceInput': [value: string]
}>()

const { categories } = useCategories()
</script>

<template>
  <div class="space-y-6" :class="compact ? 'space-y-7' : 'space-y-6'">
    <!-- Category -->
    <div>
      <p class="text-[11px] font-bold tracking-widest text-ink/40 uppercase mb-3">Category</p>
      <div class="flex flex-wrap" :class="compact ? 'gap-2' : 'gap-1.5'">
        <button
          v-for="cat in categories"
          :key="cat.id"
          class="flex items-center gap-1.5 rounded-full border font-medium transition-all cursor-pointer"
          :class="[
            compact ? 'px-3.5 py-2 text-sm' : 'px-3 py-1.5 text-[12px]',
            activeCategory === cat.id
              ? 'bg-ink text-white border-ink'
              : 'bg-white text-ink border-divider hover:border-ink/40',
          ]"
          @click="emit('toggleCategory', cat.id as ListingCategory)"
        >
          <component
            :is="cat.icon"
            :class="compact ? 'w-4 h-4' : 'w-3.5 h-3.5'"
            shrink-0
            :stroke-width="2"
          />
          {{ cat.label }}
        </button>
      </div>
    </div>

    <div class="h-px bg-divider-light" />

    <!-- Property type -->
    <div>
      <p class="text-[11px] font-bold tracking-widest text-ink/40 uppercase mb-3">Property type</p>
      <div class="flex" :class="compact ? 'gap-3' : 'gap-2'">
        <button
          v-for="type in ['Apartment', 'House'] as PropertyType[]"
          :key="type"
          class="flex-1 border font-medium transition-all cursor-pointer"
          :class="[
            compact ? 'py-3 rounded-2xl text-sm' : 'py-2 rounded-xl text-[13px]',
            activePropertyType === type
              ? 'bg-ink text-white border-ink'
              : 'bg-white text-ink border-divider hover:border-ink/40',
          ]"
          @click="emit('togglePropertyType', type)"
        >
          {{ type }}
        </button>
      </div>
    </div>

    <div class="h-px bg-divider-light" />

    <!-- Bedrooms -->
    <div>
      <p class="text-[11px] font-bold tracking-widest text-ink/40 uppercase mb-3">Min bedrooms</p>
      <div class="flex" :class="compact ? 'gap-2.5' : 'gap-2'">
        <button
          v-for="n in [1, 2, 3, 4, 5]"
          :key="n"
          class="rounded-full border font-medium transition-all cursor-pointer"
          :class="[
            compact ? 'w-12 h-12 text-sm' : 'w-10 h-10 text-[13px]',
            activeMinBedrooms === n
              ? 'bg-ink text-white border-ink'
              : 'bg-white text-ink border-divider hover:border-ink/40',
          ]"
          @click="emit('toggleBedrooms', n)"
        >
          {{ n }}+
        </button>
      </div>
    </div>

    <div class="h-px bg-divider-light" />

    <!-- Price -->
    <div>
      <p class="text-[11px] font-bold tracking-widest text-ink/40 uppercase mb-3">
        Price per night
      </p>
      <div class="flex items-center" :class="compact ? 'gap-3' : 'gap-2'">
        <input
          :value="minPriceInput"
          type="number"
          placeholder="Min $"
          min="0"
          class="w-full border border-divider text-ink placeholder:text-dim outline-none focus:border-ink/40 transition-colors bg-white"
          :class="compact ? 'rounded-2xl px-4 py-3 text-sm' : 'rounded-xl px-3 py-2 text-[13px]'"
          @input="emit('update:minPriceInput', ($event.target as HTMLInputElement).value)"
          @blur="emit('applyPrice')"
          @keydown.enter="emit('applyPrice')"
        />
        <span class="text-dim shrink-0" :class="compact ? '' : 'text-sm'">–</span>
        <input
          :value="maxPriceInput"
          type="number"
          placeholder="Max $"
          min="0"
          class="w-full border border-divider text-ink placeholder:text-dim outline-none focus:border-ink/40 transition-colors bg-white"
          :class="compact ? 'rounded-2xl px-4 py-3 text-sm' : 'rounded-xl px-3 py-2 text-[13px]'"
          @input="emit('update:maxPriceInput', ($event.target as HTMLInputElement).value)"
          @blur="emit('applyPrice')"
          @keydown.enter="emit('applyPrice')"
        />
      </div>
    </div>
  </div>
</template>
