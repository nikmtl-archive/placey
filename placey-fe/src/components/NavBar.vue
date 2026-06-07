<template>
  <header class="sticky top-0 z-50 bg-white border-b border-divider-light">
    <div class="max-w-7xl mx-auto px-10 h-18 flex items-center justify-between gap-6">
      <RouterLink to="/" class="font-serif text-2xl font-semibold text-ink tracking-tight shrink-0">
        placey
      </RouterLink>

      <nav class="flex items-center gap-4 shrink-0">
        <RouterLink
          to="/become-a-host"
          class="text-sm font-medium text-ink px-3 py-2 rounded-lg hover:bg-black/5 transition-colors"
        >
          Become a Host
        </RouterLink>
        <RouterLink
          to="/login"
          class="text-sm font-medium text-white bg-dark px-5 py-2.5 rounded-full hover:bg-black transition-colors"
        >
          Log in
        </RouterLink>
      </nav>
    </div>
  </header>
</template>
