import { createRouter, createWebHistory } from 'vue-router'

import HomeView from '../views/HomeView.vue'
import LoginView from '../views/LoginView.vue'
import PerfilView from '../views/PerfilView.vue'
import { clearAuthSession, isAuthenticated } from '../services/authStorage'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      name: 'home',
      component: HomeView,
    },
    {
      path: '/login',
      name: 'login',
      component: LoginView,
    },
    {
      path: '/perfil',
      name: 'perfil',
      component: PerfilView,
      meta: {
        requiresAuth: true,
      },
    },
  ],
})

router.beforeEach((to) => {
  if (!isAuthenticated()) {
    clearAuthSession()
  }

  if (to.meta.requiresAuth && !isAuthenticated()) {
    return { path: '/login' }
  }

  if (to.path === '/login' && isAuthenticated()) {
    return { path: '/perfil' }
  }

  return true
})

export default router
