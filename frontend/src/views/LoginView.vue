<script setup>
import axios from 'axios'
import { computed, ref } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'

import { login } from '../services/authService'

const showPassword = ref(false)
const router = useRouter()
const route = useRoute()
const identificador = ref('')
const contrasena = ref('')
const loading = ref(false)
const errorMessage = ref('')
const showSessionExpiredNotice = ref(route.query.sesionExpirada === '1')
const showPreLoginScreen = ref(route.query.sesionExpirada !== '1')
const blockedAccountNotice = ref(null)

const passwordFieldType = computed(() => (showPassword.value ? 'text' : 'password'))
const passwordIcon = computed(() => (showPassword.value ? 'visibility_off' : 'visibility'))

async function submitLogin() {
  loading.value = true
  errorMessage.value = ''
  blockedAccountNotice.value = null

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
      const errorCode = error.response?.data?.codigo
      const backendMessage =
        firstValidationMessage ??
        error.response?.data?.mensaje ??
        error.response?.data?.title ??
        'No se pudo iniciar sesion.'

      if (errorCode === 'usuario_bloqueado_temporal') {
        blockedAccountNotice.value = {
          titulo: 'Cuenta bloqueada temporalmente',
          mensaje: backendMessage,
        }
        return
      }

      errorMessage.value = backendMessage
    } else {
      errorMessage.value = 'No se pudo iniciar sesion.'
    }
  } finally {
    loading.value = false
  }
}

function closeSessionExpiredNotice() {
  showSessionExpiredNotice.value = false
  router.replace({ path: '/login' })
}

function continueToLogin() {
  showPreLoginScreen.value = false
}

function closeBlockedAccountNotice() {
  blockedAccountNotice.value = null
}
</script>

<template>
  <main v-if="blockedAccountNotice" class="blocked-login-page">
    <div class="blocked-login-scene">
      <div class="blocked-login-card">
        <div class="blocked-login-illustration">
          <div class="blocked-login-illustration-ring">
            <span class="material-symbols-outlined">support_agent</span>
          </div>
        </div>

        <h1>{{ blockedAccountNotice.titulo }}</h1>
        <p>{{ blockedAccountNotice.mensaje }}</p>

        <button class="blocked-login-button" type="button" @click="closeBlockedAccountNotice">
          Volver al inicio de sesion
        </button>
      </div>

      <div class="blocked-login-landscape">
        <div class="blocked-login-hill blocked-login-hill-left"></div>
        <div class="blocked-login-hill blocked-login-hill-center"></div>
        <div class="blocked-login-hill blocked-login-hill-right"></div>
        <div class="blocked-login-steps blocked-login-steps-left"></div>
        <div class="blocked-login-steps blocked-login-steps-right"></div>
      </div>
    </div>
  </main>

  <main v-else-if="showPreLoginScreen" class="prelogin-page">
    <div class="prelogin-scene">
      <div class="prelogin-line"></div>

      <div class="prelogin-card">
        <div class="prelogin-illustration">
          <div class="prelogin-illustration-mark">
            <span class="material-symbols-outlined">rocket_launch</span>
          </div>
          <div class="prelogin-illustration-check">
            <span class="material-symbols-outlined">check</span>
          </div>
        </div>

        <h1>!Bienvenido, Isai!</h1>
        <p>Su cuenta en el aplicativo X esta activada.</p>
        <p>Ya puede iniciar sesion.</p>

        <button class="prelogin-submit" type="button" @click="continueToLogin">
          Iniciar sesion
        </button>
      </div>

      <div class="prelogin-landscape">
        <div class="prelogin-hill prelogin-hill-left"></div>
        <div class="prelogin-hill prelogin-hill-center"></div>
        <div class="prelogin-hill prelogin-hill-right"></div>
        <div class="prelogin-stairs"></div>
        <div class="prelogin-lama"></div>
        <div class="prelogin-rock"></div>
      </div>
    </div>
  </main>

  <main v-else class="login-page">
    <section class="login-shell">
      <aside class="login-brand-panel">
        <div class="login-brand-bg">
          <div class="login-orb login-orb-yellow"></div>
          <div class="login-orb login-orb-green"></div>
        </div>

        <div class="login-brand-top">
          <div class="login-brand-mark">
            <span class="material-symbols-outlined login-brand-icon">architecture</span>
            <span class="login-brand-name">UPSCALE</span>
          </div>
        </div>

        <div class="login-brand-copy">
          <span class="login-kicker">Nivel empresarial</span>
          <h1 class="login-display">Soluciones digitales confiables para empresas modernas.</h1>
          <p class="login-supporting">
            Plataformas robustas disenadas para optimizar procesos, mejorar la eficiencia y
            escalar tu negocio con tecnologia de alto nivel.
          </p>

          <div class="login-editorial-card">
            <div class="login-editorial-image">
              <div class="login-grid-overlay"></div>
              <div class="login-photo-caption">
                <span>Espacio de trabajo digital</span>
                <strong>Gestion inteligente para operaciones criticas</strong>
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
          <p>Con la confianza de empresas en crecimiento</p>
        </div>
      </aside>

      <section class="login-form-panel">
        <div class="login-form-wrap">
          <div v-if="showSessionExpiredNotice" class="login-session-expired-alert" role="alert">
            <div class="login-session-expired-icon">
              <span class="material-symbols-outlined">info</span>
            </div>
            <div class="login-session-expired-copy">
              <strong>Su sesion ha expirado debido a inactividad.</strong>
              <span>Por favor, inicie sesion nuevamente.</span>
            </div>
            <button
              type="button"
              class="login-session-expired-close"
              @click="closeSessionExpiredNotice"
            >
              <span class="material-symbols-outlined">close</span>
            </button>
          </div>

          <div class="login-header">
            <p class="login-panel-label">Acceso seguro</p>
            <h2>Iniciar sesion</h2>
            <p>Bienvenido de nuevo. Ingresa tus datos para acceder a tu panel.</p>
          </div>

          <div class="login-provider-grid">
            <button class="login-provider-button" type="button">
              <span class="login-provider-icon login-provider-icon-google">G</span>
              <span class="login-provider-label">Google</span>
            </button>
            <button class="login-provider-button" type="button">
              <span class="login-provider-icon login-provider-icon-microsoft">
                <span></span>
                <span></span>
                <span></span>
                <span></span>
              </span>
              <span class="login-provider-label">Microsoft</span>
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
                <label for="password">Contraseña</label>
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

            <a href="#" class="login-forgot-link">¿Olvidaste tu contraseña?</a>

            <p v-if="errorMessage" class="login-error-message">{{ errorMessage }}</p>

            <button class="login-submit" type="submit" :disabled="loading">
              {{ loading ? 'Validando...' : 'Entrar a la cuenta' }}
            </button>
          </form>
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
