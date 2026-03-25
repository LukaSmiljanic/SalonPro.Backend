import React, { useState, useEffect } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { Scissors, Eye, EyeOff, AlertCircle, MailCheck, CheckCircle2, KeyRound } from 'lucide-react';
import { useAuth } from '../hooks/useAuth';
import { Button } from '../components/Button';
import { Input } from '../components/Input';
import apiClient from '../api/client';

type Mode = 'login' | 'register';

interface LoginForm {
  email: string;
  password: string;
  tenantId: string;
}

interface RegisterForm {
  email: string;
  password: string;
  confirmPassword: string;
  tenantName: string;
  ownerName: string;
}

export const LoginPage: React.FC = () => {
  const navigate = useNavigate();
  const { login, register } = useAuth();
  const [mode, setMode] = useState<Mode>('login');
  const [showPassword, setShowPassword] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [forgotMode, setForgotMode] = useState(false);
  const [forgotEmail, setForgotEmail] = useState('');
  const [forgotSent, setForgotSent] = useState(false);
  const [registrationSuccess, setRegistrationSuccess] = useState(false);
  const [registeredEmail, setRegisteredEmail] = useState('');
  const [searchParams] = useSearchParams();
  const isExpired = searchParams.get('expired') === '1';
  const isVerified = searchParams.get('verified') === '1';
  const [successMsg, setSuccessMsg] = useState<string | null>(null);

  useEffect(() => {
    if (isExpired) {
      setError('Vaša pretplata je istekla. Kontaktirajte podršku za produženje.');
    }
    if (isVerified) {
      setSuccessMsg('Nalog je uspešno aktiviran! Sada se možete prijaviti.');
    }
  }, [isExpired, isVerified]);

  const [loginForm, setLoginForm] = useState<LoginForm>({
    email: '',
    password: '',
    tenantId: '',
  });

  const [registerForm, setRegisterForm] = useState<RegisterForm>({
    email: '',
    password: '',
    confirmPassword: '',
    tenantName: '',
    ownerName: '',
  });

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    if (!loginForm.email || !loginForm.password) {
      setError('Unesite email i lozinku.');
      return;
    }
    setIsLoading(true);
    try {
      await login({
        email: loginForm.email,
        password: loginForm.password,
        ...(loginForm.tenantId.trim() && { tenantId: loginForm.tenantId.trim() }),
      });
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message;
      setError(msg ?? 'Prijava nije uspela. Proverite podatke.');
    } finally {
      setIsLoading(false);
    }
  };

  const handleRegister = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    if (!registerForm.email || !registerForm.password || !registerForm.tenantName || !registerForm.ownerName) {
      setError('Popunite sva polja.');
      return;
    }
    if (registerForm.password !== registerForm.confirmPassword) {
      setError('Lozinke se ne poklapaju.');
      return;
    }
    if (registerForm.password.length < 6) {
      setError('Lozinka mora imati najmanje 6 karaktera.');
      return;
    }
    const nameParts = registerForm.ownerName.trim().split(/\s+/);
    const firstName = nameParts[0] ?? '';
    const lastName = nameParts.slice(1).join(' ') || firstName;
    setIsLoading(true);
    try {
      const response = await register({
        email: registerForm.email.trim(),
        password: registerForm.password,
        tenantName: registerForm.tenantName.trim(),
        firstName: firstName || 'Owner',
        lastName: lastName || 'User',
      });
      if (response.requiresEmailVerification) {
        setRegisteredEmail(registerForm.email.trim());
        setRegistrationSuccess(true);
      } else {
        navigate('/dashboard', { replace: true });
      }
    } catch (err: unknown) {
      const data = (err as { response?: { data?: { errors?: Record<string, string[]>; detail?: string } } })?.response?.data;
      const errs = data?.errors;
      const msg = (errs && (errs.email?.[0] ?? errs.Email?.[0] ?? errs.tenantSlug?.[0] ?? errs.TenantSlug?.[0])) ?? data?.detail ?? 'Registracija nije uspela. Pokušajte ponovo.';
      setError(msg);
    } finally {
      setIsLoading(false);
    }
  };

  const handleForgotPassword = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    if (!forgotEmail.trim()) {
      setError('Unesite email adresu.');
      return;
    }
    setIsLoading(true);
    try {
      await apiClient.post('/auth/forgot-password', { email: forgotEmail.trim() });
      setForgotSent(true);
    } catch {
      // Always show success to prevent email enumeration
      setForgotSent(true);
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
          <p className="text-sm text-text-muted mt-1">
            {mode === 'login' ? 'Prijavite se u svoj salon' : 'Kreirajte novi nalog salona'}
          </p>
        </div>

        {/* Card */}
        <div className="card card-padded">

          {/* Registration success — email verification message */}
          {registrationSuccess ? (
            <div className="flex flex-col items-center text-center py-4">
              <div className="w-14 h-14 rounded-full bg-success/10 flex items-center justify-center mb-4">
                <MailCheck size={28} className="text-success" />
              </div>
              <h2 className="text-lg font-semibold text-text mb-2">Proverite vaš email</h2>
              <p className="text-sm text-text-muted mb-4">
                Poslali smo link za verifikaciju na{' '}
                <span className="font-medium text-text">{registeredEmail}</span>.
                Kliknite na link u poruci da aktivirate nalog.
              </p>
              <div className="w-full p-3 bg-surface-2 rounded-lg text-xs text-text-muted space-y-1">
                <p>Proverite i spam/promotions folder.</p>
                <p>Link važi 48 sati.</p>
              </div>
              <button
                onClick={() => {
                  setRegistrationSuccess(false);
                  setMode('login');
                  setError(null);
                }}
                className="mt-5 text-sm text-primary hover:text-primary/80 font-medium transition-colors"
              >
                Nazad na prijavu
              </button>
            </div>
          ) : (
          <>

          {/* Tab switcher */}
          <div className={`flex rounded-lg bg-surface-2 p-0.5 mb-5 ${forgotMode ? 'hidden' : ''}`}>
            {(['login', 'register'] as Mode[]).map(m => (
              <button
                key={m}
                onClick={() => { setMode(m); setError(null); }}
                className={`flex-1 py-2.5 md:py-1.5 text-base md:text-sm font-medium rounded-md transition-interactive
                  ${mode === m ? 'bg-surface shadow-sm text-text' : 'text-text-muted hover:text-text'}`}
              >
                {m === 'login' ? 'Prijava' : 'Registracija'}
              </button>
            ))}
          </div>

          {/* Success banner */}
          {successMsg && (
            <div className="flex items-start gap-2 p-3 border rounded-md mb-4 text-sm bg-[#edf7f2] border-[#2d7a4f]/20 text-[#2d7a4f]">
              <CheckCircle2 size={15} className="mt-0.5 shrink-0" />
              <span>{successMsg}</span>
            </div>
          )}

          {/* Error banner */}
          {error && (
            <div className={`flex items-start gap-2 p-3 border rounded-md mb-4 text-sm ${isExpired && error?.includes('pretplata') ? 'bg-warning/5 border-warning/30 text-warning' : 'bg-error-bg border-error/20 text-error'}`}>
              <AlertCircle size={15} className="mt-0.5 shrink-0" />
              <span>{error}</span>
            </div>
          )}

          {/* Login Form */}
          {mode === 'login' && !forgotMode && (
            <form onSubmit={handleLogin} className="flex flex-col gap-4">
              <Input
                label="Email"
                type="email"
                placeholder="vi@primer.com"
                value={loginForm.email}
                onChange={e => setLoginForm(f => ({ ...f, email: e.target.value }))}
                autoComplete="email"
              />
              <Input
                label="Lozinka"
                type={showPassword ? 'text' : 'password'}
                placeholder="Vaša lozinka"
                value={loginForm.password}
                onChange={e => setLoginForm(f => ({ ...f, password: e.target.value }))}
                autoComplete="current-password"
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
              <div className="flex justify-end -mt-1">
                <button
                  type="button"
                  onClick={() => { setForgotMode(true); setError(null); setSuccessMsg(null); setForgotSent(false); setForgotEmail(loginForm.email); }}
                  className="text-xs text-primary hover:text-primary/80 font-medium transition-colors"
                >
                  Zaboravljena lozinka?
                </button>
              </div>
              <Input
                label="ID ili slug salona"
                type="text"
                placeholder="npr. demo-salon"
                value={loginForm.tenantId}
                onChange={e => setLoginForm(f => ({ ...f, tenantId: e.target.value }))}
                hint="Potrebno samo ako imate više salona (isti email)"
              />
              <Button type="submit" loading={isLoading} className="mt-1 w-full">
                Prijavi se
              </Button>
            </form>
          )}

          {/* Forgot Password Form */}
          {mode === 'login' && forgotMode && !forgotSent && (
            <form onSubmit={handleForgotPassword} className="flex flex-col gap-4">
              <div className="flex flex-col items-center text-center mb-2">
                <div className="w-12 h-12 rounded-full bg-primary/10 flex items-center justify-center mb-3">
                  <KeyRound size={20} className="text-primary" />
                </div>
                <h3 className="text-sm font-semibold text-text">Resetujte lozinku</h3>
                <p className="text-xs text-text-muted mt-1">Unesite email i poslaćemo vam link za resetovanje.</p>
              </div>
              <Input
                label="Email"
                type="email"
                placeholder="vi@primer.com"
                value={forgotEmail}
                onChange={e => setForgotEmail(e.target.value)}
                autoComplete="email"
              />
              <Button type="submit" loading={isLoading} className="w-full">
                Pošalji link
              </Button>
              <button
                type="button"
                onClick={() => { setForgotMode(false); setError(null); }}
                className="text-xs text-primary hover:text-primary/80 font-medium transition-colors text-center"
              >
                Nazad na prijavu
              </button>
            </form>
          )}

          {/* Forgot Password — Email Sent */}
          {mode === 'login' && forgotMode && forgotSent && (
            <div className="flex flex-col items-center text-center py-4">
              <div className="w-14 h-14 rounded-full bg-success/10 flex items-center justify-center mb-4">
                <MailCheck size={28} className="text-success" />
              </div>
              <h2 className="text-lg font-semibold text-text mb-2">Proverite vaš email</h2>
              <p className="text-sm text-text-muted mb-4">
                Ako nalog sa adresom <span className="font-medium text-text">{forgotEmail}</span> postoji,
                poslaćemo link za resetovanje lozinke.
              </p>
              <div className="w-full p-3 bg-surface-2 rounded-lg text-xs text-text-muted space-y-1">
                <p>Proverite i spam/promotions folder.</p>
                <p>Link važi 1 sat.</p>
              </div>
              <button
                onClick={() => { setForgotMode(false); setForgotSent(false); setError(null); }}
                className="mt-5 text-sm text-primary hover:text-primary/80 font-medium transition-colors"
              >
                Nazad na prijavu
              </button>
            </div>
          )}

          {/* Register Form */}
          {mode === 'register' && (
            <form onSubmit={handleRegister} className="flex flex-col gap-4">
              <Input
                label="Naziv salona"
                type="text"
                placeholder="Moj salon"
                value={registerForm.tenantName}
                onChange={e => setRegisterForm(f => ({ ...f, tenantName: e.target.value }))}
              />
              <Input
                label="Ime vlasnika"
                type="text"
                placeholder="Jelena Petrović"
                value={registerForm.ownerName}
                onChange={e => setRegisterForm(f => ({ ...f, ownerName: e.target.value }))}
              />
              <Input
                label="Email"
                type="email"
                placeholder="vi@primer.com"
                value={registerForm.email}
                onChange={e => setRegisterForm(f => ({ ...f, email: e.target.value }))}
              />
              <Input
                label="Lozinka"
                type={showPassword ? 'text' : 'password'}
                placeholder="Vaša lozinka"
                value={registerForm.password}
                onChange={e => setRegisterForm(f => ({ ...f, password: e.target.value }))}
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
                placeholder="Vaša lozinka"
                value={registerForm.confirmPassword}
                onChange={e => setRegisterForm(f => ({ ...f, confirmPassword: e.target.value }))}
              />
              <Button type="submit" loading={isLoading} className="mt-1 w-full">
                Kreiraj nalog
              </Button>
            </form>
          )}
          </>
          )}
        </div>

        <p className="text-center text-xs text-text-faint mt-6">
          SalonPro &copy; {new Date().getFullYear()}
        </p>
      </div>
    </div>
  );
};
