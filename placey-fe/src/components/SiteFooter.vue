<template>
  <footer class="border-t border-divider-light bg-white pt-12 pb-8">
    <div class="max-w-7xl mx-auto px-10">
      <div class="grid grid-cols-4 gap-10 mb-10">
        <!-- Brand column -->
        <div>
          <span class="font-serif text-xl font-semibold text-ink tracking-tight">placey</span>
          <p class="text-[13px] text-muted mt-2 leading-relaxed">
            Unique spaces for every kind of escape.
          </p>
        </div>

        <div v-for="col in columns" :key="col.title">
          <h3 class="text-[9px] font-bold tracking-widest text-ink/50 uppercase mb-4">{{ col.title }}</h3>
          <ul class="flex flex-col gap-2.5">
            <li v-for="link in col.links" :key="link.label">
              <a href="#" class="text-[13px] text-ink hover:text-muted transition-colors">
                {{ link.label }}
              </a>
            </li>
          </ul>
        </div>
      </div>

      <div class="flex items-center justify-between pt-6 border-t border-divider-light">
        <span class="text-[12px] text-muted">© 2026 Placey, Inc. All rights reserved.</span>
        <nav class="flex gap-6">
          <a v-for="link in legalLinks" :key="link" href="#" class="text-[12px] text-muted hover:text-ink transition-colors">
            {{ link }}
          </a>
        </nav>
      </div>
    </div>
  </footer>
</template>

<script setup lang="ts">
const columns = [
  {
    title: 'Booking',
    links: [{ label: 'Find a place' }, { label: 'Manage bookings' }, { label: 'Cancellation options' }],
  },
  {
    title: 'Hosting',
    links: [{ label: 'Become a host' }, { label: 'Create a listing' }, { label: 'Manage bookings' }],
  },
  {
    title: 'Company',
    links: [{ label: 'About' }, { label: 'Careers' }, { label: 'Press' }],
  },
]

const legalLinks = ['Privacy', 'Terms', 'Sitemap']
</script>
