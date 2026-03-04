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
          DEFAULT: '#5B3A8C',
          hover: '#4a2e74',
          active: '#39235c',
          highlight: '#ede5f7',
          dark: '#a07ce8',
          'dark-hover': '#b89af0',
          'dark-active': '#d0baf8',
          'dark-highlight': '#2c2040',
        },
        surface: {
          DEFAULT: '#ffffff',
          2: '#f7f5f9',
          offset: '#f2eef7',
          'offset-2': '#ebe5f2',
          dynamic: '#e4ddef',
          dark: '#15121e',
          'dark-2': '#1b1827',
          'dark-offset': '#191525',
          'dark-offset-2': '#20192e',
          'dark-dynamic': '#28223c',
        },
        bg: {
          DEFAULT: '#faf9fb',
          dark: '#0e0c14',
        },
        divider: {
          DEFAULT: '#e8e2f0',
          dark: '#231d34',
        },
        border: {
          DEFAULT: '#ddd5ea',
          dark: '#302844',
        },
        text: {
          DEFAULT: '#1a1523',
          muted: '#6b5f7a',
          faint: '#b8abca',
          inverse: '#ffffff',
          dark: '#e8e0f5',
          'dark-muted': '#9984b8',
          'dark-faint': '#574a6e',
        },
        kosa: {
          DEFAULT: '#8b7cc4',
          bg: '#f0eef9',
          border: '#c9c0e8',
        },
        nokti: {
          DEFAULT: '#c4607e',
          bg: '#fef0f5',
          border: '#f7c4d8',
        },
        spa: {
          DEFAULT: '#3d9e72',
          bg: '#edf8f3',
          border: '#b8e4d0',
        },
        lepota: {
          DEFAULT: '#b06830',
          bg: '#fff5eb',
          border: '#fcd5b3',
        },
        success: {
          DEFAULT: '#2d7a4f',
          bg: '#edf7f2',
        },
        error: {
          DEFAULT: '#b5294e',
          bg: '#fde8ee',
        },
        warning: {
          DEFAULT: '#a05c1a',
          bg: '#fef3e6',
        },
      },
      borderRadius: {
        sm: '0.375rem',
        md: '0.625rem',
        lg: '0.875rem',
        xl: '1.25rem',
      },
      boxShadow: {
        sm: '0 1px 2px oklch(0.2 0.05 300 / 0.06)',
        md: '0 4px 14px oklch(0.2 0.05 300 / 0.09)',
        lg: '0 12px 36px oklch(0.2 0.05 300 / 0.14)',
      },
      transitionTimingFunction: {
        interactive: 'cubic-bezier(0.16, 1, 0.3, 1)',
      },
      transitionDuration: {
        interactive: '180ms',
      },
    },
  },
  plugins: [],
}
