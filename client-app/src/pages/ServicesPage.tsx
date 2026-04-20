import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Plus, Clock, Save, Pencil, Trash2 } from 'lucide-react';
import { getCategories, getServices, createCategory, createService, updateService, deleteService, type UpdateServiceRequest } from '../api/services';
import type { ServiceCategory, Service, CreateServiceCategoryRequest, CreateServiceRequest } from '../types';
import { queryKeys } from '../lib/queryKeys';
import { Button } from '../components/Button';
import { Input } from '../components/Input';
import { Modal } from '../components/Modal';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { EmptyState } from '../components/EmptyState';

const CATEGORY_TYPES: Array<{ value: ServiceCategory['type']; label: string }> = [
  { value: 'Hair', label: 'Kosa' },
  { value: 'Nails', label: 'Nokti' },
  { value: 'Skin', label: 'Koža' },
  { value: 'Massage', label: 'Masaža' },
  { value: 'Makeup', label: 'Šminka' },
  { value: 'Other', label: 'Ostalo' },
];

export const ServicesPage: React.FC = () => {
  const queryClient = useQueryClient();
  const [categoryModalOpen, setCategoryModalOpen] = useState(false);
  const [categoryForm, setCategoryForm] = useState<CreateServiceCategoryRequest>({
    name: '', description: '', colorHex: '#5B3A8C', type: 'Other',
  });
  const [categoryFormError, setCategoryFormError] = useState<string | null>(null);

  const [serviceModalOpen, setServiceModalOpen] = useState(false);
  const [editingService, setEditingService] = useState<Service | null>(null);
  const [serviceForm, setServiceForm] = useState<CreateServiceRequest & { isActive?: boolean }>({
    categoryId: '', name: '', description: '', durationMinutes: 30, price: 0,
  });
  const [serviceFormError, setServiceFormError] = useState<string | null>(null);

  const { data: categories = [], isLoading: categoriesLoading } = useQuery({
    queryKey: queryKeys.serviceCategories.list(true),
    queryFn: () => getCategories({ includeInactive: true }),
  });

  const { data: services = [], isLoading: servicesLoading } = useQuery({
    queryKey: queryKeys.services.list(true),
    queryFn: () => getServices({ includeInactive: true }),
  });

  const createCategoryMutation = useMutation({
    mutationFn: createCategory,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.serviceCategories.all });
      queryClient.invalidateQueries({ queryKey: queryKeys.services.all });
      setCategoryModalOpen(false);
    },
    onError: () => setCategoryFormError('Nije moguće kreirati kategoriju.'),
  });

  const createServiceMutation = useMutation({
    mutationFn: createService,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.serviceCategories.all });
      queryClient.invalidateQueries({ queryKey: queryKeys.services.all });
      setServiceModalOpen(false);
      setEditingService(null);
    },
    onError: () => setServiceFormError('Nije moguće kreirati uslugu.'),
  });

  const updateServiceMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateServiceRequest }) => updateService(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.serviceCategories.all });
      queryClient.invalidateQueries({ queryKey: queryKeys.services.all });
      setServiceModalOpen(false);
      setEditingService(null);
    },
    onError: () => setServiceFormError('Nije moguće sačuvati uslugu.'),
  });

  const deleteServiceMutation = useMutation({
    mutationFn: deleteService,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.serviceCategories.all });
      queryClient.invalidateQueries({ queryKey: queryKeys.services.all });
    },
  });

  const openCategoryModal = () => {
    setCategoryForm({ name: '', description: '', colorHex: '#5B3A8C', type: 'Other' });
    setCategoryFormError(null);
    setCategoryModalOpen(true);
  };

  const handleCreateCategory = () => {
    if (!categoryForm.name.trim()) {
      setCategoryFormError('Naziv kategorije je obavezan.');
      return;
    }
    setCategoryFormError(null);
    createCategoryMutation.mutate(categoryForm);
  };

  const openServiceModal = (categoryId: string, service?: Service) => {
    if (service) {
      setEditingService(service);
      setServiceForm({
        categoryId: service.categoryId ?? categoryId,
        name: service.name,
        description: service.description ?? '',
        durationMinutes: service.duration,
        price: service.price,
        isActive: service.isActive,
      });
    } else {
      setEditingService(null);
      setServiceForm({
        categoryId,
        name: '',
        description: '',
        durationMinutes: 30,
        price: 0,
      });
    }
    setServiceFormError(null);
    setServiceModalOpen(true);
  };

  const handleSaveService = () => {
    if (!serviceForm.categoryId || !serviceForm.name.trim()) {
      setServiceFormError('Kategorija i naziv usluge su obavezni.');
      return;
    }
    if (serviceForm.durationMinutes <= 0 || serviceForm.price < 0) {
      setServiceFormError('Trajanje i cena moraju biti pozitivni.');
      return;
    }
    setServiceFormError(null);
    if (editingService) {
      updateServiceMutation.mutate({
        id: editingService.id,
        data: {
          categoryId: serviceForm.categoryId,
          name: serviceForm.name.trim(),
          description: serviceForm.description?.trim() || undefined,
          durationMinutes: serviceForm.durationMinutes,
          price: serviceForm.price,
          isActive: serviceForm.isActive ?? true,
        },
      });
    } else {
      createServiceMutation.mutate(serviceForm);
    }
  };

  const handleDeleteService = (service: Service) => {
    if (!window.confirm(`Obrisati uslugu „${service.name}"? Usluga će biti uklonjena iz ponude.`)) return;
    deleteServiceMutation.mutate(service.id);
  };

  const isLoading = categoriesLoading || servicesLoading;
  const error: string | null = null;

  const servicesByCategory = categories.map(cat => ({
    category: cat,
    items: services.filter(s => s.categoryId === cat.id),
  }));

  return (
    <div className="container-main py-6 space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-xl font-semibold text-display text-text">Usluge</h1>
          <p className="text-xs text-text-faint mt-0.5">
            Kategorije i usluge vašeg salona – svaki salon upravlja svojim katalogom
          </p>
        </div>
        <Button size="sm" icon={<Plus size={13} />} onClick={openCategoryModal}>
          Nova kategorija
        </Button>
      </div>

      {error && (
        <div className="p-3 bg-error-bg border border-error/20 rounded-lg text-sm text-error">{error}</div>
      )}

      {isLoading ? (
        <div className="flex justify-center py-16">
          <LoadingSpinner />
        </div>
      ) : categories.length === 0 ? (
        <EmptyState
          title="Nema kategorija"
          description="Dodajte prvu kategoriju (npr. Šišanje, Manikir), zatim u nju usluge."
          action={
            <Button size="sm" icon={<Plus size={12} />} onClick={openCategoryModal}>
              Nova kategorija
            </Button>
          }
        />
      ) : (
        <div className="space-y-6">
          {servicesByCategory.map(({ category, items }) => (
            <div key={category.id} className="card card-padded">
              <div className="flex items-center justify-between mb-4">
                <div className="flex items-center gap-2">
                  {category.colorHex && (
                    <span
                      className="w-3 h-3 rounded-full shrink-0"
                      style={{ backgroundColor: category.colorHex }}
                    />
                  )}
                  <h2 className="text-sm font-semibold text-text">{category.name}</h2>
                  <span className="text-xs text-text-faint">({items.length} usluga)</span>
                </div>
                <Button
                  variant="secondary"
                  size="sm"
                  icon={<Plus size={12} />}
                  onClick={() => openServiceModal(category.id)}
                >
                  Dodaj uslugu
                </Button>
              </div>
              {items.length === 0 ? (
                <p className="text-sm text-text-muted py-2">Nema usluga. Kliknite „Dodaj uslugu”.</p>
              ) : (
                <ul className="space-y-2">
                  {items.map(s => (
                    <li
                      key={s.id}
                      className={`flex items-center justify-between py-2 px-3 rounded-lg bg-surface-2 ${!s.isActive ? 'opacity-60' : ''}`}
                    >
                      <div>
                        <p className="text-sm font-medium text-text">{s.name}</p>
                        {s.description && (
                          <p className="text-xs text-text-muted truncate max-w-md">{s.description}</p>
                        )}
                      </div>
                      <div className="flex items-center gap-3 text-sm text-text-faint shrink-0">
                        <span className="flex items-center gap-1">
                          <Clock size={12} /> {s.duration} min
                        </span>
                        <span className="font-medium text-text">{new Intl.NumberFormat('sr-Latn-RS', { style: 'currency', currency: 'RSD', minimumFractionDigits: 0 }).format(Number(s.price))}</span>
                        <div className="flex items-center gap-1">
                          <button
                            type="button"
                            onClick={() => openServiceModal(category.id, s)}
                            className="p-1.5 rounded-md text-text-muted hover:bg-surface hover:text-primary transition-colors"
                            title="Izmeni"
                          >
                            <Pencil size={14} />
                          </button>
                          <button
                            type="button"
                            onClick={() => handleDeleteService(s)}
                            disabled={deleteServiceMutation.isPending}
                            className="p-1.5 rounded-md text-text-muted hover:bg-error-bg hover:text-error transition-colors disabled:opacity-50"
                            title="Obriši"
                          >
                            <Trash2 size={14} />
                          </button>
                        </div>
                      </div>
                    </li>
                  ))}
                </ul>
              )}
            </div>
          ))}
        </div>
      )}

      {/* New category modal */}
      <Modal
        isOpen={categoryModalOpen}
        onClose={() => setCategoryModalOpen(false)}
        title="Nova kategorija"
        footer={
          <>
            <Button variant="secondary" onClick={() => setCategoryModalOpen(false)}>Odustani</Button>
            <Button loading={createCategoryMutation.isPending} icon={<Save size={13} />} onClick={handleCreateCategory}>
              Sačuvaj
            </Button>
          </>
        }
      >
        <div className="flex flex-col gap-4">
          {categoryFormError && (
            <div className="p-3 bg-error-bg border border-error/20 rounded-md text-sm text-error">
              {categoryFormError}
            </div>
          )}
          <Input
            label="Naziv"
            value={categoryForm.name}
            onChange={e => setCategoryForm(f => ({ ...f, name: e.target.value }))}
            placeholder="npr. Manikir i pedikir"
          />
          <Input
            label="Opis (opciono)"
            value={categoryForm.description ?? ''}
            onChange={e => setCategoryForm(f => ({ ...f, description: e.target.value || undefined }))}
            placeholder="Kratak opis kategorije"
          />
          <div>
            <label className="block text-sm font-medium text-text mb-1">Tip</label>
            <select
              value={categoryForm.type}
              onChange={e => setCategoryForm(f => ({ ...f, type: e.target.value as ServiceCategory['type'] }))}
              className="w-full bg-surface border border-border rounded-md px-3 py-2 text-sm text-text focus:outline-none focus:ring-2 focus:ring-primary/30"
            >
              {CATEGORY_TYPES.map(t => (
                <option key={t.value} value={t.value}>{t.label}</option>
              ))}
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium text-text mb-1">Boja kategorije</label>
            <div className="flex items-center gap-2">
              <input
                type="color"
                value={categoryForm.colorHex ?? '#5B3A8C'}
                onChange={e => setCategoryForm(f => ({ ...f, colorHex: e.target.value }))}
                className="h-10 w-12 cursor-pointer rounded border border-border bg-surface p-1"
                aria-label="Izaberite boju kategorije"
              />
              <input
                type="text"
                value={categoryForm.colorHex ?? '#5B3A8C'}
                onChange={e => setCategoryForm(f => ({ ...f, colorHex: e.target.value || '#5B3A8C' }))}
                className="w-full bg-surface border border-border rounded-md px-3 py-2 text-sm text-text focus:outline-none focus:ring-2 focus:ring-primary/30"
                placeholder="#5B3A8C"
              />
            </div>
          </div>
        </div>
      </Modal>

      {/* New/Edit service modal */}
      <Modal
        isOpen={serviceModalOpen}
        onClose={() => { setServiceModalOpen(false); setEditingService(null); }}
        title={editingService ? 'Izmena usluge' : 'Nova usluga'}
        footer={
          <>
            <Button variant="secondary" onClick={() => { setServiceModalOpen(false); setEditingService(null); }}>Odustani</Button>
            <Button
              loading={createServiceMutation.isPending || updateServiceMutation.isPending}
              icon={<Save size={13} />}
              onClick={handleSaveService}
            >
              Sačuvaj
            </Button>
          </>
        }
      >
        <div className="flex flex-col gap-4">
          {serviceFormError && (
            <div className="p-3 bg-error-bg border border-error/20 rounded-md text-sm text-error">
              {serviceFormError}
            </div>
          )}
          <div>
            <label className="block text-sm font-medium text-text mb-1">Kategorija</label>
            <select
              value={serviceForm.categoryId}
              onChange={e => setServiceForm(f => ({ ...f, categoryId: e.target.value }))}
              className="w-full bg-surface border border-border rounded-md px-3 py-2 text-sm text-text focus:outline-none focus:ring-2 focus:ring-primary/30"
            >
              <option value="">Izaberite kategoriju</option>
              {categories.map(c => (
                <option key={c.id} value={c.id}>{c.name}</option>
              ))}
            </select>
          </div>
          <Input
            label="Naziv usluge"
            value={serviceForm.name}
            onChange={e => setServiceForm(f => ({ ...f, name: e.target.value }))}
            placeholder="npr. Gel manikir"
          />
          <Input
            label="Opis (opciono)"
            value={serviceForm.description ?? ''}
            onChange={e => setServiceForm(f => ({ ...f, description: e.target.value || undefined }))}
            placeholder="Kratak opis"
          />
          <div className="grid grid-cols-2 gap-3">
            <Input
              label="Trajanje (min)"
              type="number"
              min={1}
              value={String(serviceForm.durationMinutes)}
              onChange={e => setServiceForm(f => ({ ...f, durationMinutes: parseInt(e.target.value, 10) || 0 }))}
            />
            <Input
              label="Cena (RSD)"
              type="number"
              min={0}
              step={0.01}
              value={String(serviceForm.price)}
              onChange={e => setServiceForm(f => ({ ...f, price: parseFloat(e.target.value) || 0 }))}
            />
          </div>
          {editingService && (
            <label className="flex items-center gap-2 cursor-pointer">
              <input
                type="checkbox"
                checked={serviceForm.isActive ?? true}
                onChange={e => setServiceForm(f => ({ ...f, isActive: e.target.checked }))}
                className="rounded border-border text-primary focus:ring-primary/30"
              />
              <span className="text-sm text-text">Aktivna usluga (prikazuje se u ponudi)</span>
            </label>
          )}
        </div>
      </Modal>
    </div>
  );
};
