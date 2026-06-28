<script setup lang="ts">
import SearchBar from '@/components/SearchBar.vue'
</script>

<template>
  <section class="relative pt-10 pb-10 sm:pt-16 sm:pb-14 px-4 sm:px-8 lg:px-10 text-center">
    <!-- Subtle radial gradient blob -->
    <div class="pointer-events-none absolute inset-0 flex items-start justify-center -top-20">
      <div
        class="w-[700px] h-[500px] rounded-full bg-[radial-gradient(ellipse_at_center,rgba(210,190,160,0.22)_0%,transparent_70%)]"
      />
    </div>

    <div class="relative max-w-2xl mx-auto">
      <h1
        class="font-serif text-[clamp(44px,5.5vw,68px)] font-medium leading-[1.08] tracking-tight text-ink mb-4"
      >
        Find your next <span class="font-script italic">escape</span>
      </h1>
      <p class="text-[15px] text-muted leading-relaxed mb-9 max-w-md mx-auto">
        Discover unique spaces to stay — from cozy mountain cabins to minimalist beachfront villas.
      </p>

      <SearchBar />
    </div>
  </section>
</template>
