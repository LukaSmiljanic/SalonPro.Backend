import React, { useState, useEffect, useCallback, useRef } from 'react';
import { Search, Plus, Phone, Mail, Calendar, X, Save, Trash2, UserCircle2 } from 'lucide-react';
import { getClients, createClient, updateClient, deleteClient } from '../api/clients';
import type { Client, CreateClientRequest } from '../types';
import { ClientListItem } from '../components/ClientListItem';
import { Button } from '../components/Button';
import { Input } from '../components/Input';
import { Modal } from '../components/Modal';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { EmptyState } from '../components/EmptyState';
import { format, parseISO } from 'date-fns';

const PAGE_SIZE = 20;

export const ClientsPage: React.FC = () => {
  const [clients, setClients] = useState<Client[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Selected client panel
  const [selectedClient, setSelectedClient] = useState<Client | null>(null);

  // New/Edit client modal
  const [modalOpen, setModalOpen] = useState(false);
  const [editingClient, setEditingClient] = useState<Client | null>(null);
  const [form, setForm] = useState<CreateClientRequest>({
    firstName: '', lastName: '', email: '', phone: '', notes: '',
  });
  const [formLoading, setFormLoading] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  // Debounce search
  const searchTimer = useRef<ReturnType<typeof setTimeout> | null>(null);

  const fetchClients = useCallback(async (searchTerm: string, currentPage: number) => {
    setIsLoading(true);
    try {
      setError(null);
      const response = await getClients({
        search: searchTerm || undefined,
        page: currentPage,
        pageSize: PAGE_SIZE,
      });
      setClients(response.items);
      setTotal(response.total);
    } catch {
      setError('Failed to load clients.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchClients(search, page);
  }, [fetchClients, page]); // intentionally not including search - handled by debounce

  const handleSearchChange = (value: string) => {
    setSearch(value);
    if (searchTimer.current) clearTimeout(searchTimer.current);
    searchTimer.current = setTimeout(() => {
      setPage(1);
      fetchClients(value, 1);
    }, 350);
  };

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

  const handleSubmit = async () => {
    if (!form.firstName.trim() || !form.lastName.trim()) {
      setFormError('First and last name are required.');
      return;
    }
    setFormLoading(true);
    setFormError(null);
    try {
      if (editingClient) {
        const updated = await updateClient(editingClient.id, form);
        setClients(prev => prev.map(c => c.id === updated.id ? updated : c));
        if (selectedClient?.id === updated.id) setSelectedClient(updated);
      } else {
        const created = await createClient(form);
        setClients(prev => [created, ...prev]);
        setTotal(t => t + 1);
      }
      setModalOpen(false);
    } catch {
      setFormError('Failed to save client. Please try again.');
    } finally {
      setFormLoading(false);
    }
  };

  const handleDelete = async (client: Client) => {
    if (!window.confirm(`Delete ${client.firstName} ${client.lastName}? This cannot be undone.`)) return;
    try {
      await deleteClient(client.id);
      setClients(prev => prev.filter(c => c.id !== client.id));
      setTotal(t => t - 1);
      if (selectedClient?.id === client.id) setSelectedClient(null);
    } catch {
      alert('Failed to delete client.');
    }
  };

  const totalPages = Math.ceil(total / PAGE_SIZE);

  return (
    <div className="container-main py-6">
      <div className="flex flex-col lg:flex-row gap-6">

        {/* Left: client list */}
        <div className="flex-1 min-w-0">

          {/* Header */}
          <div className="flex items-center justify-between mb-4">
            <div>
              <h1 className="text-xl font-semibold text-display text-text">Clients</h1>
              <p className="text-xs text-text-faint mt-0.5">{total} total</p>
            </div>
            <Button size="sm" icon={<Plus size={13} />} onClick={openNewModal}>
              Add Client
            </Button>
          </div>

          {/* Search */}
          <div className="mb-4">
            <Input
              placeholder="Search by name, email or phone…"
              value={search}
              onChange={e => handleSearchChange(e.target.value)}
              leftIcon={<Search size={14} />}
              rightIcon={
                search ? (
                  <button
                    type="button"
                    className="pointer-events-auto text-text-faint hover:text-text-muted"
                    onClick={() => handleSearchChange('')}
                  >
                    <X size={13} />
                  </button>
                ) : undefined
              }
            />
          </div>

          {/* Error */}
          {error && (
            <div className="p-3 bg-error-bg border border-error/20 rounded-lg text-sm text-error mb-4">{error}</div>
          )}

          {/* List */}
          {isLoading ? (
            <div className="flex justify-center py-16">
              <LoadingSpinner />
            </div>
          ) : clients.length === 0 ? (
            <EmptyState
              title={search ? 'No clients found' : 'No clients yet'}
              description={search ? 'Try a different search term.' : 'Add your first client to get started.'}
              action={!search ? <Button size="sm" icon={<Plus size={12} />} onClick={openNewModal}>Add Client</Button> : undefined}
            />
          ) : (
            <div className="space-y-1">
              {clients.map(client => (
                <ClientListItem
                  key={client.id}
                  client={client}
                  selected={selectedClient?.id === client.id}
                  onClick={setSelectedClient}
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
                Previous
              </Button>
              <span className="text-sm text-text-muted">{page} / {totalPages}</span>
              <Button
                variant="secondary"
                size="sm"
                disabled={page === totalPages}
                onClick={() => setPage(p => p + 1)}
              >
                Next
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
                    {selectedClient.firstName[0]}{selectedClient.lastName[0]}
                  </span>
                </div>
                <div>
                  <p className="font-semibold text-text">
                    {selectedClient.firstName} {selectedClient.lastName}
                  </p>
                  <p className="text-xs text-text-faint">Client since {format(parseISO(selectedClient.createdAt), 'MMM yyyy')}</p>
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
                    Last visit: {format(parseISO(selectedClient.lastVisit), 'MMM d, yyyy')}
                  </span>
                </div>
              )}
            </div>

            {/* Stats */}
            <div className="grid grid-cols-2 gap-3 mb-4">
              <div className="bg-surface-2 rounded-lg p-3 text-center">
                <p className="text-lg font-semibold text-text">{selectedClient.totalVisits}</p>
                <p className="text-xs text-text-faint">Visits</p>
              </div>
              <div className="bg-surface-2 rounded-lg p-3 text-center">
                <p className="text-lg font-semibold text-text">€{selectedClient.totalSpent.toFixed(0)}</p>
                <p className="text-xs text-text-faint">Spent</p>
              </div>
            </div>

            {/* Notes */}
            {selectedClient.notes && (
              <div className="mb-4">
                <p className="text-xs text-text-faint mb-1">Notes</p>
                <p className="text-sm text-text">{selectedClient.notes}</p>
              </div>
            )}

            {/* Actions */}
            <div className="flex gap-2">
              <Button
                variant="secondary"
                size="sm"
                className="flex-1"
                icon={<UserCircle2 size={13} />}
                onClick={() => openEditModal(selectedClient)}
              >
                Edit
              </Button>
              <Button
                variant="danger"
                size="sm"
                icon={<Trash2 size={13} />}
                onClick={() => handleDelete(selectedClient)}
              >
                Delete
              </Button>
            </div>
          </div>
        )}
      </div>

      {/* New/Edit Client Modal */}
      <Modal
        isOpen={modalOpen}
        onClose={() => setModalOpen(false)}
        title={editingClient ? 'Edit Client' : 'New Client'}
        footer={
          <>
            <Button variant="secondary" onClick={() => setModalOpen(false)}>Cancel</Button>
            <Button loading={formLoading} icon={<Save size={13} />} onClick={handleSubmit}>
              {editingClient ? 'Save Changes' : 'Add Client'}
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
              label="First Name"
              value={form.firstName}
              onChange={e => setForm(f => ({ ...f, firstName: e.target.value }))}
              placeholder="Jane"
            />
            <Input
              label="Last Name"
              value={form.lastName}
              onChange={e => setForm(f => ({ ...f, lastName: e.target.value }))}
              placeholder="Smith"
            />
          </div>
          <Input
            label="Email"
            type="email"
            value={form.email ?? ''}
            onChange={e => setForm(f => ({ ...f, email: e.target.value }))}
            placeholder="jane@example.com"
          />
          <Input
            label="Phone"
            type="tel"
            value={form.phone ?? ''}
            onChange={e => setForm(f => ({ ...f, phone: e.target.value }))}
            placeholder="+1 555 000 0000"
          />
          <div className="flex flex-col gap-1">
            <label className="text-sm font-medium text-text">Notes</label>
            <textarea
              value={form.notes ?? ''}
              onChange={e => setForm(f => ({ ...f, notes: e.target.value }))}
              placeholder="Any additional notes about this client…"
              rows={3}
              className="w-full bg-surface border border-border rounded-md px-3 py-2 text-sm text-text placeholder:text-text-faint resize-none focus:outline-none focus:ring-2 focus:ring-primary/30 focus:border-primary transition-interactive"
            />
          </div>
        </div>
      </Modal>
    </div>
  );
};
