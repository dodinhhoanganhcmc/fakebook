import { useState } from 'react'
import type { FormEvent } from 'react'
import { ApiError } from '../api/client'
import { useAuth } from '../lib/auth'

export function LoginPage() {
  const { login, register } = useAuth()

  const [usernameOrEmail, setUsernameOrEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const [registerOpen, setRegisterOpen] = useState(false)

  async function onLogin(e: FormEvent) {
    e.preventDefault()
    setError(null)
    setBusy(true)
    try {
      await login({ usernameOrEmail: usernameOrEmail.trim(), password })
    } catch (err) {
      setError(
        err instanceof ApiError && err.status === 401
          ? 'Incorrect username/email or password.'
          : 'Could not log in. Is the server running?',
      )
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="auth-page">
      <div className="auth-hero">
        <div className="auth-pitch">
          <img src="/brand/fakebook-full-cropped.png" alt="Fakebook" className="auth-logo" />
          <p>Connect with friends and the world around you on Fakebook.</p>
        </div>

        <div className="auth-card-wrap">
          <form className="card auth-card" onSubmit={onLogin}>
            <input
              type="text"
              placeholder="Email or username"
              value={usernameOrEmail}
              onChange={(e) => setUsernameOrEmail(e.target.value)}
              autoComplete="username"
              autoFocus
            />
            <input
              type="password"
              placeholder="Password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              autoComplete="current-password"
            />
            {error && <p className="form-error">{error}</p>}
            <button type="submit" className="btn-primary lg" disabled={busy || !usernameOrEmail || !password}>
              {busy ? 'Logging in…' : 'Log in'}
            </button>
            <a className="auth-forgot" href="#" onClick={(e) => e.preventDefault()}>
              Forgotten password?
            </a>
            <div className="auth-divider" />
            <button type="button" className="btn-create" onClick={() => setRegisterOpen(true)}>
              Create new account
            </button>
          </form>
          <p className="auth-hint">
            Demo account: <strong>alice</strong> / <strong>Password123!</strong>
          </p>
        </div>
      </div>

      {registerOpen && <RegisterModal onClose={() => setRegisterOpen(false)} onRegister={register} />}
    </div>
  )
}

function RegisterModal({
  onClose,
  onRegister,
}: {
  onClose: () => void
  onRegister: (b: { username: string; email: string; password: string; displayName: string }) => Promise<void>
}) {
  const [displayName, setDisplayName] = useState('')
  const [username, setUsername] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  async function submit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    if (password.length < 6) {
      setError('Password must be at least 6 characters.')
      return
    }
    setBusy(true)
    try {
      await onRegister({
        displayName: displayName.trim() || username.trim(),
        username: username.trim(),
        email: email.trim(),
        password,
      })
    } catch (err) {
      setError(
        err instanceof ApiError && err.status === 409
          ? 'That username or email is already taken.'
          : 'Could not create the account. Please try again.',
      )
      setBusy(false)
    }
  }

  return (
    <div className="modal-backdrop" role="presentation" onClick={() => !busy && onClose()}>
      <div className="modal auth-register" role="dialog" aria-modal="true" onClick={(e) => e.stopPropagation()}>
        <header className="modal-head register-head">
          <div>
            <h2>Sign up</h2>
            <p>It&apos;s quick and easy.</p>
          </div>
          <button type="button" className="icon-circle subtle" onClick={onClose} aria-label="Close">
            ✕
          </button>
        </header>
        <form className="modal-body register-form" onSubmit={submit}>
          <input placeholder="Full name" value={displayName} onChange={(e) => setDisplayName(e.target.value)} autoFocus />
          <input placeholder="Username" value={username} onChange={(e) => setUsername(e.target.value)} autoComplete="username" />
          <input type="email" placeholder="Email address" value={email} onChange={(e) => setEmail(e.target.value)} autoComplete="email" />
          <input
            type="password"
            placeholder="New password (min 6 chars)"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            autoComplete="new-password"
          />
          {error && <p className="form-error">{error}</p>}
          <button type="submit" className="btn-create lg" disabled={busy || !username || !email || !password}>
            {busy ? 'Creating…' : 'Sign up'}
          </button>
        </form>
      </div>
    </div>
  )
}
