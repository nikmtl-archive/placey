import { createApp, watch } from 'vue'
import { createPinia } from 'pinia'

import App from './App.vue'
import router from './router'
import './assets/main.css'

import { useAuthStore, useFavoritesStore } from '@/stores'

const app   = createApp(App)
const pinia = createPinia()

app.use(pinia)
app.use(router)

const authStore      = useAuthStore()
const favoritesStore = useFavoritesStore()

// Hydrate favorites when logged in (including restored sessions from localStorage)
watch(
  () => authStore.isLoggedIn,
  (loggedIn) => {
    if (loggedIn) favoritesStore.hydrate()
    else          favoritesStore.reset()
  },
  { immediate: true },
)

app.mount('#app')
