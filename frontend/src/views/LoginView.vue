<script setup>
import axios from 'axios'
import { computed, ref } from 'vue'
import { RouterLink, useRouter } from 'vue-router'

import { login } from '../services/authService'

const showPassword = ref(false)
const router = useRouter()
const identificador = ref('')
const contrasena = ref('')
const loading = ref(false)
const errorMessage = ref('')

const passwordFieldType = computed(() => (showPassword.value ? 'text' : 'password'))
const passwordIcon = computed(() => (showPassword.value ? 'visibility_off' : 'visibility'))

async function submitLogin() {
  loading.value = true
  errorMessage.value = ''

  try {
    await login({
      identificador: identificador.value,
      contrasena: contrasena.value,
    })

    router.push('/perfil')
  } catch (error) {
    if (axios.isAxiosError(error)) {
      const validationErrors = error.response?.data?.errors
      const firstValidationMessage = validationErrors
        ? Object.values(validationErrors).flat()[0]
        : null

      errorMessage.value =
        firstValidationMessage ??
        error.response?.data?.mensaje ??
        error.response?.data?.title ??
        'No se pudo iniciar sesion.'
    } else {
      errorMessage.value = 'No se pudo iniciar sesion.'
    }
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <main class="login-page">
    <section class="login-shell">
      <aside class="login-brand-panel">
        <div class="login-brand-bg">
          <div class="login-orb login-orb-yellow"></div>
          <div class="login-orb login-orb-green"></div>
        </div>

        <div class="login-brand-top">
          <div class="login-brand-mark">
            <span class="material-symbols-outlined login-brand-icon">architecture</span>
            <span class="login-brand-name">TechArch</span>
          </div>
        </div>

        <div class="login-brand-copy">
          <span class="login-kicker">Nivel empresarial</span>
          <h1 class="login-display">Arquitectura confiable para datos empresariales.</h1>
          <p class="login-supporting">
            Interfaces precisas para equipos de alto rendimiento y operaciones con
            enfoque tecnico.
          </p>

          <div class="login-editorial-card">
            <div class="login-editorial-image">
              <div class="login-grid-overlay"></div>
              <div class="login-photo-caption">
                <span>Espacio de trabajo editorial</span>
                <strong>Orden visual para accesos criticos</strong>
              </div>
            </div>
          </div>
        </div>

        <div class="login-brand-footer">
          <div class="login-progress">
            <span class="login-progress-main"></span>
            <span class="login-progress-dot"></span>
            <span class="login-progress-dot"></span>
          </div>
          <p>Con la confianza de mas de 500 empresas globales</p>
        </div>
      </aside>

      <section class="login-form-panel">
        <div class="login-form-wrap">
          <div class="login-header">
            <p class="login-panel-label">Acceso seguro</p>
            <h2>Iniciar sesion</h2>
            <p>Bienvenido de nuevo. Ingresa tus datos para acceder a tu panel.</p>
          </div>

          <div class="login-provider-grid">
            <button class="login-provider-button" type="button">
              <span class="material-symbols-outlined">google</span>
              <span>Google</span>
            </button>
            <button class="login-provider-button" type="button">
              <span class="material-symbols-outlined">grid_view</span>
              <span>Microsoft</span>
            </button>
          </div>

          <div class="login-divider">
            <span>O con credenciales</span>
          </div>

          <form class="login-form" @submit.prevent="submitLogin">
            <div class="login-field">
              <label for="identificador">Correo, usuario o DNI</label>
              <input
                id="identificador"
                v-model.trim="identificador"
                type="text"
                placeholder="correo, username o DNI"
              />
            </div>

            <div class="login-field">
              <div class="login-field-header">
                <label for="password">Contrasena</label>
                <a href="#">Olvidaste tu contrasena?</a>
              </div>
              <div class="login-password-wrap">
                <input
                  id="password"
                  v-model="contrasena"
                  :type="passwordFieldType"
                  placeholder="********"
                />
                <button type="button" @click="showPassword = !showPassword">
                  <span class="material-symbols-outlined">{{ passwordIcon }}</span>
                </button>
              </div>
            </div>

            <label class="login-checkbox">
              <input type="checkbox" />
              <span>Mantener sesion iniciada por 30 dias</span>
            </label>

            <p v-if="errorMessage" class="login-error-message">{{ errorMessage }}</p>

            <button class="login-submit" type="submit" :disabled="loading">
              {{ loading ? 'Validando...' : 'Entrar a la cuenta' }}
            </button>
          </form>

          <p class="login-footer-text">
            No tienes una cuenta?
            <a href="#">Crear cuenta</a>
          </p>
        </div>

        <footer class="login-links">
          <a href="#">Privacidad</a>
          <a href="#">Terminos</a>
          <a href="#">Soporte</a>
          <RouterLink to="/">Inicio</RouterLink>
        </footer>
      </section>
    </section>

    <button class="login-help-button" type="button" aria-label="Ayuda">
      <span class="material-symbols-outlined">help_outline</span>
    </button>
  </main>
</template>
