import React from 'react';
import { Outlet } from 'react-router-dom';
import { TopNav } from './TopNav';

export const Layout: React.FC = () => {
  return (
    <div className="min-h-dvh bg-bg flex flex-col">
      <TopNav />
      <main className="flex-1">
        <Outlet />
      </main>
    </div>
  );
};
