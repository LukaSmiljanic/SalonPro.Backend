import React from 'react';
import { useNavigate } from 'react-router-dom';
import {
  CalendarClock, UserX, TrendingUp, TrendingDown, AlertTriangle,
  Clock, Sparkles, CalendarPlus, UserCheck, type LucideIcon
} from 'lucide-react';
import type { Insight, InsightPriority, InsightType, InactiveClient } from '../types';

const iconMap: Record<string, LucideIcon> = {
  CalendarClock,
  UserX,
  TrendingUp,
  TrendingDown,
  AlertTriangle,
  Clock,
  Sparkles,
  CalendarPlus,
  UserCheck,
};

const priorityClass: Record<InsightPriority, string> = {
  Urgent: 'insight-priority-urgent',
  High: 'insight-priority-high',
  Medium: 'insight-priority-medium',
  Low: 'insight-priority-low',
};

const priorityDotClass: Record<InsightPriority, string> = {
  Urgent: 'bg-error',
  High: 'bg-warning',
  Medium: 'bg-primary',
  Low: 'bg-text-faint',
};

const priorityLabels: Record<InsightPriority, string> = {
  Urgent: 'Hitno',
  High: 'Visok',
  Medium: 'Srednji',
  Low: 'Nizak',
};

interface InsightCardProps {
  insight: Insight;
  compact?: boolean;
  inactiveClients?: InactiveClient[];
}

const insightRouteMap: Partial<Record<InsightType, string>> = {
  ScheduleGap: '/calendar',
  ClientReEngagement: '/clients',
  NoShowRisk: '/clients',
  ChurnRisk: '/clients',
  PeakHours: '/calendar',
  RebookingSuggestion: '/calendar',
  ServiceUpsell: '/services',
  ServiceHistory: '/services',
};

const formatLastVisit = (dateStr?: string): string => {
  if (!dateStr) return 'nikad';
  const date = new Date(dateStr);
  const now = new Date();
  const diffDays = Math.floor((now.getTime() - date.getTime()) / (1000 * 60 * 60 * 24));
  if (diffDays < 1) return 'danas';
  if (diffDays === 1) return 'juče';
  if (diffDays < 7) return `pre ${diffDays} dana`;
  if (diffDays < 30) return `pre ${Math.floor(diffDays / 7)} ned.`;
  return `pre ${Math.floor(diffDays / 30)} mes.`;
};

function buildInsightRoute(targetRoute: string, insight: Insight): string {
  if (targetRoute === '/clients' && insight.actionData) {
    return `${targetRoute}?clientId=${encodeURIComponent(insight.actionData)}`;
  }

  return targetRoute;
}

export const InsightCard: React.FC<InsightCardProps> = ({ insight, compact = false, inactiveClients }) => {
  const navigate = useNavigate();
  const IconComponent = iconMap[insight.icon] || Sparkles;
  const priorityCls = priorityClass[insight.priority];
  const targetRoute = insightRouteMap[insight.type];

  if (compact) {
    return (
      <div className={`flex items-start gap-2.5 p-2.5 rounded-lg border ${priorityCls}`}>
        <div className="shrink-0 mt-0.5">
          <IconComponent size={14} className="text-primary" />
        </div>
        <div className="min-w-0 flex-1">
          <p className="text-xs font-medium text-text leading-snug">{insight.title}</p>
          <p className="text-[11px] text-text-muted leading-snug mt-0.5">{insight.description}</p>
        </div>
      </div>
    );
  }

  return (
    <div className={`p-3 rounded-lg border ${priorityCls}`}>
      <div className="flex items-start gap-3">
        <div className="w-8 h-8 rounded-lg insight-icon-box flex items-center justify-center shrink-0">
          <IconComponent size={15} className="text-primary" />
        </div>
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2 mb-0.5">
            <p className="text-sm font-medium text-text leading-snug">{insight.title}</p>
            <span className="flex items-center gap-1 shrink-0">
              <span className={`w-1.5 h-1.5 rounded-full ${priorityDotClass[insight.priority]}`} />
              <span className="text-[10px] text-text-faint uppercase tracking-wider">
                {priorityLabels[insight.priority]}
              </span>
            </span>
          </div>
          <p className="text-xs text-text-muted leading-relaxed">{insight.description}</p>
          {insight.type === 'ClientReEngagement' && inactiveClients && inactiveClients.length > 0 && (
            <div className="mt-2 space-y-1">
              {inactiveClients.slice(0, 5).map((client) => (
                <button
                  key={client.id}
                  type="button"
                  onClick={() => navigate(`/clients?clientId=${encodeURIComponent(client.id)}`)}
                  className="insight-client-row flex items-center justify-between w-full text-left px-2 py-1.5 rounded-md transition-colors group"
                >
                  <span className="text-xs font-medium text-primary group-hover:text-primary-hover">
                    {client.fullName}
                  </span>
                  <span className="text-[10px] text-text-faint">
                    {formatLastVisit(client.lastVisit)}
                  </span>
                </button>
              ))}
              {inactiveClients.length > 5 && (
                <button
                  type="button"
                  onClick={() => navigate('/clients')}
                  className="text-[11px] text-text-faint hover:text-primary transition-colors px-2"
                >
                  + još {inactiveClients.length - 5} klijenata
                </button>
              )}
            </div>
          )}
          {insight.actionLabel && targetRoute && insight.type !== 'ClientReEngagement' && (
            <button
              type="button"
              onClick={() => navigate(buildInsightRoute(targetRoute, insight))}
              className="mt-2 text-xs font-medium text-primary hover:text-primary-hover transition-colors"
            >
              {insight.actionLabel} →
            </button>
          )}
        </div>
      </div>
    </div>
  );
};
