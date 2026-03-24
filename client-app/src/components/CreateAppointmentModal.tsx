import React, { useState, useEffect, useMemo } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { format, setHours, setMinutes } from 'date-fns';
import { getClients } from '../api/clients';
import { getStaff } from '../api/staff';
import { getServices } from '../api/services';
import { createAppointment } from '../api/appointments';
import { getWorkingHours } from '../api/settings';
import type { Client, StaffMember, Service, CreateAppointmentRequest } from '../types';
import { queryKeys } from '../lib/queryKeys';
import { Modal } from './Modal';
import { Button } from './Button';
import { Input } from './Input';
import { LoadingSpinner } from './LoadingSpinner';

/** Build 24h time options in 15-min steps between startHour and endHour */
function buildTimeSlots(startHour: number, endHour: number): string[] {
  const slots: string[] = [];
  for (let h = startHour; h < endHour; h++) {
    for (const m of [0, 15, 30, 45]) {
      slots.push(`${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}`);
    }
  }
  return slots;
}

const DEFAULT_TIME_SLOTS = buildTimeSlots(7, 22);

interface CreateAppointmentModalProps {
  isOpen: boolean;
  onClose: () => void;
  /** Optional: callback after create (e.g. navigate). Invalidation is done via React Query. */
  onCreated?: () => void;
  initialDate?: Date;
}

