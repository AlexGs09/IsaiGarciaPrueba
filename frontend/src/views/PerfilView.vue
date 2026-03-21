<script setup>
import axios from 'axios'
import { onMounted, ref } from 'vue'
import { RouterLink, useRouter } from 'vue-router'

import { clearAuthSession, getAuthSession } from '../services/authStorage'
import { obtenerPerfil } from '../services/authService'

const router = useRouter()
const perfil = ref(null)
const loading = ref(true)
const errorMessage = ref('')

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

onMounted(cargarPerfil)

const sesion = getAuthSession()
</script>

<template>
  <main class="profile-page">
    <div class="container py-5">
      <div class="row justify-content-center">
        <div class="col-12 col-xl-10">
          <section class="profile-shell">
            <div class="profile-header">
              <div>
                <p class="profile-kicker">Perfil de usuario</p>
                <h1>Informacion personal</h1>
                <p class="profile-header-copy">
                  Datos del usuario autenticado obtenidos desde el backend.
                </p>
              </div>

              <div class="profile-header-actions">
                <RouterLink class="btn btn-outline-secondary rounded-pill" to="/">
                  Inicio
                </RouterLink>
                <RouterLink class="btn btn-warning rounded-pill fw-semibold" to="/login">
                  Volver al login
                </RouterLink>
              </div>
            </div>

            <div v-if="loading" class="profile-state-card">
              Cargando perfil...
            </div>

            <div v-else-if="errorMessage" class="profile-state-card profile-state-card-error">
              {{ errorMessage }}
            </div>

            <div v-else-if="perfil" class="row g-4">
              <div class="col-12 col-lg-4">
                <article class="profile-summary-card">
                  <span class="profile-summary-label">Sesion activa</span>
                  <h2>{{ perfil.nombres }} {{ perfil.primerApellido }}</h2>
                  <p>{{ sesion?.usuario?.correo ?? perfil.correo }}</p>
                  <div class="profile-summary-chip">DNI {{ perfil.dni }}</div>
                </article>
              </div>

              <div class="col-12 col-lg-8">
                <article class="profile-data-card">
                  <div class="profile-grid">
                    <div class="profile-field">
                      <span>DNI</span>
                      <strong>{{ perfil.dni }}</strong>
                    </div>
                    <div class="profile-field">
                      <span>Nombres</span>
                      <strong>{{ perfil.nombres }}</strong>
                    </div>
                    <div class="profile-field">
                      <span>Primer apellido</span>
                      <strong>{{ perfil.primerApellido }}</strong>
                    </div>
                    <div class="profile-field">
                      <span>Segundo apellido</span>
                      <strong>{{ perfil.segundoApellido || 'No registrado' }}</strong>
                    </div>
                    <div class="profile-field">
                      <span>Fecha de nacimiento</span>
                      <strong>{{ perfil.fechaNacimiento }}</strong>
                    </div>
                    <div class="profile-field">
                      <span>Nacionalidad</span>
                      <strong>{{ perfil.nacionalidad }}</strong>
                    </div>
                    <div class="profile-field">
                      <span>Correo</span>
                      <strong>{{ perfil.correo }}</strong>
                    </div>
                    <div class="profile-field">
                      <span>Numero celular</span>
                      <strong>{{ perfil.numeroCelular }}</strong>
                    </div>
                  </div>
                </article>
              </div>
            </div>
          </section>
        </div>
      </div>
    </div>
  </main>
</template>
