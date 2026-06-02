import { createContext, useContext, useMemo, useState, type ReactNode } from 'react'
import type { LoginResponse } from '../types'

type AuthState = {
  token: string | null
  user: Omit<LoginResponse, 'token' | 'expiresAt'> | null
  login: (response: LoginResponse) => void
  logout: () => void
}

const AuthContext = createContext<AuthState | null>(null)
const storageKey = 'taskmgmt.auth'

function loadStored(): { token: string; user: AuthState['user'] } | null {
  const raw = localStorage.getItem(storageKey)
  if (!raw) return null
  try {
    return JSON.parse(raw)
  } catch {
    return null
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const stored = loadStored()
  const [token, setToken] = useState<string | null>(stored?.token ?? null)
  const [user, setUser] = useState<AuthState['user']>(stored?.user ?? null)

  const value = useMemo<AuthState>(
    () => ({
      token,
      user,
      login: (response) => {
        const { token: newToken, expiresAt: _, ...userData } = response
        setToken(newToken)
        setUser(userData)
        localStorage.setItem(storageKey, JSON.stringify({ token: newToken, user: userData }))
      },
      logout: () => {
        setToken(null)
        setUser(null)
        localStorage.removeItem(storageKey)
      },
    }),
    [token, user],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}
