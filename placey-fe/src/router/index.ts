import { createRouter, createWebHistory } from 'vue-router'

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

    // ── Listings ────────────────────────────────────────────────────────────
    { path: '/listing/:id', name: 'listing', component: () => import('@/views/ListingView.vue'), meta: { title: 'Placey – Listing' } },
    {
      path: '/search',
      name: 'search',
      component: () => import('@/views/SearchView.vue'),
      meta: { title: 'Placey – Search' },
    },

    // ── Hosting ─────────────────────────────────────────────────────────────
    { path: '/become-a-host', name: 'become-a-host', component: () => import('@/views/BecomeAHostView.vue'), meta: { title: 'Placey – Become a Host' } },

    // ── Auth ────────────────────────────────────────────────────────────────
    // { path: '/login',    name: 'login',    component: () => import('@/views/LoginView.vue') },
    // { path: '/register', name: 'register', component: () => import('@/views/RegisterView.vue') },

    // ── Info / placeholder ───────────────────────────────────────────────────
    { path: '/about',   name: 'about',   component: () => import('@/views/PlaceholderView.vue'), meta: { title: 'Placey – About' } },
    { path: '/privacy', name: 'privacy', component: () => import('@/views/PlaceholderView.vue'), meta: { title: 'Placey – Privacy' } },
    { path: '/terms',   name: 'terms',   component: () => import('@/views/PlaceholderView.vue'), meta: { title: 'Placey – Terms' } },

    // ── Catch-all ───────────────────────────────────────────────────────────
    // { path: '/:pathMatch(.*)*', name: 'not-found', component: () => import('@/views/NotFoundView.vue') },
  ],
})

router.afterEach((to) => {
  const title = to.meta.title
  if (title) document.title = title
})

export default router
