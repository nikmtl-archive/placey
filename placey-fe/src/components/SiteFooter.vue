<template>
  <footer class="border-t border-divider-light bg-white pt-12 pb-8">
    <div class="max-w-7xl mx-auto px-6 sm:px-10">
      <div class="grid grid-cols-2 sm:grid-cols-4 gap-8 sm:gap-10 mb-10">
        <!-- Brand column -->
        <div class="col-span-2 sm:col-span-1">
          <RouterLink to="/" class="font-serif text-xl font-semibold text-ink tracking-tight"
            >placey</RouterLink
          >
          <p class="text-[13px] text-muted mt-2 leading-relaxed">
            Unique spaces for every kind of escape.
          </p>
        </div>

        <div v-for="col in columns" :key="col.title">
          <h3 class="text-[9px] font-bold tracking-widest text-ink/50 uppercase mb-4">
            {{ col.title }}
          </h3>
          <ul class="flex flex-col gap-2.5">
            <li v-for="link in col.links" :key="link.label">
              <RouterLink v-if="link.to" :to="link.to" class="text-[13px]">{{
                link.label
              }}</RouterLink>
              <span v-else class="text-[13px] text-ink/30 cursor-default">{{ link.label }}</span>
            </li>
          </ul>
        </div>
      </div>

      <div
        class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 pt-6 border-t border-divider-light"
      >
        <span class="text-[12px] text-muted">© 2026 Placey, University Project</span>
        <nav class="flex gap-6">
          <RouterLink
            v-for="link in legalLinks"
            :key="link.label"
            :to="link.to"
            class="text-[12px]"
            >{{ link.label }}</RouterLink
          >
        </nav>
      </div>
    </div>
  </footer>
</template>

<script setup lang="ts">
const columns = [
  {
    title: 'Booking',
    links: [
      { label: 'Find a place', to: '/search' },
      { label: 'Manage bookings' },
      { label: 'Cancellation options' },
    ],
  },
  {
    title: 'Hosting',
    links: [
      { label: 'Become a host', to: '/become-a-host' },
      { label: 'Create a listing' },
      { label: 'Manage bookings' },
    ],
  },
  {
    title: 'Placey',
    links: [{ label: 'About', to: '/about' }],
  },
]

const legalLinks = [
  { label: 'Privacy', to: '/privacy' },
  { label: 'Terms', to: '/terms' },
]
</script>
