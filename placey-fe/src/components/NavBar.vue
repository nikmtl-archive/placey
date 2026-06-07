<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'

const scrolled = ref(false)

function onScroll() {
  scrolled.value = window.scrollY > 8
}

onMounted(() => window.addEventListener('scroll', onScroll, { passive: true }))
onUnmounted(() => window.removeEventListener('scroll', onScroll))
</script>

<template>
  <header
    class="sticky top-0 z-50 bg-white transition-shadow duration-200"
    :class="scrolled ? 'shadow-[0_1px_12px_rgba(0,0,0,0.08)]' : 'border-b border-divider-light'"
  >
    <div class="max-w-7xl mx-auto px-10 h-16 flex items-center justify-between gap-6">
      <RouterLink to="/" class="font-serif text-2xl font-semibold text-ink tracking-tight shrink-0 select-none">
        placey
      </RouterLink>

      <nav class="flex items-center gap-2 shrink-0">
        <RouterLink
          to="/become-a-host"
          class="text-sm font-medium text-ink px-3.5 py-2 rounded-lg hover:bg-black/5 transition-colors"
        >
          Become a Host
        </RouterLink>
        <RouterLink
          to="/login"
          class="text-sm font-medium text-white bg-dark px-5 py-2 rounded-full hover:bg-black transition-colors"
        >
          Log in
        </RouterLink>
      </nav>
    </div>
  </header>
</template>
