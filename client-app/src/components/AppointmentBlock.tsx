import React, { useRef, useCallback } from 'react';
import type { Appointment } from '../types';

interface AppointmentBlockProps {
  appointment: Appointment;
  topPx: number;
  heightPx: number;
  onClick?: (appt: Appointment) => void;
  onDragStart?: (appt: Appointment, startY: number, originalTop: number) => void;
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
  onDragStart,
}) => {
  const cls = categoryClass[appointment.serviceCategory] ?? 'appt-block-default';
  const isDraggingRef = useRef(false);
  const startYRef = useRef(0);

  const handleMouseDown = useCallback((e: React.MouseEvent) => {
    // Only left click
    if (e.button !== 0) return;
    isDraggingRef.current = false;
    startYRef.current = e.clientY;

    const handleMouseMove = (moveEvt: MouseEvent) => {
      const delta = Math.abs(moveEvt.clientY - startYRef.current);
      if (delta > 5 && !isDraggingRef.current) {
        isDraggingRef.current = true;
        onDragStart?.(appointment, startYRef.current, topPx);
      }
    };

    const handleMouseUp = () => {
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
      if (!isDraggingRef.current) {
        onClick?.(appointment);
      }
    };

    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);
  }, [appointment, topPx, onClick, onDragStart]);

  return (
    <div
      className={`appt-block ${cls}`}
      style={{ top: topPx, height: Math.max(heightPx - 4, 20) }}
      onMouseDown={handleMouseDown}
      title={`${appointment.clientName} – ${appointment.serviceName}`}
    >
      <p className="text-[11px] font-semibold leading-tight truncate">{appointment.clientName}</p>
      {heightPx > 30 && (
        <p className="text-[10px] leading-tight truncate opacity-80">{appointment.serviceName}</p>
      )}
    </div>
  );
};
