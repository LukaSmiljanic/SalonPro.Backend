import React, { useState, useEffect, useRef } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useSearchParams } from 'react-router-dom';
import { Search, Plus, Phone, Mail, Calendar, X, Save, Trash2, UserCircle2, Award, Star, Trophy, Crown } from 'lucide-react';
import { getClients, createClient, updateClient, deleteClient, getClient } from '../api/clients';
import type { Client, CreateClientRequest, LoyaltyTier } from '../types';
import { queryKeys } from '../lib/queryKeys';
import { ClientListItem } from '../components/ClientListItem';
import { Button } from '../components/Button';
import { Input } from '../components/Input';
import { Modal } from '../components/Modal';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { EmptyState } from '../components/EmptyState';
import { ClientInsightsPanel } from '../components/ClientInsightsPanel';
import { format, parseISO } from 'date-fns';

const PAGE_SIZE = 20;

const loyaltyColors: Record<string, { bg: string; border: string; text: string }> = {
  Bronze: { bg: '#CD7F3220', border: '#CD7F3240', text: '#CD7F32' },
  Silver: { bg: '#C0C0C020', border: '#C0C0C040', text: '#808080' },
  Gold: { bg: '#FFD70020', border: '#FFD70040', text: '#B8860B' },
  Platinum: { bg: '#5B3A8C15', border: '#5B3A8C30', text: '#5B3A8C' },
};

const loyaltyLabels: Record<string, string> = {
  Bronze: 'Bronze nivo',
  Silver: 'Silver nivo',
  Gold: 'Gold nivo',
  Platinum: 'Platinum nivo',
};

function getLoyaltyIcon(tier: LoyaltyTier) {
  const size = 15;
  switch (tier) {
    case 'Bronze': return <Award size={size} color="#CD7F32" />;
    case 'Silver': return <Star size={size} color="#808080" />;
    case 'Gold': return <Trophy size={size} color="#B8860B" />;
    case 'Platinum': return <Crown size={size} color="#5B3A8C" />;
    default: return null;
  }
}

