import React from 'react';
import { Brain } from 'lucide-react';
import { useQuery } from '@tanstack/react-query';
import { getClientInsights } from '../api/clients';
import { queryKeys } from '../lib/queryKeys';
import { InsightCard } from './InsightCard';
import { LoadingSpinner } from './LoadingSpinner';
import { format, parseISO } from 'date-fns';

interface ClientInsightsPanelProps {
  clientId: string;
}

export const ClientInsightsPanel: React.FC<ClientInsightsPanelProps> = ({ clientId }) => {
  const { data, isLoading, error } = useQuery({
    queryKey: queryKeys.clients.insights(clientId),
    queryFn: () => getClientInsights(clientId),
    enabled: !!clientId,
    staleTime: 2 * 60_000,
  });

  const formatCurrency = (value: number) => {
    return new Intl.NumberFormat('sr-Latn-RS', {
      style: 'currency',
      currency: 'RSD',
      minimumFractionDigits: 0,
      maximumFractionDigits: 0,
    }).format(value);
  };

  if (isLoading) {
    return (
      <div className="flex justify-center py-4">
        <LoadingSpinner size="sm" />
      </div>
    );
  }

  if (error || !data) return null;

  const hasInsights = data.insights.length > 0;
  const hasStats = data.averageVisitCycleDays > 0 || data.topService || data.preferredStaffName;

  if (!hasInsights && !hasStats) return null;

  return (
    <div className="mt-4 pt-3 border-t border-border">
      <div className="flex items-center gap-1.5 mb-3">
        <Brain size={13} className="text-[#5B3A8C]" />
        <span className="text-xs font-semibold text-text">AI Uvidi</span>
      </div>

      {/* Quick stats row */}
      {hasStats && (
        <div className="grid grid-cols-2 gap-2 mb-3">
          {data.averageVisitCycleDays > 0 && (
            <div className="bg-[#5B3A8C]/5 rounded-md p-2 text-center">
              <p className="text-xs font-semibold text-text">~{Math.round(data.averageVisitCycleDays)} dana</p>
              <p className="text-[10px] text-text-faint">Ciklus poseta</p>
            </div>
          )}
          {data.averageSpendPerVisit > 0 && (
            <div className="bg-[#5B3A8C]/5 rounded-md p-2 text-center">
              <p className="text-xs font-semibold text-text">{formatCurrency(data.averageSpendPerVisit)}</p>
              <p className="text-[10px] text-text-faint">Prosek/poseta</p>
            </div>
          )}
          {data.topService && (
            <div className="bg-[#5B3A8C]/5 rounded-md p-2 text-center">
              <p className="text-xs font-semibold text-text truncate">{data.topService}</p>
              <p className="text-[10px] text-text-faint">Top usluga</p>
            </div>
          )}
          {data.suggestedNextVisit && (
            <div className="bg-[#5B3A8C]/5 rounded-md p-2 text-center">
              <p className="text-xs font-semibold text-text">
                {format(parseISO(data.suggestedNextVisit), 'dd.MM.')}
              </p>
              <p className="text-[10px] text-text-faint">Sledeća poseta</p>
            </div>
          )}
        </div>
      )}

      {/* Insight cards */}
      {hasInsights && (
        <div className="space-y-1.5">
          {data.insights.slice(0, 3).map((insight, idx) => (
            <InsightCard key={`${insight.type}-${idx}`} insight={insight} compact />
          ))}
        </div>
      )}
    </div>
  );
};
