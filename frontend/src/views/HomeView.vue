<script setup>
import axios from 'axios'
import { onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5090'

const apiStatus = ref({
  loading: true,
  success: false,
  message: 'Consultando backend...',
})

const dbStatus = ref({
  loading: true,
  success: false,
  message: 'Probando conexion con SQL Server...',
  database: '',
  server: '',
  auth: '',
})

async function loadStatuses() {
  try {
    const healthResponse = await axios.get(`${apiBaseUrl}/api/health`)
    apiStatus.value = {
      loading: false,
      success: true,
      message: healthResponse.data.message,
    }
  } catch (error) {
    apiStatus.value = {
      loading: false,
      success: false,
      message: error.response?.data?.detail ?? 'No se pudo consultar el backend.',
    }
  }

  try {
    const dbResponse = await axios.get(`${apiBaseUrl}/api/database`)
    dbStatus.value = {
      loading: false,
      success: true,
      message: 'Conexion exitosa con SQL Server.',
      database: dbResponse.data.database,
      server: dbResponse.data.server,
      auth: dbResponse.data.auth,
    }
  } catch (error) {
    dbStatus.value = {
      loading: false,
      success: false,
      message: error.response?.data?.detail ?? 'No se pudo validar la base de datos.',
      database: '',
      server: '',
      auth: '',
    }
  }
}

onMounted(loadStatuses)
</script>

<template>
  <main class="app-shell d-flex align-items-center">
    <div class="container py-5">
      <div class="row justify-content-center">
        <div class="col-12 col-lg-10">
          <section class="hero-card shadow-lg border-0 overflow-hidden">
            <div class="row g-0">
              <div class="col-md-6 hero-copy p-4 p-md-5">
                <span class="badge rounded-pill text-bg-warning mb-3">Prueba tecnica</span>
                <h1 class="display-5 fw-semibold mb-3">Vue.js + Bootstrap + .NET Web API</h1>
                <p class="lead mb-4">
                  Frontend y backend listos para empezar, con verificacion directa del API y de la
                  conexion a tu SQL Server local.
                </p>
                <div class="d-flex flex-wrap gap-2 mb-4">
                  <span class="pill">Vue 3</span>
                  <span class="pill">Bootstrap 5</span>
                  <span class="pill">ASP.NET Core 8</span>
                  <span class="pill">SQL Server</span>
                </div>
                <RouterLink class="btn btn-warning rounded-pill px-4 py-2 fw-semibold" to="/login">
                  Ver login
                </RouterLink>
              </div>

              <div class="col-md-6 hero-panel p-4 p-md-5">
                <div class="status-card mb-3">
                  <div class="d-flex justify-content-between align-items-start gap-3">
                    <div>
                      <p class="eyebrow mb-1">API</p>
                      <h2 class="h4 mb-2">Estado del backend</h2>
                    </div>
                    <span
                      class="badge rounded-pill"
                      :class="apiStatus.success ? 'text-bg-success' : 'text-bg-danger'"
                    >
                      {{ apiStatus.loading ? 'Cargando' : apiStatus.success ? 'Activo' : 'Error' }}
                    </span>
                  </div>
                  <p class="mb-0 text-secondary">{{ apiStatus.message }}</p>
                </div>

                <div class="status-card">
                  <div class="d-flex justify-content-between align-items-start gap-3">
                    <div>
                      <p class="eyebrow mb-1">Base de datos</p>
                      <h2 class="h4 mb-2">Estado de SQL Server</h2>
                    </div>
                    <span
                      class="badge rounded-pill"
                      :class="dbStatus.success ? 'text-bg-success' : 'text-bg-danger'"
                    >
                      {{ dbStatus.loading ? 'Cargando' : dbStatus.success ? 'Conectado' : 'Error' }}
                    </span>
                  </div>

                  <p class="mb-3 text-secondary">{{ dbStatus.message }}</p>

                  <ul v-if="dbStatus.success" class="list-group list-group-flush rounded overflow-hidden">
                    <li class="list-group-item d-flex justify-content-between">
                      <span>Servidor</span>
                      <strong>{{ dbStatus.server }}</strong>
                    </li>
                    <li class="list-group-item d-flex justify-content-between">
                      <span>Base de datos</span>
                      <strong>{{ dbStatus.database }}</strong>
                    </li>
                    <li class="list-group-item d-flex justify-content-between">
                      <span>Autenticacion</span>
                      <strong class="text-capitalize">{{ dbStatus.auth }}</strong>
                    </li>
                  </ul>
                </div>
              </div>
            </div>
          </section>
        </div>
      </div>
    </div>
  </main>
</template>
