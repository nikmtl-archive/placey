<script setup lang="ts">
import { computed, watch } from 'vue'
import { useRoute } from 'vue-router'
import NavBar from '@/components/NavBar.vue'
import SiteFooter from '@/components/SiteFooter.vue'
import { useAuthStore, useMessagingStore } from '@/stores'

const authStore = useAuthStore()
const messagingStore = useMessagingStore()
const route = useRoute()

const isMessaging = computed(() => route.path.startsWith('/messages'))

watch(
  () => authStore.isLoggedIn,
  (loggedIn) => {
    if (loggedIn) {
      messagingStore.connectStream()
    } else {
      messagingStore.disconnectStream()
    }
  },
  { immediate: true },
)
</script>

<template>
  <div class="flex flex-col min-h-screen">
    <NavBar />
    <main class="flex-1">
      <RouterView />
    </main>
    <SiteFooter v-if="!isMessaging" />
  </div>
</template>
