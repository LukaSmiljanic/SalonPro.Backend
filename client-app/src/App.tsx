import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { Toaster } from 'sonner';
import { AuthProvider } from './context/AuthContext';
import { ThemeProvider } from './context/ThemeContext';
import { useTheme } from './context/ThemeContext';
import { Layout } from './components/Layout';
import { ProtectedRoute, PublicOnlyRoute } from './components/ProtectedRoute';
import { LoginPage } from './pages/LoginPage';
import { DashboardPage } from './pages/DashboardPage';
import { CalendarPage } from './pages/CalendarPage';
import { ClientsPage } from './pages/ClientsPage';
import { ServicesPage } from './pages/ServicesPage';
import { StaffPage } from './pages/StaffPage';
import { SettingsPage } from './pages/SettingsPage';
import { ReportsPage } from './pages/ReportsPage';
import { PaymentsPage } from './pages/PaymentsPage';
import { TenantsPage } from './pages/TenantsPage';
import { SocialMarketingPage } from './pages/SocialMarketingPage';
import { InstagramOAuthCallbackPage } from './pages/InstagramOAuthCallbackPage';
import { VerifyEmailPage } from './pages/VerifyEmailPage';
import { ResetPasswordPage } from './pages/ResetPasswordPage';
import { PublicBookingPage } from './pages/PublicBookingPage';

const ThemedToaster: React.FC = () => {
  const { isDark } = useTheme();
  return <Toaster richColors position="top-right" theme={isDark ? 'dark' : 'light'} />;
};

const App: React.FC = () => {
  return (
    <BrowserRouter>
      <AuthProvider>
        <ThemeProvider>
        <ThemedToaster />
        <Routes>
          {/* Public routes */}
          <Route element={<PublicOnlyRoute />}>
            <Route path="/" element={<LoginPage />} />
          </Route>

          {/* Protected routes */}
          <Route element={<ProtectedRoute />}>
            <Route element={<Layout />}>
              <Route path="/dashboard" element={<DashboardPage />} />
              <Route path="/calendar" element={<CalendarPage />} />
              <Route path="/clients" element={<ClientsPage />} />
              <Route path="/staff" element={<StaffPage />} />
              <Route path="/services" element={<ServicesPage />} />
              <Route path="/reports" element={<ReportsPage />} />
              <Route path="/marketing" element={<SocialMarketingPage />} />
              <Route path="/settings" element={<SettingsPage />} />
              <Route path="/payments" element={<PaymentsPage />} />
              <Route path="/tenants" element={<TenantsPage />} />
            </Route>
          </Route>

          {/* Public — no auth wrapper */}
          <Route path="/verify-email" element={<VerifyEmailPage />} />
          <Route path="/reset-password" element={<ResetPasswordPage />} />
          <Route path="/instagram/oauth-callback" element={<InstagramOAuthCallbackPage />} />
          <Route path="/book/:slug" element={<PublicBookingPage />} />

          {/* Catch-all */}
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
        </ThemeProvider>
      </AuthProvider>
    </BrowserRouter>
  );
};

export default App;
