import React, { useState } from 'react';
import { Scissors, Eye, EyeOff, AlertCircle } from 'lucide-react';
import { useAuth } from '../hooks/useAuth';
import { Button } from '../components/Button';
import { Input } from '../components/Input';

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
  const { login } = useAuth();
  const [mode, setMode] = useState<Mode>('login');
  const [showPassword, setShowPassword] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

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
    if (!loginForm.email || !loginForm.password || !loginForm.tenantId) {
      setError('Please fill in all fields.');
      return;
    }
    setIsLoading(true);
    try {
      await login(loginForm);
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message;
      setError(msg ?? 'Login failed. Please check your credentials.');
    } finally {
      setIsLoading(false);
    }
  };

  const handleRegister = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    if (!registerForm.email || !registerForm.password || !registerForm.tenantName || !registerForm.ownerName) {
      setError('Please fill in all fields.');
      return;
    }
    if (registerForm.password !== registerForm.confirmPassword) {
      setError('Passwords do not match.');
      return;
    }
    if (registerForm.password.length < 6) {
      setError('Password must be at least 6 characters.');
      return;
    }
    setIsLoading(true);
    try {
      // Registration not yet in backend; placeholder
      setError('Registration is not yet available. Please contact your administrator.');
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
            {mode === 'login' ? 'Sign in to your salon' : 'Create a new salon account'}
          </p>
        </div>

        {/* Card */}
        <div className="card card-padded">

          {/* Tab switcher */}
          <div className="flex rounded-lg bg-surface-2 p-0.5 mb-5">
            {(['login', 'register'] as Mode[]).map(m => (
              <button
                key={m}
                onClick={() => { setMode(m); setError(null); }}
                className={`flex-1 py-1.5 text-sm font-medium rounded-md transition-interactive
                  ${mode === m ? 'bg-surface shadow-sm text-text' : 'text-text-muted hover:text-text'}`}
              >
                {m === 'login' ? 'Sign In' : 'Register'}
              </button>
            ))}
          </div>

          {/* Error banner */}
          {error && (
            <div className="flex items-start gap-2 p-3 bg-error-bg border border-error/20 rounded-md mb-4 text-sm text-error">
              <AlertCircle size={15} className="mt-0.5 shrink-0" />
              <span>{error}</span>
            </div>
          )}

          {/* Login Form */}
          {mode === 'login' && (
            <form onSubmit={handleLogin} className="flex flex-col gap-4">
              <Input
                label="Email"
                type="email"
                placeholder="you@example.com"
                value={loginForm.email}
                onChange={e => setLoginForm(f => ({ ...f, email: e.target.value }))}
                autoComplete="email"
              />
              <Input
                label="Password"
                type={showPassword ? 'text' : 'password'}
                placeholder="••••••••"
                value={loginForm.password}
                onChange={e => setLoginForm(f => ({ ...f, password: e.target.value }))}
                autoComplete="current-password"
                rightIcon={
                  <button
                    type="button"
                    onClick={() => setShowPassword(v => !v)}
                    className="pointer-events-auto text-text-faint hover:text-text-muted"
                  >
                    {showPassword ? <EyeOff size={14} /> : <Eye size={14} />}
                  </button>
                }
              />
              <Input
                label="Salon ID"
                type="text"
                placeholder="your-salon-id"
                value={loginForm.tenantId}
                onChange={e => setLoginForm(f => ({ ...f, tenantId: e.target.value }))}
                hint="Your unique salon identifier"
              />
              <Button type="submit" loading={isLoading} className="mt-1 w-full">
                Sign In
              </Button>
            </form>
          )}

          {/* Register Form */}
          {mode === 'register' && (
            <form onSubmit={handleRegister} className="flex flex-col gap-4">
              <Input
                label="Salon Name"
                type="text"
                placeholder="My Salon"
                value={registerForm.tenantName}
                onChange={e => setRegisterForm(f => ({ ...f, tenantName: e.target.value }))}
              />
              <Input
                label="Owner Name"
                type="text"
                placeholder="Jane Smith"
                value={registerForm.ownerName}
                onChange={e => setRegisterForm(f => ({ ...f, ownerName: e.target.value }))}
              />
              <Input
                label="Email"
                type="email"
                placeholder="you@example.com"
                value={registerForm.email}
                onChange={e => setRegisterForm(f => ({ ...f, email: e.target.value }))}
              />
              <Input
                label="Password"
                type={showPassword ? 'text' : 'password'}
                placeholder="••••••••"
                value={registerForm.password}
                onChange={e => setRegisterForm(f => ({ ...f, password: e.target.value }))}
                rightIcon={
                  <button
                    type="button"
                    onClick={() => setShowPassword(v => !v)}
                    className="pointer-events-auto text-text-faint hover:text-text-muted"
                  >
                    {showPassword ? <EyeOff size={14} /> : <Eye size={14} />}
                  </button>
                }
              />
              <Input
                label="Confirm Password"
                type={showPassword ? 'text' : 'password'}
                placeholder="••••••••"
                value={registerForm.confirmPassword}
                onChange={e => setRegisterForm(f => ({ ...f, confirmPassword: e.target.value }))}
              />
              <Button type="submit" loading={isLoading} className="mt-1 w-full">
                Create Account
              </Button>
            </form>
          )}
        </div>

        <p className="text-center text-xs text-text-faint mt-6">
          SalonPro &copy; {new Date().getFullYear()}
        </p>
      </div>
    </div>
  );
};
