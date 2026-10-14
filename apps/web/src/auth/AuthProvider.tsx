import { useCallback, useEffect, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import { authApi } from '../api/auth';
import {
  AUTH_EXPIRED_EVENT,
  clearAuthSession,
  getAccessToken,
  getStoredUser,
  saveAuthSession,
} from '../lib/authSession';
import type { CurrentUser, LoginRequest } from '../types/api';
import { AuthContext } from './authContext';

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<CurrentUser | null>(() => {
    if (!getAccessToken()) return null;
    const storedUser = getStoredUser();
    if (!storedUser) clearAuthSession();
    return storedUser;
  });

  useEffect(() => {
    const expireSession = () => setUser(null);
    window.addEventListener(AUTH_EXPIRED_EVENT, expireSession);
    return () => window.removeEventListener(AUTH_EXPIRED_EVENT, expireSession);
  }, []);

  const login = useCallback(async (credentials: LoginRequest) => {
    const session = await authApi.login(credentials);
    saveAuthSession(session);
    setUser(session.user);
  }, []);

  const logout = useCallback(() => {
    clearAuthSession();
    setUser(null);
  }, []);

  const value = useMemo(() => ({ user, login, logout }), [user, login, logout]);
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
