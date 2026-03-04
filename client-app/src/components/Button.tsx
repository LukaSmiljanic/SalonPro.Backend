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
  primary:   'bg-primary text-white hover:bg-primary-hover active:bg-primary-active',
  secondary: 'bg-surface border border-border text-text hover:bg-surface-2 active:bg-surface-offset',
  ghost:     'text-text-muted hover:bg-surface-2 active:bg-surface-offset',
  danger:    'bg-error text-white hover:brightness-90 active:brightness-75',
};

const sizeClasses: Record<Size, string> = {
  sm: 'h-7 px-3 text-xs gap-1.5',
  md: 'h-9 px-4 text-sm gap-2',
  lg: 'h-11 px-5 text-base gap-2',
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
      className={`inline-flex items-center justify-center rounded-md font-medium transition-interactive select-none
        ${variantClasses[variant]} ${sizeClasses[size]} ${className}
        ${disabled || loading ? 'opacity-50 cursor-not-allowed' : 'cursor-pointer'}`}
      disabled={disabled || loading}
      {...props}
    >
      {loading ? <span className="w-4 h-4 border-2 border-current border-t-transparent rounded-full animate-spin" /> : icon}
      {children}
    </button>
  );
};
