import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores'

declare module 'vue-router' {
  interface RouteMeta {
    requiresAuth?: boolean
    title?: string
  }
}

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  scrollBehavior: () => ({ top: 0 }),
  routes: [
    {
      path: '/',
      name: 'home',
      component: () => import('@/views/HomeView.vue'),
      meta: { title: 'Placey – Find your next escape' },
    },

    // Listings
    {
      path: '/listing/:id',
      name: 'listing',
      component: () => import('@/views/ListingView.vue'),
      meta: { title: 'Placey – Listing' },
    },
    {
      path: '/search',
      name: 'search',
      component: () => import('@/views/SearchView.vue'),
      meta: { title: 'Placey – Search' },
    },

    // Favorites
    {
      path: '/favorites',
      name: 'favorites',
      component: () => import('@/views/FavoritesView.vue'),
      meta: { title: 'Placey – Favorites', requiresAuth: true },
    },

    // Host profile
    {
      path: '/host/:id',
      name: 'host-profile',
      component: () => import('@/views/HostProfileView.vue'),
      meta: { title: 'Placey – Host Profile' },
    },

    // Hosting
    {
      path: '/become-a-host',
      name: 'become-a-host',
      component: () => import('@/views/BecomeAHostView.vue'),
      meta: { title: 'Placey – Become a Host' },
    },

    // Bookings & hosting dashboard
    {
      path: '/bookings',
      name: 'bookings',
      component: () => import('@/views/PlaceholderView.vue'),
      meta: { title: 'Placey – My Bookings', requiresAuth: true },
    },
    {
      path: '/host-dashboard',
      name: 'host-dashboard',
      component: () => import('@/views/PlaceholderView.vue'),
      meta: { title: 'Placey – Host Dashboard', requiresAuth: true },
    },

    // Auth
    {
      path: '/login',
      name: 'login',
      component: () => import('@/views/AuthView.vue'),
      meta: { title: 'Placey – Log in' },
    },
    {
      path: '/register',
      name: 'register',
      component: () => import('@/views/AuthView.vue'),
      meta: { title: 'Placey – Sign up' },
    },

    // Info / placeholder
    {
      path: '/about',
      name: 'about',
      component: () => import('@/views/PlaceholderView.vue'),
      meta: { title: 'Placey – About' },
    },
    {
      path: '/privacy',
      name: 'privacy',
      component: () => import('@/views/PlaceholderView.vue'),
      meta: { title: 'Placey – Privacy' },
    },
    {
      path: '/terms',
      name: 'terms',
      component: () => import('@/views/PlaceholderView.vue'),
      meta: { title: 'Placey – Terms' },
    },

    // Messaging
    {
      path: '/messages/:id?',
      name: 'messages',
      component: () => import('@/views/MessagesView.vue'),
      meta: { requiresAuth: true, title: 'Placey – Messages' },
    },

    // Catch-all
    {
      path: '/:pathMatch(.*)*',
      name: 'not-found',
      component: () => import('@/views/PlaceholderView.vue'),
    },
  ],
})

router.beforeEach((to, _from, next) => {
  if (to.meta.requiresAuth && !useAuthStore().isLoggedIn) {
    next({ name: 'login', query: { redirect: to.fullPath } })
  } else {
    next()
  }
})

router.afterEach((to) => {
  const title = to.meta.title
  if (title) document.title = title
})

export default router
