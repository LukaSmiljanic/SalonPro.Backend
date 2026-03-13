import React from 'react';

interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  error?: string;
  hint?: string;
  leftIcon?: React.ReactNode;
  rightIcon?: React.ReactNode;
}

export const Input: React.FC<InputProps> = ({
  label,
  error,
  hint,
  leftIcon,
  rightIcon,
  className = '',
  id,
  ...props
}) => {
  const inputId = id ?? `input-${Math.random().toString(36).slice(2)}`;

  return (
    <div className="flex flex-col gap-1">
      {label && (
        <label htmlFor={inputId} className="text-sm font-medium text-text">
          {label}
        </label>
      )}
      <div className="relative flex items-center">
        {leftIcon && (
          <span className="absolute left-3 text-text-muted pointer-events-none">{leftIcon}</span>
        )}
        <input
          id={inputId}
          className={`w-full h-11 md:h-9 bg-surface border rounded-lg md:rounded-md px-3 text-base md:text-sm text-text placeholder:text-text-faint
            transition-interactive
            focus:outline-none focus:ring-2 focus:ring-primary/30 focus:border-primary
            ${error ? 'border-error' : 'border-border'}
            ${leftIcon ? 'pl-9' : ''}
            ${rightIcon ? 'pr-9' : ''}
            ${className}`}
          {...props}
        />
        {rightIcon && (
          <span className="absolute right-3 text-text-muted pointer-events-none">{rightIcon}</span>
        )}
      </div>
      {error && <p className="text-xs text-error">{error}</p>}
      {hint && !error && <p className="text-xs text-text-faint">{hint}</p>}
    </div>
  );
};
