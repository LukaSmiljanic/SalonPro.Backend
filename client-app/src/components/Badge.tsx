import React from 'react';
import type { AppointmentStatus } from '../types';

interface BadgeProps {
  status: AppointmentStatus;
  className?: string;
}

const statusMap: Record<AppointmentStatus, { label: string; className: string }> = {
  Pending:    { label: 'Na čekanju',   className: 'badge-pending' },
  Confirmed:  { label: 'Potvrđeno',    className: 'badge-confirmed' },
  InProgress: { label: 'U toku',       className: 'badge-in-progress' },
  Completed:  { label: 'Završeno',     className: 'badge-completed' },
  Cancelled:  { label: 'Otkazano',     className: 'badge-cancelled' },
  NoShow:     { label: 'Nije se pojavio', className: 'badge-no-show' },
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
