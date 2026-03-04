import React from 'react';
import type { Appointment } from '../types';

interface AppointmentBlockProps {
  appointment: Appointment;
  topPx: number;
  heightPx: number;
  onClick?: (appt: Appointment) => void;
}

const categoryClass: Record<string, string> = {
  Kosa:   'appt-block-kosa',
  Nokti:  'appt-block-nokti',
  Spa:    'appt-block-spa',
  Lepota: 'appt-block-lepota',
};

export const AppointmentBlock: React.FC<AppointmentBlockProps> = ({
  appointment,
  topPx,
  heightPx,
  onClick,
}) => {
  const cls = categoryClass[appointment.serviceCategory] ?? 'appt-block-default';
  return (
    <div
      className={`appt-block ${cls}`}
      style={{ top: topPx, height: Math.max(heightPx - 4, 20) }}
      onClick={() => onClick?.(appointment)}
      title={`${appointment.clientName} – ${appointment.serviceName}`}
    >
      <p className="text-[11px] font-semibold leading-tight truncate">{appointment.clientName}</p>
      {heightPx > 30 && (
        <p className="text-[10px] leading-tight truncate opacity-80">{appointment.serviceName}</p>
      )}
    </div>
  );
};
