<script setup>
import axios from 'axios'
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'

import { clearAuthSession } from '../services/authStorage'
import { logoutSesion, obtenerPerfil } from '../services/authService'

const router = useRouter()
const perfil = ref(null)
const loading = ref(true)
const errorMessage = ref('')

function formatDate(dateValue) {
  if (!dateValue) {
    return 'No registrado'
  }

  const parsedDate = new Date(dateValue)

  if (Number.isNaN(parsedDate.getTime())) {
    return dateValue
  }

  return new Intl.DateTimeFormat('es-PE', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  }).format(parsedDate)
}

const nombreCompleto = computed(() => 'Isai Alexander Garcia Sanchez')
const nombreOperador = computed(() => 'Alex')
const inicialesUsuario = computed(() => 'IA')
const edadUsuario = computed(() => '22 Años')

const datosPrincipales = computed(() => {
  if (!perfil.value) {
    return []
  }

  return [
    { label: 'Nombres', value: 'Isai Alexander' },
    { label: 'Primer apellido', value: 'Garcia' },
    { label: 'Segundo apellido', value: 'Sanchez' },
    { label: 'Tipo de documento', value: 'DNI' },
    { label: 'Numero de documento', value: perfil.value.dni },
    { label: 'Fecha de nacimiento', value: '02/09/2003' },
    { label: 'Nacionalidad', value: perfil.value.nacionalidad },
    { label: 'Sexo', value: 'Hombre' },
    { label: 'Correo principal', value: perfil.value.correo },
    { label: 'Correo secundario', value: 'No registrado' },
    { label: 'Telefono movil', value: perfil.value.numeroCelular },
    { label: 'Telefono secundario', value: 'No registrado' },
  ]
})

async function cargarPerfil() {
  loading.value = true
  errorMessage.value = ''

  try {
    perfil.value = await obtenerPerfil()
  } catch (error) {
    if (axios.isAxiosError(error) && error.response?.status === 401) {
      clearAuthSession()
      router.push('/login')
      return
    }

    errorMessage.value = error.response?.data?.mensaje ?? error.message ?? 'No se pudo cargar el perfil.'
  } finally {
    loading.value = false
  }
}

async function cerrarSesion() {
  await logoutSesion()
  router.push('/login')
}

onMounted(cargarPerfil)
</script>

