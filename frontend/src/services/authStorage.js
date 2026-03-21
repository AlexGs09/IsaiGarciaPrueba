const AUTH_STORAGE_KEY = 'auth_session'

export function getAuthSession() {
  const rawValue = localStorage.getItem(AUTH_STORAGE_KEY)

  if (!rawValue) {
    return null
  }

  try {
    return JSON.parse(rawValue)
  } catch {
    localStorage.removeItem(AUTH_STORAGE_KEY)
    return null
  }
}

export function saveAuthSession(session) {
  localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(session))
}

export function clearAuthSession() {
  localStorage.removeItem(AUTH_STORAGE_KEY)
}

export function getSessionExpirationTimestamp(session = getAuthSession()) {
  if (!session?.lastRefreshAt || !session?.expiresInSeconds) {
    return null
  }

  return session.lastRefreshAt + session.expiresInSeconds * 1000
}

export function getRemainingSessionMs(session = getAuthSession()) {
  const expirationTimestamp = getSessionExpirationTimestamp(session)

  if (!expirationTimestamp) {
    return 0
  }

  return Math.max(expirationTimestamp - Date.now(), 0)
}

export function isAuthenticated() {
  const session = getAuthSession()
  return Boolean(session?.accessToken && session?.refreshToken && getRemainingSessionMs(session) > 0)
}
