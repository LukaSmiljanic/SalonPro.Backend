import React, { useEffect, useState } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import { Scissors, CheckCircle2, XCircle, Loader2 } from 'lucide-react';
import { Button } from '../components/Button';
import apiClient from '../api/client';

type Status = 'loading' | 'success' | 'error';

export const VerifyEmailPage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const [status, setStatus] = useState<Status>('loading');
  const [message, setMessage] = useState('');
  const token = searchParams.get('token');

  useEffect(() => {
    if (!token) {
      setStatus('error');
      setMessage('Link za verifikaciju nije validan.');
      return;
    }

    const verify = async () => {
      try {
        const response = await apiClient.get('/auth/verify-email', {
          params: { token },
        });
        setStatus('success');
        setMessage(response.data?.message ?? 'Email je uspešno verifikovan!');
      } catch (err: unknown) {
        setStatus('error');
        const data = (err as { response?: { data?: { message?: string } } })?.response?.data;
        setMessage(data?.message ?? 'Verifikacija nije uspela. Token je možda istekao.');
      }
    };

    verify();
  }, [token]);

  return (
    <div className="min-h-dvh bg-bg flex items-center justify-center p-4">
      <div className="w-full max-w-sm">

        {/* Logo */}
        <div className="flex flex-col items-center mb-8">
          <div className="w-12 h-12 rounded-xl bg-primary flex items-center justify-center mb-3 shadow-md">
            <Scissors size={22} className="text-white" />
          </div>
          <h1 className="text-xl font-semibold text-display text-text">SalonPro</h1>
        </div>

        {/* Card */}
        <div className="card card-padded">
          <div className="flex flex-col items-center text-center py-4">

            {status === 'loading' && (
              <>
                <Loader2 size={36} className="text-primary animate-spin mb-4" />
                <h2 className="text-lg font-semibold text-text mb-2">Verifikacija u toku...</h2>
                <p className="text-sm text-text-muted">Sačekajte trenutak.</p>
              </>
            )}

            {status === 'success' && (
              <>
                <div className="w-14 h-14 rounded-full bg-success/10 flex items-center justify-center mb-4">
                  <CheckCircle2 size={28} className="text-success" />
                </div>
                <h2 className="text-lg font-semibold text-text mb-2">Nalog aktiviran</h2>
                <p className="text-sm text-text-muted mb-5">{message}</p>
                <Button onClick={() => navigate('/?verified=1', { replace: true })} className="w-full">
                  Prijavite se
                </Button>
              </>
            )}

            {status === 'error' && (
              <>
                <div className="w-14 h-14 rounded-full bg-error-bg flex items-center justify-center mb-4">
                  <XCircle size={28} className="text-error" />
                </div>
                <h2 className="text-lg font-semibold text-text mb-2">Verifikacija nije uspela</h2>
                <p className="text-sm text-text-muted mb-5">{message}</p>
                <Button variant="secondary" onClick={() => navigate('/', { replace: true })} className="w-full">
                  Nazad na prijavu
                </Button>
              </>
            )}

          </div>
        </div>

        <p className="text-center text-xs text-text-faint mt-6">
          SalonPro &copy; {new Date().getFullYear()}
        </p>
      </div>
    </div>
  );
};
