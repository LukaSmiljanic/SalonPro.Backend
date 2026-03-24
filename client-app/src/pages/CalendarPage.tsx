import React, { useState, useMemo, useCallback, useRef, useEffect } from 'react';
import {
  format, parseISO, startOfWeek, addDays, addWeeks, subWeeks, isToday
} from 'date-fns';
import { srLatn } from 'date-fns/locale';
import { useQuery, useQueryClient, useMutation } from '@tanstack/react-query';
import { ChevronLeft, ChevronRight, Plus, RefreshCw, Filter } from 'lucide-react';
import { getAppointments, cancelAppointment, completeAppointment, rescheduleAppointment } from '../api/appointments';
import { getStaff } from '../api/staff';
import { getWorkingHours } from '../api/settings';
import type { Appointment } from '../types';
import { queryKeys } from '../lib/queryKeys';
import { toLocalISOString } from '../lib/dateUtils';
import { AppointmentBlock } from '../components/AppointmentBlock';
import { Badge } from '../components/Badge';
import { Button } from '../components/Button';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { Modal } from '../components/Modal';
import { CreateAppointmentModal } from '../components/CreateAppointmentModal';

const HOUR_HEIGHT = 60;
const FALLBACK_DAY_START = 8;
const FALLBACK_DAY_END   = 22;
const SNAP_MINUTES = 15;
const SNAP_PX = HOUR_HEIGHT * (SNAP_MINUTES / 60); // 15px per 15 min

function snapToGrid(px: number): number {
  return Math.round(px / SNAP_PX) * SNAP_PX;
}

function pxToTime(px: number, dayStart: number): { hours: number; minutes: number } {
  const totalMinutes = dayStart * 60 + (px / HOUR_HEIGHT) * 60;
  return {
    hours: Math.floor(totalMinutes / 60),
    minutes: Math.round(totalMinutes % 60),
  };
}

