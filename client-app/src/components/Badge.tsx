import React from 'react';
import type { AppointmentStatus } from '../types';

interface BadgeProps {
  status: AppointmentStatus;
  className?: string;
}

const statusMap: Record<AppointmentStatus, { label: string; className: string }> = {
  Pending:    { label: 'Pending',     className: 'badge-pending' },
  Confirmed:  { label: 'Confirmed',   className: 'badge-confirmed' },
  InProgress: { label: 'In Progress', className: 'badge-in-progress' },
  Completed:  { label: 'Completed',   className: 'badge-completed' },
  Cancelled:  { label: 'Cancelled',   className: 'badge-cancelled' },
  NoShow:     { label: 'No Show',     className: 'badge-no-show' },
};

export const Badge: React.FC<BadgeProps> = ({ status, className = '' }) => {
  const { label, className: statusClass } = statusMap[status] ?? {
    label: status,
    className: 'badge-pending',
  };
  return (
    <span className={`badge ${statusClass} ${className}`}>
      {label}
    </span>
  );
};
