import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Plus, Save, UserCircle, Mail } from 'lucide-react';
import { getStaff, createStaff } from '../api/staff';
import type { StaffMember, CreateStaffRequest } from '../types';
import { queryKeys } from '../lib/queryKeys';
import { Button } from '../components/Button';
import { Input } from '../components/Input';
import { Modal } from '../components/Modal';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { EmptyState } from '../components/EmptyState';

export const StaffPage: React.FC = () => {
  const queryClient = useQueryClient();
  const [modalOpen, setModalOpen] = useState(false);
  const [form, setForm] = useState<CreateStaffRequest>({
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    specialization: '',
  });
  const [formError, setFormError] = useState<string | null>(null);

  const { data: staff = [], isLoading } = useQuery({
    queryKey: queryKeys.staff.list(),
    queryFn: () => getStaff({ includeInactive: true }),
  });

  const createMutation = useMutation({
    mutationFn: createStaff,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.staff.all });
      queryClient.invalidateQueries({ queryKey: queryKeys.appointments.all });
      setModalOpen(false);
      setForm({ firstName: '', lastName: '', email: '', phone: '', specialization: '' });
      setFormError(null);
    },
    onError: () => setFormError('Nije moguće dodati zaposlenog. Pokušajte ponovo.'),
  });

  const openModal = () => {
    setForm({ firstName: '', lastName: '', email: '', phone: '', specialization: '' });
    setFormError(null);
    setModalOpen(true);
  };

  const handleSubmit = () => {
    if (!form.firstName.trim() || !form.lastName.trim()) {
      setFormError('Ime i prezime su obavezni.');
      return;
    }
    setFormError(null);
    createMutation.mutate(form);
  };

  return (
    <div className="container-main py-6 space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-xl font-semibold text-display text-text">Zaposleni</h1>
          <p className="text-xs text-text-faint mt-0.5">
            Osoblje salona – dodajte zaposlene koji mogu da imaju termine
          </p>
        </div>
        <Button size="sm" icon={<Plus size={13} />} onClick={openModal}>
          Dodaj zaposlenog
        </Button>
      </div>

      {isLoading ? (
        <div className="flex justify-center py-16">
          <LoadingSpinner />
        </div>
      ) : staff.length === 0 ? (
        <EmptyState
          title="Nema zaposlenih"
          description="Dodajte prvog zaposlenog da biste mogli da zakazujete termine."
          action={
            <Button size="sm" icon={<Plus size={12} />} onClick={openModal}>
              Dodaj zaposlenog
            </Button>
          }
        />
      ) : (
        <div className="card card-padded">
          <ul className="space-y-2">
            {staff.map(s => (
              <li
                key={s.id}
                className={`flex items-center justify-between py-3 px-4 rounded-lg bg-surface-2 ${!s.isActive ? 'opacity-60' : ''}`}
              >
                <div className="flex items-center gap-3">
                  <div className="w-10 h-10 rounded-full bg-primary-highlight flex items-center justify-center shrink-0">
                    <UserCircle size={20} className="text-primary" />
                  </div>
                  <div>
                    <p className="text-sm font-medium text-text">{s.name}</p>
                    {s.role && (
                      <p className="text-xs text-text-muted">{s.role}</p>
                    )}
                    {(s.email || s.id) && (
                      <div className="flex items-center gap-3 mt-0.5 text-xs text-text-faint">
                        {s.email && (
                          <span className="flex items-center gap-1">
                            <Mail size={10} /> {s.email}
                          </span>
                        )}
                      </div>
                    )}
                  </div>
                </div>
                <span className="text-xs text-text-faint">
                  {s.isActive ? 'Aktivan' : 'Neaktivan'}
                </span>
              </li>
            ))}
          </ul>
        </div>
      )}

      <Modal
        isOpen={modalOpen}
        onClose={() => setModalOpen(false)}
        title="Novi zaposleni"
        footer={
          <>
            <Button variant="secondary" onClick={() => setModalOpen(false)}>
              Odustani
            </Button>
            <Button
              loading={createMutation.isPending}
              icon={<Save size={13} />}
              onClick={handleSubmit}
            >
              Dodaj
            </Button>
          </>
        }
      >
        <div className="flex flex-col gap-4">
          {formError && (
            <div className="p-3 bg-error-bg border border-error/20 rounded-md text-sm text-error">
              {formError}
            </div>
          )}
          <div className="grid grid-cols-2 gap-3">
            <Input
              label="Ime"
              value={form.firstName}
              onChange={e => setForm(f => ({ ...f, firstName: e.target.value }))}
              placeholder="Ana"
            />
            <Input
              label="Prezime"
              value={form.lastName}
              onChange={e => setForm(f => ({ ...f, lastName: e.target.value }))}
              placeholder="Jovanović"
            />
          </div>
          <Input
            label="Email (opciono)"
            type="email"
            value={form.email ?? ''}
            onChange={e => setForm(f => ({ ...f, email: e.target.value }))}
            placeholder="ana@salon.rs"
          />
          <Input
            label="Telefon (opciono)"
            type="tel"
            value={form.phone ?? ''}
            onChange={e => setForm(f => ({ ...f, phone: e.target.value }))}
            placeholder="+381 6x xxx xxxx"
          />
          <Input
            label="Specijalizacija / zanimanje (opciono)"
            value={form.specialization ?? ''}
            onChange={e => setForm(f => ({ ...f, specialization: e.target.value }))}
            placeholder="npr. Frizer, Manikerka"
          />
        </div>
      </Modal>
    </div>
  );
};
