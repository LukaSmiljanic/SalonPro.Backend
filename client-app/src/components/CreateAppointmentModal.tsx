import React, { useState, useEffect } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { format, setHours, setMinutes } from 'date-fns';
import { getClients } from '../api/clients';
import { getStaff } from '../api/staff';
import { getServices } from '../api/services';
import { createAppointment } from '../api/appointments';
import type { Client, StaffMember, Service, CreateAppointmentRequest } from '../types';
import { queryKeys } from '../lib/queryKeys';
import { Modal } from './Modal';
import { Button } from './Button';
import { Input } from './Input';
import { LoadingSpinner } from './LoadingSpinner';

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
            <Input
              label="Vreme *"
              type="time"
              value={time}
              onChange={e => setTime(e.target.value)}
              required
            />
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
