import axios from 'axios'

import { clearAuthSession, getAuthSession, saveAuthSession } from './authStorage'

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5090'

function buildAuthHeaders(accessToken) {
  return {
    Authorization: `Bearer ${accessToken}`,
  }
}

export async function login({ identificador, contrasena }) {
  const response = await axios.post(`${apiBaseUrl}/api/auth/login`, {
    identificador,
    contrasena,
  })

  const session = {
    accessToken: response.data.accessToken,
    refreshToken: response.data.refreshToken,
    expiresInSeconds: response.data.expiresInSeconds,
    usuario: response.data.usuario,
    lastRefreshAt: Date.now(),
  }

  saveAuthSession(session)
  return session
}

export async function obtenerPerfil() {
  const session = getAuthSession()

  if (!session?.accessToken) {
    throw new Error('No existe una sesion activa.')
  }

  const response = await axios.get(`${apiBaseUrl}/api/usuarios/perfil`, {
    headers: buildAuthHeaders(session.accessToken),
  })

  return response.data
}

export async function refreshSesion() {
  const session = getAuthSession()

  if (!session?.refreshToken) {
    throw new Error('No existe refresh token disponible.')
  }

  const response = await axios.post(`${apiBaseUrl}/api/auth/refresh`, {
    refreshToken: session.refreshToken,
  })

  const updatedSession = {
    ...session,
    accessToken: response.data.accessToken,
    refreshToken: response.data.refreshToken,
    expiresInSeconds: response.data.expiresInSeconds,
    lastRefreshAt: Date.now(),
  }

  saveAuthSession(updatedSession)
  return updatedSession
}

export async function logoutSesion() {
  const session = getAuthSession()

  if (session?.accessToken) {
    try {
      await axios.post(
        `${apiBaseUrl}/api/auth/logout`,
        {},
        {
          headers: buildAuthHeaders(session.accessToken),
        },
      )
    } catch {
      // Si el backend ya no reconoce la sesion, igual limpiamos frontend.
    }
  }

  clearAuthSession()
}
