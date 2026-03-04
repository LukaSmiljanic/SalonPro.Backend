import React from 'react';

interface LoadingSpinnerProps {
  size?: 'sm' | 'md' | 'lg';
  className?: string;
  fullPage?: boolean;
}

const sizeMap = {
  sm: 'w-4 h-4 border-2',
  md: 'w-7 h-7 border-[3px]',
  lg: 'w-10 h-10 border-4',
};

export const LoadingSpinner: React.FC<LoadingSpinnerProps> = ({
  size = 'md',
  className = '',
  fullPage = false,
}) => {
  const spinner = (
    <span
      className={`inline-block rounded-full border-primary border-t-transparent animate-spin ${sizeMap[size]} ${className}`}
    />
  );

  if (fullPage) {
    return (
      <div className="fixed inset-0 flex items-center justify-center bg-bg/60 backdrop-blur-sm z-50">
        {spinner}
      </div>
    );
  }

  return spinner;
};
