import React from 'react';
import { Moon, Sun } from 'lucide-react';
import { useTheme } from '../context/ThemeContext';

interface ThemeToggleProps {
  /** Compact icon-only button for nav bars */
  variant?: 'icon' | 'labeled';
  className?: string;
}

export const ThemeToggle: React.FC<ThemeToggleProps> = ({ variant = 'icon', className = '' }) => {
  const { isDark, toggleTheme } = useTheme();

  if (variant === 'labeled') {
    return (
      <button
        type="button"
        onClick={toggleTheme}
        className={`flex items-center justify-between w-full gap-3 px-4 py-3 rounded-lg border border-border bg-surface-2 hover:bg-surface-offset transition-interactive ${className}`}
        aria-pressed={isDark}
        aria-label={isDark ? 'Uključi svetlu temu' : 'Uključi tamnu temu'}
      >
        <span className="flex items-center gap-2 text-sm font-medium text-text">
          {isDark ? <Moon size={18} className="text-primary" /> : <Sun size={18} className="text-warning" />}
          {isDark ? 'Tamna tema' : 'Svetla tema'}
        </span>
        <span
          className={`relative inline-flex h-6 w-11 shrink-0 rounded-full transition-colors ${
            isDark ? 'bg-primary' : 'bg-border'
          }`}
        >
          <span
            className={`absolute top-0.5 h-5 w-5 rounded-full bg-white shadow-sm transition-transform ${
              isDark ? 'translate-x-5' : 'translate-x-0.5'
            }`}
          />
        </span>
      </button>
    );
  }

  return (
    <button
      type="button"
      onClick={toggleTheme}
      className={`p-2 rounded-lg text-text-muted hover:bg-surface-2 hover:text-text transition-interactive ${className}`}
      title={isDark ? 'Svetla tema' : 'Tamna tema'}
      aria-label={isDark ? 'Uključi svetlu temu' : 'Uključi tamnu temu'}
    >
      {isDark ? <Sun size={18} /> : <Moon size={18} />}
    </button>
  );
};
