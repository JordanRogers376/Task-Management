import { useState, type FormEvent } from 'react'
import { Navigate } from 'react-router-dom'
import { login } from '../api/tasks'
import { useAuth } from '../context/AuthContext'

export default function LoginPage() {
  const { token, login: saveAuth } = useAuth()
  const [email, setEmail] = useState('admin@acme.com')
  const [password, setPassword] = useState('Password123!')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  if (token) return <Navigate to="/" replace />

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      const response = await login(email, password)
      saveAuth(response)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Login failed')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="auth-layout">
      <form className="card" onSubmit={handleSubmit}>
        <h1>Task Management</h1>
        <p className="muted">Sign in to your tenant workspace</p>
        {error && <p className="error">{error}</p>}
        <label>
          Email
          <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
        </label>
        <label>
          Password
          <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} required />
        </label>
        <button type="submit" disabled={loading}>
          {loading ? 'Signing in...' : 'Sign in'}
        </button>
        <p className="hint">Try admin@acme.com or user@acme.com — Password123!</p>
      </form>
    </div>
  )
}
