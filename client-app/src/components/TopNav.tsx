import React, { useState } from 'react';
import { NavLink } from 'react-router-dom';
import { LayoutDashboard, Calendar, Users, LogOut, Scissors, Menu, X, Package, UserPlus } from 'lucide-react';
import { useAuth } from '../hooks/useAuth';

const navItems = [
  { to: '/dashboard', icon: LayoutDashboard, label: 'Kontrolna tabla' },
  { to: '/calendar',  icon: Calendar,        label: 'Kalendar'  },
  { to: '/clients',   icon: Users,           label: 'Klijenti'   },
  { to: '/staff',     icon: UserPlus,        label: 'Zaposleni'  },
  { to: '/services',  icon: Package,         label: 'Usluge'  },
];

export const TopNav: React.FC = () => {
  const { user, logout } = useAuth();
  const [mobileOpen, setMobileOpen] = useState(false);

  return (
    <header className="sticky top-0 z-40 bg-surface border-b border-border">
      <div className="container-main flex items-center h-14">
        {/* Logo */}
        <NavLink to="/dashboard" className="flex items-center gap-2 mr-8 shrink-0">
          <span className="w-7 h-7 rounded-lg bg-primary flex items-center justify-center">
            <Scissors size={14} className="text-white" />
          </span>
          <span className="font-semibold text-sm text-display text-text">SalonPro</span>
        </NavLink>

        {/* Desktop nav */}
        <nav className="hidden md:flex items-center gap-1 flex-1">
          {navItems.map(({ to, icon: Icon, label }) => (
            <NavLink
              key={to}
              to={to}
              className={({ isActive }) =>
                `flex items-center gap-2 px-3 py-1.5 rounded-md text-sm font-medium transition-interactive
                ${ isActive
                  ? 'bg-primary-highlight text-primary'
                  : 'text-text-muted hover:bg-surface-2 hover:text-text'
                }`
              }
            >
              <Icon size={15} />{label}
            </NavLink>
          ))}
        </nav>

        {/* User & logout */}
        <div className="hidden md:flex items-center gap-3 ml-auto">
          {user && (
            <span className="text-xs text-text-muted">
              {user.name} · <span className="text-text-faint">{user.tenantName}</span>
            </span>
          )}
          <button
            onClick={logout}
            className="flex items-center gap-1.5 text-xs text-text-muted hover:text-error transition-interactive px-2 py-1 rounded-md hover:bg-error-bg"
          >
            <LogOut size={13} /> Odjava
          </button>
        </div>

        {/* Mobile hamburger */}
        <button
          className="ml-auto md:hidden p-2.5 -mr-1 rounded-lg text-text-muted hover:bg-surface-2 active:bg-surface-offset"
          onClick={() => setMobileOpen(v => !v)}
        >
          {mobileOpen ? <X size={22} /> : <Menu size={22} />}
        </button>
      </div>

      {/* Mobile drawer */}
      {mobileOpen && (
        <div className="md:hidden border-t border-divider px-4 py-3 flex flex-col gap-1 bg-surface">
          {navItems.map(({ to, icon: Icon, label }) => (
            <NavLink
              key={to}
              to={to}
              onClick={() => setMobileOpen(false)}
              className={({ isActive }) =>
                `flex items-center gap-3 px-3 py-3 rounded-lg text-base font-medium transition-interactive
                ${ isActive ? 'bg-primary-highlight text-primary' : 'text-text-muted hover:bg-surface-2 active:bg-surface-offset' }`
              }
            >
              <Icon size={18} />{label}
            </NavLink>
          ))}
          <button
            onClick={() => { logout(); setMobileOpen(false); }}
            className="flex items-center gap-3 px-3 py-3 rounded-lg text-base text-error hover:bg-error-bg active:bg-error-bg mt-1"
          >
            <LogOut size={18} /> Odjava
          </button>
        </div>
      )}
    </header>
  );
};
