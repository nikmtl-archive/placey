<template>
  <div class="min-h-[60vh] flex flex-col items-center justify-center px-6 text-center">
    <span class="font-serif text-3xl font-semibold text-ink tracking-tight mb-4">placey</span>
    <h1 class="text-xl font-semibold text-ink mb-3">{{ title }}</h1>
    <p class="text-[14px] text-muted max-w-sm leading-relaxed">
      This is a university project. Placey is not a real service.
    </p>
    <RouterLink
      to="/"
      class="mt-8 text-[13px] text-ink underline underline-offset-4 hover:text-muted transition-colors"
    >
      Back to home
    </RouterLink>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'

const route = useRoute()

const titles: Record<string, string> = {
  privacy: 'Privacy Policy',
  terms: 'Terms of Service',
  about: 'About Placey',
}

const title = computed(() => titles[route.name as string] ?? 'Page not found')
</script>
