/** Run from apps/admin-web: npx tsx ../../contracts/generate-openapi.ts */
import { createRequire } from 'node:module';
import { resolve } from 'node:path';
import { writeFileSync } from 'node:fs';
import { createSeed } from '../mock/seed';
import { resources, roles } from '../mock/types';
const { stringify } = createRequire(resolve(process.cwd(), 'package.json'))('yaml');
type Schema = Record<string, any>;
const ref = (name: string) => ({ $ref: `#/components/schemas/${name}` });
const str = (maxLength = 200, minLength = 1): Schema => ({ type: 'string', minLength, maxLength });
const en = (...values: string[]): Schema => ({ type: 'string', enum: values });
const integer = (maximum = 1_000_000_000, minimum = 0): Schema => ({ type: 'integer', format: 'int64', minimum, maximum });
const array = (items: Schema, extra = {}): Schema => ({ type: 'array', items, ...extra });
const obj = (properties: Schema, required: string[] = Object.keys(properties), extra = {}): Schema => ({ type: 'object', additionalProperties: false, properties, ...(required.length ? { required } : {}), ...extra });
const id = str(200), money = { ...integer(Number.MAX_SAFE_INTEGER), description: 'Integer VND, server calculated for balances and totals.' }, inputMoney = integer();
const day = { type: 'string', format: 'date', example: '2026-10-03' }, stamp = { type: 'string', format: 'date-time', example: '2026-10-03T05:00:00.000Z' };
const phone = { ...str(20, 9), pattern: '^\\+?[0-9 ()-]{9,20}$' }, email = { ...str(200), format: 'email' };
const username = { ...str(50, 3), pattern: '^[a-z0-9_.-]{3,50}$', description: 'Case-insensitive unique across accounts and customers; normalized to lowercase.' };
const password = { ...str(100, 8), format: 'password', writeOnly: true };
const note = str(2000, 0), bool = { type: 'boolean' };
const schemas: Schema = {
  Role: en(...roles), Money: money,
  AuthUser: obj({ id, username: str(50), fullName: str(120), role: ref('Role'), permissions: array(str(100)) }),
  AuthSession: obj({ accessToken: str(200), user: ref('AuthUser') }),
  PageMeta: obj({ total: integer(Number.MAX_SAFE_INTEGER), page: integer(1_000_000, 1), pageSize: integer(100, 1) }),
  ApiError: obj({ error: obj({ code: str(100), message: { type: 'string' }, fields: { type: 'object', additionalProperties: { type: 'string' } } }, ['code', 'message']) }),
  Deleted: obj({ id, deleted: { type: 'boolean', enum: [true] } }),
  OrderItem: obj({ productId: id, name: str(), quantity: integer(99, 1), unitPrice: money }),
  SurveyQuestion: obj({ id: str(50), text: str(500), options: array(str(200), { minItems: 2, maxItems: 10, uniqueItems: true }) }),
  SurveyResponse: obj({ customerId: id, answers: { type: 'object', additionalProperties: { type: 'string' } }, createdAt: stamp }),
  Computer: obj({ id, name: str(), zone: en('Standard', 'VIP'), status: en('Available', 'InUse', 'Maintenance'), online: bool, hourlyRate: inputMoney, customerId: id, sessionId: id }, ['id', 'name', 'zone', 'status', 'online', 'hourlyRate']),
  Customer: obj({ id, username, fullName: str(120), phone, email, dateOfBirth: day, hobbies: str(500, 0), tier: en('Silver', 'Gold', 'Diamond'), balance: money, status: en('Active', 'Banned', 'Inactive') }),
  Category: obj({ id, name: str() }),
  Product: obj({ id, name: str(), categoryId: id, price: inputMoney, stock: integer(Number.MAX_SAFE_INTEGER), active: bool, imageUrl: { type: 'string', description: 'Empty string or HTTP(S) image URL.' } }),
  Order: obj({ id, customerId: id, computerId: id, items: array(ref('OrderItem'), { minItems: 1, maxItems: 30 }), total: money, status: en('Pending', 'Preparing', 'Served', 'Cancelled'), note, createdAt: stamp }),
  Topup: obj({ id, customerId: id, amount: integer(10_000_000, 1000), status: en('Pending', 'Approved', 'Rejected'), note, createdAt: stamp }),
  Transaction: obj({ id, customerId: id, type: en('TopUp', 'FoodOrder', 'Rental', 'Refund'), amount: money, createdAt: stamp, referenceId: id }),
  Supplier: obj({ id, name: str(), phone, email, address: str(), active: bool }),
  Inventory: obj({ id, productId: id, supplierId: id, type: en('Import', 'Export'), quantity: integer(100000, 1), note, createdAt: stamp }, ['id', 'productId', 'type', 'quantity', 'note', 'createdAt']),
  Feedback: obj({ id, customerId: id, subject: str(), content: str(4000), status: en('Pending', 'Processing', 'Resolved'), response: note, createdAt: stamp }),
  Survey: obj({ id, title: str(), description: note, questions: array(ref('SurveyQuestion'), { minItems: 1, maxItems: 20 }), customerIds: array(id, { uniqueItems: true }), status: en('Draft', 'Published', 'Closed'), responses: array(ref('SurveyResponse')) }),
  Employee: obj({ id, fullName: str(120), phone, email, department: str(), position: str(), status: en('Active', 'Inactive'), baseSalary: inputMoney, joinedAt: day, qualification: str() }),
  Shift: obj({ id, name: str(), startTime: { type: 'string', pattern: '^([01]\\d|2[0-3]):[0-5]\\d$' }, endTime: { type: 'string', pattern: '^([01]\\d|2[0-3]):[0-5]\\d$' } }),
  Schedule: obj({ id, employeeId: id, shiftId: id, date: day, status: en('Scheduled', 'Completed', 'Absent', 'OnLeave') }),
  Leave: obj({ id, employeeId: id, type: en('Annual', 'Sick', 'Resignation'), startDate: day, endDate: day, reason: str(1000), status: en('Pending', 'Approved', 'Rejected') }),
  Attendance: obj({ id, employeeId: id, scheduleId: id, checkIn: stamp, checkOut: stamp }, ['id', 'employeeId', 'scheduleId', 'checkIn']),
  Payroll: obj({ id, employeeId: id, month: { type: 'string', pattern: '^\\d{4}-(0[1-9]|1[0-2])$' }, baseSalary: inputMoney, bonus: inputMoney, deduction: inputMoney, total: money, status: en('Draft', 'Approved', 'Paid') }),
  Account: obj({ id, username, fullName: str(120), role: en('Admin', 'Owner', 'Manager', 'Cashier', 'Staff'), status: en('Active', 'Banned'), employeeId: id }, ['id', 'username', 'fullName', 'role', 'status']),
  Session: obj({ id, customerId: id, computerId: id, startTime: stamp, endTime: stamp, startBalance: money, amount: money, status: en('Active', 'Completed') }, ['id', 'customerId', 'computerId', 'startTime', 'startBalance', 'amount', 'status']),
  ReportSummary: obj({ rentalRevenue: money, foodRevenue: money, refunds: money, netRevenue: { type: 'integer', format: 'int64', description: 'Rental + food - refunds; may be negative for a date range with refunds of older sales.' }, topups: money, transactionCount: integer(Number.MAX_SAFE_INTEGER), from: { ...day, nullable: true }, to: { ...day, nullable: true }, source: en('mock') }),
};
const names: Record<string, string> = { computers: 'Computer', customers: 'Customer', categories: 'Category', products: 'Product', orders: 'Order', topups: 'Topup', transactions: 'Transaction', suppliers: 'Supplier', inventory: 'Inventory', feedback: 'Feedback', surveys: 'Survey', employees: 'Employee', shifts: 'Shift', schedules: 'Schedule', leaves: 'Leave', attendance: 'Attendance', payroll: 'Payroll', accounts: 'Account', sessions: 'Session' };
const seed = createSeed(new Date('2026-10-03T05:00:00.000Z'));
for (const resource of resources) schemas[names[resource]].example = seed[resource][0];
schemas.Workspace = obj(Object.fromEntries(resources.map(resource => [resource, array(ref(names[resource]))])), [...resources], { description: 'RBAC scoped, unpaginated bootstrap. Unreadable resources remain present as empty arrays. No passwords or tokens.' });
const M = ['Owner', 'Manager'], O = [...M, 'Cashier'];
const reads: Record<string, readonly string[]> = { Admin: ['accounts', 'products', 'categories', 'suppliers'], Owner: resources.filter(x => x !== 'accounts'), Manager: resources.filter(x => x !== 'accounts'), Cashier: ['computers', 'customers', 'products', 'categories', 'orders', 'topups', 'transactions', 'sessions'], Staff: ['employees', 'shifts', 'schedules', 'leaves', 'attendance', 'payroll'], Customer: ['computers', 'customers', 'products', 'categories', 'orders', 'topups', 'transactions', 'feedback', 'surveys', 'sessions'] };
const edit: Record<string, string[]> = { computers: ['name', 'zone', 'hourlyRate'], customers: ['fullName', 'phone', 'email', 'dateOfBirth', 'hobbies', 'tier', 'status'], categories: ['name'], products: ['name', 'categoryId', 'price', 'active', 'imageUrl'], suppliers: ['name', 'phone', 'email', 'address', 'active'], feedback: ['status', 'response'], surveys: ['title', 'description', 'questions', 'status'], employees: ['fullName', 'phone', 'email', 'department', 'position', 'status', 'baseSalary', 'joinedAt', 'qualification'], shifts: ['name', 'startTime', 'endTime'], schedules: ['employeeId', 'shiftId', 'date', 'status'], leaves: ['type', 'startDate', 'endDate', 'reason'], payroll: ['employeeId', 'month', 'baseSalary', 'bonus', 'deduction', 'status'], accounts: ['fullName', 'role', 'status', 'employeeId'] };
const required: Record<string, string[]> = { computers: ['name', 'zone', 'hourlyRate'], customers: ['username', 'fullName', 'phone', 'email', 'dateOfBirth'], categories: ['name'], products: ['name', 'categoryId', 'price'], suppliers: ['name', 'phone', 'email', 'address'], surveys: ['title', 'questions'], employees: ['fullName', 'phone', 'email', 'department', 'position', 'baseSalary', 'joinedAt', 'qualification'], shifts: ['name', 'startTime', 'endTime'], schedules: ['employeeId', 'shiftId', 'date'], leaves: ['type', 'startDate', 'endDate', 'reason'], payroll: ['employeeId', 'month', 'baseSalary'], accounts: ['username', 'fullName', 'role'] };
const extras: Record<string, Schema> = { customers: { username, password }, accounts: { username, password }, products: { stock: integer(100000) }, leaves: { employeeId: id } };
const pick = (name: string, fields: string[]) => Object.fromEntries(fields.map(key => [key, schemas[name].properties[key]]));
const scopes = 'Staff sees only rows tied to own employeeId (except all shifts). Customer sees own rows, targeted non-Draft surveys with only own answers, active products and all computers with other customer/session IDs removed. Other permitted roles see all rows.';
const mutations: Record<string, string> = {
  computers: 'PATCH cannot change status or online; use command for status. DELETE blocked if any session history exists.',
  customers: 'Cashier PATCH cannot change tier/status. Balance cannot be written. Active session blocks ban/deactivation. DELETE marks Inactive. New password defaults to Demo@123 if omitted in this mock.',
  categories: 'DELETE blocked while products reference the category.', products: 'PATCH cannot change stock. Use inventory adjustments. DELETE sets active=false. Creation accepts initial stock 0..100000.',
  suppliers: 'DELETE sets active=false.', feedback: 'Only managers may update response/status. No DELETE.',
  surveys: 'Create Draft only. Publish via action. After publishing only Published to Closed is permitted; questions/title cannot change. Delete Draft only.',
  employees: 'Staff PATCH own fullName/phone/email/qualification only. DELETE sets Inactive and blocks linked login.', shifts: 'Overnight shifts allowed; identical start/end rejected. Delete blocked if scheduled.',
  schedules: 'Duplicate employee/shift/date and schedules overlapping approved leave rejected. Attendance history blocks identity/date changes and deletion.',
  leaves: 'Staff create/update/delete own Pending leave only; managers may do this for employees. Manager creation must supply employeeId. endDate must be at least startDate.',
  payroll: 'Total=max(0,baseSalary+bonus-deduction) on server. Unique employee/month. Draft to Approved to Paid only; Paid immutable. No DELETE.',
  accounts: 'Admin only. Staff role requires employeeId. Customer role must use customer registration instead. Current admin cannot demote/ban itself. DELETE marks Banned. New password defaults to Demo@123 if omitted.',
};
const params: Schema = {
  Id: { name: 'id', in: 'path', required: true, schema: id, example: 'c-001' },
  Search: { name: 'search', in: 'query', description: 'Case-insensitive substring across JSON-visible row values; empty means all.', schema: { type: 'string' } },
  Status: { name: 'status', in: 'query', description: 'Exact row status; unsupported values return an empty list.', schema: { type: 'string' } },
  Sort: { name: 'sort', in: 'query', description: 'Top-level field; numeric or Vietnamese locale string sort. Unknown field leaves equivalent values in existing order.', schema: { type: 'string', pattern: '^[a-zA-Z][a-zA-Z0-9]*$' } },
  Order: { name: 'order', in: 'query', schema: { ...en('asc', 'desc'), default: 'asc' } },
  Page: { name: 'page', in: 'query', schema: { ...integer(1_000_000, 1), default: 1 } },
  PageSize: { name: 'pageSize', in: 'query', schema: { ...integer(100, 1), default: 20 } },
  From: { name: 'from', in: 'query', schema: day, description: 'Inclusive Asia/Ho_Chi_Minh date lower bound. from must be <= to.' },
  To: { name: 'to', in: 'query', schema: day, description: 'Inclusive Asia/Ho_Chi_Minh date upper bound. List timestamp priority: createdAt, date, startDate, checkIn, startTime, joinedAt, month day 01; rows without dates are excluded.' },
};
const parameter = (name: string) => ({ $ref: `#/components/parameters/${name}` });
const headers = { Date: { description: 'HTTP server date used to estimate client clock offset. Exposed through CORS in HTTP mock host.', schema: { type: 'string' }, example: 'Sat, 03 Oct 2026 05:00:00 GMT' } };
const errorExamples: Record<string, [string, string]> = { '400': ['INVALID_JSON', 'Request body is not valid JSON.'], '401': ['UNAUTHENTICATED', 'Vui lòng đăng nhập lại.'], '403': ['FORBIDDEN', 'Bạn không có quyền thực hiện thao tác này.'], '404': ['NOT_FOUND', 'Không tìm thấy dữ liệu.'], '405': ['METHOD_NOT_ALLOWED', 'Dùng endpoint hành động nghiệp vụ.'], '409': ['OUT_OF_STOCK', 'Món không đủ hàng.'], '413': ['PAYLOAD_TOO_LARGE', 'JSON body exceeds 1 MiB.'], '415': ['UNSUPPORTED_MEDIA_TYPE', 'Use Content-Type: application/json.'], '422': ['VALIDATION_ERROR', 'Dữ liệu không hợp lệ.'], '500': ['MOCK_INTERNAL_ERROR', 'Mock gặp lỗi nội bộ.'] };
const responses: Schema = Object.fromEntries(Object.entries(errorExamples).map(([code, [kind, message]]) => [`Error${code}`, {
  description: `${kind}; see error catalog in backend-gaps.md.`, headers,
  content: { 'application/json': { schema: ref('ApiError'), example: { error: { code: kind, message, ...(code === '422' ? { fields: { amount: 'amount phải là số nguyên từ 1000 đến 10000000.' } } : {}) } } } },
}]));
const paths: Schema = {};
function operation(path: string, method: string, title: string, allowed: readonly string[], result: Schema, options: { body?: Schema; example?: unknown; status?: number; description?: string; parameters?: Schema[]; public?: boolean; list?: boolean; successExample?: unknown } = {}) {
  const status = String(options.status ?? 200);
  const schema = obj({ data: result, ...(options.list ? { meta: ref('PageMeta') } : {}) });
  paths[path] ??= {};
  paths[path][method] = { tags: [path.split('/')[1]], operationId: `${method}_${path.replace(/[^a-zA-Z0-9]+/g, '_').replace(/^_|_$/g, '')}`, summary: title, description: options.description ?? title, 'x-roles': allowed, ...(options.public ? { security: [] } : {}), ...(path.includes('{id}') || options.parameters ? { parameters: [...(path.includes('{id}') ? [parameter('Id')] : []), ...(options.parameters ?? [])] } : {}), ...(options.body ? { requestBody: { required: true, content: { 'application/json': { schema: options.body, ...(options.example !== undefined ? { example: options.example } : {}) } } } } : {}), responses: { [status]: { description: status === '201' ? 'Created' : 'Success', headers, content: { 'application/json': { schema, ...(options.successExample !== undefined ? { example: { data: options.successExample } } : {}) } } }, ...Object.fromEntries(Object.keys(errorExamples).map(code => [code, { $ref: `#/components/responses/Error${code}` }])) } };
}
for (const resource of resources) {
  const name = names[resource], readRoles = roles.filter(role => reads[role].includes(resource));
  operation(`/${resource}`, 'get', `List ${resource}`, readRoles, array(ref(name)), { list: true, parameters: ['Search', 'Status', 'Sort', 'Order', 'Page', 'PageSize', 'From', 'To'].map(parameter), description: scopes });
  operation(`/${resource}/{id}`, 'get', `Read ${resource} detail`, readRoles, ref(name), { description: `${scopes} Out-of-scope ID returns 404.`, successExample: seed[resource][0] });
  if (!edit[resource]) continue;
  const writeRoles = resource === 'accounts' ? ['Admin'] : resource === 'customers' ? O : resource === 'leaves' || resource === 'employees' ? [...M, 'Staff'] : M;
  schemas[`${name}Patch`] = obj(pick(name, edit[resource]), [], { minProperties: 1 });
  operation(`/${resource}/{id}`, 'patch', `Update ${resource}`, writeRoles, ref(name), { body: ref(`${name}Patch`), description: mutations[resource], example: Object.fromEntries(edit[resource].slice(0, 1).map(key => [key, seed[resource][0][key]])) });
  if (resource !== 'feedback') {
    const createProperties = { ...pick(name, edit[resource]), ...extras[resource] };
    if (resource === 'surveys' || resource === 'payroll') createProperties.status = en('Draft');
    schemas[`${name}Create`] = obj(createProperties, required[resource]);
    operation(`/${resource}`, 'post', `Create ${resource}`, resource === 'employees' ? M : writeRoles, ref(name), { body: ref(`${name}Create`), status: 201, description: mutations[resource], example: Object.fromEntries(required[resource].map(key => [key, seed[resource][0][key]])) });
  }
  if (!['feedback', 'payroll'].includes(resource)) operation(`/${resource}/{id}`, 'delete', `Delete or deactivate ${resource}`, resource === 'accounts' ? ['Admin'] : resource === 'leaves' ? [...M, 'Staff'] : M, ref(['customers', 'accounts', 'employees', 'products', 'suppliers'].includes(resource) ? name : 'Deleted'), { description: mutations[resource] });
}
schemas.RegisterRequest = obj({ username, password, fullName: str(120), phone, email, dateOfBirth: day, hobbies: str(500, 0) }, ['username', 'password', 'fullName', 'phone', 'email', 'dateOfBirth']);
operation('/auth/login', 'post', 'Sign in with a demo identity', [], ref('AuthSession'), { public: true, body: obj({ username: str(50), password: { ...str(100), format: 'password', writeOnly: true } }), example: { username: 'gamer', password: 'Demo@123' }, successExample: { accessToken: 'demo_example', user: { id: 'c-001', username: 'gamer', fullName: 'Nguyễn Minh Huy', role: 'Customer', permissions: reads.Customer.map(x => `${x}:read`) } }, description: 'Demo users admin, owner, manager, cashier, staff, gamer share Demo@123. banned returns 403 ACCOUNT_BANNED. Wrong password returns 401 INVALID_CREDENTIALS. Token is opaque, not a JWT; mock tokens expire at host restart/logout.' });
operation('/auth/register', 'post', 'Register customer profile', [], ref('Customer'), { public: true, status: 201, body: ref('RegisterRequest'), example: { username: 'newgamer', password: 'Demo@123', fullName: 'Nguyễn Văn An', phone: '0987654321', email: 'an@example.test', dateOfBirth: '2003-01-01', hobbies: 'FPS' }, description: 'Returns Customer DTO, not AuthSession. Call login separately. Server initializes balance=0, tier=Silver, status=Active.' });
operation('/auth/logout', 'post', 'Revoke all mock tokens for current username', roles, obj({ loggedOut: bool }), { body: obj({}), example: {} });
operation('/me', 'get', 'Read current profile', roles, { oneOf: [ref('Customer'), ref('Employee'), ref('AuthUser')] }, { description: 'Customer receives Customer DTO (gamer -> c-001); Staff receives Employee DTO (staff -> e-001); other roles receive AuthUser.' });
schemas.CustomerProfilePatch = obj(pick('Customer', ['fullName', 'phone', 'email', 'dateOfBirth', 'hobbies']), [], { minProperties: 1 });
schemas.StaffProfilePatch = obj(pick('Employee', ['fullName', 'phone', 'email', 'qualification']), [], { minProperties: 1 });
operation('/me', 'patch', 'Update own profile', ['Customer', 'Staff'], { oneOf: [ref('Customer'), ref('Employee')] }, { body: { anyOf: [ref('CustomerProfilePatch'), ref('StaffProfilePatch')] }, example: { fullName: 'Nguyễn Minh Huy' }, description: 'Customer accepts fullName/phone/email/dateOfBirth/hobbies; Staff accepts fullName/phone/email/qualification. No balance, salary, role, tier or status writes.' });
operation('/me/password', 'post', 'Change own password', roles, obj({ changed: bool }), { body: obj({ currentPassword: { ...str(100), format: 'password', writeOnly: true }, newPassword: password }), example: { currentPassword: 'Demo@123', newPassword: 'Changed@123' }, description: 'Wrong current password -> 422 INVALID_PASSWORD. New password 8..100 characters and different. Other tokens remain valid in mock.' });
operation('/workspace', 'get', 'Read RBAC scoped bootstrap', roles, ref('Workspace'), { description: scopes });
operation('/reports/summary', 'get', 'Aggregate full filtered transaction ledger', O, ref('ReportSummary'), { parameters: [parameter('From'), parameter('To')], description: 'Includes every transaction in inclusive Vietnam date range, independent of list pagination. netRevenue=Rental+FoodOrder-Refund; TopUp is not sales. Mock values are operational demo figures, not audited accounting.' });
const key = { ...str(128, 8), description: 'Optional request-body idempotency key. Unique per authenticated user and endpoint. Same canonical payload returns original response; changed payload returns 409 IDEMPOTENCY_CONFLICT. In-memory only; reset on restart.' };
schemas.OrderCreate = obj({ computerId: id, items: array(obj({ productId: id, quantity: integer(99, 1), expectedUnitPrice: inputMoney }, ['productId', 'quantity']), { minItems: 1, maxItems: 30 }), note, idempotencyKey: key, expectedTotal: money }, ['computerId', 'items']);
operation('/orders', 'post', 'Order food for own active computer session', ['Customer'], ref('Order'), { body: ref('OrderCreate'), status: 201, example: { computerId: 'pc-01', items: [{ productId: 'p-001', quantity: 2, expectedUnitPrice: 20000 }], expectedTotal: 40000, note: 'Ít đá', idempotencyKey: 'order-demo-0001' }, description: 'Requires own active session on computerId. Server loads prices, checks all stock and balance before writes, creates FoodOrder transaction, debits balance and reserves stock atomically in the single-process engine. Duplicate products rejected. Expected price/total are optional guards, not authoritative prices.' });
operation('/orders/{id}/transition', 'post', 'Advance or cancel order', [...O, 'Customer'], ref('Order'), { body: obj({ status: en('Preparing', 'Served', 'Cancelled') }), example: { status: 'Preparing' }, description: 'Operators: Pending -> Preparing -> Served; Pending -> Cancelled. Owner/Manager may also Preparing -> Cancelled. Customer may only cancel own Pending. Cancellation refunds and restores stock once. Repeat/illegal transition -> 409 INVALID_TRANSITION.' });
operation('/topups', 'post', 'Request a balance top-up', ['Customer'], ref('Topup'), { body: obj({ amount: integer(10_000_000, 1000), note, idempotencyKey: key }, ['amount']), status: 201, example: { amount: 100000, note: 'Nạp tại quầy', idempotencyKey: 'topup-demo-0001' }, description: 'Creates Pending request only. No balance credit until operator approval. Same-key retries are safe while engine lives.' });
operation('/topups/{id}/decision', 'post', 'Approve or reject pending top-up', O, ref('Topup'), { body: obj({ status: en('Approved', 'Rejected') }), example: { status: 'Approved' }, description: 'Approval atomically adds balance and one TopUp ledger entry. Rejection leaves money unchanged. Repeat -> 409 ALREADY_PROCESSED. Mock does not prove cash/payment was received.' });
operation('/computers/{id}/command', 'post', 'Simulate room control command', O, { oneOf: [ref('Computer'), ref('Session')] }, { body: obj({ command: en('Maintenance', 'Available', 'EndSession') }), example: { command: 'Maintenance' }, description: 'Maintenance/Available returns Computer, rejected during active session. EndSession returns billed Session; no active session -> 409 NO_ACTIVE_SESSION. This changes demo state only, never OS/network hardware.' });
operation('/sessions/start', 'post', 'Start own rental session', ['Customer'], ref('Session'), { body: obj({ computerId: id }), status: 201, example: { computerId: 'pc-01' }, description: 'Requires positive balance, online Available computer, and no existing active customer session. Server stamps time and freezes hourly rate. Creates active session and marks computer InUse together.' });
operation('/sessions/{id}/end', 'post', 'Settle rental and release computer', [...O, 'Customer'], ref('Session'), { body: obj({}), example: {}, description: 'Customer owns session or operator role required. amount=min(balance,ceil(elapsedMilliseconds*startHourlyRate/3600000)); one Rental ledger entry, balance debit and Available computer. Repeating completed session returns same result. No continuous debit/background automatic stop in mock.' });
operation('/inventory', 'post', 'Append stock movement', M, ref('Inventory'), { body: obj({ productId: id, supplierId: id, type: en('Import', 'Export'), quantity: integer(100000, 1), note }, ['productId', 'type', 'quantity']), status: 201, example: { productId: 'p-001', supplierId: 'supplier-001', type: 'Import', quantity: 10, note: 'Nhập kho' }, description: 'Positive quantity; Import adds, Export subtracts. Insufficient stock -> 409 OUT_OF_STOCK without mutation. Movements immutable. No purchasing payment or supplier debt.' });
operation('/feedback', 'post', 'Submit own feedback', ['Customer'], ref('Feedback'), { body: obj({ subject: str(), content: str(4000) }), status: 201, example: { subject: 'Tai nghe PC 01', content: 'Nhờ quán kiểm tra tai nghe.' } });
operation('/leaves/{id}/decision', 'post', 'Approve or reject leave', M, ref('Leave'), { body: obj({ status: en('Approved', 'Rejected') }), example: { status: 'Approved' }, description: 'Only Pending. Approved leave marks matching Scheduled rows OnLeave; completed/absent rows remain unchanged. Repeat -> 409 ALREADY_PROCESSED.' });
operation('/attendance/check-in', 'post', 'Check in for own scheduled shift today', ['Staff'], ref('Attendance'), { body: obj({ scheduleId: id }), status: 201, example: { scheduleId: 'schedule-today' }, description: 'Own employeeId, Vietnam current date and Scheduled status required. One attendance per schedule; duplicate -> 409 DUPLICATE_CHECK_IN. Mock does not enforce actual shift start/end hours.' });
operation('/attendance/{id}/check-out', 'post', 'Check out of own attendance', ['Staff'], ref('Attendance'), { body: obj({}), example: {}, description: 'Stamps checkOut, marks schedule Completed. Repeat -> 409 ALREADY_PROCESSED; foreign attendance -> 404.' });
operation('/surveys/{id}/publish', 'post', 'Publish draft to selected active customers', M, ref('Survey'), { body: obj({ customerIds: array(id, { minItems: 1 }) }), example: { customerIds: ['c-001', 'c-002'] }, description: 'Every customer must exist and be Active. Deduplicates IDs. Draft only; sets Published. No broadcast of answers or customer lists to customers.' });
operation('/surveys/{id}/responses', 'post', 'Answer targeted published survey once', ['Customer'], ref('SurveyResponse'), { body: obj({ answers: { type: 'object', additionalProperties: { type: 'string' } } }), status: 201, example: { answers: { 'q-1': 'Rất hài lòng', 'q-2': 'Giải đấu' } }, description: 'Must be targeted, Published and not answered before. Exactly every question ID with one allowed option, no extra keys. Out-of-scope -> 404; closed -> 409 SURVEY_CLOSED; duplicate -> 409 DUPLICATE_RESPONSE.' });
const document = {
  openapi: '3.0.3',
  info: { title: 'InternetCafe shared frontend mock contract', version: '1.0.0', description: 'Proposed FE contract, implemented by mock/engine.ts and shared loopback mock/server.ts. NOT a production backend. JSON camelCase; {data,meta?}/{error}; integer VND; UTC timestamps; calendar filtering in Asia/Ho_Chi_Minh. No passwords/hash in responses. All endpoints below /api/v1. GET /health at server root is HTTP-host diagnostics only. OPTIONS is CORS transport, not a business operation. OpenAPI generated by contracts/generate-openapi.ts; edit generator then regenerate.' },
  servers: [{ url: 'http://127.0.0.1:5055/api/v1', description: 'Shared web/WPF in-memory demo host; reset on restart' }, { url: '/api/v1', description: 'Browser MSW engine, isolated from HTTP-host state' }],
  security: [{ BearerAuth: [] }],
  tags: [...new Set(Object.keys(paths).map(path => path.split('/')[1]))].map(name => ({ name })),
  'x-demo-permissions': { readByRole: reads, management: M, operators: O, customerScope: 'Own customer and dependent rows; targeted surveys only.', staffScope: 'Own employeeId HR rows and all shift definitions.', permissionStrings: ['<resource>:read', 'management:write (Owner/Manager)', 'operations:write (Owner/Manager/Cashier)', 'reports:read (Owner/Manager/Cashier)', 'accounts:write (Admin)'], note: 'Auth permission strings support UI navigation; server additionally checks action role, ownership, field whitelist and state transitions. Staff in prose is not the Staff role for operator commands.' },
  paths, components: { securitySchemes: { BearerAuth: { type: 'http', scheme: 'bearer', description: 'Opaque demo token from /auth/login. Not a JWT. Send Authorization: Bearer <accessToken>.' } }, parameters: params, schemas, responses },
};
writeFileSync(resolve(process.cwd(), '../../contracts/openapi.yaml'), stringify(document, { aliasDuplicateObjects: false, lineWidth: 110 }), 'utf8');
console.log(`Generated OpenAPI 3.0.3: ${Object.keys(paths).length} paths, ${Object.values(paths).reduce((n: number, path: any) => n + Object.keys(path).length, 0)} operations, ${Object.keys(schemas).length} schemas.`);