export const ClientsPage: React.FC = () => {
  const queryClient = useQueryClient();
  const [searchParams, setSearchParams] = useSearchParams();
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const searchTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const highlightHandled = useRef(false);

  const [selectedClient, setSelectedClient] = useState<Client | null>(null);

  // Auto-select client if navigated with ?highlight=clientId
  useEffect(() => {
    const highlightId = searchParams.get('highlight');
    if (highlightId && !highlightHandled.current) {
      highlightHandled.current = true;
      getClient(highlightId)
        .then((client) => setSelectedClient(client))
        .catch(() => { /* client not found, ignore */ });
      // Clean up the URL param
      searchParams.delete('highlight');
      setSearchParams(searchParams, { replace: true });
    }
  }, [searchParams, setSearchParams]);
  const [modalOpen, setModalOpen] = useState(false);
  const [editingClient, setEditingClient] = useState<Client | null>(null);
  const [form, setForm] = useState<CreateClientRequest>({
    firstName: '', lastName: '', email: '', phone: '', notes: '',
  });
  const [formError, setFormError] = useState<string | null>(null);

  useEffect(() => {
    if (searchTimerRef.current) clearTimeout(searchTimerRef.current);
    searchTimerRef.current = setTimeout(() => {
      setDebouncedSearch(search);
      setPage(1);
    }, 350);
    return () => { if (searchTimerRef.current) clearTimeout(searchTimerRef.current); };
  }, [search]);

  const { data, isLoading, error } = useQuery({
    queryKey: queryKeys.clients.list({
      search: debouncedSearch || undefined,
      page,
      pageSize: PAGE_SIZE,
    }),
    queryFn: () => getClients({
      search: debouncedSearch || undefined,
      page,
      pageSize: PAGE_SIZE,
    }),
  });

  const clients = data?.items ?? [];
  const total = data?.total ?? 0;
  const totalPages = Math.ceil(total / PAGE_SIZE);

  const createMutation = useMutation({
    mutationFn: createClient,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.clients.all });
      setModalOpen(false);
    },
    onError: () => setFormError('Nije moguće sačuvati klijenta. Pokušajte ponovo.'),
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: Partial<CreateClientRequest> & { isVip?: boolean; tags?: string } }) => updateClient(id, data),
    onSuccess: (updated) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.clients.all });
      if (selectedClient?.id === updated.id) setSelectedClient(updated);
      setModalOpen(false);
    },
    onError: () => setFormError('Nije moguće sačuvati klijenta. Pokušajte ponovo.'),
  });

  const deleteMutation = useMutation({
    mutationFn: deleteClient,
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.clients.all });
      if (selectedClient?.id === id) setSelectedClient(null);
    },
  });

  const openNewModal = () => {
    setEditingClient(null);
    setForm({ firstName: '', lastName: '', email: '', phone: '', notes: '' });
    setFormError(null);
    setModalOpen(true);
  };

  const openEditModal = (client: Client) => {
    setEditingClient(client);
    setForm({
      firstName: client.firstName,
      lastName: client.lastName,
      email: client.email ?? '',
      phone: client.phone ?? '',
      notes: client.notes ?? '',
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
    if (editingClient) {
      updateMutation.mutate({
        id: editingClient.id,
        data: {
          ...form,
          isVip: editingClient.isVip ?? false,
          tags: editingClient.tags ?? undefined,
        },
      });
    } else {
      createMutation.mutate(form);
    }
  };

  const handleDelete = (client: Client) => {
    if (!window.confirm(`Obrisati ${client.firstName} ${client.lastName}? Ova akcija se ne može poništiti.`)) return;
    deleteMutation.mutate(client.id);
  };

  const formLoading = createMutation.isPending || updateMutation.isPending;
  const listError = error ? 'Nije moguće učitati klijente.' : null;

  return (
    <div className="container-main py-6">
      <div className="flex flex-col lg:flex-row gap-6">

        {/* Left: client list */}
        <div className="flex-1 min-w-0">

          {/* Header */}
          <div className="flex items-center justify-between mb-4">
            <div>
              <h1 className="text-xl font-semibold text-display text-text">Klijenti</h1>
              <p className="text-xs text-text-faint mt-0.5">{total} ukupno</p>
            </div>
            <Button size="sm" icon={<Plus size={13} />} onClick={openNewModal}>
              Dodaj klijenta
            </Button>
          </div>

          {/* Search */}
          <div className="mb-4">
            <Input
              placeholder="Pretraga po imenu, emailu ili telefonu…"
              value={search}
              onChange={e => setSearch(e.target.value)}
              leftIcon={<Search size={14} />}
              rightIcon={
                search ? (
                  <button
                    type="button"
                    className="pointer-events-auto text-text-faint hover:text-text-muted"
                    onClick={() => setSearch('')}
                  >
                    <X size={13} />
                  </button>
                ) : undefined
              }
            />
          </div>

          {/* Error */}
          {listError && (
            <div className="p-3 bg-error-bg border border-error/20 rounded-lg text-sm text-error mb-4">{listError}</div>
          )}

          {/* List */}
          {isLoading ? (
            <div className="flex justify-center py-16">
              <LoadingSpinner />
            </div>
          ) : clients.length === 0 ? (
            <EmptyState
              title={search ? 'Nema pronađenih klijenata' : 'Još nema klijenata'}
              description={search ? 'Pokušajte drugi pojam za pretragu.' : 'Dodajte prvog klijenta da biste počeli.'}
              action={!search ? <Button size="sm" icon={<Plus size={12} />} onClick={openNewModal}>Dodaj klijenta</Button> : undefined}
            />
          ) : (
            <div className="space-y-1">
              {clients.map(client => (
                <ClientListItem
                  key={client.id}
                  client={client}
                  selected={selectedClient?.id === client.id}
                  onClick={async (c) => {
                    try {
                      const full = await getClient(c.id);
                      setSelectedClient(full);
                    } catch {
                      setSelectedClient(c);
                    }
                  }}
                />
              ))}
            </div>
          )}

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="flex items-center justify-center gap-2 mt-6">
              <Button
                variant="secondary"
                size="sm"
                disabled={page === 1}
                onClick={() => setPage(p => p - 1)}
              >
                Prethodna
              </Button>
              <span className="text-sm text-text-muted">{page} / {totalPages}</span>
              <Button
                variant="secondary"
                size="sm"
                disabled={page === totalPages}
                onClick={() => setPage(p => p + 1)}
              >
                Sledeća
              </Button>
            </div>
          )}
        </div>

        {/* Right: client detail panel */}
        {selectedClient && (
          <div className="lg:w-80 card card-padded shrink-0 h-fit">
            <div className="flex items-start justify-between mb-4">
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 rounded-full bg-primary-highlight flex items-center justify-center">
                  <span className="text-sm font-semibold text-primary">
                    {((selectedClient.firstName?.[0] ?? '') + (selectedClient.lastName?.[0] ?? '')).toUpperCase() || '?'}
                  </span>
                </div>
                <div>
                  <p className="font-semibold text-text">
                    {selectedClient.firstName} {selectedClient.lastName}
                  </p>
                  {selectedClient.createdAt ? (
                    <p className="text-xs text-text-faint">Klijent od {format(parseISO(selectedClient.createdAt), 'MMM yyyy')}</p>
                  ) : (
                    <p className="text-xs text-text-faint">—</p>
                  )}
                </div>
              </div>
              <button
                onClick={() => setSelectedClient(null)}
                className="p-1 text-text-faint hover:text-text rounded-md hover:bg-surface-2"
              >
                <X size={15} />
              </button>
            </div>

            {/* Contact */}
            <div className="space-y-2 mb-4">
              {selectedClient.phone && (
                <div className="flex items-center gap-2 text-sm">
                  <Phone size={13} className="text-text-faint shrink-0" />
                  <span className="text-text">{selectedClient.phone}</span>
                </div>
              )}
              {selectedClient.email && (
                <div className="flex items-center gap-2 text-sm">
                  <Mail size={13} className="text-text-faint shrink-0" />
                  <span className="text-text truncate">{selectedClient.email}</span>
                </div>
              )}
              {selectedClient.lastVisit && (
                <div className="flex items-center gap-2 text-sm">
                  <Calendar size={13} className="text-text-faint shrink-0" />
                  <span className="text-text">
                    Poslednja poseta: {format(parseISO(selectedClient.lastVisit), 'd. MMM yyyy.')}
                  </span>
                </div>
              )}
            </div>

            {/* Stats */}
            <div className="grid grid-cols-2 gap-3 mb-4">
              <div className="bg-surface-2 rounded-lg p-3 text-center">
                <p className="text-lg font-semibold text-text">{selectedClient.totalVisits}</p>
                <p className="text-xs text-text-faint">Posete</p>
              </div>
              <div className="bg-surface-2 rounded-lg p-3 text-center">
                <p className="text-lg font-semibold text-text">{new Intl.NumberFormat('sr-Latn-RS', { style: 'currency', currency: 'RSD', minimumFractionDigits: 0 }).format(selectedClient.totalSpent)}</p>
                <p className="text-xs text-text-faint">Potrošeno</p>
              </div>
            </div>

            {/* Loyalty Section */}
            {selectedClient.loyalty && selectedClient.loyalty.loyaltyTier !== 'None' && (
              <div className="mb-4 p-3 rounded-lg border" style={{
                borderColor: loyaltyColors[selectedClient.loyalty.loyaltyTier]?.border || 'var(--color-border)',
                backgroundColor: loyaltyColors[selectedClient.loyalty.loyaltyTier]?.bg || 'var(--color-surface-2)',
              }}>
                <div className="flex items-center gap-2 mb-1">
                  {getLoyaltyIcon(selectedClient.loyalty.loyaltyTier)}
                  <span className="text-sm font-semibold" style={{ color: loyaltyColors[selectedClient.loyalty.loyaltyTier]?.text || 'var(--color-text)' }}>
                    {loyaltyLabels[selectedClient.loyalty.loyaltyTier] || selectedClient.loyalty.loyaltyTier}
                  </span>
                </div>
                {selectedClient.loyalty.loyaltyBenefit && (
                  <p className="text-xs text-text-muted mb-1">
                    Pogodnost: {selectedClient.loyalty.loyaltyBenefit}
                  </p>
                )}
                {selectedClient.loyalty.nextMilestone && (
                  <div className="mt-2">
                    <div className="flex items-center justify-between text-xs text-text-faint mb-1">
                      <span>Sledeći nivo: {selectedClient.loyalty.nextMilestoneBenefit}</span>
                      <span>{selectedClient.loyalty.visitsUntilNextMilestone} poseta do cilja</span>
                    </div>
                    <div className="w-full bg-surface rounded-full h-1.5">
                      <div
                        className="h-1.5 rounded-full transition-all"
                        style={{
                          width: `${Math.min(100, ((selectedClient.totalVisits / selectedClient.loyalty.nextMilestone) * 100))}%`,
                          backgroundColor: loyaltyColors[selectedClient.loyalty.loyaltyTier]?.text || '#5B3A8C',
                        }}
                      />
                    </div>
                  </div>
                )}
              </div>
            )}

            {selectedClient.loyalty && selectedClient.loyalty.loyaltyTier === 'None' && selectedClient.loyalty.nextMilestone && (
              <div className="mb-4 p-3 rounded-lg bg-surface-2 border border-border">
                <p className="text-xs text-text-muted mb-1">
                  Loyalty program: Još {selectedClient.loyalty.visitsUntilNextMilestone} poseta do pogodnosti ({selectedClient.loyalty.nextMilestoneBenefit})
                </p>
                <div className="w-full bg-surface rounded-full h-1.5">
                  <div
                    className="h-1.5 rounded-full bg-[#5B3A8C]/40 transition-all"
                    style={{ width: `${Math.min(100, ((selectedClient.totalVisits / selectedClient.loyalty.nextMilestone) * 100))}%` }}
                  />
                </div>
              </div>
            )}

            {/* Notes */}
            {selectedClient.notes && (
              <div className="mb-4">
                <p className="text-xs text-text-faint mb-1">Napomene</p>
                <p className="text-sm text-text">{selectedClient.notes}</p>
              </div>
            )}

            {/* AI Insights */}
            <ClientInsightsPanel clientId={selectedClient.id} />

            {/* Actions */}
            <div className="flex gap-2 mt-4">
              <Button
                variant="secondary"
                size="sm"
                className="flex-1"
                icon={<UserCircle2 size={13} />}
                onClick={() => openEditModal(selectedClient)}
              >
                Izmeni
              </Button>
              <Button
                variant="danger"
                size="sm"
                icon={<Trash2 size={13} />}
                onClick={() => handleDelete(selectedClient)}
              >
                Obriši
              </Button>
            </div>
          </div>
        )}
      </div>

      {/* New/Edit Client Modal */}
      <Modal
        isOpen={modalOpen}
        onClose={() => setModalOpen(false)}
        title={editingClient ? 'Izmena klijenta' : 'Novi klijent'}
        footer={
          <>
            <Button variant="secondary" onClick={() => setModalOpen(false)}>Odustani</Button>
            <Button loading={formLoading} icon={<Save size={13} />} onClick={handleSubmit}>
              {editingClient ? 'Sačuvaj izmene' : 'Dodaj klijenta'}
            </Button>
          </>
        }
      >
        <div className="flex flex-col gap-4">
          {formError && (
            <div className="p-3 bg-error-bg border border-error/20 rounded-md text-sm text-error">{formError}</div>
          )}
          <div className="grid grid-cols-2 gap-3">
            <Input
              label="Ime"
              value={form.firstName}
              onChange={e => setForm(f => ({ ...f, firstName: e.target.value }))}
              placeholder="Jelena"
            />
            <Input
              label="Prezime"
              value={form.lastName}
              onChange={e => setForm(f => ({ ...f, lastName: e.target.value }))}
              placeholder="Petrović"
            />
          </div>
          <Input
            label="Email"
            type="email"
            value={form.email ?? ''}
            onChange={e => setForm(f => ({ ...f, email: e.target.value }))}
            placeholder="jela@primer.com"
          />
          <Input
            label="Telefon"
            type="tel"
            value={form.phone ?? ''}
            onChange={e => setForm(f => ({ ...f, phone: e.target.value }))}
            placeholder="+381 6x xxx xxxx"
          />
          <div className="flex flex-col gap-1">
            <label className="text-sm font-medium text-text">Napomene</label>
            <textarea
              value={form.notes ?? ''}
              onChange={e => setForm(f => ({ ...f, notes: e.target.value }))}
              placeholder="Dodatne napomene o klijentu…"
              rows={3}
              className="w-full bg-surface border border-border rounded-md px-3 py-2 text-sm text-text placeholder:text-text-faint resize-none focus:outline-none focus:ring-2 focus:ring-primary/30 focus:border-primary transition-interactive"
            />
          </div>
        </div>
      </Modal>
    </div>
  );
};
