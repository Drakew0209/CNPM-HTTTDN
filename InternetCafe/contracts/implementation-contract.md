# Contract triển khai thống nhất (03/10/2026)

Đây là contract FE đề xuất, chưa phải backend production. OpenAPI là bản đặc tả máy đọc được tương ứng.

Base `/api/v1`. JSON camelCase. Thành công `{data: T, meta?: {total,page,pageSize}}`. Lỗi `{error:{code,message,fields?}}`. Bearer token. ID string. Tiền VND nguyên không âm; UTC ISO 8601; ngày YYYY-MM-DD. PATCH chỉ nhận trường được phép. Không gửi mật khẩu/hash trong response.

`POST /auth/login {username,password}` -> `{data:{accessToken,user:{id,username,fullName,role,permissions:string[]}}}`. Roles: Admin, Owner, Manager, Cashier, Staff, Customer. Demo usernames `admin`, `owner`, `manager`, `cashier`, `staff`, `gamer`, cùng password `Demo@123`. `banned` mô phỏng bị khóa. Admin chỉ system; Owner/Manager quản lý; Cashier thu ngân/phòng máy/đơn; Staff cổng cá nhân; Customer desktop.

`GET /workspace` -> `{data: {computers:Row[],customers:Row[],products:Row[],categories:Row[],orders:Row[],topups:Row[],transactions:Row[],suppliers:Row[],inventory:Row[],feedback:Row[],surveys:Row[],employees:Row[],shifts:Row[],schedules:Row[],leaves:Row[],attendance:Row[],payroll:Row[],accounts:Row[],sessions:Row[]}}`. Chỉ trả collections và bản ghi người gọi được quyền xem. Quyền cũng kiểm tra trên từng endpoint; `/workspace` là bootstrap read model, không thay thế API phân trang.

`GET /{resource}?search=&status=&sort=&order=asc&page=1&pageSize=20&from=&to=` trả danh sách với meta. `POST /{resource}`, `PATCH /{resource}/{id}`, `DELETE /{resource}/{id}` hỗ trợ CRUD có whitelist. Row gồm `id:string` và các trường dưới. DELETE khách/account dùng soft delete, bảo toàn lịch sử.

Collections và trường thống nhất:

- computers: name, zone (Standard/VIP), status (Available/InUse/Maintenance), online:boolean, hourlyRate, customerId?, sessionId?
- customers: username, fullName, phone, email, dateOfBirth, hobbies:string, tier (Silver/Gold/Diamond), balance, status (Active/Banned/Inactive)
- categories: name
- products: name, categoryId, price, stock, active:boolean, imageUrl?
- orders: customerId, computerId, items:[{productId,name,quantity,unitPrice}], total, status (Pending/Preparing/Served/Cancelled), note, createdAt
- topups: customerId, amount, status (Pending/Approved/Rejected), note, createdAt
- transactions: customerId, type (TopUp/FoodOrder/Rental/Refund), amount, createdAt, referenceId
- suppliers: name, phone, email, address, active:boolean
- inventory: productId, supplierId?, type (Import/Export), quantity, note, createdAt
- feedback: customerId, subject, content, status (Pending/Processing/Resolved), response, createdAt
- surveys: title, description, questions:[{id,text,options:string[]}], customerIds:string[], status (Draft/Published/Closed), responses:[{customerId,answers:Record<string,string>,createdAt}]
- employees: fullName, phone, email, department, position, status (Active/Inactive), baseSalary, joinedAt, qualification
- shifts: name, startTime, endTime
- schedules: employeeId, shiftId, date, status (Scheduled/Completed/Absent/OnLeave)
- leaves: employeeId, type (Annual/Sick/Resignation), startDate, endDate, reason, status (Pending/Approved/Rejected)
- attendance: employeeId, scheduleId, checkIn, checkOut?
- payroll: employeeId, month (YYYY-MM), baseSalary, bonus, deduction, total, status (Draft/Approved/Paid)
- accounts: username, fullName, role, status (Active/Banned), employeeId?
- sessions: customerId, computerId, startTime, endTime?, startBalance, amount, status (Active/Completed)

Actions:

- `POST /orders {computerId,items:[{productId,quantity}],note}`: Customer only, validate session/balance/stock, calculate total server-side; reserve stock and debit once. `POST /orders/{id}/transition {status}` Pending->Preparing->Served, Pending->Cancelled; manager may cancel Preparing. Refund once on cancel.
- `POST /topups {amount,note}` Customer creates Pending. `POST /topups/{id}/decision {status:Approved|Rejected}` Cashier/Manager/Owner; balance update/TopUp transaction exactly once.
- `POST /computers/{id}/command {command:Maintenance|Available|EndSession}` staff permission; reject maintenance for active session. `PATCH /computers/{id}` only hourlyRate/zone/name (no status overwrite).
- `POST /sessions/start {computerId}` Customer only; starts once with available/online machine and positive balance. `POST /sessions/{id}/end {}` owner customer or authorized operator; calculates bill server-side, clamps at balance, updates machine, returns session summary. Desktop UI never deducts.
- `POST /leaves/{id}/decision {status:Approved|Rejected}` Manager/Owner.
- `POST /attendance/check-in {scheduleId}` Staff; `POST /attendance/{id}/check-out {}` owner staff. Duplicate check-in rejected.
- `POST /surveys/{id}/publish {customerIds:string[]}` management; `POST /surveys/{id}/responses {answers:Record<string,string>}` targeted Customer once.
- `GET /reports/summary?from=YYYY-MM-DD&to=YYYY-MM-DD` aggregates transactions by type; rentalRevenue, foodRevenue, refunds, netRevenue, topups distinct. UI can aggregate scoped returned rows for charts/CSV, label source mock.

Customer APIs for desktop: `POST /auth/register {username,password,fullName,phone,email,dateOfBirth,hobbies}`, `GET /me`, `PATCH /me {fullName,phone,email,dateOfBirth,hobbies}`, `POST /me/password {currentPassword,newPassword}`. `GET /workspace` for Customer filters own customers/sessions/orders/topups/transactions/feedback/surveys plus available catalog/computers; never HR/payroll/accounts. `POST /feedback {subject,content}`.

Staff identity maps employeeId from user.id (`e-001` demo). Staff workspace contains own HR rows only, plus shifts. Customer demo id `c-001`, machine `pc-01`. Auth user.id is entity id where applicable. Dates in seeds relative to today for meaningful filtering. Mock simulation can use test engine isolated instances; do not expose passwords in snapshot.

Shared mock engine exports `createMockEngine()` -> `{handle(method,path,body?,token?):{status:number,body:unknown}, snapshot()}`. Both MSW browser and local HTTP demo host invoke same engine. Browser storage optional and clearly separate from HTTP host. HTTP demo host binds loopback only (127.0.0.1:5055). Web defaults MSW; HTTP mode via env VITE_API_MODE=http + VITE_API_BASE_URL=http://127.0.0.1:5055/api/v1. Desktop defaults HTTP demo host to enable actual cross-app demo when web also selects HTTP. Polling resync in mock; real SignalR adapter separate.
