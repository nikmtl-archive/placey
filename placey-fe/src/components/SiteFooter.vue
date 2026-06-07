<template>
  <footer class="border-t border-divider-light pt-12 pb-8">
    <div class="max-w-7xl mx-auto px-10">
      <div class="grid grid-cols-4 gap-10 mb-10">
        <div v-for="col in columns" :key="col.title">
          <h3 class="text-[10px] font-bold tracking-widest text-ink mb-4">{{ col.title }}</h3>
          <ul class="flex flex-col gap-2.5">
            <li v-for="link in col.links" :key="link.label">
              <a href="#" class="text-sm text-ink hover:opacity-60 transition-opacity">
                {{ link.label }}
              </a>
            </li>
          </ul>
        </div>
      </div>

      <div class="flex items-center justify-between pt-6 border-t border-divider-light">
        <span class="text-[13px] text-muted">© 2026 Placey, Inc. All rights reserved.</span>
        <nav class="flex gap-6">
          <a v-for="link in legalLinks" :key="link" href="#" class="text-[13px] text-ink hover:opacity-60 transition-opacity">
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
    title: 'BOOKING',
    links: [{ label: 'Find a place' }, { label: 'Manage bookings' }, { label: 'Cancellation options' }],
  },
  {
    title: 'HOSTING',
    links: [{ label: 'Become a host' }, { label: 'Create a listing' }, { label: 'Manage bookings' }],
  },
  {
    title: 'PLACEY',
    links: [{ label: 'About' }],
  },
]

const legalLinks = ['Privacy']
</script>