export const CreateAppointmentModal: React.FC<CreateAppointmentModalProps> = ({
  isOpen,
  onClose,
  onCreated,
  initialDate,
}) => {
  const queryClient = useQueryClient();
  const [error, setError] = useState<string | null>(null);

  const [clientId, setClientId] = useState('');
  const [staffId, setStaffId] = useState('');
  const [serviceId, setServiceId] = useState('');
  const [date, setDate] = useState('');
  const [time, setTime] = useState('09:00');
  const [notes, setNotes] = useState('');

  const { data: clientsData, isLoading: loadingData } = useQuery({
    queryKey: queryKeys.clients.list({ page: 1, pageSize: 500 }),
    queryFn: () => getClients({ page: 1, pageSize: 500 }),
    enabled: isOpen,
  });

  const { data: staff = [] } = useQuery({
    queryKey: queryKeys.staff.list(),
    queryFn: () => getStaff(),
    enabled: isOpen,
  });

  const { data: services = [] } = useQuery({
    queryKey: queryKeys.services.list(false),
    queryFn: () => getServices(),
    enabled: isOpen,
  });

  const { data: workingHoursData } = useQuery({
    queryKey: queryKeys.settings.workingHours(),
    queryFn: getWorkingHours,
    enabled: isOpen,
  });

  const timeSlots = useMemo(() => {
    if (!workingHoursData || workingHoursData.length === 0) return DEFAULT_TIME_SLOTS;
    const workingDays = workingHoursData.filter(d => d.isWorkingDay);
    if (workingDays.length === 0) return DEFAULT_TIME_SLOTS;
    const minH = Math.min(...workingDays.map(d => parseInt(d.startTime.slice(0, 2), 10)));
    const maxH = Math.max(...workingDays.map(d => {
      const h = parseInt(d.endTime.slice(0, 2), 10);
      const m = parseInt(d.endTime.slice(3, 5), 10);
      return m > 0 ? h + 1 : h;
    }));
    return buildTimeSlots(
      isNaN(minH) ? 7 : Math.max(0, minH),
      isNaN(maxH) ? 22 : Math.min(24, maxH)
    );
  }, [workingHoursData]);

  const createMutation = useMutation({
    mutationFn: (payload: CreateAppointmentRequest) => createAppointment(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.dashboard.all });
      queryClient.invalidateQueries({ queryKey: queryKeys.appointments.all });
      onCreated?.();
      handleClose();
    },
    onError: (err: unknown) => {
      const message = err && typeof err === 'object' && 'response' in err
        ? (err as { response?: { data?: { detail?: string } } }).response?.data?.detail
        : 'Failed to create appointment.';
      setError(typeof message === 'string' ? message : 'Nije moguće kreirati termin.');
    },
  });

  const clients = clientsData?.items ?? [];

  useEffect(() => {
    if (isOpen && initialDate) {
      setDate(format(initialDate, 'yyyy-MM-dd'));
      setTime(format(initialDate, 'HH:mm'));
    } else if (isOpen && !date) {
      const today = new Date();
      setDate(format(today, 'yyyy-MM-dd'));
      setTime('09:00');
    }
  }, [isOpen, initialDate]);

  const resetForm = () => {
    setClientId('');
    setStaffId('');
    setServiceId('');
    const today = new Date();
    setDate(format(today, 'yyyy-MM-dd'));
    setTime('09:00');
    setNotes('');
    setError(null);
  };

  const handleClose = () => {
    resetForm();
    onClose();
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!clientId || !staffId || !serviceId || !date || !time) {
      setError('Popunite klijenta, osoblje, uslugu, datum i vreme.');
      return;
    }
    setError(null);
    const [year, month, day] = date.split('-').map(Number);
    const [hours, minutes] = time.split(':').map(Number);
    const startDateTime = setMinutes(setHours(new Date(year, month - 1, day), hours), minutes);
    const startTimeIso = startDateTime.toISOString();
    createMutation.mutate({
      clientId,
      staffId,
      serviceId,
      startTime: startTimeIso,
      notes: notes.trim() || undefined,
    });
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={handleClose}
      title="Novi termin"
      footer={
        <>
          <Button variant="secondary" onClick={handleClose} disabled={createMutation.isPending}>
            Odustani
          </Button>
          <Button onClick={handleSubmit} loading={createMutation.isPending} disabled={loadingData}>
            Kreiraj
          </Button>
        </>
      }
    >
      {loadingData ? (
        <div className="flex justify-center py-8">
          <LoadingSpinner />
        </div>
      ) : (
        <form onSubmit={handleSubmit} className="space-y-4">
          {error && (
            <div className="p-3 bg-error-bg border border-error/20 rounded-lg text-sm text-error">
              {error}
            </div>
          )}

          <div>
            <label className="block text-sm font-medium text-text mb-1">Klijent *</label>
            <select
              value={clientId}
              onChange={e => setClientId(e.target.value)}
              className="w-full h-9 bg-surface border border-border rounded-md px-3 text-sm text-text focus:outline-none focus:ring-2 focus:ring-primary/30"
              required
            >
              <option value="">Izaberite klijenta</option>
              {clients.map(c => (
                <option key={c.id} value={c.id}>
                  {c.firstName} {c.lastName}
                  {c.email ? ` (${c.email})` : ''}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium text-text mb-1">Osoblje *</label>
            <select
              value={staffId}
              onChange={e => setStaffId(e.target.value)}
              className="w-full h-9 bg-surface border border-border rounded-md px-3 text-sm text-text focus:outline-none focus:ring-2 focus:ring-primary/30"
              required
            >
              <option value="">Izaberite zaposlenog</option>
              {staff.map(s => (
                <option key={s.id} value={s.id}>{s.name}</option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium text-text mb-1">Usluga *</label>
            <select
              value={serviceId}
              onChange={e => setServiceId(e.target.value)}
              className="w-full h-9 bg-surface border border-border rounded-md px-3 text-sm text-text focus:outline-none focus:ring-2 focus:ring-primary/30"
              required
            >
              <option value="">Izaberite uslugu</option>
              {services.map(s => (
                <option key={s.id} value={s.id}>
                  {s.name} – {s.duration} min, {new Intl.NumberFormat('sr-Latn-RS', { style: 'currency', currency: 'RSD', minimumFractionDigits: 0 }).format(s.price)}
                </option>
              ))}
            </select>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <Input
              label="Datum *"
              type="date"
              value={date}
              onChange={e => setDate(e.target.value)}
              required
            />
            <div>
              <label className="block text-sm font-medium text-text mb-1">Vreme *</label>
              <select
                value={time}
                onChange={e => setTime(e.target.value)}
                className="w-full h-11 md:h-9 bg-surface border border-border rounded-lg md:rounded-md px-3 text-base md:text-sm text-text focus:outline-none focus:ring-2 focus:ring-primary/30 focus:border-primary transition-interactive"
                required
              >
                {timeSlots.map(t => (
                  <option key={t} value={t}>{t}</option>
                ))}
              </select>
            </div>
          </div>

          <Input
            label="Napomene"
            value={notes}
            onChange={e => setNotes(e.target.value)}
            placeholder="Opciono"
          />
        </form>
      )}
    </Modal>
  );
};
