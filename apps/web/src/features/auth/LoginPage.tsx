import { useState } from 'react';
import type { FormEvent } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { Field } from '../../components/Field';
import { ApiError } from '../../api/client';
import { useAuth } from '../../auth/authContext';

interface LoginLocationState {
  from?: {
    pathname?: string;
    search?: string;
    hash?: string;
  };
}

export function LoginPage() {
  const { login } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setIsSubmitting(true);

    try {
      await login({ email, password });
      const destination = (location.state as LoginLocationState | null)?.from;
      navigate(
        destination?.pathname
          ? `${destination.pathname}${destination.search ?? ''}${destination.hash ?? ''}`
          : '/tickets',
        { replace: true },
      );
    } catch (loginError) {
      setError(loginError instanceof ApiError && loginError.status === 401
        ? 'Invalid email or password.'
        : loginError instanceof Error ? loginError.message : 'Unable to sign in.');
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <main className="login-page">
      <section className="login-card" aria-labelledby="login-title">
        <h1 id="login-title">Sign in</h1>
        <p>Sign in with your Northwind Support agent account.</p>

        {error && <p className="error" role="alert">{error}</p>}

        <form onSubmit={handleSubmit}>
          <Field
            id="email"
            label="Email"
          >
            {(props) => (
              <input
                {...props}
                type="email"
                autoComplete="username"
                required
                maxLength={254}
                value={email}
                onChange={(event) => setEmail(event.target.value)}
              />
            )}
          </Field>
          <Field
            id="password"
            label="Password"
          >
            {(props) => (
              <input
                {...props}
                type="password"
                autoComplete="current-password"
                required
                maxLength={200}
                value={password}
                onChange={(event) => setPassword(event.target.value)}
              />
            )}
          </Field>
          <button className="button button--primary" type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Signing in…' : 'Sign in'}
          </button>
        </form>
      </section>
    </main>
  );
}
