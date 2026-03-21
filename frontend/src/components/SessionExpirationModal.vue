<script setup>
defineProps({
  visible: {
    type: Boolean,
    default: false,
  },
  extending: {
    type: Boolean,
    default: false,
  },
})

const emit = defineEmits(['extend', 'close-session'])
</script>

<template>
  <div v-if="visible" class="session-modal-backdrop">
    <div class="session-modal-card">
      <p class="session-modal-kicker">Sesion</p>
      <h2>Tu sesion esta a punto de expirar</h2>
      <p>
        Detectamos inactividad. Si deseas continuar, extiende tu sesion para seguir usando la
        plataforma.
      </p>

      <div class="session-modal-actions">
        <button class="btn btn-outline-secondary rounded-pill px-4" type="button" @click="emit('close-session')">
          Cerrar sesion
        </button>
        <button
          class="btn btn-warning rounded-pill px-4 fw-semibold"
          type="button"
          :disabled="extending"
          @click="emit('extend')"
        >
          {{ extending ? 'Extendiendo...' : 'Extender sesion' }}
        </button>
      </div>
    </div>
  </div>
</template>
