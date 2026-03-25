import React, { useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { Scissors, Eye, EyeOff, CheckCircle2, XCircle, KeyRound } from 'lucide-react';
import { Button } from '../components/Button';
import { Input } from '../components/Input';
import apiClient from '../api/client';

type Status = 'form' | 'success' | 'error';

export const ResetPasswordPage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const token = searchParams.get('token');

  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [status, setStatus] = useState<Status>(token ? 'form' : 'error');
  const [message, setMessage] = useState(
    token ? '' : 'Link za resetovanje lozinke nije validan.'
  );
  const [fieldError, setFieldError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setFieldError(null);

    if (!password || password.length < 6) {
      setFieldError('Lozinka mora imati najmanje 6 karaktera.');
      return;
    }
    if (password !== confirmPassword) {
      setFieldError('Lozinke se ne poklapaju.');
      return;
    }

    setIsLoading(true);
    try {
      const response = await apiClient.post('/auth/reset-password', {
        token,
        newPassword: password,
      });
      setStatus('success');
      setMessage(response.data?.message ?? 'Lozinka je uspešno promenjena.');
    } catch (err: unknown) {
      const data = (err as { response?: { data?: { message?: string } } })?.response?.data;
      setStatus('error');
      setMessage(data?.message ?? 'Resetovanje lozinke nije uspelo. Pokušajte ponovo.');
    } finally {
      setIsLoading(false);
    }
  };

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
          <div className="flex flex-col items-center text-center py-2">

            {status === 'form' && (
              <>
                <div className="w-14 h-14 rounded-full bg-primary/10 flex items-center justify-center mb-4">
                  <KeyRound size={24} className="text-primary" />
                </div>
                <h2 className="text-lg font-semibold text-text mb-2">Nova lozinka</h2>
                <p className="text-sm text-text-muted mb-5">
                  Unesite novu lozinku za vaš nalog.
                </p>

                <form onSubmit={handleSubmit} className="w-full flex flex-col gap-4 text-left">
                  <Input
                    label="Nova lozinka"
                    type={showPassword ? 'text' : 'password'}
                    placeholder="Minimum 6 karaktera"
                    value={password}
                    onChange={e => setPassword(e.target.value)}
                    autoComplete="new-password"
                    rightIcon={
                      <button
                        type="button"
                        onClick={() => setShowPassword(v => !v)}
                        className="text-text-faint hover:text-text-muted transition-colors p-1"
                      >
                        {showPassword ? <EyeOff size={14} /> : <Eye size={14} />}
                      </button>
                    }
                  />
                  <Input
                    label="Potvrdi lozinku"
                    type={showPassword ? 'text' : 'password'}
                    placeholder="Ponovite lozinku"
                    value={confirmPassword}
                    onChange={e => setConfirmPassword(e.target.value)}
                    autoComplete="new-password"
                    error={fieldError ?? undefined}
                  />
                  <Button type="submit" loading={isLoading} className="w-full">
                    Postavi novu lozinku
                  </Button>
                </form>
              </>
            )}

            {status === 'success' && (
              <>
                <div className="w-14 h-14 rounded-full bg-success/10 flex items-center justify-center mb-4">
                  <CheckCircle2 size={28} className="text-success" />
                </div>
                <h2 className="text-lg font-semibold text-text mb-2">Lozinka promenjena</h2>
                <p className="text-sm text-text-muted mb-5">{message}</p>
                <Button onClick={() => navigate('/', { replace: true })} className="w-full">
                  Prijavite se
                </Button>
              </>
            )}

            {status === 'error' && (
              <>
                <div className="w-14 h-14 rounded-full bg-error-bg flex items-center justify-center mb-4">
                  <XCircle size={28} className="text-error" />
                </div>
                <h2 className="text-lg font-semibold text-text mb-2">Greška</h2>
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
