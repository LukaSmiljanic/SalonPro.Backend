import React, { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import {
  applyTheme,
  getStoredUserIdFromToken,
  getThemeForUser,
  setThemeForUser,
  type ThemeMode,
} from '../lib/theme';
import { useAuth } from '../hooks/useAuth';

interface ThemeContextValue {
  theme: ThemeMode;
  setTheme: (mode: ThemeMode) => void;
  toggleTheme: () => void;
  isDark: boolean;
}

const ThemeContext = createContext<ThemeContextValue | null>(null);

export const ThemeProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { user, isLoading: authLoading } = useAuth();
  const userId = user?.id ?? null;

  const [theme, setThemeState] = useState<ThemeMode>(() => getThemeForUser(getStoredUserIdFromToken()));

  useEffect(() => {
    if (authLoading) return;
    const mode = getThemeForUser(userId);
    setThemeState(mode);
    applyTheme(mode);
  }, [userId, authLoading]);

  const setTheme = useCallback(
    (mode: ThemeMode) => {
      setThemeState(mode);
      setThemeForUser(userId ?? getStoredUserIdFromToken(), mode);
    },
    [userId],
  );

  const toggleTheme = useCallback(() => {
    setTheme(theme === 'dark' ? 'light' : 'dark');
  }, [theme, setTheme]);

  const value = useMemo(
    () => ({
      theme,
      setTheme,
      toggleTheme,
      isDark: theme === 'dark',
    }),
    [theme, setTheme, toggleTheme],
  );

  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>;
};

export function useTheme(): ThemeContextValue {
  const ctx = useContext(ThemeContext);
  if (!ctx) throw new Error('useTheme must be used within ThemeProvider');
  return ctx;
}
