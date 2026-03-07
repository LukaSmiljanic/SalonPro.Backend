import React, { useState, useMemo, useCallback } from 'react';
import {
  format, parseISO, startOfWeek, addDays, addWeeks, subWeeks, isToday
} from 'date-fns';
import { srLatn } from 'date-fns/locale';
import { useQuery, useQueryClient, useMutation } from '@tanstack/react-query';
import { ChevronLeft, ChevronRight, Plus, RefreshCw, Filter } from 'lucide-react';
import { getAppointments, cancelAppointment, completeAppointment } from '../api/appointments';
import { getStaff } from '../api/staff';
import type { Appointment, StaffMember } from '../types';
import { queryKeys } from '../lib/queryKeys';
import { AppointmentBlock } from '../components/AppointmentBlock';
import { Badge } from '../components/Badge';
import { Button } from '../components/Button';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { Modal } from '../components/Modal';
import { CreateAppointmentModal } from '../components/CreateAppointmentModal';

const HOUR_HEIGHT = 60;
const DAY_START = 8;
const DAY_END   = 22;
const HOURS = Array.from({ length: DAY_END - DAY_START }, (_, i) => DAY_START + i);

export const CalendarPage: React.FC = () => {
  const queryClient = useQueryClient();
  const [weekStart, setWeekStart] = useState(() =>
    startOfWeek(new Date(), { weekStartsOn: 1 })
  );
  const [selectedStaffId, setSelectedStaffId] = useState<string>('all');
  const [selectedAppt, setSelectedAppt] = useState<Appointment | null>(null);
  const [createModalOpen, setCreateModalOpen] = useState(false);

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

  const getApptPosition = (appt: Appointment): { top: number; height: number } => {
    try {
      const start = parseISO(appt.startTime);
      const end = parseISO(appt.endTime);
      const dayStart = new Date(start);
      dayStart.setHours(DAY_START, 0, 0, 0);
      const minutesFromStart = (start.getTime() - dayStart.getTime()) / (60 * 1000);
      const durationMinutes = (end.getTime() - start.getTime()) / (60 * 1000);
      return {
        top: Math.max(0, (minutesFromStart / 60) * HOUR_HEIGHT),
        height: Math.max(20, (durationMinutes / 60) * HOUR_HEIGHT),
      };
    } catch {
      return { top: 0, height: HOUR_HEIGHT };
    }
  };

  return (
    <div className="flex flex-col h-[calc(100vh-56px)]">

      {/* Toolbar */}
      <div className="container-main py-3 flex items-center gap-3 border-b border-divider flex-wrap">
        {/* Week nav */}
        <div className="flex items-center gap-1">
          <button
            onClick={() => setWeekStart(w => subWeeks(w, 1))}
            className="p-1.5 rounded-md hover:bg-surface-2 text-text-muted"
          >
            <ChevronLeft size={16} />
          </button>
          <span className="text-sm font-medium text-text px-1 min-w-[160px] text-center">
            {format(weekStart, 'MMM d', { locale: srLatn })} – {format(addDays(weekStart, 6), 'MMM d, yyyy', { locale: srLatn })}
          </span>
          <button
            onClick={() => setWeekStart(w => addWeeks(w, 1))}
            className="p-1.5 rounded-md hover:bg-surface-2 text-text-muted"
          >
            <ChevronRight size={16} />
          </button>
        </div>

        <Button
          variant="secondary"
          size="sm"
          onClick={() => setWeekStart(startOfWeek(new Date(), { weekStartsOn: 1 }))}
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
              className="text-sm bg-surface border border-border rounded-md px-2 py-1 text-text focus:outline-none focus:ring-2 focus:ring-primary/30"
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
            Novi
          </Button>
        </div>
      </div>

      {error && (
        <div className="container-main py-2">
          <div className="p-3 bg-error-bg border border-error/20 rounded-lg text-sm text-error">{error?.message ?? 'Greška'}</div>
        </div>
      )}

      {/* Calendar grid */}
      <div className="flex-1 overflow-auto">
        <div className="min-w-[700px]">

          {/* Day headers */}
          <div className="calendar-grid border-b border-divider sticky top-0 bg-surface z-10">
            <div className="" /> {/* time col spacer */}
            {weekDays.map(day => (
              <div
                key={day.toISOString()}
                className={`px-2 py-2 text-center border-l border-divider
                  ${isToday(day) ? 'bg-primary-highlight' : ''}`}
              >
                <p className="text-[11px] text-text-faint uppercase tracking-wide">
                  {format(day, 'EEE', { locale: srLatn })}
                </p>
                <p className={`text-sm font-semibold ${ isToday(day) ? 'text-primary' : 'text-text' }`}>
                  {format(day, 'd')}
                </p>
              </div>
            ))}
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
                {HOURS.map(hour => (
                  <div key={hour} className="calendar-time-label">
                    {hour}:00
                  </div>
                ))}
              </div>

              {/* Day columns */}
              {weekDays.map(day => (
                <div key={day.toISOString()} className="calendar-day-col border-l border-divider">
                  {/* Hour slots */}
                  {HOURS.map(hour => (
                    <div key={hour} className="calendar-slot" />
                  ))}

                  {/* Appointment blocks */}
                  {getAppointmentsForDay(day).map(appt => {
                    const { top, height } = getApptPosition(appt);
                    return (
                      <AppointmentBlock
                        key={appt.id}
                        appointment={appt}
                        topPx={top}
                        heightPx={height}
                        onClick={setSelectedAppt}
                      />
                    );
                  })}
                </div>
              ))}
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
            {/* Actions: Cancel (manual), Complete (manual) */}
            <div className="flex flex-wrap gap-2 pt-2 border-t border-divider">
              {selectedAppt.status !== 'Completed' && selectedAppt.status !== 'Cancelled' && (
                <>
                  <Button
                    variant="secondary"
                    size="sm"
                    onClick={() => completeMutation.mutate(selectedAppt.id)}
                    disabled={completeMutation.isPending}
                  >
                    {completeMutation.isPending ? '...' : 'Završi'}
                  </Button>
                  <Button
                    variant="secondary"
                    size="sm"
                    onClick={() => cancelMutation.mutate(selectedAppt.id)}
                    disabled={cancelMutation.isPending}
                  >
                    {cancelMutation.isPending ? '...' : 'Otkaži'}
                  </Button>
                </>
              )}
            </div>
          </div>
        )}
      </Modal>

      <CreateAppointmentModal
        isOpen={createModalOpen}
        onClose={() => setCreateModalOpen(false)}
      />
    </div>
  );
};
