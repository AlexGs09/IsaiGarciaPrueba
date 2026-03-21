<script setup>
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { RouterView } from 'vue-router'

import SessionExpirationModal from './components/SessionExpirationModal.vue'
import { getAuthSession, getRemainingSessionMs, isAuthenticated } from './services/authStorage'
import { logoutSesion, refreshSesion } from './services/authService'

const route = useRoute()
const router = useRouter()

const sessionModalVisible = ref(false)
const extendingSession = ref(false)
const idleTimerId = ref(null)
const warningTimerId = ref(null)

const minutosInactividad = Number(import.meta.env.VITE_MINUTOS_INACTIVIDAD_SESION ?? 20)
const minutosAviso = Number(import.meta.env.VITE_MINUTOS_AVISO_EXPIRACION ?? 2)

const shouldTrackSession = computed(() => route.meta.requiresAuth && isAuthenticated())

function clearTimers() {
  if (idleTimerId.value) {
    window.clearTimeout(idleTimerId.value)
  }

  if (warningTimerId.value) {
    window.clearTimeout(warningTimerId.value)
  }

  idleTimerId.value = null
  warningTimerId.value = null
}

function removeActivityListeners() {
  for (const eventName of ['click', 'mousemove', 'keydown', 'scroll']) {
    window.removeEventListener(eventName, resetSessionTimers)
  }
}

function addActivityListeners() {
  for (const eventName of ['click', 'mousemove', 'keydown', 'scroll']) {
    window.addEventListener(eventName, resetSessionTimers, { passive: true })
  }
}

async function closeSessionAndRedirect() {
  clearTimers()
  sessionModalVisible.value = false
  removeActivityListeners()
  await logoutSesion()
  router.push('/login')
}

function resetSessionTimers() {
  if (!shouldTrackSession.value) {
    return
  }

  if (sessionModalVisible.value) {
    return
  }

  clearTimers()
  sessionModalVisible.value = false

  const session = getAuthSession()
  const totalInactividadMs = minutosInactividad * 60 * 1000
  const remainingTokenMs = getRemainingSessionMs(session)

  if (remainingTokenMs <= 0) {
    closeSessionAndRedirect()
    return
  }

  const totalMs = Math.min(totalInactividadMs, remainingTokenMs)
  const avisoConfiguradoMs = minutosAviso * 60 * 1000
  const warningLeadMs = Math.min(avisoConfiguradoMs, Math.max(totalMs - 1_000, 0))
  const warningMs = Math.max(totalMs - warningLeadMs, 0)

  warningTimerId.value = window.setTimeout(() => {
    sessionModalVisible.value = true
    removeActivityListeners()
  }, warningMs)

  idleTimerId.value = window.setTimeout(() => {
    closeSessionAndRedirect()
  }, totalMs)
}

async function extendSession() {
  extendingSession.value = true

  try {
    await refreshSesion()
    sessionModalVisible.value = false
    addActivityListeners()
    resetSessionTimers()
  } catch {
    await closeSessionAndRedirect()
  } finally {
    extendingSession.value = false
  }
}

watch(
  () => route.fullPath,
  () => {
    removeActivityListeners()
    clearTimers()
    sessionModalVisible.value = false

    if (shouldTrackSession.value) {
      addActivityListeners()
      resetSessionTimers()
    }
  },
  { immediate: true },
)

onMounted(() => {
  if (shouldTrackSession.value) {
    addActivityListeners()
    resetSessionTimers()
  }
})

onBeforeUnmount(() => {
  clearTimers()
  removeActivityListeners()
})
</script>

<template>
  <RouterView />
  <SessionExpirationModal
    :visible="sessionModalVisible"
    :extending="extendingSession"
    @extend="extendSession"
    @close-session="closeSessionAndRedirect"
  />
</template>