export const CalendarPage: React.FC = () => {
  const queryClient = useQueryClient();
  const [weekStart, setWeekStart] = useState(() =>
    startOfWeek(new Date(), { weekStartsOn: 1 })
  );
  const [selectedStaffId, setSelectedStaffId] = useState<string>('all');
  const [selectedAppt, setSelectedAppt] = useState<Appointment | null>(null);
  const [createModalOpen, setCreateModalOpen] = useState(false);
  const [createModalDate, setCreateModalDate] = useState<Date | undefined>(undefined);

  // ── Drag state ───────────────────────────────────────────────────────────
  // Use a single state object + refs so mouse handlers never read stale values.
  const [dragState, setDragState] = useState<{
    appt: Appointment;
    dayIndex: number;
    ghostTop: number;
    ghostHeight: number;
  } | null>(null);

  const dragGhostTopRef = useRef(0);
  const dragStartYRef = useRef(0);
  const dragOriginalTopRef = useRef(0);
  const dragApptRef = useRef<Appointment | null>(null);
  const gridScrollRef = useRef<HTMLDivElement>(null);

  const weekDays = useMemo(
    () => Array.from({ length: 7 }, (_, i) => addDays(weekStart, i)),
    [weekStart]
  );

  const startDate = format(weekStart, 'yyyy-MM-dd');
  const endDate = format(addDays(weekStart, 6), 'yyyy-MM-dd');
  const dateRange = `${startDate}/${endDate}`;

  const { data: appointmentsData, isLoading, error } = useQuery({
    queryKey: queryKeys.appointments.byDate(dateRange, selectedStaffId === 'all' ? undefined : selectedStaffId),
    queryFn: () => getAppointments({
      date: dateRange,
      staffId: selectedStaffId !== 'all' ? selectedStaffId : undefined,
    }),
  });

  const { data: staff = [] } = useQuery({
    queryKey: queryKeys.staff.list(),
    queryFn: () => getStaff(),
  });

  const { data: workingHoursData } = useQuery({
    queryKey: queryKeys.settings.workingHours(),
    queryFn: getWorkingHours,
    staleTime: 5 * 60 * 1000, // 5 min — settings don't change often
  });

  const dayStart = useMemo(() => {
    if (!workingHoursData || workingHoursData.length === 0) return FALLBACK_DAY_START;
    const workingDays = workingHoursData.filter(d => d.isWorkingDay);
    if (workingDays.length === 0) return FALLBACK_DAY_START;
    const minHour = Math.min(...workingDays.map(d => parseInt(d.startTime.slice(0, 2), 10)));
    return isNaN(minHour) ? FALLBACK_DAY_START : Math.max(0, minHour);
  }, [workingHoursData]);

  const dayEnd = useMemo(() => {
    if (!workingHoursData || workingHoursData.length === 0) return FALLBACK_DAY_END;
    const workingDays = workingHoursData.filter(d => d.isWorkingDay);
    if (workingDays.length === 0) return FALLBACK_DAY_END;
    const maxHour = Math.max(...workingDays.map(d => {
      const h = parseInt(d.endTime.slice(0, 2), 10);
      const m = parseInt(d.endTime.slice(3, 5), 10);
      return m > 0 ? h + 1 : h; // round up if minutes > 0
    }));
    return isNaN(maxHour) ? FALLBACK_DAY_END : Math.min(24, maxHour);
  }, [workingHoursData]);

  const hours = useMemo(
    () => Array.from({ length: dayEnd - dayStart }, (_, i) => dayStart + i),
    [dayStart, dayEnd]
  );

  // Build a Set of non-working day-of-week indices (0=Sunday)
  const nonWorkingDays = useMemo(() => {
    if (!workingHoursData || workingHoursData.length === 0) return new Set<number>();
    return new Set(
      workingHoursData.filter(d => !d.isWorkingDay).map(d => d.dayOfWeek)
    );
  }, [workingHoursData]);

  const isDayOff = (date: Date) => nonWorkingDays.has(date.getDay());

  const appointments = appointmentsData?.items ?? [];

  const cancelMutation = useMutation({
    mutationFn: (id: string) => cancelAppointment(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.appointments.all });
      setSelectedAppt(null);
    },
  });

  const completeMutation = useMutation({
    mutationFn: (id: string) => completeAppointment(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.appointments.all });
      setSelectedAppt(null);
    },
  });

  const rescheduleMutation = useMutation({
    mutationFn: ({ id, newStartTime }: { id: string; newStartTime: string }) =>
      rescheduleAppointment(id, newStartTime),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.appointments.all });
    },
  });

  const getAppointmentsForDay = useCallback((day: Date) => {
    return appointments.filter(appt => {
      try {
        const apptStart = parseISO(appt.startTime);
        return (
          apptStart.getFullYear() === day.getFullYear() &&
          apptStart.getMonth() === day.getMonth() &&
          apptStart.getDate() === day.getDate()
        );
      } catch {
        return false;
      }
    });
  }, [appointments]);

  const getApptPosition = useCallback((appt: Appointment): { top: number; height: number } => {
    try {
      const start = parseISO(appt.startTime);
      const end = parseISO(appt.endTime);
      const dayStartDate = new Date(start);
      dayStartDate.setHours(dayStart, 0, 0, 0);
      const minutesFromStart = (start.getTime() - dayStartDate.getTime()) / (60 * 1000);
      const durationMinutes = (end.getTime() - start.getTime()) / (60 * 1000);
      return {
        top: Math.max(0, (minutesFromStart / 60) * HOUR_HEIGHT),
        height: Math.max(20, (durationMinutes / 60) * HOUR_HEIGHT),
      };
    } catch {
      return { top: 0, height: HOUR_HEIGHT };
    }
  }, [dayStart]);

  // ── Double-click on empty slot ────────────────────────────────────────────────────────────────────
  const handleSlotDoubleClick = useCallback((day: Date, hour: number) => {
    const d = new Date(day);
    d.setHours(hour, 0, 0, 0);
    setCreateModalDate(d);
    setCreateModalOpen(true);
  }, []);

  // ── Drag start (called from AppointmentBlock) ──────────────────────────────────────────────────────
  const handleDragStart = useCallback((appt: Appointment, startY: number, originalTop: number) => {
    const pos = getApptPosition(appt);
    const apptDate = parseISO(appt.startTime);
    const dayIdx = weekDays.findIndex(d =>
      d.getFullYear() === apptDate.getFullYear() &&
      d.getMonth() === apptDate.getMonth() &&
      d.getDate() === apptDate.getDate()
    );

    // Store everything in refs for the global handlers
    dragStartYRef.current = startY;
    dragOriginalTopRef.current = originalTop;
    dragGhostTopRef.current = originalTop;
    dragApptRef.current = appt;

    setDragState({
      appt,
      dayIndex: dayIdx,
      ghostTop: originalTop,
      ghostHeight: pos.height,
    });
  }, [weekDays, getApptPosition]);

  // ── Global mouse handlers (mounted only while dragging) ────
  useEffect(() => {
    if (!dragState) return;

    const handleMouseMove = (e: MouseEvent) => {
      const deltaY = e.clientY - dragStartYRef.current;
      const raw = dragOriginalTopRef.current + deltaY;
      const maxTop = (dayEnd - dayStart) * HOUR_HEIGHT - dragState.ghostHeight;
      const clamped = Math.max(0, Math.min(raw, maxTop));
      const snapped = snapToGrid(clamped);
      dragGhostTopRef.current = snapped;
      setDragState(prev => prev ? { ...prev, ghostTop: snapped } : null);
    };

    const handleMouseUp = () => {
      const appt = dragApptRef.current;
      const ghostTop = dragGhostTopRef.current;

      if (appt) {
        const { hours: newHours, minutes: newMinutes } = pxToTime(ghostTop, dayStart);
        const apptDate = parseISO(appt.startTime);
        const newStart = new Date(apptDate);
        newStart.setHours(newHours, newMinutes, 0, 0);

        const origStart = parseISO(appt.startTime);
        if (newStart.getTime() !== origStart.getTime()) {
          rescheduleMutation.mutate({
            id: appt.id,
            newStartTime: toLocalISOString(newStart),
          });
        }
      }

      dragApptRef.current = null;
      setDragState(null);
    };

    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);
    document.body.style.userSelect = 'none';
    document.body.style.cursor = 'grabbing';

    return () => {
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
      document.body.style.userSelect = '';
      document.body.style.cursor = '';
    };
    // Only re-subscribe when drag starts/stops — NOT on every ghost move
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [dragState?.appt?.id, dayStart, dayEnd]);

  const handleCreateModalClose = useCallback(() => {
    setCreateModalOpen(false);
    setCreateModalDate(undefined);
  }, []);

  // Auto-scroll to current time on initial load
  useEffect(() => {
    const timer = setTimeout(() => {
      if (gridScrollRef.current) {
        const now = new Date();
        const currentHour = now.getHours();
        const currentMin = now.getMinutes();
        const scrollTarget = Math.max(0, ((currentHour - dayStart) + currentMin / 60) * HOUR_HEIGHT - 100);
        gridScrollRef.current.scrollTo({ top: scrollTarget, behavior: 'smooth' });
      }
    }, 300);
    return () => clearTimeout(timer);
  }, [dayStart]); // re-scroll when dayStart changes

  // Helper to format the drag time hint
  const dragTimeHint = dragState
    ? (() => {
        const { hours: h, minutes: m } = pxToTime(dragState.ghostTop, dayStart);
        return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}`;
      })()
    : '';

  return (
    <div className="flex flex-col h-[calc(100vh-56px)]">

      {/* Toolbar */}
      <div className="container-main py-3 flex items-center gap-3 border-b border-divider flex-wrap">
        {/* Week nav */}
        <div className="flex items-center gap-1">
          <button
            onClick={() => setWeekStart(w => subWeeks(w, 1))}
            className="p-1.5 rounded-lg hover:bg-surface-2 text-text-muted transition-all duration-200"
          >
            <ChevronLeft size={16} />
          </button>
          <span className="text-sm font-medium text-text px-1 min-w-[160px] text-center">
            {format(weekStart, 'MMM d', { locale: srLatn })} – {format(addDays(weekStart, 6), 'MMM d, yyyy', { locale: srLatn })}
          </span>
          <button
            onClick={() => setWeekStart(w => addWeeks(w, 1))}
            className="p-1.5 rounded-lg hover:bg-surface-2 text-text-muted transition-all duration-200"
          >
            <ChevronRight size={16} />
          </button>
        </div>

        <Button
          variant="secondary"
          size="sm"
          onClick={() => {
            setWeekStart(startOfWeek(new Date(), { weekStartsOn: 1 }));
            // Scroll to current time
            requestAnimationFrame(() => {
              if (gridScrollRef.current) {
                const now = new Date();
                const currentHour = now.getHours();
                const currentMin = now.getMinutes();
                const scrollTarget = Math.max(0, ((currentHour - dayStart) + currentMin / 60) * HOUR_HEIGHT - 100);
                gridScrollRef.current.scrollTo({ top: scrollTarget, behavior: 'smooth' });
              }
            });
          }}
        >
          Danas
        </Button>

        {/* Staff filter */}
        {staff.length > 0 && (
          <div className="flex items-center gap-1.5">
            <Filter size={13} className="text-text-faint" />
            <select
              value={selectedStaffId}
              onChange={e => setSelectedStaffId(e.target.value)}
              className="text-sm bg-surface border border-border rounded-lg px-2 py-1.5 text-text focus:outline-none focus:ring-2 focus:ring-primary/30 transition-all duration-200"
            >
              <option value="all">Sve osoblje</option>
              {staff.map(s => (
                <option key={s.id} value={s.id}>{s.name}</option>
              ))}
            </select>
          </div>
        )}

        <div className="ml-auto flex items-center gap-2">
          <Button
            variant="secondary"
            size="sm"
            icon={<RefreshCw size={13} />}
            onClick={() => queryClient.invalidateQueries({ queryKey: queryKeys.appointments.all })}
          >
            Osveži
          </Button>
          <Button size="sm" icon={<Plus size={13} />} onClick={() => setCreateModalOpen(true)}>
            Novi termin
          </Button>
        </div>
      </div>

      {error && (
        <div className="container-main py-2">
          <div className="p-3 bg-error-bg border border-error/20 rounded-lg text-sm text-error">{error?.message ?? 'Greška'}</div>
        </div>
      )}

      {/* Calendar grid */}
      <div className="flex-1 overflow-auto" ref={gridScrollRef}>
        <div className="min-w-[700px]">

          {/* Day headers */}
          <div className="calendar-grid border-b border-divider sticky top-0 bg-surface z-10">
            <div className="" /> {/* time col spacer */}
            {weekDays.map(day => {
              const off = isDayOff(day);
              return (
                <div
                  key={day.toISOString()}
                  className={`px-2 py-2 text-center border-l border-divider
                    ${isToday(day) ? 'bg-primary-highlight' : off ? 'bg-surface-offset/60' : ''}`}
                >
                  <p className={`text-[11px] uppercase tracking-wide ${off ? 'text-text-faint/60' : 'text-text-faint'}`}>
                    {format(day, 'EEE', { locale: srLatn })}
                  </p>
                  <p className={`text-sm font-semibold ${isToday(day) ? 'text-primary' : off ? 'text-text-faint/60' : 'text-text'}`}>
                    {format(day, 'd')}
                  </p>
                  {off && <p className="text-[9px] text-text-faint/50 mt-0.5">Neradni dan</p>}
                </div>
              );
            })}
          </div>

          {/* Time rows */}
          {isLoading ? (
            <div className="flex items-center justify-center py-20">
              <LoadingSpinner />
            </div>
          ) : (
            <div className="calendar-grid">
              {/* Time labels column */}
              <div className="calendar-time-col">
                {hours.map(hour => (
                  <div key={hour} className="calendar-time-label">
                    {String(hour).padStart(2, '0')}:00
                  </div>
                ))}
              </div>

              {/* Day columns */}
              {weekDays.map((day, dayIndex) => {
                const off = isDayOff(day);
                return (
                <div
                  key={day.toISOString()}
                  className={`calendar-day-col border-l border-divider ${off ? 'bg-surface-offset/30' : ''}`}
                  style={off ? { backgroundImage: 'repeating-linear-gradient(135deg, transparent, transparent 10px, rgba(0,0,0,0.02) 10px, rgba(0,0,0,0.02) 11px)' } : undefined}
                >
                  {/* Hour slots with double-click */}
                  {hours.map(hour => (
                    <div
                      key={hour}
                      className="calendar-slot"
                      onDoubleClick={() => {
                        if (off) return; // block creating appointments on non-working days
                        handleSlotDoubleClick(day, hour);
                      }}
                      style={off ? { cursor: 'not-allowed' } : undefined}
                    />
                  ))}

                  {/* Current time indicator */}
                  {isToday(day) && (() => {
                    const now = new Date();
                    const h = now.getHours();
                    const m = now.getMinutes();
                    if (h >= dayStart && h < dayEnd) {
                      const topPx = ((h - dayStart) + m / 60) * HOUR_HEIGHT;
                      return (
                        <div
                          className="absolute left-0 right-0 z-20 pointer-events-none"
                          style={{ top: topPx }}
                        >
                          <div className="flex items-center">
                            <div className="w-2 h-2 rounded-full bg-red-500 -ml-1" />
                            <div className="flex-1 h-[2px] bg-red-500" />
                          </div>
                        </div>
                      );
                    }
                    return null;
                  })()}

                  {/* Appointment blocks */}
                  {getAppointmentsForDay(day).map(appt => {
                    const { top, height } = getApptPosition(appt);
                    const isDragging = dragState?.appt.id === appt.id;

                    if (isDragging) {
                      return (
                        <React.Fragment key={appt.id}>
                          {/* Original position — faded */}
                          <div
                            className="appt-block appt-block-default opacity-20 pointer-events-none"
                            style={{ top, height: Math.max(height - 4, 20) }}
                          >
                            <p className="text-[11px] font-semibold leading-tight truncate">{appt.clientName}</p>
                          </div>
                          {/* Dragged ghost */}
                          <div
                            className="appt-block appt-block-default pointer-events-none border-2 border-primary shadow-lg"
                            style={{
                              top: dragState.ghostTop,
                              height: Math.max(height - 4, 20),
                              zIndex: 50,
                              opacity: 0.9,
                            }}
                          >
                            <p className="text-[11px] font-semibold leading-tight truncate">
                              {appt.clientName}
                            </p>
                            <p className="text-[10px] font-medium text-primary leading-tight">
                              {dragTimeHint}
                            </p>
                          </div>
                        </React.Fragment>
                      );
                    }

                    return (
                      <AppointmentBlock
                        key={appt.id}
                        appointment={appt}
                        topPx={top}
                        heightPx={height}
                        onClick={setSelectedAppt}
                        onDragStart={handleDragStart}
                      />
                    );
                  })}
                </div>
              );
              })}
            </div>
          )}
        </div>
      </div>

      {/* Appointment detail modal */}
      <Modal
        isOpen={!!selectedAppt}
        onClose={() => setSelectedAppt(null)}
        title="Detalji termina"
      >
        {selectedAppt && (
          <div className="space-y-3">
            <div className="flex items-center justify-between">
              <span className="font-semibold text-text">{selectedAppt.clientName}</span>
              <Badge status={selectedAppt.status} />
            </div>
            <div className="grid grid-cols-2 gap-3 text-sm">
              <div>
                <p className="text-text-faint text-xs">Usluga</p>
                <p className="text-text">{selectedAppt.serviceName}</p>
              </div>
              <div>
                <p className="text-text-faint text-xs">Osoblje</p>
                <p className="text-text">{selectedAppt.staffName}</p>
              </div>
              <div>
                <p className="text-text-faint text-xs">Datum i vreme</p>
                <p className="text-text">
                  {format(parseISO(selectedAppt.startTime), 'EEE, MMM d · HH:mm', { locale: srLatn })} –
                  {format(parseISO(selectedAppt.endTime), ' HH:mm', { locale: srLatn })}
                </p>
              </div>
              <div>
                <p className="text-text-faint text-xs">Cena</p>
                <p className="text-text font-medium">{new Intl.NumberFormat('sr-Latn-RS', { style: 'currency', currency: 'RSD', minimumFractionDigits: 0 }).format(selectedAppt.price)}</p>
              </div>
            </div>
            {selectedAppt.notes && (
              <div>
                <p className="text-text-faint text-xs">Napomene</p>
                <p className="text-sm text-text">{selectedAppt.notes}</p>
              </div>
            )}
            {/* Actions */}
            <div className="flex flex-wrap gap-2 pt-2 border-t border-divider">
              {selectedAppt.status !== 'Completed' && selectedAppt.status !== 'Cancelled' && (
                <>
                  <Button
                    variant="secondary"
                    size="sm"
                    onClick={() => completeMutation.mutate(selectedAppt.id)}
                    disabled={completeMutation.isPending}
                    loading={completeMutation.isPending}
                  >
                    Završi
                  </Button>
                  <Button
                    variant="danger"
                    size="sm"
                    onClick={() => cancelMutation.mutate(selectedAppt.id)}
                    disabled={cancelMutation.isPending}
                    loading={cancelMutation.isPending}
                  >
                    Otkaži
                  </Button>
                </>
              )}
            </div>
          </div>
        )}
      </Modal>

      <CreateAppointmentModal
        isOpen={createModalOpen}
        onClose={handleCreateModalClose}
        initialDate={createModalDate}
      />
    </div>
  );
};
