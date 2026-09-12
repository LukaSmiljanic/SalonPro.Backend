import React, { useRef, useCallback } from 'react';
import type { Appointment } from '../types';

interface AppointmentBlockProps {
  appointment: Appointment;
  topPx: number;
  heightPx: number;
  /** When set, block is narrowed and offset for overlapping appointments in the same column (e.g. “all staff”). */
  columnLayout?: { leftPct: number; widthPct: number };
  /** Show staff name under client when calendar shows all employees. */
  showStaffLabel?: boolean;
  onClick?: (appt: Appointment) => void;
  onDragStart?: (appt: Appointment, startY: number, originalTop: number) => void;
}

const categoryClass: Record<string, string> = {
  Kosa:   'appt-block-kosa',
  Nokti:  'appt-block-nokti',
  Spa:    'appt-block-spa',
  Lepota: 'appt-block-lepota',
};

const statusClass: Partial<Record<Appointment['status'], string>> = {
  Pending: 'appt-block-pending',
  Confirmed: 'appt-block-confirmed',
  InProgress: 'appt-block-in-progress',
  Completed: 'appt-block-completed',
  Cancelled: 'appt-block-cancelled',
  NoShow: 'appt-block-no-show',
};

export const AppointmentBlock: React.FC<AppointmentBlockProps> = ({
  appointment,
  topPx,
  heightPx,
  columnLayout,
  showStaffLabel,
  onClick,
  onDragStart,
}) => {
  const cls = statusClass[appointment.status] ?? categoryClass[appointment.serviceCategory] ?? 'appt-block-default';
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

  const layoutStyle: React.CSSProperties | undefined = columnLayout
    ? {
        left: `${columnLayout.leftPct}%`,
        width: `${columnLayout.widthPct}%`,
        right: 'auto',
        boxSizing: 'border-box',
        paddingLeft: 2,
        paddingRight: 2,
      }
    : undefined;

  const compact = heightPx < 46;
  const padY = compact ? 2 : 4;
  const padX = compact ? 5 : 6;
  const boxHeight = Math.max(heightPx - 4, 26);

  const visitTitle =
    typeof appointment.visitNumber === 'number'
      ? ` · ${appointment.visitNumber}. poseta${appointment.isLoyaltyMilestoneVisit ? ' (jubilarna / loyalty prag)' : ''}`
      : '';

  return (
    <div
      className={`appt-block ${cls} relative ${appointment.isLoyaltyMilestoneVisit ? 'ring-2 ring-amber-500/85 z-[5]' : ''}`}
      style={{
        top: topPx,
        height: boxHeight,
        padding: `${padY}px ${padX}px`,
        ...layoutStyle,
      }}
      onMouseDown={handleMouseDown}
      title={`${appointment.clientName} – ${appointment.serviceName}${appointment.staffName ? ` (${appointment.staffName})` : ''}${visitTitle}`}
    >
      {appointment.isLoyaltyMilestoneVisit && (
        <span
          className="absolute top-0.5 right-0.5 flex h-3.5 min-w-[14px] items-center justify-center rounded bg-amber-500/95 px-0.5 text-[8px] font-bold text-white shadow-sm pointer-events-none"
          title="Jubilarna poseta (loyalty prag)"
        >
          ★
        </span>
      )}
      <p
        className={`font-semibold leading-tight truncate pr-4 ${compact ? 'text-[10px]' : 'text-[11px]'}`}
      >
        {appointment.clientName}
      </p>
      {typeof appointment.visitNumber === 'number' && heightPx > 20 && (
        <p
          className={`leading-tight truncate ${compact ? 'text-[8px]' : 'text-[9px]'} ${
            appointment.isLoyaltyMilestoneVisit ? 'text-amber-900 font-semibold' : 'opacity-75'
          }`}
        >
          {appointment.visitNumber}. poseta
          {appointment.isLoyaltyMilestoneVisit ? ' · jubilej' : ''}
        </p>
      )}
      {showStaffLabel && appointment.staffName && heightPx > 22 && (
        <p
          className={`leading-tight truncate opacity-75 font-medium ${compact ? 'text-[8px]' : 'text-[9px]'}`}
        >
          {appointment.staffName}
        </p>
      )}
      {heightPx > 18 && (
        <p className={`leading-tight truncate opacity-80 ${compact ? 'text-[9px]' : 'text-[10px]'}`}>
          {appointment.serviceName}
        </p>
      )}
    </div>
  );
};
