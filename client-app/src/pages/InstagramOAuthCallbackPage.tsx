import React, { useEffect, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { completeInstagramOAuth } from '../api/social';
import { LoadingSpinner } from '../components/LoadingSpinner';

function apiErrorMessage(err: unknown): string {
  const ax = err as { response?: { data?: { message?: string; detail?: string } }; message?: string };
  return ax.response?.data?.detail
    ?? ax.response?.data?.message
    ?? ax.message
    ?? 'Povezivanje nije uspelo.';
}

export const InstagramOAuthCallbackPage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const [message, setMessage] = useState('Završavamo povezivanje sa Instagramom…');

  useEffect(() => {
    const error = searchParams.get('error');
    if (error) {
      navigate(`/marketing?instagram=error&reason=${encodeURIComponent(error)}`, { replace: true });
      return;
    }

    const code = searchParams.get('code');
    const state = searchParams.get('state');
    if (!code || !state) {
      navigate('/marketing?instagram=error&reason=missing_code', { replace: true });
      return;
    }

    let cancelled = false;

    (async () => {
      try {
        await completeInstagramOAuth(code, state);
        if (!cancelled) {
          navigate('/marketing?instagram=connecting', { replace: true });
        }
      } catch (err) {
        if (!cancelled) {
          const reason = encodeURIComponent(apiErrorMessage(err));
          navigate(`/marketing?instagram=error&reason=${reason}`, { replace: true });
        }
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [navigate, searchParams]);

  return (
    <div className="min-h-screen flex flex-col items-center justify-center gap-4 p-6 bg-surface-0 text-text">
      <LoadingSpinner />
      <p className="text-sm text-text-muted text-center max-w-sm">{message}</p>
    </div>
  );
};