<template>
  <main class="profile-upscale-page">
    <aside class="profile-upscale-sidebar">
      <div class="profile-upscale-brand">UPSCALE</div>

      <nav class="profile-upscale-nav">
        <button class="profile-upscale-nav-item" type="button">
          <span class="material-symbols-outlined">dashboard</span>
          <span>Dashboard</span>
        </button>
        <button class="profile-upscale-nav-item" type="button">
          <span class="material-symbols-outlined">architecture</span>
          <span>Proyectos</span>
        </button>
        <button class="profile-upscale-nav-item" type="button">
          <span class="material-symbols-outlined">monitoring</span>
          <span>Analitica</span>
        </button>
        <button class="profile-upscale-nav-item" type="button">
          <span class="material-symbols-outlined">deployed_code</span>
          <span>Colecciones</span>
        </button>
        <button class="profile-upscale-nav-item profile-upscale-nav-item-active" type="button">
          <span class="material-symbols-outlined">settings</span>
          <span>Perfil</span>
        </button>
      </nav>

      <div class="profile-upscale-sidebar-footer">
        <button class="profile-upscale-nav-item" type="button">
          <span class="material-symbols-outlined">help</span>
          <span>Ayuda</span>
        </button>
      </div>
    </aside>

    <section class="profile-upscale-main">
      <header class="profile-upscale-topbar">
        <div class="profile-upscale-topbar-left">
          <h1>USER PROFILE</h1>
          <nav class="profile-upscale-tabs">
            <button class="profile-upscale-tab profile-upscale-tab-active" type="button">General</button>
            <button class="profile-upscale-tab" type="button">Security</button>
            <button class="profile-upscale-tab" type="button">Billing</button>
          </nav>
        </div>

        <div class="profile-upscale-topbar-right">
          <button class="profile-upscale-top-icon" type="button" aria-label="Buscar">
            <span class="material-symbols-outlined">search</span>
          </button>
          <button class="profile-upscale-top-icon" type="button" aria-label="Cerrar sesion" @click="cerrarSesion">
            <span class="material-symbols-outlined">logout</span>
          </button>
          <button class="profile-upscale-top-icon" type="button" aria-label="Notificaciones">
            <span class="material-symbols-outlined">notifications</span>
          </button>
          <button class="profile-upscale-avatar-chip" type="button">
            <span class="material-symbols-outlined">account_circle</span>
          </button>
        </div>
      </header>

      <section class="profile-upscale-content">
        <div v-if="loading" class="profile-state-card profile-state-card-dark">Cargando perfil...</div>

        <div v-else-if="errorMessage" class="profile-state-card profile-state-card-dark profile-state-card-error">
          {{ errorMessage }}
        </div>

        <template v-else-if="perfil">
          <div class="profile-upscale-grid profile-upscale-grid-top">
            <article class="profile-upscale-hero-card">
              <div class="profile-upscale-hero-avatar">
                <span class="material-symbols-outlined">account_circle</span>
              </div>

              <div class="profile-upscale-hero-copy">
                <div class="profile-upscale-hero-headline">
                  <h2>{{ nombreCompleto }}</h2>
                  <span>ACTIVE</span>
                </div>
                <p>Administrador de Recursos y Operaciones</p>

                <div class="profile-upscale-metrics">
                  <div>
                    <small>Documento</small>
                    <strong>{{ perfil.dni }}</strong>
                  </div>
                  <div>
                    <small>Nacionalidad</small>
                    <strong>{{ perfil.nacionalidad }}</strong>
                  </div>
                  <div>
                    <small>Edad</small>
                    <strong>{{ edadUsuario }}</strong>
                  </div>
                </div>
              </div>
            </article>

            <article class="profile-upscale-tier-card">
              <p>Perfil de operador</p>
              <h3>{{ nombreOperador }}</h3>
              <span>Acceso activo al panel administrativo y a los modulos protegidos del sistema.</span>
              <div class="profile-upscale-tier-bar">
                <div></div>
              </div>
              <small>Sesion autenticada</small>
            </article>
          </div>

          <div class="profile-upscale-grid profile-upscale-grid-bottom">
            <article class="profile-upscale-panel profile-upscale-panel-large">
              <div class="profile-upscale-panel-title">
                <span class="material-symbols-outlined">person</span>
                <h3>Informacion personal</h3>
              </div>

              <div class="profile-upscale-fields">
                <div v-for="dato in datosPrincipales" :key="dato.label" class="profile-upscale-field">
                  <label>{{ dato.label }}</label>
                  <div>{{ dato.value }}</div>
                </div>
              </div>

              <div class="profile-upscale-panel-actions">
                <button type="button" @click="cerrarSesion">Cerrar sesion</button>
              </div>
            </article>

            <div class="profile-upscale-side-stack">
              <article class="profile-upscale-panel">
                <div class="profile-upscale-panel-title">
                  <span class="material-symbols-outlined">link</span>
                  <h3>Cuentas conectadas</h3>
                </div>

                <div class="profile-upscale-connected-list">
                  <div class="profile-upscale-connected-item">
                    <div>
                      <strong>Correo principal</strong>
                      <span>{{ perfil.correo }}</span>
                    </div>
                    <em>Activo</em>
                  </div>

                  <div class="profile-upscale-connected-item">
                    <div>
                      <strong>Celular</strong>
                      <span>{{ perfil.numeroCelular }}</span>
                    </div>
                    <em>Verificado</em>
                  </div>
                </div>
              </article>

              <article class="profile-upscale-map-card">
                <div class="profile-upscale-map-overlay"></div>
                <div class="profile-upscale-map-copy">
                  <strong>Residencia principal</strong>
                  <span>{{ perfil.nacionalidad }}</span>
                </div>
              </article>
            </div>
          </div>
        </template>
      </section>
    </section>
  </main>
</template>
