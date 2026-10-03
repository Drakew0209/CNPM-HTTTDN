export type Role = 'Admin' | 'Owner' | 'Manager' | 'Cashier' | 'Staff' | 'Customer';
export interface User { id: string; username: string; fullName: string; role: Role; permissions: string[] }
export interface Session { accessToken: string; user: User }
export interface Row { id: string; [key: string]: unknown }
export type Workspace = Record<string, Row[]>;
export const managementRoles: Role[] = ['Owner', 'Manager'];
export const operationRoles: Role[] = ['Owner', 'Manager', 'Cashier'];
export const money = (n: unknown) => new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 }).format(Number(n) || 0);
export const dateTime = (v: unknown) => v ? new Date(String(v)).toLocaleString('vi-VN', { dateStyle: 'short', timeStyle: 'short' }) : '—';
export const roleLabel: Record<Role, string> = { Admin: 'Quản trị hệ thống', Owner: 'Chủ quán', Manager: 'Quản lý', Cashier: 'Thu ngân', Staff: 'Nhân viên', Customer: 'Khách hàng' };
export const homeFor = (role: Role) => role === 'Admin' ? '/system/accounts' : role === 'Staff' ? '/staff/home' : role === 'Customer' ? '/forbidden' : '/management/overview';
export const labels: Record<string, string> = {
  Active: 'Hoạt động', Banned: 'Đã khóa', Inactive: 'Ngừng hoạt động', Available: 'Sẵn sàng', InUse: 'Đang sử dụng', Maintenance: 'Bảo trì',
  Pending: 'Chờ xử lý', Approved: 'Đã duyệt', Rejected: 'Từ chối', Preparing: 'Đang chuẩn bị', Served: 'Đã phục vụ', Cancelled: 'Đã hủy',
  Processing: 'Đang xử lý', Resolved: 'Đã giải quyết', Published: 'Đang phát hành', Draft: 'Bản nháp', Closed: 'Đã đóng', Completed: 'Hoàn tất',
  Scheduled: 'Đã xếp lịch', Absent: 'Vắng', OnLeave: 'Nghỉ phép', Paid: 'Đã trả', Silver: 'Bạc', Gold: 'Vàng', Diamond: 'Kim cương',
  Standard: 'Tiêu chuẩn', VIP: 'VIP', TopUp: 'Nạp tiền', FoodOrder: 'Dịch vụ F&B', Rental: 'Giờ chơi', Refund: 'Hoàn tiền',
  Import: 'Nhập kho', Export: 'Xuất kho', Annual: 'Nghỉ phép', Sick: 'Nghỉ ốm', Resignation: 'Nghỉ việc', ...roleLabel,
};
export function revenue(rows: Row[]) {
  const sum = (type: string) => rows.filter(r => r.type === type).reduce((s, r) => s + Math.abs(Number(r.amount) || 0), 0);
  const rental = sum('Rental'), food = sum('FoodOrder'), refund = sum('Refund');
  return { rental, food, refund, net: rental + food - refund, topup: sum('TopUp') };
}
export function filterDates(rows: Row[], from: string, to: string, field = 'createdAt') {
  return rows.filter(r => {
    if (!from && !to) return true;
    const raw = r[field]; if (!raw) return false;
    const d = new Date(String(raw)); if (Number.isNaN(d.getTime())) return false;
    const day = [d.getFullYear(), String(d.getMonth() + 1).padStart(2, '0'), String(d.getDate()).padStart(2, '0')].join('-');
    return (!from || day >= from) && (!to || day <= to);
  });
}
export function csvText(rows: Row[], columns: { key: string; label: string }[]) {
  const cell = (v: unknown) => {
    let s = typeof v === 'object' && v !== null ? JSON.stringify(v) : String(v ?? '');
    if (/^[=+@\-\t\r]/.test(s)) s = "'" + s;
    return '"' + s.replaceAll('"', '""') + '"';
  };
  return '\uFEFF' + [columns.map(c => cell(c.label)).join(','), ...rows.map(r => columns.map(c => cell(r[c.key])).join(','))].join('\r\n');
}
export function downloadCsv(name: string, rows: Row[], columns: { key: string; label: string }[]) {
  const url = URL.createObjectURL(new Blob([csvText(rows, columns)], { type: 'text/csv;charset=utf-8;' }));
  const a = document.createElement('a'); a.href = url; a.download = `${name}.csv`; a.click(); setTimeout(() => URL.revokeObjectURL(url), 1000);
}
