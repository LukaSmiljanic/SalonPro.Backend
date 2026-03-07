import React from 'react';
import {
  CalendarClock, UserX, TrendingUp, TrendingDown, AlertTriangle,
  Clock, Sparkles, CalendarPlus, UserCheck, type LucideIcon
} from 'lucide-react';
import type { Insight, InsightPriority } from '../types';

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

const priorityStyles: Record<InsightPriority, { bg: string; border: string; dot: string }> = {
  Urgent: { bg: 'bg-red-50', border: 'border-red-200', dot: 'bg-red-500' },
  High: { bg: 'bg-orange-50', border: 'border-orange-200', dot: 'bg-orange-500' },
  Medium: { bg: 'bg-[#5B3A8C]/5', border: 'border-[#5B3A8C]/15', dot: 'bg-[#5B3A8C]' },
  Low: { bg: 'bg-surface-2', border: 'border-border', dot: 'bg-text-faint' },
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
}

export const InsightCard: React.FC<InsightCardProps> = ({ insight, compact = false }) => {
  const IconComponent = iconMap[insight.icon] || Sparkles;
  const styles = priorityStyles[insight.priority];

  if (compact) {
    return (
      <div className={`flex items-start gap-2.5 p-2.5 rounded-lg ${styles.bg} border ${styles.border}`}>
        <div className="shrink-0 mt-0.5">
          <IconComponent size={14} className="text-[#5B3A8C]" />
        </div>
        <div className="min-w-0 flex-1">
          <p className="text-xs font-medium text-text leading-snug">{insight.title}</p>
          <p className="text-[11px] text-text-muted leading-snug mt-0.5">{insight.description}</p>
        </div>
      </div>
    );
  }

  return (
    <div className={`p-3 rounded-lg ${styles.bg} border ${styles.border}`}>
      <div className="flex items-start gap-3">
        <div className="w-8 h-8 rounded-lg bg-white/70 flex items-center justify-center shrink-0">
          <IconComponent size={15} className="text-[#5B3A8C]" />
        </div>
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2 mb-0.5">
            <p className="text-sm font-medium text-text leading-snug">{insight.title}</p>
            <span className="flex items-center gap-1 shrink-0">
              <span className={`w-1.5 h-1.5 rounded-full ${styles.dot}`} />
              <span className="text-[10px] text-text-faint uppercase tracking-wider">
                {priorityLabels[insight.priority]}
              </span>
            </span>
          </div>
          <p className="text-xs text-text-muted leading-relaxed">{insight.description}</p>
          {insight.actionLabel && (
            <button className="mt-2 text-xs font-medium text-[#5B3A8C] hover:text-[#4A2E73] transition-colors">
              {insight.actionLabel} →
            </button>
          )}
        </div>
      </div>
    </div>
  );
};
