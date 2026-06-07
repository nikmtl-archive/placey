<script setup lang="ts">
import { Home, DollarSign, Shield, Star, ChevronRight, Clock, Users } from '@lucide/vue'
</script>

<template>
  <main>
    <!-- Hero -->
    <section class="relative bg-ink text-white overflow-hidden">
      <div class="absolute inset-0 opacity-20 bg-[radial-gradient(ellipse_at_60%_40%,#a08060_0%,transparent_70%)]" />
      <div class="relative max-w-6xl mx-auto px-6 sm:px-10 py-16 sm:py-28 grid grid-cols-1 md:grid-cols-2 gap-10 md:gap-16 items-center">
        <div>
          <p class="text-[11px] font-semibold tracking-[0.2em] uppercase text-white/50 mb-5">Hosting on Placey</p>
          <h1 class="font-serif text-[clamp(36px,5vw,64px)] font-medium leading-tight tracking-tight mb-6">
            Turn your space<br />into income
          </h1>
          <p class="text-white/60 text-lg leading-relaxed mb-10 max-w-md">
            Join now to get first host earnings. Set your own schedule, your own price, your own rules.
          </p>
          <RouterLink
            to="/listing/new"
            class="inline-flex items-center gap-2 bg-white text-ink text-sm font-semibold px-7 py-4 rounded-full hover:bg-cream transition-colors"
          >
            Get started
            <ChevronRight class="w-4 h-4" :stroke-width="2.5" />
          </RouterLink>
        </div>

        <!-- Mock earnings card -->
        <div class="flex justify-center md:justify-end">
          <div class="bg-white/10 backdrop-blur-sm border border-white/10 rounded-2xl p-8 w-72">
            <p class="text-white/50 text-xs font-semibold tracking-widest uppercase mb-1">Estimated monthly</p>
            <p class="font-serif text-5xl font-medium text-white mb-1">$2,400</p>
            <p class="text-white/40 text-sm mb-8">for 3 nights/week · 2 bed</p>
            <div class="space-y-3">
              <div class="flex items-center justify-between text-sm">
                <span class="text-white/50">Avg. nightly rate</span>
                <span class="text-white font-medium">$200</span>
              </div>
              <div class="flex items-center justify-between text-sm">
                <span class="text-white/50">Occupancy</span>
                <span class="text-white font-medium">~43%</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- Why host -->
    <section class="max-w-6xl mx-auto px-6 sm:px-10 py-16 sm:py-24">
      <p class="text-[11px] font-semibold tracking-[0.2em] uppercase text-muted mb-4 text-center">Why Placey</p>
      <h2 class="font-serif text-3xl sm:text-4xl font-medium text-ink text-center mb-12 sm:mb-16">Everything you need to host</h2>

      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6 sm:gap-8">
        <div class="group p-6 sm:p-8 rounded-2xl border border-divider-light hover:border-divider hover:shadow-sm transition-all">
          <div class="w-11 h-11 rounded-xl bg-cream flex items-center justify-center mb-5">
            <DollarSign class="w-5 h-5 text-ink" :stroke-width="2" />
          </div>
          <h3 class="text-base font-semibold text-ink mb-2">You set the price we take nothing</h3>
          <p class="text-sm text-muted leading-relaxed">
            Full control over your nightly rate and all earnings are yours to keep.
          </p>
        </div>

        <div class="group p-6 sm:p-8 rounded-2xl border border-divider-light hover:border-divider hover:shadow-sm transition-all">
          <div class="w-11 h-11 rounded-xl bg-cream flex items-center justify-center mb-5">
            <Clock class="w-5 h-5 text-ink" :stroke-width="2" />
          </div>
          <h3 class="text-base font-semibold text-ink mb-2">Host on your schedule</h3>
          <p class="text-sm text-muted leading-relaxed">
            Block dates whenever you need. Toggle if bookings need to be approved.
          </p>
        </div>

        <div class="group p-6 sm:p-8 rounded-2xl border border-divider-light hover:border-divider hover:shadow-sm transition-all sm:col-span-2 lg:col-span-1">
          <div class="w-11 h-11 rounded-xl bg-cream flex items-center justify-center mb-5">
            <Star class="w-5 h-5 text-ink" :stroke-width="2" />
          </div>
          <h3 class="text-base font-semibold text-ink mb-2">Build your reputation</h3>
          <p class="text-sm text-muted leading-relaxed">
            Verified reviews build trust with guests and push your listing higher in search.
          </p>
        </div>
      </div>
    </section>

    <!-- How it works -->
    <section class="bg-cream">
      <div class="max-w-6xl mx-auto px-6 sm:px-10 py-16 sm:py-24">
        <p class="text-[11px] font-semibold tracking-[0.2em] uppercase text-muted mb-4 text-center">Getting started</p>
        <h2 class="font-serif text-3xl sm:text-4xl font-medium text-ink text-center mb-12 sm:mb-16">List in three steps</h2>

        <div class="grid grid-cols-1 sm:grid-cols-3 gap-10 sm:gap-12">
          <div class="text-center">
            <div class="w-12 h-12 rounded-full bg-ink text-white flex items-center justify-center font-serif text-lg font-medium mx-auto mb-5">1</div>
            <h3 class="text-base font-semibold text-ink mb-2">Describe your place</h3>
            <p class="text-sm text-muted leading-relaxed">Add photos, a title, your amenities, and what makes your space special.</p>
          </div>
          <div class="text-center">
            <div class="w-12 h-12 rounded-full bg-ink text-white flex items-center justify-center font-serif text-lg font-medium mx-auto mb-5">2</div>
            <h3 class="text-base font-semibold text-ink mb-2">Set your price</h3>
            <p class="text-sm text-muted leading-relaxed">We'll suggest a competitive rate based on comparable listings nearby.</p>
          </div>
          <div class="text-center">
            <div class="w-12 h-12 rounded-full bg-ink text-white flex items-center justify-center font-serif text-lg font-medium mx-auto mb-5">3</div>
            <h3 class="text-base font-semibold text-ink mb-2">Publish and earn</h3>
            <p class="text-sm text-muted leading-relaxed">Go live instantly. Payouts hit your account 24 hours after each check-in.</p>
          </div>
        </div>
      </div>
    </section>

    <!-- CTA -->
    <section class="max-w-3xl mx-auto px-6 sm:px-10 py-20 sm:py-28 text-center">
      <h2 class="font-serif text-[clamp(28px,4vw,52px)] font-medium text-ink leading-tight mb-5">
        Ready to open<br />your doors?
      </h2>
      <p class="text-muted text-base leading-relaxed mb-10 max-w-lg mx-auto">
        It takes less than 10 minutes to publish your first listing. No subscription, no cost. We just connect you with guests and take nothing in return.
      </p>
      <RouterLink
        to="/listing/new"
        class="inline-flex items-center gap-2 bg-ink text-white text-sm font-semibold px-8 py-4 rounded-full hover:bg-black transition-colors"
      >
        Create your listing
        <ChevronRight class="w-4 h-4" :stroke-width="2.5" />
      </RouterLink>
    </section>
  </main>
</template>
