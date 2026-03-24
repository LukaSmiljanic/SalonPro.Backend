// ─── Auth ────────────────────────────────────────────────────────────────────

export interface LoginRequest {
  email: string;
  password: string;
  tenantId?: string; // optional; backend resolves tenant from user account
}

export interface RegisterRequest {
  email: string;
  password: string;
  tenantName: string;
  tenantSlug?: string; // optional; derived from tenantName if not provided
  firstName: string;
  lastName: string;
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
  requiresEmailVerification?: boolean;
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
  birthdayReminders: BirthdayReminder[];
}

export interface BirthdayReminder {
  clientId: string;
  fullName: string;
  phone?: string;
  email?: string;
  dateOfBirth: string;
  daysUntilBirthday: number;
  age: number;
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
  isVip?: boolean;
  tags?: string;
  totalVisits: number;
  totalSpent: number;
  lastVisit?: string;
  createdAt: string;
  loyalty?: ClientLoyalty;
}

export interface ClientLoyalty {
  totalVisits: number;
  loyaltyTier: LoyaltyTier;
  loyaltyBenefit?: string;
  nextMilestone?: number;
  visitsUntilNextMilestone: number;
  nextMilestoneBenefit?: string;
}

export type LoyaltyTier = 'None' | 'Bronze' | 'Silver' | 'Gold' | 'Platinum';

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

// ─── AI Insights ─────────────────────────────────────────────────────────

export type InsightType =
  | 'ScheduleGap'
  | 'ClientReEngagement'
  | 'RevenueChange'
  | 'NoShowRisk'
  | 'PeakHours'
  | 'ServiceUpsell'
  | 'VisitPattern'
  | 'RebookingSuggestion'
  | 'ChurnRisk'
  | 'SpendingTrend'
  | 'PreferredStaff'
  | 'ServiceHistory';

export type InsightPriority = 'Low' | 'Medium' | 'High' | 'Urgent';

export interface Insight {
  type: InsightType;
  priority: InsightPriority;
  title: string;
  description: string;
  icon: string;
  actionLabel?: string;
  actionData?: string;
}

export interface DashboardInsights {
  insights: Insight[];
  inactiveClientsCount: number;
  scheduleGapsCount: number;
  weekRevenueChangePercent: number;
}

export interface ClientInsights {
  insights: Insight[];
  averageVisitCycleDays: number;
  suggestedNextVisit?: string;
  preferredStaffName?: string;
  topService?: string;
  averageSpendPerVisit: number;
}

// ─── Staff ───────────────────────────────────────────────────────────────────

export interface StaffMember {
  id: string;
  firstName: string;
  lastName: string;
  name: string;
  email: string;
  phone: string;
  role: string;
  specialties: string[];
  isActive: boolean;
  colorIndex: number;
  totalAppointments: number;
}

export interface CreateStaffRequest {
  firstName: string;
  lastName: string;
  email?: string;
  phone?: string;
  title?: string;
  specialization?: string;
  colorIndex?: number;
}

// ─── Services ────────────────────────────────────────────────────────────────

export type ServiceCategoryType =
  | 'Hair'
  | 'Nails'
  | 'Skin'
  | 'Massage'
  | 'Makeup'
  | 'Other';

export interface ServiceCategory {
  id: string;
  name: string;
  description?: string;
  colorHex?: string;
  type: ServiceCategoryType;
  isActive: boolean;
  serviceCount: number;
}

export interface CreateServiceCategoryRequest {
  name: string;
  description?: string;
  colorHex?: string;
  type?: ServiceCategoryType;
}

export interface Service {
  id: string;
  name: string;
  category: string;
  categoryId?: string;
  duration: number; // minutes
  price: number;
  description?: string;
  isActive: boolean;
}

export interface CreateServiceRequest {
  categoryId: string;
  name: string;
  description?: string;
  durationMinutes: number;
  price: number;
}

// ─── Tenants (SuperAdmin) ────────────────────────────────────────────────────

export interface TenantInfo {
  id: string;
  name: string;
  slug: string;
  email?: string;
  phone?: string;
  city?: string;
  isActive: boolean;
  emailVerified: boolean;
  subscriptionStatus: string;
  subscriptionEndDate?: string;
  daysRemaining?: number;
  userCount: number;
  clientCount: number;
  createdAt: string;
  lastLoginAt?: string;
}

// ─── Payments (SuperAdmin) ───────────────────────────────────────────────────

export type PaymentStatus = 'Pending' | 'Paid' | 'Overdue' | 'Cancelled';

export interface Payment {
  id: string;
  tenantId: string;
  tenantName: string;
  amount: number;
  currency: string;
  periodStart: string;
  periodEnd: string;
  status: PaymentStatus;
  paidAt?: string;
  notes?: string;
  paidBy?: string;
  createdAt: string;
}

export interface PaymentSummary {
  tenantId: string;
  tenantName: string;
  totalPaid: number;
  totalPending: number;
  lastPaymentDate?: string;
}

export interface CreatePaymentRequest {
  tenantId: string;
  amount: number;
  currency: string;
  periodStart: string;
  periodEnd: string;
  notes?: string;
}

export interface UpdatePaymentStatusRequest {
  id: string;
  status: PaymentStatus;
  paidBy?: string;
}

// ─── Settings ────────────────────────────────────────────────────────────────
export interface WorkingHoursEntry {
  dayOfWeek: number; // 0=Sunday, 1=Monday, ..., 6=Saturday
  startTime: string; // "09:00:00"
  endTime: string;   // "17:00:00"
  isWorkingDay: boolean;
}

export interface LoyaltyTierConfig {
  tierName: string;
  minVisits: number;
  benefit: string;
}

// ─── Reports ─────────────────────────────────────────────────────────────────
export interface StaffRevenue {
  staffId: string;
  staffName: string;
  totalRevenue: number;
  appointmentCount: number;
  averagePerAppointment: number;
}

export interface ServiceRevenue {
  serviceName: string;
  category: string;
  totalRevenue: number;
  bookingCount: number;
  averagePrice: number;
}

export interface ReportSummary {
  totalRevenue: number;
  totalAppointments: number;
  completedCount: number;
  cancelledCount: number;
  noShowCount: number;
  cancellationRate: number;
  noShowRate: number;
  uniqueClients: number;
  averageRevenuePerDay: number;
}
