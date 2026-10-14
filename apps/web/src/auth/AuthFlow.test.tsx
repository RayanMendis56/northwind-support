import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { useEffect } from 'react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AuthProvider } from './AuthProvider';
import { RequireAuth } from './RequireAuth';
import { LoginPage } from '../features/auth/LoginPage';
import { request } from '../api/client';
import { saveAuthSession } from '../lib/authSession';
import { App } from '../App';

function renderRoutes(initialEntry: string) {
  return render(
    <AuthProvider>
      <MemoryRouter initialEntries={[initialEntry]}>
        <Routes>
          <Route element={<RequireAuth />}>
            <Route path="/tickets/:id" element={<h1>Ticket page</h1>} />
          </Route>
          <Route path="/login" element={<LoginPage />} />
        </Routes>
      </MemoryRouter>
    </AuthProvider>,
  );
}

describe('authentication flow', () => {
  beforeEach(() => {
    window.localStorage.clear();
    vi.restoreAllMocks();
  });

  it('redirects a signed-out visitor to login and returns them to their requested page', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response(JSON.stringify({
      token: 'signed-token',
      expiresAtUtc: '2030-01-01T00:00:00Z',
      user: { id: 1, fullName: 'Alex Turner', email: 'alex@example.com' },
    }), { status: 200, headers: { 'Content-Type': 'application/json' } }));

    renderRoutes('/tickets/12');

    expect(await screen.findByRole('heading', { name: 'Sign in' })).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('Email'), { target: { value: 'alex@example.com' } });
    fireEvent.change(screen.getByLabelText('Password'), { target: { value: 'dev-password' } });
    fireEvent.click(screen.getByRole('button', { name: 'Sign in' }));

    expect(await screen.findByRole('heading', { name: 'Ticket page' })).toBeInTheDocument();
    expect(window.localStorage.getItem('northwind-support.token')).toBe('signed-token');
  });

  it('clears the session and redirects after any API request returns 401', async () => {
    saveAuthSession({
      token: 'expired-token',
      expiresAtUtc: '2020-01-01T00:00:00Z',
      user: { id: 1, fullName: 'Alex Turner', email: 'alex@example.com' },
    });
    const fetchMock = vi.spyOn(globalThis, 'fetch')
      .mockResolvedValue(new Response(null, { status: 401 }));

    function ProtectedPage() {
      useEffect(() => {
        void request('/tickets').catch(() => undefined);
      }, []);
      return <h1>Protected ticket list</h1>;
    }

    render(
      <AuthProvider>
        <MemoryRouter initialEntries={['/tickets/12']}>
          <Routes>
            <Route element={<RequireAuth />}>
              <Route path="/tickets/:id" element={<ProtectedPage />} />
            </Route>
            <Route path="/login" element={<LoginPage />} />
          </Routes>
        </MemoryRouter>
      </AuthProvider>,
    );

    expect(await screen.findByRole('heading', { name: 'Sign in' })).toBeInTheDocument();
    expect(fetchMock).toHaveBeenCalledWith('/api/tickets', expect.objectContaining({
      headers: expect.objectContaining({ Authorization: 'Bearer expired-token' }),
    }));
    await waitFor(() => {
      expect(window.localStorage.getItem('northwind-support.token')).toBeNull();
    });
  });

  it('shows the signed-in agent and clears the session on logout', async () => {
    saveAuthSession({
      token: 'signed-token',
      expiresAtUtc: '2030-01-01T00:00:00Z',
      user: { id: 1, fullName: 'Alex Turner', email: 'alex@example.com' },
    });

    render(
      <AuthProvider>
        <MemoryRouter initialEntries={['/tickets/12']}>
          <Routes>
            <Route element={<RequireAuth />}>
              <Route element={<App />}>
                <Route path="/tickets/:id" element={<h1>Ticket page</h1>} />
              </Route>
            </Route>
            <Route path="/login" element={<LoginPage />} />
          </Routes>
        </MemoryRouter>
      </AuthProvider>,
    );

    expect(await screen.findByText('Alex Turner')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Sign out' }));

    expect(await screen.findByRole('heading', { name: 'Sign in' })).toBeInTheDocument();
    expect(window.localStorage.getItem('northwind-support.token')).toBeNull();
  });
});
