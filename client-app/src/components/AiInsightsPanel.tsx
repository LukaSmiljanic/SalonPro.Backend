import React from 'react';
import { Brain, RefreshCw } from 'lucide-react';
import { useQuery } from '@tanstack/react-query';
import { getDashboardInsights } from '../api/dashboard';
import { queryKeys } from '../lib/queryKeys';
import { InsightCard } from './InsightCard';
import { LoadingSpinner } from './LoadingSpinner';

export const AiInsightsPanel: React.FC = () => {
  const { data, isLoading, error, refetch, isFetching } = useQuery({
    queryKey: queryKeys.dashboard.insights(),
    queryFn: getDashboardInsights,
    refetchInterval: 5 * 60_000, // refresh every 5 min
    staleTime: 2 * 60_000,
  });

  return (
    <div className="card card-padded">
      {/* Header */}
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-2">
          <div className="w-8 h-8 rounded-lg bg-[#5B3A8C]/10 flex items-center justify-center">
            <Brain size={16} className="text-[#5B3A8C]" />
          </div>
          <div>
            <h2 className="text-sm font-semibold text-text">AI Uvidi</h2>
            <p className="text-[11px] text-text-faint">Pametni saveti za vaš salon</p>
          </div>
        </div>
        <button
          onClick={() => refetch()}
          disabled={isFetching}
          className="p-1.5 rounded-md text-text-faint hover:text-[#5B3A8C] hover:bg-[#5B3A8C]/10 transition-interactive disabled:opacity-50"
          title="Osveži uvide"
        >
          <RefreshCw size={13} className={isFetching ? 'animate-spin' : ''} />
        </button>
      </div>

      {/* Content */}
      {isLoading ? (
        <div className="flex justify-center py-8">
          <LoadingSpinner size="sm" />
        </div>
      ) : error ? (
        <p className="text-xs text-text-faint text-center py-4">
          Nije moguće učitati uvide.
        </p>
      ) : data && data.insights.length > 0 ? (
        <div className="space-y-2">
          {data.insights.map((insight, idx) => (
            <InsightCard key={`${insight.type}-${idx}`} insight={insight} />
          ))}
        </div>
      ) : (
        <div className="text-center py-6">
          <Brain size={24} className="mx-auto text-text-faint/40 mb-2" />
          <p className="text-xs text-text-faint">Sve izgleda odlično! Nema preporuka za sada.</p>
        </div>
      )}

      {/* Summary stats */}
      {data && data.insights.length > 0 && (
        <div className="mt-4 pt-3 border-t border-border grid grid-cols-3 gap-2 text-center">
          <div>
            <p className="text-sm font-semibold text-text">{data.inactiveClientsCount}</p>
            <p className="text-[10px] text-text-faint">Neaktivni</p>
          </div>
          <div>
            <p className="text-sm font-semibold text-text">{data.scheduleGapsCount}</p>
            <p className="text-[10px] text-text-faint">Praznine</p>
          </div>
          <div>
            <p className="text-sm font-semibold text-text">
              {data.weekRevenueChangePercent > 0 ? '+' : ''}{data.weekRevenueChangePercent}%
            </p>
            <p className="text-[10px] text-text-faint">Prihod W/W</p>
          </div>
        </div>
      )}
    </div>
  );
};
