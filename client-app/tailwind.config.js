/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  darkMode: ['class', '[data-theme="dark"]'],
  theme: {
    extend: {
      fontFamily: {
        display: ['"DM Sans"', 'system-ui', 'sans-serif'],
        body: ['"Inter"', 'system-ui', 'sans-serif'],
      },
      colors: {
        primary: {
          DEFAULT: 'var(--color-primary)',
          hover: 'var(--color-primary-hover)',
          active: 'var(--color-primary-active)',
          highlight: 'var(--color-primary-highlight)',
        },
        surface: {
          DEFAULT: 'var(--color-surface)',
          2: 'var(--color-surface-2)',
          offset: 'var(--color-surface-offset)',
          'offset-2': 'var(--color-surface-offset-2)',
          dynamic: 'var(--color-surface-dynamic)',
        },
        bg: {
          DEFAULT: 'var(--color-bg)',
        },
        divider: {
          DEFAULT: 'var(--color-divider)',
        },
        border: {
          DEFAULT: 'var(--color-border)',
        },
        text: {
          DEFAULT: 'var(--color-text)',
          muted: 'var(--color-text-muted)',
          faint: 'var(--color-text-faint)',
          inverse: 'var(--color-text-inverse)',
        },
        kosa: {
          DEFAULT: 'var(--color-kosa)',
          bg: 'var(--color-kosa-bg)',
          border: 'var(--color-kosa-border)',
        },
        nokti: {
          DEFAULT: 'var(--color-nokti)',
          bg: 'var(--color-nokti-bg)',
          border: 'var(--color-nokti-border)',
        },
        spa: {
          DEFAULT: 'var(--color-spa)',
          bg: 'var(--color-spa-bg)',
          border: 'var(--color-spa-border)',
        },
        lepota: {
          DEFAULT: 'var(--color-lepota)',
          bg: 'var(--color-lepota-bg)',
          border: 'var(--color-lepota-border)',
        },
        success: {
          DEFAULT: 'var(--color-success)',
          bg: 'var(--color-success-bg)',
        },
        error: {
          DEFAULT: 'var(--color-error)',
          bg: 'var(--color-error-bg)',
        },
        warning: {
          DEFAULT: 'var(--color-warning)',
          bg: 'var(--color-warning-bg)',
        },
      },
      borderRadius: {
        sm: 'var(--radius-sm)',
        md: 'var(--radius-md)',
        lg: 'var(--radius-lg)',
        xl: 'var(--radius-xl)',
      },
      boxShadow: {
        sm: 'var(--shadow-sm)',
        md: 'var(--shadow-md)',
        lg: 'var(--shadow-lg)',
      },
      transitionTimingFunction: {
        interactive: 'var(--ease-interactive)',
      },
      transitionDuration: {
        interactive: 'var(--duration-interactive)',
      },
    },
  },
  plugins: [],
}
