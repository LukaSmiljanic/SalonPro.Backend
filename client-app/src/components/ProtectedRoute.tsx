import React from 'react';
import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { LoadingSpinner } from './LoadingSpinner';

export const ProtectedRoute: React.FC = () => {
  const { isAuthenticated, isLoading } = useAuth();
  if (isLoading) return <LoadingSpinner fullPage />;
  if (!isAuthenticated) return <Navigate to="/" replace />;
  return <Outlet />;
};

export const PublicOnlyRoute: React.FC = () => {
  const { isAuthenticated, isLoading, user } = useAuth();
  if (isLoading) return <LoadingSpinner fullPage />;
  if (isAuthenticated) {
    const target = user?.role === 'SuperAdmin' ? '/tenants' : '/dashboard';
    return <Navigate to={target} replace />;
  }
  return <Outlet />;
};
