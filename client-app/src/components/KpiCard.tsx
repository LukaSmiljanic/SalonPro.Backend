import React from 'react';

interface KpiCardProps {
  label: string;
  value: string | number;
  subtext?: string;
  icon?: React.ReactNode;
  trend?: { value: number; positive?: boolean };
  className?: string;
}

export const KpiCard: React.FC<KpiCardProps> = ({
  label,
  value,
  subtext,
  icon,
  trend,
  className = '',
}) => {
  return (
    <div className={`card card-padded flex flex-col gap-2 ${className}`}>
      <div className="flex items-center justify-between">
        <span className="text-sm text-text-muted">{label}</span>
        {icon && <span className="text-text-faint">{icon}</span>}
      </div>
      <div className="flex items-end gap-2">
        <span className="text-2xl font-semibold text-text text-display">{value}</span>
        {trend && (
          <span className={`text-xs font-medium pb-0.5 ${trend.positive !== false ? 'text-success' : 'text-error'}`}>
            {trend.value >= 0 ? '+' : ''}{trend.value}%
          </span>
        )}
      </div>
      {subtext && <p className="text-xs text-text-faint">{subtext}</p>}
    </div>
  );
};
