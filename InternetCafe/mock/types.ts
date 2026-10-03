export const roles = ['Admin', 'Owner', 'Manager', 'Cashier', 'Staff', 'Customer'] as const;
export type Role = typeof roles[number];
// Dynamic rows are intentional: this demo read model serves multiple CRUD modules.
export type Row = { id: string; [key: string]: any };
export const resources = ['computers', 'customers', 'products', 'categories', 'orders', 'topups', 'transactions', 'suppliers', 'inventory', 'feedback', 'surveys', 'employees', 'shifts', 'schedules', 'leaves', 'attendance', 'payroll', 'accounts', 'sessions'] as const;
export type Resource = typeof resources[number];
export type Workspace = Record<Resource, Row[]>;
export interface AuthUser { id: string; username: string; fullName: string; role: Role; permissions: string[] }
export interface ApiResponse { status: number; body: any }
export interface EngineOptions { now?: () => Date; seed?: Workspace }
export const clone = <T>(value: T): T => JSON.parse(JSON.stringify(value));
export const vietnamDate = (date: Date) => new Intl.DateTimeFormat('en-CA', { timeZone: 'Asia/Ho_Chi_Minh', year: 'numeric', month: '2-digit', day: '2-digit' }).format(date);
