// ─── Auth ────────────────────────────────────────────────────────────────────

export interface LoginRequest {
  email: string;
  password: string;
  tenantId: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  tenantName: string;
  ownerName: string;
}

export interface AuthUser {
  id: string;
  email: string;
  name: string;
  role: string;
  tenantId: string;
  tenantName: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  user: AuthUser;
}

export interface RefreshTokenRequest {
  accessToken: string;
  refreshToken: string;
}

export interface RefreshTokenResponse {
  accessToken: string;
  refreshToken: string;
}

// ─── Dashboard ───────────────────────────────────────────────────────────────

export interface DashboardStats {
  todayAppointments: number;
  weekRevenue: number;
  activeClients: number;
  completionRate: number;
  upcomingAppointments: AppointmentSummary[];
}

export interface AppointmentSummary {
  id: string;
  clientName: string;
  serviceName: string;
  staffName: string;
  startTime: string;
  status: AppointmentStatus;
}

export interface RevenueChartPoint {
  date: string;
  revenue: number;
}

// ─── Appointments ────────────────────────────────────────────────────────────

export type AppointmentStatus =
  | 'Pending'
  | 'Confirmed'
  | 'InProgress'
  | 'Completed'
  | 'Cancelled'
  | 'NoShow';

export interface Appointment {
  id: string;
  clientId: string;
  clientName: string;
  staffId: string;
  staffName: string;
  serviceId: string;
  serviceName: string;
  serviceCategory: string;
  startTime: string;
  endTime: string;
  status: AppointmentStatus;
  notes?: string;
  price: number;
}

export interface AppointmentListResponse {
  items: Appointment[];
  total: number;
  page: number;
  pageSize: number;
}

export interface CreateAppointmentRequest {
  clientId: string;
  staffId: string;
  serviceId: string;
  startTime: string;
  notes?: string;
}

// ─── Clients ─────────────────────────────────────────────────────────────────

export interface Client {
  id: string;
  firstName: string;
  lastName: string;
  email?: string;
  phone?: string;
  notes?: string;
  totalVisits: number;
  totalSpent: number;
  lastVisit?: string;
  createdAt: string;
}

export interface ClientListResponse {
  items: Client[];
  total: number;
  page: number;
  pageSize: number;
}

export interface CreateClientRequest {
  firstName: string;
  lastName: string;
  email?: string;
  phone?: string;
  notes?: string;
}

// ─── Staff ───────────────────────────────────────────────────────────────────

export interface StaffMember {
  id: string;
  name: string;
  email: string;
  role: string;
  specialties: string[];
  isActive: boolean;
}

// ─── Services ────────────────────────────────────────────────────────────────

export interface Service {
  id: string;
  name: string;
  category: string;
  duration: number; // minutes
  price: number;
  description?: string;
  isActive: boolean;
}
