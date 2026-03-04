import React, { useState, useEffect, useCallback, useMemo } from 'react';
import {
  format, parseISO, startOfWeek, addDays, addWeeks, subWeeks,
  isSameDay, differenceInMinutes, setHours, setMinutes, isToday
} from 'date-fns';
import { ChevronLeft, ChevronRight, Plus, RefreshCw, Filter } from 'lucide-react';
import { getAppointments } from '../api/appointments';
import { getStaff } from '../api/staff';
import type { Appointment, StaffMember } from '../types';
import { AppointmentBlock } from '../components/AppointmentBlock';
import { Badge } from '../components/Badge';
import { Button } from '../components/Button';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { Modal } from '../components/Modal';

const HOUR_HEIGHT = 60; // px per hour
const DAY_START = 8;    // 08:00
const DAY_END   = 20;   // 20:00
const HOURS = Array.from({ length: DAY_END - DAY_START }, (_, i) => DAY_START + i);

export const CalendarPage: React.FC = () => {
  const [weekStart, setWeekStart] = useState(() =>
    startOfWeek(new Date(), { weekStartsOn: 1 })
  );
  const [appointments, setAppointments] = useState<Appointment[]>([]);
  const [staff, setStaff] = useState<StaffMember[]>([]);
  const [selectedStaffId, setSelectedStaffId] = useState<string>('all');
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedAppt, setSelectedAppt] = useState<Appointment | null>(null);

  const weekDays = useMemo(
    () => Array.from({ length: 7 }, (_, i) => addDays(weekStart, i)),
    [weekStart]
  );

  const fetchAppointments = useCallback(async () => {
    try {
      setError(null);
      const startDate = format(weekStart, 'yyyy-MM-dd');
      const endDate   = format(addDays(weekStart, 6), 'yyyy-MM-dd');
      const response  = await getAppointments({
        date: `${startDate}/${endDate}`,
        staffId: selectedStaffId !== 'all' ? selectedStaffId : undefined,
      });
      setAppointments(response.items);
    } catch (err) {
      console.error('Calendar fetch error:', err);
      setError('Failed to load appointments.');
    } finally {
      setIsLoading(false);
    }
  }, [weekStart, selectedStaffId]);

  useEffect(() => {
    const fetchStaff = async () => {
      try {
        const members = await getStaff();
        setStaff(members);
      } catch {
        // non-critical
      }
    };
    fetchStaff();
  }, []);

  useEffect(() => {
    fetchAppointments();
  }, [fetchAppointments]);

  const getAppointmentsForDay = useCallback((day: Date) => {
    return appointments.filter(appt => {
      try {
        return isSameDay(parseISO(appt.startTime), day);
      } catch {
        return false;
      }
    });
  }, [appointments]);

  const getApptPosition = (appt: Appointment): { top: number; height: number } => {
    try {
      const start = parseISO(appt.startTime);
      const end   = parseISO(appt.endTime);
      const dayStart = setMinutes(setHours(start, DAY_START), 0);
      const minutesFromStart = differenceInMinutes(start, dayStart);
      const duration = differenceInMinutes(end, start);
      return {
        top:    (minutesFromStart / 60) * HOUR_HEIGHT,
        height: (duration / 60) * HOUR_HEIGHT,
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
            {format(weekStart, 'MMM d')} – {format(addDays(weekStart, 6), 'MMM d, yyyy')}
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
          Today
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
              <option value="all">All Staff</option>
              {staff.map(s => (
                <option key={s.id} value={s.id}>{s.name}</option>
              ))}
            </select>
          </div>
        )}

        <div className="ml-auto flex items-center gap-2">
          <Button variant="secondary" size="sm" icon={<RefreshCw size={13} />} onClick={fetchAppointments}>
            Refresh
          </Button>
          <Button size="sm" icon={<Plus size={13} />}>
            New
          </Button>
        </div>
      </div>

      {error && (
        <div className="container-main py-2">
          <div className="p-3 bg-error-bg border border-error/20 rounded-lg text-sm text-error">{error}</div>
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
                  {format(day, 'EEE')}
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
        title="Appointment Details"
      >
        {selectedAppt && (
          <div className="space-y-3">
            <div className="flex items-center justify-between">
              <span className="font-semibold text-text">{selectedAppt.clientName}</span>
              <Badge status={selectedAppt.status} />
            </div>
            <div className="grid grid-cols-2 gap-3 text-sm">
              <div>
                <p className="text-text-faint text-xs">Service</p>
                <p className="text-text">{selectedAppt.serviceName}</p>
              </div>
              <div>
                <p className="text-text-faint text-xs">Staff</p>
                <p className="text-text">{selectedAppt.staffName}</p>
              </div>
              <div>
                <p className="text-text-faint text-xs">Date & Time</p>
                <p className="text-text">
                  {format(parseISO(selectedAppt.startTime), 'EEE, MMM d · HH:mm')} –
                  {format(parseISO(selectedAppt.endTime), ' HH:mm')}
                </p>
              </div>
              <div>
                <p className="text-text-faint text-xs">Price</p>
                <p className="text-text font-medium">€{selectedAppt.price.toFixed(2)}</p>
              </div>
            </div>
            {selectedAppt.notes && (
              <div>
                <p className="text-text-faint text-xs">Notes</p>
                <p className="text-sm text-text">{selectedAppt.notes}</p>
              </div>
            )}
          </div>
        )}
      </Modal>
    </div>
  );
};
