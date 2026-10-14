import type { CurrentUser, LoginResponse } from '../types/api';

const TOKEN_KEY = 'northwind-support.token';
const USER_KEY = 'northwind-support.user';
export const AUTH_EXPIRED_EVENT = 'northwind-support:auth-expired';

export function getAccessToken(): string | null {
  return window.localStorage.getItem(TOKEN_KEY);
}

export function saveAuthSession(session: LoginResponse): void {
  window.localStorage.setItem(TOKEN_KEY, session.token);
  window.localStorage.setItem(USER_KEY, JSON.stringify(session.user));
}

export function getStoredUser(): CurrentUser | null {
  const raw = window.localStorage.getItem(USER_KEY);
  if (!raw) return null;

  try {
    const user = JSON.parse(raw) as CurrentUser;
    return typeof user.id === 'number'
      && typeof user.fullName === 'string'
      && typeof user.email === 'string'
      ? user
      : null;
  } catch {
    return null;
  }
}

export function clearAuthSession(): void {
  window.localStorage.removeItem(TOKEN_KEY);
  window.localStorage.removeItem(USER_KEY);
}

export function notifyAuthExpired(): void {
  window.dispatchEvent(new Event(AUTH_EXPIRED_EVENT));
}
