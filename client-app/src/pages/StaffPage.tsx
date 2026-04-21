import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Plus, Save, UserCircle, Mail, Phone, Pencil, Trash2, ShieldCheck, ShieldOff } from 'lucide-react';
import { getStaff, createStaff, updateStaff, deleteStaff } from '../api/staff';
import type { UpdateStaffRequest } from '../api/staff';
import type { StaffMember, CreateStaffRequest } from '../types';
import { queryKeys } from '../lib/queryKeys';
import { useAuth } from '../hooks/useAuth';
import { Button } from '../components/Button';
import { Input } from '../components/Input';
import { Modal } from '../components/Modal';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { EmptyState } from '../components/EmptyState';

type StaffForm = {
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  specialization: string;
  isActive: boolean;
  colorIndex: number;
};

const emptyForm: StaffForm = {
  firstName: '',
  lastName: '',
  email: '',
  phone: '',
  specialization: '',
  isActive: true,
  colorIndex: 0,
};

export const StaffPage: React.FC = () => {
  const { plan, features } = useAuth();
  const queryClient = useQueryClient();
  const [modalOpen, setModalOpen] = useState(false);
  const [editingStaff, setEditingStaff] = useState<StaffMember | null>(null);
  const [form, setForm] = useState<StaffForm>(emptyForm);
  const [formError, setFormError] = useState<string | null>(null);
  const [deleteMessage, setDeleteMessage] = useState<string | null>(null);

  const { data: staff = [], isLoading } = useQuery({
    queryKey: queryKeys.staff.list(true),
    queryFn: () => getStaff({ includeInactive: true }),
  });

  const createMutation = useMutation({
    mutationFn: createStaff,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.staff.all });
      queryClient.invalidateQueries({ queryKey: queryKeys.appointments.all });
      closeModal();
    },
    onError: () => setFormError('Nije moguće dodati zaposlenog. Pokušajte ponovo.'),
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateStaffRequest }) => updateStaff(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.staff.all });
      queryClient.invalidateQueries({ queryKey: queryKeys.appointments.all });
      closeModal();
    },
    onError: () => setFormError('Nije moguće sačuvati izmene. Pokušajte ponovo.'),
  });

  const deleteMutation = useMutation({
    mutationFn: deleteStaff,
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.staff.all });
      queryClient.invalidateQueries({ queryKey: queryKeys.appointments.all });
      if (result.wasSoftDeleted) {
        setDeleteMessage(result.message);
      }
    },
  });

  const closeModal = () => {
    setModalOpen(false);
    setEditingStaff(null);
    setForm(emptyForm);
    setFormError(null);
  };

  const openNewModal = () => {
    setEditingStaff(null);
    setForm(emptyForm);
    setFormError(null);
    setModalOpen(true);
  };

  const openEditModal = (s: StaffMember) => {
    setEditingStaff(s);
    setForm({
      firstName: s.firstName,
      lastName: s.lastName,
      email: s.email ?? '',
      phone: s.phone ?? '',
      specialization: s.role ?? '',
      isActive: s.isActive,
      colorIndex: s.colorIndex ?? 0,
    });
    setFormError(null);
    setModalOpen(true);
  };

  const handleSubmit = () => {
    if (!form.firstName.trim() || !form.lastName.trim()) {
      setFormError('Ime i prezime su obavezni.');
      return;
    }
    setFormError(null);
    if (editingStaff) {
      updateMutation.mutate({
        id: editingStaff.id,
        data: {
          firstName: form.firstName,
          lastName: form.lastName,
          email: form.email || undefined,
          phone: form.phone || undefined,
          specialization: form.specialization || undefined,
          isActive: form.isActive,
          colorIndex: form.colorIndex,
        },
      });
    } else {
      createMutation.mutate({
        firstName: form.firstName,
        lastName: form.lastName,
        email: form.email || undefined,
        phone: form.phone || undefined,
        specialization: form.specialization || undefined,
        colorIndex: form.colorIndex,
      });
    }
  };

  const handleDelete = (s: StaffMember) => {
    const msg = s.totalAppointments > 0
      ? `${s.name} ima ${s.totalAppointments} termina u istoriji. Zaposleni će biti deaktiviran (podaci se čuvaju). Nastaviti?`
      : `Obrisati ${s.name}? Ova akcija se ne može poništiti.`;
    if (!window.confirm(msg)) return;
    deleteMutation.mutate(s.id);
  };

  const formLoading = createMutation.isPending || updateMutation.isPending;
  const activeStaffCount = staff.filter(s => s.isActive).length;
  const staffLimitReached = activeStaffCount >= features.maxStaffMembers;

  return (
    <div className="container-main py-6 space-y-6">
      <div className="flex items-center justify-between gap-3">
        <div className="min-w-0">
          <h1 className="text-xl font-semibold text-display text-text">Zaposleni</h1>
          <p className="text-xs text-text-faint mt-0.5 hidden sm:block">
            Osoblje salona — dodajte, izmenite ili uklonite zaposlene
          </p>
        </div>
        <Button
          size="sm"
          icon={<Plus size={13} />}
          onClick={openNewModal}
          className="shrink-0"
          disabled={staffLimitReached}
          title={staffLimitReached ? `Dostignut limit zaposlenih za ${plan} paket` : undefined}
        >
          <span className="hidden sm:inline">Dodaj zaposlenog</span>
          <span className="sm:hidden">Dodaj</span>
        </Button>
      </div>

      {staffLimitReached && (
        <div className="p-3 bg-warning-bg border border-warning/20 rounded-lg text-sm text-warning">
          Paket <strong>{plan}</strong> dozvoljava najviše <strong>{features.maxStaffMembers}</strong> aktivnih zaposlenih.
          Nadogradite paket za više članova osoblja.
        </div>
      )}

      {/* Soft-delete info message */}
      {deleteMessage && (
        <div className="p-3 bg-warning-bg border border-warning/20 rounded-lg text-sm text-warning flex items-center justify-between">
          <span>{deleteMessage}</span>
          <button
            className="text-xs underline ml-4"
            onClick={() => setDeleteMessage(null)}
          >
            Zatvori
          </button>
        </div>
      )}

      {isLoading ? (
        <div className="flex justify-center py-16">
          <LoadingSpinner />
        </div>
      ) : staff.length === 0 ? (
        <EmptyState
          title="Nema zaposlenih"
          description="Dodajte prvog zaposlenog da biste mogli da zakazujete termine."
          action={
            <Button size="sm" icon={<Plus size={12} />} onClick={openNewModal}>
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
                className={`flex items-center justify-between py-3 px-4 rounded-lg bg-surface-2 transition-all duration-200 ${!s.isActive ? 'opacity-50' : ''}`}
              >
                <div className="flex items-center gap-3">
                  <div className="w-10 h-10 rounded-full bg-primary-highlight flex items-center justify-center shrink-0">
                    <UserCircle size={20} className="text-primary" />
                  </div>
                  <div>
                    <div className="flex items-center gap-2">
                      <p className="text-sm font-medium text-text">{s.name}</p>
                      {!s.isActive && (
                        <span className="text-[10px] px-1.5 py-0.5 rounded bg-text-faint/10 text-text-faint font-medium">
                          Neaktivan
                        </span>
                      )}
                    </div>
                    {s.role && (
                      <p className="text-xs text-text-muted">{s.role}</p>
                    )}
                    <div className="flex flex-wrap items-center gap-x-3 gap-y-0.5 mt-0.5 text-xs text-text-faint">
                      {s.email && (
                        <span className="flex items-center gap-1 truncate max-w-[180px] sm:max-w-none">
                          <Mail size={10} className="shrink-0" /> <span className="truncate">{s.email}</span>
                        </span>
                      )}
                      {s.phone && (
                        <span className="flex items-center gap-1">
                          <Phone size={10} className="shrink-0" /> {s.phone}
                        </span>
                      )}
                    </div>
                  </div>
                </div>
                <div className="flex items-center gap-1">
                  <button
                    onClick={() => openEditModal(s)}
                    className="p-3 md:p-2 rounded-lg hover:bg-surface active:bg-surface-offset text-text-muted hover:text-text transition-all duration-200"
                    title="Izmeni"
                  >
                    <Pencil size={16} className="md:w-3.5 md:h-3.5" />
                  </button>
                  <button
                    onClick={() => handleDelete(s)}
                    className="p-3 md:p-2 rounded-lg hover:bg-error-bg active:bg-error-bg text-text-muted hover:text-error transition-all duration-200"
                    title={s.totalAppointments > 0 ? 'Deaktiviraj' : 'Obriši'}
                    disabled={deleteMutation.isPending}
                  >
                    <Trash2 size={16} className="md:w-3.5 md:h-3.5" />
                  </button>
                </div>
              </li>
            ))}
          </ul>
        </div>
      )}

      <Modal
        isOpen={modalOpen}
        onClose={closeModal}
        title={editingStaff ? 'Izmeni zaposlenog' : 'Novi zaposleni'}
        footer={
          <>
            <Button variant="secondary" onClick={closeModal}>
              Odustani
            </Button>
            <Button
              loading={formLoading}
              icon={<Save size={13} />}
              onClick={handleSubmit}
              disabled={!editingStaff && staffLimitReached}
            >
              {editingStaff ? 'Sačuvaj' : 'Dodaj'}
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
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
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
            value={form.email}
            onChange={e => setForm(f => ({ ...f, email: e.target.value }))}
            placeholder="ana@salon.rs"
          />
          <Input
            label="Telefon (opciono)"
            type="tel"
            value={form.phone}
            onChange={e => setForm(f => ({ ...f, phone: e.target.value }))}
            placeholder="+381 6x xxx xxxx"
          />
          <Input
            label="Specijalizacija / zanimanje (opciono)"
            value={form.specialization}
            onChange={e => setForm(f => ({ ...f, specialization: e.target.value }))}
            placeholder="npr. Frizer, Manikerka"
          />
          {/* Active toggle - only show when editing */}
          {editingStaff && (
            <div className="flex items-center justify-between py-2 px-3 bg-surface-2 rounded-lg">
              <div className="flex items-center gap-2">
                {form.isActive ? (
                  <ShieldCheck size={16} className="text-success" />
                ) : (
                  <ShieldOff size={16} className="text-text-faint" />
                )}
                <span className="text-sm text-text">
                  {form.isActive ? 'Aktivan' : 'Neaktivan'}
                </span>
              </div>
              <button
                type="button"
                onClick={() => setForm(f => ({ ...f, isActive: !f.isActive }))}
                className={`relative w-10 h-5 rounded-full transition-colors duration-200 ${
                  form.isActive ? 'bg-primary' : 'bg-border'
                }`}
              >
                <span
                  className={`absolute top-0.5 left-0.5 w-4 h-4 rounded-full bg-white shadow-sm transition-transform duration-200 ${
                    form.isActive ? 'translate-x-5' : 'translate-x-0'
                  }`}
                />
              </button>
            </div>
          )}
        </div>
      </Modal>
    </div>
  );
};
