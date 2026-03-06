import React from 'react';
import { Phone, Mail, Calendar, TrendingUp } from 'lucide-react';
import type { Client } from '../types';
import { format, parseISO } from 'date-fns';

interface ClientListItemProps {
  client: Client;
  onClick?: (client: Client) => void;
  selected?: boolean;
}

export const ClientListItem: React.FC<ClientListItemProps> = ({ client, onClick, selected }) => {
  const first = (client.firstName || '').trim();
  const last = (client.lastName || '').trim();
  const fullName = [first, last].filter(Boolean).join(' ') || '—';
  const initials = (first[0] ?? '') + (last[0] ?? '');
  const initialsDisplay = initials.toUpperCase() || '?';
  const lastVisitStr = client.lastVisit
    ? format(parseISO(client.lastVisit), 'MMM d, yyyy')
    : 'Never';

  return (
    <div
      className={`flex items-start gap-4 p-4 rounded-lg cursor-pointer transition-interactive
        ${selected ? 'bg-primary-highlight border border-primary/20' : 'hover:bg-surface-2'}`}
      onClick={() => onClick?.(client)}
    >
      {/* Avatar */}
      <div className="w-10 h-10 rounded-full bg-primary-highlight flex items-center justify-center shrink-0">
        <span className="text-sm font-semibold text-primary">{initialsDisplay}</span>
      </div>

      {/* Info */}
      <div className="flex-1 min-w-0">
        <p className="font-semibold text-text truncate">{fullName}</p>
        <div className="mt-1 flex flex-wrap gap-x-4 gap-y-1">
          {client.phone && (
            <span className="flex items-center gap-1 text-xs text-text-muted">
              <Phone size={11} />{client.phone}
            </span>
          )}
          {client.email && (
            <span className="flex items-center gap-1 text-xs text-text-muted">
              <Mail size={11} />{client.email}
            </span>
          )}
        </div>
      </div>

      {/* Stats */}
      <div className="flex flex-col items-end gap-1 shrink-0">
        <span className="flex items-center gap-1 text-xs text-text-muted">
          <Calendar size={11} />{lastVisitStr}
        </span>
        <span className="flex items-center gap-1 text-xs font-medium text-success">
          <TrendingUp size={11} />{client.totalVisits} visits
        </span>
      </div>
    </div>
  );
};
