import { createContext, useContext } from 'react';
import type { CurrentUser, LoginRequest } from '../types/api';

interface AuthContextValue {
  user: CurrentUser | null;
  login: (credentials: LoginRequest) => Promise<void>;
  logout: () => void;
}

export const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function useAuth(): AuthContextValue {
  const auth = useContext(AuthContext);
  if (!auth) throw new Error('useAuth must be used within an AuthProvider.');
  return auth;
}
