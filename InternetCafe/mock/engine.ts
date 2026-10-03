import { createSeed } from './seed';
import { clone, resources, roles, vietnamDate, type ApiResponse, type AuthUser, type EngineOptions, type Resource, type Role, type Row, type Workspace } from './types';
export { createSeed } from './seed';
export type { ApiResponse, AuthUser, EngineOptions, Resource, Role, Row, Workspace } from './types';

const managers: Role[] = ['Owner', 'Manager'];
const operators: Role[] = ['Owner', 'Manager', 'Cashier'];
const readable: Record<Role, readonly Resource[]> = {
  Admin: ['accounts', 'products', 'categories', 'suppliers'],
  Owner: resources.filter(x => x !== 'accounts'), Manager: resources.filter(x => x !== 'accounts'),
  Cashier: ['computers', 'customers', 'products', 'categories', 'orders', 'topups', 'transactions', 'sessions'],
  Staff: ['employees', 'shifts', 'schedules', 'leaves', 'attendance', 'payroll'],
  Customer: ['computers', 'customers', 'products', 'categories', 'orders', 'topups', 'transactions', 'feedback', 'surveys', 'sessions'],
};
const editable: Partial<Record<Resource, string[]>> = {
  computers: ['name', 'zone', 'hourlyRate'], customers: ['fullName', 'phone', 'email', 'dateOfBirth', 'hobbies', 'tier', 'status'],
  categories: ['name'], products: ['name', 'categoryId', 'price', 'active', 'imageUrl'],
  suppliers: ['name', 'phone', 'email', 'address', 'active'], feedback: ['status', 'response'],
  surveys: ['title', 'description', 'questions', 'status'], employees: ['fullName', 'phone', 'email', 'department', 'position', 'status', 'baseSalary', 'joinedAt', 'qualification'],
  shifts: ['name', 'startTime', 'endTime'], schedules: ['employeeId', 'shiftId', 'date', 'status'],
  leaves: ['type', 'startDate', 'endDate', 'reason'], payroll: ['employeeId', 'month', 'baseSalary', 'bonus', 'deduction', 'status'],
  accounts: ['fullName', 'role', 'status', 'employeeId'],
};
const enumFields: Record<string, string[]> = {
  'computers.zone': ['Standard', 'VIP'], 'customers.tier': ['Silver', 'Gold', 'Diamond'],
  'customers.status': ['Active', 'Banned', 'Inactive'], 'employees.status': ['Active', 'Inactive'],
  'accounts.status': ['Active', 'Banned'], 'accounts.role': [...roles],
  'feedback.status': ['Pending', 'Processing', 'Resolved'], 'surveys.status': ['Draft', 'Published', 'Closed'],
  'schedules.status': ['Scheduled', 'Completed', 'Absent', 'OnLeave'], 'leaves.type': ['Annual', 'Sick', 'Resignation'],
  'payroll.status': ['Draft', 'Approved', 'Paid'],
};
class ApiFault extends Error {
  constructor(public status: number, public code: string, message: string, public fields?: Record<string, string>) { super(message); }
}
function fail(status: number, code: string, message: string, fields?: Record<string, string>): never { throw new ApiFault(status, code, message, fields); }
function requireValue(condition: unknown, message: string, field?: string): asserts condition { if (!condition) fail(422, 'VALIDATION_ERROR', message, field ? { [field]: message } : undefined); }
function allow(role: Role, allowed: readonly Role[]) { if (!allowed.includes(role)) fail(403, 'FORBIDDEN', 'Bạn không có quyền thực hiện thao tác này.'); }
function object(value: unknown): Record<string, any> { requireValue(value !== null && typeof value === 'object' && !Array.isArray(value), 'Nội dung phải là một JSON object.'); return value as Record<string, any>; }
function whitelist(value: Record<string, any>, keys: string[]) {
  const unexpected = Object.keys(value).filter(key => !keys.includes(key));
  requireValue(!unexpected.length, `Trường không được phép: ${unexpected.join(', ')}.`, unexpected[0]);
}
function text(value: unknown, field: string, max = 200, minimum = 1): string {
  requireValue(typeof value === 'string' && value.trim().length >= minimum && value.trim().length <= max, `${field} phải có ${minimum}–${max} ký tự.`, field); return value.trim();
}
function integer(value: unknown, field: string, min = 0, max = 1_000_000_000): number {
  requireValue(Number.isSafeInteger(value) && (value as number) >= min && (value as number) <= max, `${field} phải là số nguyên từ ${min} đến ${max}.`, field); return value as number;
}
function date(value: unknown, field: string): string {
  const s = text(value, field, 10); requireValue(/^\d{4}-\d{2}-\d{2}$/.test(s) && !Number.isNaN(Date.parse(s)) && new Date(s).toISOString().slice(0, 10) === s, `${field} phải là ngày YYYY-MM-DD hợp lệ.`, field); return s;
}
function enumValue(value: unknown, field: string, allowed: readonly string[]): string { requireValue(typeof value === 'string' && allowed.includes(value), `${field}: chọn ${allowed.join(', ')}.`, field); return value; }
function canonical(value: unknown): string {
  if (Array.isArray(value)) return `[${value.map(canonical).join(',')}]`;
  if (value && typeof value === 'object') return `{${Object.entries(value).sort(([a], [b]) => a.localeCompare(b)).map(([key, item]) => `${JSON.stringify(key)}:${canonical(item)}`).join(',')}}`;
  return JSON.stringify(value);
}

