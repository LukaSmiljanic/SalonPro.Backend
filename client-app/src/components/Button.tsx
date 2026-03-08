import React from 'react';

type Variant = 'primary' | 'secondary' | 'ghost' | 'danger';
type Size = 'sm' | 'md' | 'lg';

interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: Variant;
  size?: Size;
  loading?: boolean;
  icon?: React.ReactNode;
}

const variantClasses: Record<Variant, string> = {
  primary:
    'bg-primary text-white shadow-sm ' +
    'hover:bg-primary-hover hover:shadow-md ' +
    'active:bg-primary-active active:shadow-sm active:scale-[0.98]',
  secondary:
    'bg-surface border border-border text-text shadow-sm ' +
    'hover:bg-surface-2 hover:border-primary/30 hover:shadow-md ' +
    'active:bg-surface-offset active:scale-[0.98]',
  ghost:
    'text-text-muted bg-transparent ' +
    'hover:bg-surface-2 hover:text-text ' +
    'active:bg-surface-offset active:scale-[0.98]',
  danger:
    'bg-error text-white shadow-sm ' +
    'hover:brightness-110 hover:shadow-md ' +
    'active:brightness-90 active:shadow-sm active:scale-[0.98]',
};

const sizeClasses: Record<Size, string> = {
  sm: 'h-8 px-3 text-xs gap-1.5 rounded-lg',
  md: 'h-10 px-4 text-sm gap-2 rounded-lg',
  lg: 'h-12 px-6 text-sm gap-2.5 rounded-xl',
};

export const Button: React.FC<ButtonProps> = ({
  variant = 'primary',
  size = 'md',
  loading = false,
  icon,
  children,
  className = '',
  disabled,
  ...props
}) => {
  return (
    <button
      className={[
        'inline-flex items-center justify-center font-medium select-none',
        'transition-all duration-200 ease-[cubic-bezier(0.16,1,0.3,1)]',
        'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary/40 focus-visible:ring-offset-2 focus-visible:ring-offset-surface',
        variantClasses[variant],
        sizeClasses[size],
        disabled || loading
          ? 'opacity-50 cursor-not-allowed pointer-events-none'
          : 'cursor-pointer',
        className,
      ].join(' ')}
      disabled={disabled || loading}
      {...props}
    >
      {loading ? (
        <span className="w-4 h-4 border-2 border-current border-t-transparent rounded-full animate-spin shrink-0" />
      ) : icon ? (
        <span className="shrink-0 flex items-center">{icon}</span>
      ) : null}
      {children}
    </button>
  );
};