/** In-memory FE simulator. It is deliberately not a production authentication or payment service. */
export function createMockEngine(options: EngineOptions = {}) {
  const now = options.now ?? (() => new Date());
  const state = clone(options.seed ?? createSeed(now()));
  const credentials = new Map<string, string>([...state.accounts, ...state.customers].map(row => [row.username.toLowerCase(), 'Demo@123']));
  const tokens = new Map<string, { username: string }>();
  const idempotency = new Map<string, { fingerprint: string; response: ApiResponse }>();
  let sequence = 0;
  const id = (prefix: string) => `${prefix}-${now().getTime().toString(36)}-${++sequence}`;
  const stamp = () => now().toISOString();
  const find = (resource: Resource, rowId: string): Row => state[resource].find(row => row.id === rowId) ?? fail(404, 'NOT_FOUND', 'Không tìm thấy dữ liệu.');
  const ok = (data: unknown, status = 200): ApiResponse => ({ status, body: { data: clone(data) } });
  function identity(username: string): AuthUser {
    const account = state.accounts.find(row => row.username.toLowerCase() === username.toLowerCase());
    const customer = state.customers.find(row => row.username.toLowerCase() === username.toLowerCase());
    const person = account ?? customer;
    if (!person) fail(401, 'UNAUTHENTICATED', 'Phiên đăng nhập không còn hợp lệ.');
    if (person.status !== 'Active') fail(403, 'ACCOUNT_BANNED', 'Tài khoản đã bị khóa hoặc ngừng hoạt động.');
    if (account?.employeeId && find('employees', account.employeeId).status !== 'Active') fail(403, 'ACCOUNT_BANNED', 'Hồ sơ nhân viên đã ngừng hoạt động.');
    const role: Role = account?.role ?? 'Customer';
    return { id: account?.employeeId ?? person.id, username: person.username, fullName: person.fullName, role, permissions: [...new Set([...readable[role].map(resource => `${resource}:read`), ...(managers.includes(role) ? ['management:write', 'reports:read'] : []), ...(role === 'Admin' ? ['accounts:write'] : []), ...(operators.includes(role) ? ['operations:write', 'reports:read'] : [])])] };
  }
  function auth(token?: string): AuthUser {
    const entry = tokens.get((token ?? '').replace(/^Bearer\s+/i, ''));
    if (!entry) fail(401, 'UNAUTHENTICATED', 'Vui lòng đăng nhập lại.');
    return identity(entry.username);
  }
  function view(user: AuthUser, resource: Resource): Row[] {
    if (!readable[user.role].includes(resource)) return [];
    let rows = state[resource];
    if (user.role === 'Staff' && resource !== 'shifts') rows = rows.filter(row => resource === 'employees' ? row.id === user.id : row.employeeId === user.id);
    if (user.role === 'Customer') {
      if (resource === 'customers') rows = rows.filter(row => row.id === user.id);
      else if (resource === 'products') rows = rows.filter(row => row.active);
      else if (resource === 'surveys') rows = rows.filter(row => row.status !== 'Draft' && row.customerIds.includes(user.id)).map(row => ({ ...row, customerIds: [user.id], responses: row.responses.filter((response: any) => response.customerId === user.id) }));
      else if (resource === 'computers') rows = rows.map(row => { const safe = { ...row }; if (safe.customerId !== user.id) { delete safe.customerId; delete safe.sessionId; } return safe; });
      else if (!['categories'].includes(resource)) rows = rows.filter(row => row.customerId === user.id);
    }
    return clone(rows);
  }
  function visible(user: AuthUser, resource: Resource, rowId: string): Row {
    if (!readable[user.role].includes(resource)) fail(403, 'FORBIDDEN', 'Bạn không có quyền truy cập dữ liệu này.');
    return view(user, resource).find(row => row.id === rowId) ?? fail(404, 'NOT_FOUND', 'Không tìm thấy dữ liệu.');
  }
  function uniqueUsername(username: string) { if ([...state.accounts, ...state.customers].some(row => row.username.toLowerCase() === username.toLowerCase())) fail(409, 'USERNAME_EXISTS', 'Tên đăng nhập đã tồn tại.'); }
  function profile(values: Record<string, any>) {
    const result = { ...values };
    if ('fullName' in result) result.fullName = text(result.fullName, 'fullName', 120);
    if ('phone' in result) requireValue(typeof result.phone === 'string' && /^\+?[0-9 ()-]{9,20}$/.test(result.phone), 'Số điện thoại không hợp lệ.', 'phone');
    if ('email' in result) requireValue(typeof result.email === 'string' && /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(result.email) && result.email.length <= 200, 'Email không hợp lệ.', 'email');
    if ('dateOfBirth' in result) { date(result.dateOfBirth, 'dateOfBirth'); requireValue(result.dateOfBirth <= vietnamDate(now()) && result.dateOfBirth >= '1900-01-01', 'Ngày sinh không hợp lệ.', 'dateOfBirth'); }
    if ('hobbies' in result) result.hobbies = text(result.hobbies, 'hobbies', 500, 0);
    return result;
  }
  function transaction(customerId: string, type: string, amount: number, referenceId: string) {
    requireValue(Number.isSafeInteger(amount) && amount >= 0, 'Số tiền giao dịch không hợp lệ.');
    if (state.transactions.some(row => row.referenceId === referenceId && row.type === type)) fail(409, 'DUPLICATE_TRANSACTION', 'Giao dịch đã được xử lý.');
    state.transactions.unshift({ id: id('tx'), customerId, type, amount, referenceId, createdAt: stamp() });
  }
  function endSession(session: Row): ApiResponse {
    if (session.status === 'Completed') return ok(session);
    const customer = find('customers', session.customerId), computer = find('computers', session.computerId);
    // Price is frozen at start in the private map; restored demo snapshots use current machine rate.
    const rate = sessionRates.get(session.id) ?? computer.hourlyRate;
    const elapsedMs = Math.max(0, now().getTime() - Date.parse(session.startTime));
    const amount = Math.min(customer.balance, Math.ceil(elapsedMs * rate / 3_600_000));
    transaction(customer.id, 'Rental', amount, session.id);
    customer.balance -= amount; session.amount = amount; session.status = 'Completed'; session.endTime = stamp();
    computer.status = 'Available'; delete computer.customerId; delete computer.sessionId;
    return ok(session);
  }
  const sessionRates = new Map<string, number>();
  function list(user: AuthUser, resource: Resource, query: URLSearchParams): ApiResponse {
    if (!readable[user.role].includes(resource)) fail(403, 'FORBIDDEN', 'Bạn không có quyền truy cập phân hệ này.');
    const page = integer(Number(query.get('page') ?? 1), 'page', 1, 1_000_000);
    const pageSize = integer(Number(query.get('pageSize') ?? 20), 'pageSize', 1, 100);
    const from = query.get('from'), to = query.get('to');
    if (from) date(from, 'from'); if (to) date(to, 'to');
    requireValue(!from || !to || from <= to, 'Ngày bắt đầu phải trước ngày kết thúc.', 'from');
    let rows = view(user, resource);
    const search = (query.get('search') ?? '').trim().toLocaleLowerCase('vi');
    if (search) rows = rows.filter(row => JSON.stringify(row).toLocaleLowerCase('vi').includes(search));
    const status = query.get('status'); if (status) rows = rows.filter(row => row.status === status);
    if (from || to) rows = rows.filter(row => {
      const raw = row.createdAt ?? row.date ?? row.startDate ?? row.checkIn ?? row.startTime ?? row.joinedAt ?? (row.month ? `${row.month}-01` : undefined);
      if (!raw) return false;
      const d = raw.includes('T') ? vietnamDate(new Date(raw)) : raw;
      return (!from || d >= from) && (!to || d <= to);
    });
    const sort = query.get('sort'), order = query.get('order') ?? 'asc'; enumValue(order, 'order', ['asc', 'desc']);
    if (sort) {
      requireValue(/^[a-zA-Z][a-zA-Z0-9]*$/.test(sort) && !['constructor', 'prototype', '__proto__'].includes(sort), 'Trường sắp xếp không hợp lệ.', 'sort');
      rows.sort((a, b) => (typeof a[sort] === 'number' && typeof b[sort] === 'number' ? a[sort] - b[sort] : String(a[sort] ?? '').localeCompare(String(b[sort] ?? ''), 'vi')) * (order === 'asc' ? 1 : -1));
    }
    return { status: 200, body: { data: rows.slice((page - 1) * pageSize, page * pageSize), meta: { total: rows.length, page, pageSize } } };
  }
  function validate(resource: Resource, values: Record<string, any>, old?: Row): Record<string, any> {
    const result = profile(values);
    for (const [key, value] of Object.entries(result)) {
      if (enumFields[`${resource}.${key}`]) enumValue(value, key, enumFields[`${resource}.${key}`]);
      if (['price', 'hourlyRate', 'baseSalary', 'bonus', 'deduction'].includes(key)) integer(value, key);
      if (['active', 'online'].includes(key)) requireValue(typeof value === 'boolean', `${key} phải là boolean.`, key);
      if (['name', 'title', 'department', 'position', 'qualification', 'address', 'reason'].includes(key)) result[key] = text(value, key, key === 'reason' ? 1000 : 200);
      if (['description', 'response', 'note'].includes(key)) result[key] = text(value, key, 2000, 0);
      if (key === 'imageUrl') requireValue(typeof value === 'string' && (value === '' || /^https?:\/\//.test(value)), 'Ảnh phải dùng URL http(s).', key);
      if (['date', 'startDate', 'endDate', 'joinedAt'].includes(key)) date(value, key);
      if (key === 'month') requireValue(typeof value === 'string' && /^\d{4}-(0[1-9]|1[0-2])$/.test(value), 'Tháng phải có định dạng YYYY-MM.', key);
      if (key === 'categoryId') find('categories', value);
      if (key === 'employeeId' && value) find('employees', value);
      if (key === 'shiftId') find('shifts', value);
      if (['startTime', 'endTime'].includes(key) && resource === 'shifts') requireValue(typeof value === 'string' && /^([01]\d|2[0-3]):[0-5]\d$/.test(value), 'Giờ phải có định dạng HH:mm.', key);
    }
    const merged = { ...old, ...result };
    if (resource === 'accounts') {
      requireValue(merged.role !== 'Customer', 'Customer cần hồ sơ khách hàng, không phải tài khoản nhân viên.', 'role');
      if (merged.role === 'Staff') { requireValue(typeof merged.employeeId === 'string' && merged.employeeId.length > 0, 'Staff cần employeeId.', 'employeeId'); find('employees', merged.employeeId); }
    }
    if (resource === 'shifts') requireValue(merged.startTime !== merged.endTime, 'Giờ bắt đầu và kết thúc phải khác nhau.', 'endTime');
    if (resource === 'leaves') requireValue(merged.startDate <= merged.endDate, 'Ngày kết thúc phải từ ngày bắt đầu trở đi.', 'endDate');
    if (resource === 'payroll') {
      result.total = Math.max(0, merged.baseSalary + merged.bonus - merged.deduction);
      requireValue(Number.isSafeInteger(result.total), 'Tổng lương vượt giới hạn.');
      if (state.payroll.some(row => row.id !== old?.id && row.employeeId === merged.employeeId && row.month === merged.month)) fail(409, 'PAYROLL_EXISTS', 'Bảng lương nhân viên trong tháng đã tồn tại.');
    }
    if (resource === 'schedules') {
      if (state.schedules.some(row => row.id !== old?.id && row.employeeId === merged.employeeId && row.date === merged.date && row.shiftId === merged.shiftId)) fail(409, 'SCHEDULE_EXISTS', 'Nhân viên đã được phân ca này.');
      if (state.leaves.some(row => row.employeeId === merged.employeeId && row.status === 'Approved' && row.startDate <= merged.date && row.endDate >= merged.date)) fail(409, 'EMPLOYEE_ON_LEAVE', 'Nhân viên đang nghỉ phép trong ngày này.');
    }
    if (resource === 'surveys' && 'questions' in result) {
      requireValue(Array.isArray(result.questions) && result.questions.length > 0 && result.questions.length <= 20, 'Khảo sát cần 1–20 câu hỏi.', 'questions');
      const seen = new Set<string>();
      result.questions = result.questions.map((question: unknown) => {
        const q = object(question); whitelist(q, ['id', 'text', 'options']);
        const qid = text(q.id, 'question.id', 50); requireValue(!seen.has(qid) && !['__proto__', 'constructor', 'prototype'].includes(qid), 'ID câu hỏi bị trùng hoặc không hợp lệ.', 'questions'); seen.add(qid);
        requireValue(Array.isArray(q.options) && q.options.length >= 2 && q.options.length <= 10, 'Mỗi câu hỏi cần 2–10 lựa chọn.', 'questions');
        const opts = q.options.map((option: unknown) => text(option, 'option', 200)); requireValue(new Set(opts).size === opts.length, 'Lựa chọn không được trùng.');
        return { id: qid, text: text(q.text, 'question.text', 500), options: opts };
      });
    }
    return result;
  }
  function mutate(user: AuthUser, method: string, resource: Resource, rowId: string | undefined, input: Record<string, any>): ApiResponse {
    const ownLeave = resource === 'leaves' && user.role === 'Staff';
    const ownProfile = resource === 'employees' && user.role === 'Staff' && method === 'PATCH' && rowId === user.id;
    if (resource === 'accounts') allow(user.role, ['Admin']);
    else if (ownLeave || ownProfile) { /* scoped below */ }
    else if (resource === 'customers' && method !== 'DELETE') allow(user.role, operators);
    else allow(user.role, managers);
    if (['transactions', 'sessions', 'orders', 'topups', 'attendance'].includes(resource)) fail(405, 'METHOD_NOT_ALLOWED', 'Dùng endpoint hành động nghiệp vụ; dữ liệu này không hỗ trợ CRUD trực tiếp.');
    const row = rowId ? find(resource, rowId) : undefined;
    if (ownLeave && row && row.employeeId !== user.id) fail(404, 'NOT_FOUND', 'Không tìm thấy đơn nghỉ.');
    if (resource === 'leaves' && row?.status !== 'Pending' && row) fail(409, 'LEAVE_FINAL', 'Đơn đã xử lý không được thay đổi.');
    if (method === 'DELETE') {
      whitelist(input, []); if (!row) fail(404, 'NOT_FOUND', 'Không tìm thấy dữ liệu.');
      if (['inventory', 'feedback', 'payroll'].includes(resource)) fail(405, 'METHOD_NOT_ALLOWED', 'Lịch sử nghiệp vụ được giữ lại.');
      if (resource === 'customers') {
        if (state.sessions.some(s => s.customerId === row.id && s.status === 'Active')) fail(409, 'ACTIVE_SESSION', 'Khách đang sử dụng máy.');
        row.status = 'Inactive'; return ok(row);
      }
      if (resource === 'accounts') {
        if (row.username === user.username) fail(409, 'SELF_LOCK', 'Không thể khóa tài khoản đang đăng nhập.'); row.status = 'Banned'; return ok(row);
      }
      if (resource === 'employees') { row.status = 'Inactive'; return ok(row); }
      if (resource === 'computers' && state.sessions.some(s => s.computerId === row.id)) fail(409, 'HAS_HISTORY', 'Máy có lịch sử phiên; hãy chuyển bảo trì.');
      if (resource === 'categories' && state.products.some(p => p.categoryId === row.id)) fail(409, 'IN_USE', 'Danh mục đang có sản phẩm.');
      if (resource === 'products') { row.active = false; return ok(row); }
      if (resource === 'suppliers') { row.active = false; return ok(row); }
      if (resource === 'shifts' && state.schedules.some(s => s.shiftId === row.id)) fail(409, 'IN_USE', 'Ca đang có lịch phân công.');
      if (resource === 'schedules' && state.attendance.some(a => a.scheduleId === row.id)) fail(409, 'HAS_HISTORY', 'Lịch đã có chấm công.');
      if (resource === 'surveys' && row.status !== 'Draft') fail(409, 'SURVEY_PUBLISHED', 'Chỉ xóa khảo sát nháp.');
      state[resource] = state[resource].filter(item => item.id !== row.id); return ok({ id: row.id, deleted: true });
    }
    if (method === 'PATCH') {
      if (!row) fail(404, 'NOT_FOUND', 'Không tìm thấy dữ liệu.');
      if (!editable[resource]) fail(405, 'METHOD_NOT_ALLOWED', 'Dữ liệu không được sửa trực tiếp.');
      const fields = ownProfile ? ['fullName', 'phone', 'email', 'qualification'] : editable[resource]!;
      whitelist(input, fields); requireValue(Object.keys(input).length > 0, 'Cần ít nhất một trường để cập nhật.');
      if (resource === 'customers' && user.role === 'Cashier' && ('status' in input || 'tier' in input)) fail(403, 'FORBIDDEN', 'Chỉ quản lý được đổi trạng thái/hạng khách.');
      if (resource === 'customers' && input.status && input.status !== 'Active' && state.sessions.some(s => s.customerId === row.id && s.status === 'Active')) fail(409, 'ACTIVE_SESSION', 'Kết thúc phiên trước khi khóa khách.');
      if (resource === 'accounts' && row.username === user.username && (input.status === 'Banned' || (input.role && input.role !== 'Admin'))) fail(409, 'SELF_LOCK', 'Không thể tự hạ quyền/khóa tài khoản quản trị.');
      if (resource === 'payroll' && row.status === 'Paid') fail(409, 'PAYROLL_FINAL', 'Bảng lương đã trả không được sửa.');
      if (resource === 'schedules' && state.attendance.some(a => a.scheduleId === row.id) && Object.keys(input).some(key => key !== 'status')) fail(409, 'HAS_HISTORY', 'Lịch đã có chấm công không được đổi nhân viên, ca hoặc ngày.');
      if (resource === 'payroll' && input.status && input.status !== row.status && !((row.status === 'Draft' && input.status === 'Approved') || (row.status === 'Approved' && input.status === 'Paid'))) fail(409, 'INVALID_TRANSITION', 'Chuyển trạng thái lương không hợp lệ.');
      if (resource === 'surveys') {
        if (row.status !== 'Draft' && Object.keys(input).some(key => key !== 'status')) fail(409, 'SURVEY_PUBLISHED', 'Khảo sát đã phát không được sửa câu hỏi.');
        if ('status' in input && !(row.status === 'Published' && input.status === 'Closed')) fail(409, 'INVALID_TRANSITION', 'Dùng hành động publish để phát khảo sát.');
      }
      const values = validate(resource, input, row); Object.assign(row, values); return ok(row);
    }
    if (method !== 'POST') fail(405, 'METHOD_NOT_ALLOWED', 'Phương thức không hỗ trợ.');
    if (rowId) fail(405, 'METHOD_NOT_ALLOWED', 'Tạo dữ liệu tại endpoint collection, không kèm ID.');
    if (resource === 'inventory') {
      whitelist(input, ['productId', 'supplierId', 'type', 'quantity', 'note']);
      const product = find('products', text(input.productId, 'productId')); const type = enumValue(input.type, 'type', ['Import', 'Export']);
      const quantity = integer(input.quantity, 'quantity', 1, 100000); if (input.supplierId) find('suppliers', input.supplierId);
      const note = text(input.note ?? '', 'note', 2000, 0);
      if (type === 'Export' && product.stock < quantity) fail(409, 'OUT_OF_STOCK', 'Số lượng xuất vượt tồn kho.');
      requireValue(Number.isSafeInteger(product.stock + quantity), 'Tồn kho vượt giới hạn.');
      product.stock += type === 'Import' ? quantity : -quantity;
      const created = { id: id('inv'), productId: product.id, ...(input.supplierId ? { supplierId: input.supplierId } : {}), type, quantity, note, createdAt: stamp() }; state.inventory.unshift(created); return ok(created, 201);
    }
    const additional: Partial<Record<Resource, string[]>> = { customers: ['username', 'password'], accounts: ['username', 'password'], products: ['stock'], leaves: ['employeeId'] };
    if (!editable[resource] || resource === 'feedback') fail(405, 'METHOD_NOT_ALLOWED', 'Không hỗ trợ tạo dữ liệu trực tiếp.');
    whitelist(input, [...editable[resource]!, ...(additional[resource] ?? [])]);
    const required: Partial<Record<Resource, string[]>> = {
      computers: ['name', 'zone', 'hourlyRate'], customers: ['username', 'fullName', 'phone', 'email', 'dateOfBirth'], categories: ['name'], products: ['name', 'categoryId', 'price'],
      suppliers: ['name', 'phone', 'email', 'address'], surveys: ['title', 'questions'], employees: ['fullName', 'phone', 'email', 'department', 'position', 'baseSalary', 'joinedAt', 'qualification'],
      shifts: ['name', 'startTime', 'endTime'], schedules: ['employeeId', 'shiftId', 'date'], leaves: ['type', 'startDate', 'endDate', 'reason'], payroll: ['employeeId', 'month', 'baseSalary'], accounts: ['username', 'fullName', 'role'],
    };
    for (const key of required[resource] ?? []) requireValue(input[key] !== undefined && input[key] !== '', `${key} là bắt buộc.`, key);
    const defaults: Partial<Record<Resource, object>> = { computers: { status: 'Available', online: true }, customers: { hobbies: '', tier: 'Silver', balance: 0, status: 'Active' }, products: { active: true, stock: 0, imageUrl: '' }, suppliers: { active: true }, surveys: { description: '', status: 'Draft', customerIds: [], responses: [] }, employees: { status: 'Active' }, schedules: { status: 'Scheduled' }, leaves: { status: 'Pending', employeeId: user.id }, payroll: { bonus: 0, deduction: 0, status: 'Draft' }, accounts: { status: 'Active' } };
    const values: Record<string, any> = { ...defaults[resource], ...input };
    if (resource === 'surveys') requireValue(values.status === 'Draft', 'Khảo sát mới phải là bản nháp.', 'status');
    if (resource === 'payroll') requireValue(values.status === 'Draft', 'Bảng lương mới phải là Draft.', 'status');
    if (resource === 'products') integer(values.stock, 'stock', 0, 100000);
    if (resource === 'leaves') { if (ownLeave && input.employeeId && input.employeeId !== user.id) fail(403, 'FORBIDDEN', 'Chỉ tạo đơn nghỉ của chính mình.'); find('employees', values.employeeId); }
    if (resource === 'accounts' || resource === 'customers') {
      values.username = text(values.username, 'username', 50).toLowerCase(); requireValue(/^[a-z0-9_.-]{3,50}$/.test(values.username), 'Tên đăng nhập gồm 3–50 ký tự chữ/số/_.-.', 'username'); uniqueUsername(values.username);
      if (resource === 'accounts' && values.role === 'Customer') fail(422, 'VALIDATION_ERROR', 'Tạo khách qua customers hoặc đăng ký.', { role: 'Customer cần hồ sơ khách hàng.' });
      if (resource === 'accounts' && values.role === 'Staff') { requireValue(values.employeeId, 'Staff cần employeeId.', 'employeeId'); find('employees', values.employeeId); }
      if (values.password !== undefined) text(values.password, 'password', 100, 8);
    }
    const password = values.password ?? 'Demo@123'; delete values.password;
    const created = { id: id(resource.slice(0, -1)), ...validate(resource, values) } as Row;
    state[resource].unshift(created);
    if (resource === 'accounts' || resource === 'customers') credentials.set(created.username, password);
    return ok(created, 201);
  }

  function dispatch(method: string, url: URL, body: unknown, user?: AuthUser): ApiResponse {
    const path = url.pathname.replace(/^\/api\/v1/, '').replace(/\/$/, '') || '/';
    const input = method === 'GET' ? {} : object(body ?? {});
    if (path === '/auth/login' && method === 'POST') {
      whitelist(input, ['username', 'password']); const username = text(input.username, 'username', 50).toLowerCase(); text(input.password, 'password', 100);
      if (credentials.get(username) !== input.password) fail(401, 'INVALID_CREDENTIALS', 'Tên đăng nhập hoặc mật khẩu không đúng.');
      const found = identity(username); const accessToken = `demo_${globalThis.crypto?.randomUUID?.() ?? `${Math.random().toString(36).slice(2)}${++sequence}`}`;
      tokens.set(accessToken, { username }); return ok({ accessToken, user: found });
    }
    if (path === '/auth/register' && method === 'POST') {
      whitelist(input, ['username', 'password', 'fullName', 'phone', 'email', 'dateOfBirth', 'hobbies']); text(input.password, 'password', 100, 8);
      return mutate({ id: 'registration', username: '', fullName: '', role: 'Owner', permissions: [] }, 'POST', 'customers', undefined, input);
    }
    if (!user) fail(401, 'UNAUTHENTICATED', 'Vui lòng đăng nhập.');
    if (path === '/auth/logout' && method === 'POST') { whitelist(input, []); for (const [key, entry] of tokens) if (entry.username === user.username) tokens.delete(key); return ok({ loggedOut: true }); }
    if (path === '/me') {
      const own = user.role === 'Customer' ? find('customers', user.id) : user.role === 'Staff' ? find('employees', user.id) : user;
      if (method === 'GET') return ok(own);
      if (method === 'PATCH') { allow(user.role, ['Customer', 'Staff']); whitelist(input, user.role === 'Customer' ? ['fullName', 'phone', 'email', 'dateOfBirth', 'hobbies'] : ['fullName', 'phone', 'email', 'qualification']); requireValue(Object.keys(input).length > 0, 'Cần ít nhất một trường để cập nhật.'); Object.assign(own, validate(user.role === 'Customer' ? 'customers' : 'employees', input, own)); return ok(own); }
    }
    if (path === '/me/password' && method === 'POST') {
      whitelist(input, ['currentPassword', 'newPassword']); text(input.newPassword, 'newPassword', 100, 8);
      if (credentials.get(user.username.toLowerCase()) !== input.currentPassword) fail(422, 'INVALID_PASSWORD', 'Mật khẩu hiện tại không đúng.', { currentPassword: 'Mật khẩu không đúng.' });
      requireValue(input.currentPassword !== input.newPassword, 'Mật khẩu mới phải khác mật khẩu cũ.', 'newPassword'); credentials.set(user.username.toLowerCase(), input.newPassword); return ok({ changed: true });
    }
    if (path === '/workspace' && method === 'GET') return ok(Object.fromEntries(resources.map(resource => [resource, view(user!, resource)])));
    if (path === '/reports/summary' && method === 'GET') {
      allow(user.role, operators); const filtered = list(user, 'transactions', new URLSearchParams({ ...Object.fromEntries(url.searchParams), page: '1', pageSize: '100' }));
      // Aggregate the full filtered set, independently of the pagination limit.
      const from = url.searchParams.get('from'), to = url.searchParams.get('to');
      const rows = state.transactions.filter(row => { const d = vietnamDate(new Date(row.createdAt)); return (!from || d >= from) && (!to || d <= to); });
      const sum = (type: string) => rows.filter(row => row.type === type).reduce((total, row) => total + row.amount, 0);
      void filtered; return ok({ rentalRevenue: sum('Rental'), foodRevenue: sum('FoodOrder'), refunds: sum('Refund'), netRevenue: sum('Rental') + sum('FoodOrder') - sum('Refund'), topups: sum('TopUp'), transactionCount: rows.length, from: from ?? null, to: to ?? null, source: 'mock' });
    }
    if (path === '/sessions/start' && method === 'POST') {
      allow(user.role, ['Customer']); whitelist(input, ['computerId']); const computer = find('computers', text(input.computerId, 'computerId')); const customer = find('customers', user.id);
      if (state.sessions.some(s => s.customerId === user.id && s.status === 'Active')) fail(409, 'ACTIVE_SESSION', 'Bạn đã có một phiên đang sử dụng.');
      if (computer.status !== 'Available' || !computer.online) fail(409, 'COMPUTER_UNAVAILABLE', 'Máy chưa sẵn sàng hoặc đang ngoại tuyến.');
      if (customer.balance <= 0) fail(409, 'INSUFFICIENT_BALANCE', 'Vui lòng nạp tiền trước khi bắt đầu.');
      const session = { id: id('session'), customerId: user.id, computerId: computer.id, startTime: stamp(), startBalance: customer.balance, amount: 0, status: 'Active' };
      state.sessions.unshift(session); sessionRates.set(session.id, computer.hourlyRate); Object.assign(computer, { status: 'InUse', customerId: user.id, sessionId: session.id }); return ok(session, 201);
    }
    const parts = path.split('/').filter(Boolean); const [resource, rowId, action] = parts as [Resource, string | undefined, string | undefined];
    if (parts.length > 3 || (action && method !== 'POST')) fail(404, 'NOT_FOUND', 'Endpoint không tồn tại.');
    if (!resources.includes(resource)) fail(404, 'NOT_FOUND', 'Endpoint không tồn tại.');
    if (action && method === 'POST') {
      const row = find(resource, rowId!);
      if (resource === 'sessions' && action === 'end') { whitelist(input, []); if (user.role === 'Customer') { if (row.customerId !== user.id) fail(404, 'NOT_FOUND', 'Không tìm thấy phiên.'); } else allow(user.role, operators); return endSession(row); }
      if (resource === 'computers' && action === 'command') {
        allow(user.role, operators); whitelist(input, ['command']); enumValue(input.command, 'command', ['Maintenance', 'Available', 'EndSession']);
        if (input.command === 'EndSession') { const session = state.sessions.find(s => s.computerId === row.id && s.status === 'Active'); if (!session) fail(409, 'NO_ACTIVE_SESSION', 'Máy không có phiên đang chạy.'); return endSession(session); }
        if (state.sessions.some(s => s.computerId === row.id && s.status === 'Active')) fail(409, 'ACTIVE_SESSION', 'Kết thúc phiên trước khi đổi trạng thái máy.'); row.status = input.command; return ok(row);
      }
      if (resource === 'topups' && action === 'decision') {
        allow(user.role, operators); whitelist(input, ['status']); enumValue(input.status, 'status', ['Approved', 'Rejected']);
        if (row.status !== 'Pending') fail(409, 'ALREADY_PROCESSED', 'Yêu cầu nạp đã được xử lý.');
        if (input.status === 'Approved') { const customer = find('customers', row.customerId); requireValue(Number.isSafeInteger(customer.balance + row.amount), 'Số dư vượt giới hạn.'); transaction(customer.id, 'TopUp', row.amount, row.id); customer.balance += row.amount; }
        row.status = input.status; return ok(row);
      }
      if (resource === 'orders' && action === 'transition') {
        whitelist(input, ['status']); enumValue(input.status, 'status', ['Preparing', 'Served', 'Cancelled']);
        if (user.role === 'Customer') { if (row.customerId !== user.id) fail(404, 'NOT_FOUND', 'Không tìm thấy đơn.'); if (input.status !== 'Cancelled') fail(403, 'FORBIDDEN', 'Khách chỉ được hủy đơn đang chờ.'); } else allow(user.role, operators);
        const allowed = (row.status === 'Pending' && ['Preparing', 'Cancelled'].includes(input.status)) || (row.status === 'Preparing' && (input.status === 'Served' || (input.status === 'Cancelled' && managers.includes(user.role))));
        if (!allowed) fail(409, 'INVALID_TRANSITION', 'Không thể chuyển đơn từ trạng thái hiện tại.');
        if (input.status === 'Cancelled') {
          const customer = find('customers', row.customerId);
          requireValue(Number.isSafeInteger(customer.balance + row.total), 'Số dư vượt giới hạn.');
          const restock = row.items.map((item: any) => { const product = find('products', item.productId); requireValue(Number.isSafeInteger(product.stock + item.quantity), 'Tồn kho vượt giới hạn.'); return { product, quantity: item.quantity }; });
          transaction(customer.id, 'Refund', row.total, row.id); customer.balance += row.total;
          for (const item of restock) item.product.stock += item.quantity;
        }
        row.status = input.status; return ok(row);
      }
      if (resource === 'leaves' && action === 'decision') {
        allow(user.role, managers); whitelist(input, ['status']); enumValue(input.status, 'status', ['Approved', 'Rejected']); if (row.status !== 'Pending') fail(409, 'ALREADY_PROCESSED', 'Đơn đã được xử lý.');
        row.status = input.status; if (row.status === 'Approved') for (const schedule of state.schedules) if (schedule.employeeId === row.employeeId && schedule.date >= row.startDate && schedule.date <= row.endDate && schedule.status === 'Scheduled') schedule.status = 'OnLeave'; return ok(row);
      }
      if (resource === 'attendance' && action === 'check-out') { allow(user.role, ['Staff']); whitelist(input, []); if (row.employeeId !== user.id) fail(404, 'NOT_FOUND', 'Không tìm thấy chấm công.'); if (row.checkOut) fail(409, 'ALREADY_PROCESSED', 'Bạn đã chấm công ra.'); row.checkOut = stamp(); find('schedules', row.scheduleId).status = 'Completed'; return ok(row); }
      if (resource === 'surveys' && action === 'publish') {
        allow(user.role, managers); whitelist(input, ['customerIds']); if (row.status !== 'Draft') fail(409, 'SURVEY_PUBLISHED', 'Khảo sát đã được phát.'); requireValue(Array.isArray(input.customerIds) && input.customerIds.length > 0, 'Chọn ít nhất một khách hàng.', 'customerIds');
        const customerIds = [...new Set(input.customerIds)] as string[]; for (const customerId of customerIds) { const c = find('customers', customerId); requireValue(c.status === 'Active', 'Chỉ phát cho khách đang hoạt động.', 'customerIds'); }
        row.customerIds = customerIds; row.status = 'Published'; return ok(row);
      }
      if (resource === 'surveys' && action === 'responses') {
        allow(user.role, ['Customer']); whitelist(input, ['answers']); visible(user, 'surveys', row.id); if (row.status !== 'Published') fail(409, 'SURVEY_CLOSED', 'Khảo sát không nhận câu trả lời.');
        if (row.responses.some((response: any) => response.customerId === user.id)) fail(409, 'DUPLICATE_RESPONSE', 'Bạn đã trả lời khảo sát này.');
        const answers = object(input.answers); whitelist(answers, row.questions.map((q: any) => q.id)); for (const q of row.questions) enumValue(answers[q.id], q.id, q.options);
        const response = { customerId: user.id, answers: clone(answers), createdAt: stamp() }; row.responses.push(response); return ok(response, 201);
      }
      fail(404, 'NOT_FOUND', 'Hành động không tồn tại.');
    }
    if (resource === 'attendance' && rowId === 'check-in' && method === 'POST') {
      allow(user.role, ['Staff']); whitelist(input, ['scheduleId']); const schedule = find('schedules', text(input.scheduleId, 'scheduleId'));
      if (schedule.employeeId !== user.id) fail(404, 'NOT_FOUND', 'Không tìm thấy lịch của bạn.');
      if (state.attendance.some(row => row.scheduleId === schedule.id)) fail(409, 'DUPLICATE_CHECK_IN', 'Ca này đã chấm công vào.');
      if (schedule.date !== vietnamDate(now()) || schedule.status !== 'Scheduled') fail(409, 'OUTSIDE_SCHEDULE', 'Chỉ chấm công cho ca được phân trong hôm nay.');
      const attendance = { id: id('attendance'), employeeId: user.id, scheduleId: schedule.id, checkIn: stamp() }; state.attendance.unshift(attendance); return ok(attendance, 201);
    }
    if (rowId && method === 'GET' && !action) return ok(visible(user, resource, rowId));
    if (!rowId && method === 'GET') return list(user, resource, url.searchParams);
    if (!rowId && method === 'POST' && resource === 'topups') {
      allow(user.role, ['Customer']); whitelist(input, ['amount', 'note', 'idempotencyKey']); integer(input.amount, 'amount', 1000, 10_000_000); const note = text(input.note ?? '', 'note', 2000, 0);
      const topup = { id: id('topup'), customerId: user.id, amount: input.amount, note, status: 'Pending', createdAt: stamp() }; state.topups.unshift(topup); return ok(topup, 201);
    }
    if (!rowId && method === 'POST' && resource === 'orders') {
      allow(user.role, ['Customer']); whitelist(input, ['computerId', 'items', 'note', 'idempotencyKey', 'expectedTotal']);
      const customer = find('customers', user.id); const session = state.sessions.find(s => s.customerId === user.id && s.computerId === input.computerId && s.status === 'Active');
      if (!session) fail(409, 'NO_ACTIVE_SESSION', 'Bắt đầu phiên tại máy này trước khi đặt món.');
      requireValue(Array.isArray(input.items) && input.items.length >= 1 && input.items.length <= 30, 'Đơn hàng cần 1–30 món.', 'items');
      const seen = new Set<string>();
      const items = input.items.map((raw: unknown) => {
        const item = object(raw); whitelist(item, ['productId', 'quantity', 'expectedUnitPrice']); const product = find('products', text(item.productId, 'productId'));
        requireValue(!seen.has(product.id), 'Một sản phẩm chỉ được xuất hiện một dòng trong đơn.', 'items'); seen.add(product.id);
        integer(item.quantity, 'quantity', 1, 99); if (!product.active || product.stock < item.quantity) fail(409, 'OUT_OF_STOCK', `Món ${product.name} không đủ hàng.`);
        if (item.expectedUnitPrice !== undefined && item.expectedUnitPrice !== product.price) fail(409, 'PRICE_CHANGED', `Giá ${product.name} đã thay đổi. Vui lòng tải lại.`);
        return { productId: product.id, name: product.name, quantity: item.quantity, unitPrice: product.price };
      });
      const total = items.reduce((sum: number, item: any) => sum + item.unitPrice * item.quantity, 0); requireValue(Number.isSafeInteger(total), 'Tổng tiền vượt giới hạn.');
      if (input.expectedTotal !== undefined && input.expectedTotal !== total) fail(409, 'PRICE_CHANGED', 'Tổng tiền đã thay đổi. Vui lòng xác nhận giá mới.');
      if (customer.balance < total) fail(409, 'INSUFFICIENT_BALANCE', 'Số dư không đủ để đặt món.');
      const note = text(input.note ?? '', 'note', 2000, 0); const order = { id: id('order'), customerId: user.id, computerId: session.computerId, items, total, note, status: 'Pending', createdAt: stamp() };
      transaction(customer.id, 'FoodOrder', total, order.id); customer.balance -= total; for (const item of items) find('products', item.productId).stock -= item.quantity; state.orders.unshift(order); return ok(order, 201);
    }
    if (!rowId && method === 'POST' && resource === 'feedback') {
      allow(user.role, ['Customer']); whitelist(input, ['subject', 'content']); const feedback = { id: id('feedback'), customerId: user.id, subject: text(input.subject, 'subject', 200), content: text(input.content, 'content', 4000), status: 'Pending', response: '', createdAt: stamp() }; state.feedback.unshift(feedback); return ok(feedback, 201);
    }
    if (parts.length > 2) fail(404, 'NOT_FOUND', 'Endpoint không tồn tại.');
    return mutate(user, method, resource, rowId, input);
  }

  return {
    handle(method: string, path: string, body?: unknown, token?: string): ApiResponse {
      try {
        const verb = method.toUpperCase(); const url = new URL(path, 'http://mock.local');
        const publicAuth = /^\/api\/v1\/auth\/(login|register)\/?$/.test(url.pathname) || /^\/auth\/(login|register)\/?$/.test(url.pathname);
        const user = publicAuth ? undefined : auth(token);
        const data = body && typeof body === 'object' && !Array.isArray(body) ? body as Record<string, any> : {};
        const supportsKey = verb === 'POST' && /^\/(api\/v1\/)?(orders|topups)\/?$/.test(url.pathname);
        let requestKey: string | undefined, fingerprint: string | undefined;
        if (supportsKey && data.idempotencyKey !== undefined) {
          const key = text(data.idempotencyKey, 'idempotencyKey', 128, 8); requestKey = `${user?.id}:${url.pathname.replace(/^\/api\/v1/, '').replace(/\/$/, '')}:${key}`;
          fingerprint = canonical(data);
          const previous = idempotency.get(requestKey); if (previous) { if (previous.fingerprint !== fingerprint) fail(409, 'IDEMPOTENCY_CONFLICT', 'Khóa chống lặp đã được dùng cho nội dung khác.'); return clone(previous.response); }
        }
        const result = dispatch(verb, url, body, user);
        if (requestKey && result.status < 300) idempotency.set(requestKey, { fingerprint: fingerprint!, response: clone(result) });
        return result;
      } catch (error) {
        if (error instanceof ApiFault) return { status: error.status, body: { error: { code: error.code, message: error.message, ...(error.fields ? { fields: error.fields } : {}) } } };
        return { status: 500, body: { error: { code: 'MOCK_INTERNAL_ERROR', message: 'Mock gặp lỗi nội bộ. Vui lòng thử lại hoặc khởi động lại demo.' } } };
      }
    },
    snapshot(): Workspace { return clone(state); },
  };
}
