import { resources, vietnamDate, type Workspace } from './types';

/** Public demo data only; credentials and access tokens never enter the snapshot. */
export function createSeed(now = new Date()): Workspace {
  const state = Object.fromEntries(resources.map(key => [key, []])) as unknown as Workspace;
  const day = (offset = 0) => vietnamDate(new Date(now.getTime() + offset * 86_400_000));
  const at = (hoursAgo = 0) => new Date(now.getTime() - hoursAgo * 3_600_000).toISOString();
  const month = day().slice(0, 7);
  state.computers = Array.from({ length: 16 }, (_, index) => ({
    id: `pc-${String(index + 1).padStart(2, '0')}`, name: `PC ${String(index + 1).padStart(2, '0')}`,
    zone: index < 10 ? 'Standard' : 'VIP', status: index === 1 ? 'InUse' : index === 8 ? 'Maintenance' : 'Available',
    online: index !== 8 && index !== 15, hourlyRate: index < 10 ? 10000 : 18000,
    ...(index === 1 ? { customerId: 'c-002', sessionId: 'session-active' } : {}),
  }));
  state.customers = [
    { id: 'c-001', username: 'gamer', fullName: 'Nguyễn Minh Huy', phone: '0901234567', email: 'gamer@example.test', dateOfBirth: '2003-08-12', hobbies: 'Liên Minh Huyền Thoại, FPS', tier: 'Gold', balance: 185000, status: 'Active' },
    { id: 'c-002', username: 'linh', fullName: 'Trần Ngọc Linh', phone: '0912345678', email: 'linh@example.test', dateOfBirth: '2001-03-22', hobbies: 'Valorant, âm nhạc', tier: 'Diamond', balance: 312000, status: 'Active' },
    { id: 'c-003', username: 'khoa', fullName: 'Lê Anh Khoa', phone: '0923456789', email: 'khoa@example.test', dateOfBirth: '2005-11-04', hobbies: 'FC Online', tier: 'Silver', balance: 40000, status: 'Active' },
    { id: 'c-004', username: 'banned', fullName: 'Tài khoản bị khóa', phone: '0934567890', email: 'banned@example.test', dateOfBirth: '1999-01-01', hobbies: '', tier: 'Silver', balance: 0, status: 'Banned' },
  ];
  state.categories = [{ id: 'cat-drinks', name: 'Đồ uống' }, { id: 'cat-food', name: 'Đồ ăn' }, { id: 'cat-snacks', name: 'Ăn vặt' }];
  state.products = [
    { id: 'p-001', name: 'Cà phê sữa đá', categoryId: 'cat-drinks', price: 20000, stock: 38, active: true, imageUrl: '' },
    { id: 'p-002', name: 'Mì trộn trứng xúc xích', categoryId: 'cat-food', price: 35000, stock: 21, active: true, imageUrl: '' },
    { id: 'p-003', name: 'Nước tăng lực', categoryId: 'cat-drinks', price: 15000, stock: 45, active: true, imageUrl: '' },
    { id: 'p-004', name: 'Khoai tây chiên', categoryId: 'cat-snacks', price: 25000, stock: 4, active: true, imageUrl: '' },
    { id: 'p-005', name: 'Trà đào cam sả', categoryId: 'cat-drinks', price: 25000, stock: 0, active: true, imageUrl: '' },
    { id: 'p-006', name: 'Cơm gà', categoryId: 'cat-food', price: 45000, stock: 10, active: false, imageUrl: '' },
  ];
  state.sessions = [
    { id: 'session-active', customerId: 'c-002', computerId: 'pc-02', startTime: at(1.5), startBalance: 332000, amount: 0, status: 'Active' },
    { id: 'session-yesterday', customerId: 'c-001', computerId: 'pc-01', startTime: at(26), endTime: at(24), startBalance: 205000, amount: 20000, status: 'Completed' },
  ];
  state.orders = [
    { id: 'order-pending', customerId: 'c-002', computerId: 'pc-02', items: [{ productId: 'p-001', name: 'Cà phê sữa đá', quantity: 1, unitPrice: 20000 }], total: 20000, status: 'Pending', note: 'Ít đá giúp mình', createdAt: at(0.12) },
    { id: 'order-served', customerId: 'c-001', computerId: 'pc-01', items: [{ productId: 'p-002', name: 'Mì trộn trứng xúc xích', quantity: 1, unitPrice: 35000 }], total: 35000, status: 'Served', note: '', createdAt: at(25) },
  ];
  state.topups = [
    { id: 'topup-pending', customerId: 'c-001', amount: 100000, status: 'Pending', note: 'Nạp tiền tại quầy', createdAt: at(0.2) },
    { id: 'topup-approved', customerId: 'c-002', amount: 200000, status: 'Approved', note: 'Đã nhận tiền mặt', createdAt: at(3) },
  ];
  state.transactions = [
    { id: 'tx-topup', customerId: 'c-002', type: 'TopUp', amount: 200000, createdAt: at(3), referenceId: 'topup-approved' },
    { id: 'tx-food-now', customerId: 'c-002', type: 'FoodOrder', amount: 20000, createdAt: at(0.12), referenceId: 'order-pending' },
    { id: 'tx-food', customerId: 'c-001', type: 'FoodOrder', amount: 35000, createdAt: at(25), referenceId: 'order-served' },
    { id: 'tx-rental', customerId: 'c-001', type: 'Rental', amount: 20000, createdAt: at(24), referenceId: 'session-yesterday' },
  ];
  state.suppliers = [
    { id: 'supplier-001', name: 'Đại lý Minh Phát', phone: '02838123456', email: 'contact@minhphat.example', address: 'Quận 5, TP. Hồ Chí Minh', active: true },
    { id: 'supplier-002', name: 'Thực phẩm An Việt', phone: '02838987654', email: 'sales@anviet.example', address: 'Quận 10, TP. Hồ Chí Minh', active: true },
  ];
  state.inventory = [
    { id: 'inv-001', productId: 'p-003', supplierId: 'supplier-001', type: 'Import', quantity: 48, note: 'Nhập đầu ngày', createdAt: at(4) },
    { id: 'inv-002', productId: 'p-004', supplierId: 'supplier-002', type: 'Export', quantity: 2, note: 'Kiểm tra chất lượng', createdAt: at(1) },
  ];
  state.feedback = [
    { id: 'feedback-001', customerId: 'c-001', subject: 'Tai nghe máy PC 01', content: 'Tai nghe bên trái hơi nhỏ, nhờ quán kiểm tra.', status: 'Pending', response: '', createdAt: at(2) },
    { id: 'feedback-002', customerId: 'c-002', subject: 'Góp ý thực đơn', content: 'Mong quán bổ sung nước ép ít đường.', status: 'Processing', response: 'Quán đang xem xét bổ sung.', createdAt: at(26) },
    { id: 'feedback-003', customerId: 'c-003', subject: 'Điều hòa khu VIP', content: 'Nhiệt độ rất dễ chịu, cảm ơn quán.', status: 'Resolved', response: 'Cảm ơn bạn đã góp ý!', createdAt: at(50) },
  ];
  state.surveys = [
    { id: 'survey-001', title: 'Trải nghiệm tại InternetCafe', description: 'Chia sẻ để quán phục vụ bạn tốt hơn.', questions: [{ id: 'q-1', text: 'Bạn hài lòng với chất lượng máy?', options: ['Rất hài lòng', 'Hài lòng', 'Cần cải thiện'] }, { id: 'q-2', text: 'Bạn muốn thêm dịch vụ nào?', options: ['Giải đấu', 'Combo giờ chơi', 'Món mới'] }], customerIds: ['c-001', 'c-002', 'c-003'], status: 'Published', responses: [{ customerId: 'c-002', answers: { 'q-1': 'Rất hài lòng', 'q-2': 'Giải đấu' }, createdAt: at(1) }] },
    { id: 'survey-002', title: 'Khảo sát thực đơn tháng mới', description: 'Bản nháp do quản lý chuẩn bị.', questions: [{ id: 'q-1', text: 'Món ăn bạn thích?', options: ['Mì', 'Cơm', 'Bánh mì'] }], customerIds: [], status: 'Draft', responses: [] },
  ];
  state.employees = [
    { id: 'e-001', fullName: 'Phạm Gia Bảo', phone: '0945678901', email: 'staff@example.test', department: 'Vận hành', position: 'Nhân viên phục vụ', status: 'Active', baseSalary: 7000000, joinedAt: '2025-03-01', qualification: 'Cao đẳng' },
    { id: 'e-002', fullName: 'Nguyễn Thu Hà', phone: '0956789012', email: 'cashier@example.test', department: 'Thu ngân', position: 'Thu ngân', status: 'Active', baseSalary: 8500000, joinedAt: '2024-06-15', qualification: 'Đại học' },
    { id: 'e-003', fullName: 'Trần Quốc Nam', phone: '0967890123', email: 'manager@example.test', department: 'Quản lý', position: 'Quản lý cửa hàng', status: 'Active', baseSalary: 14000000, joinedAt: '2023-08-01', qualification: 'Đại học' },
  ];
  state.shifts = [{ id: 'shift-am', name: 'Ca sáng', startTime: '08:00', endTime: '16:00' }, { id: 'shift-pm', name: 'Ca chiều', startTime: '16:00', endTime: '23:00' }, { id: 'shift-night', name: 'Ca đêm', startTime: '23:00', endTime: '07:00' }];
  state.schedules = [
    { id: 'schedule-today', employeeId: 'e-001', shiftId: 'shift-am', date: day(), status: 'Scheduled' },
    { id: 'schedule-tomorrow', employeeId: 'e-001', shiftId: 'shift-pm', date: day(1), status: 'Scheduled' },
    { id: 'schedule-yesterday', employeeId: 'e-001', shiftId: 'shift-am', date: day(-1), status: 'Completed' },
    { id: 'schedule-cashier', employeeId: 'e-002', shiftId: 'shift-am', date: day(), status: 'Scheduled' },
  ];
  state.leaves = [{ id: 'leave-001', employeeId: 'e-001', type: 'Annual', startDate: day(3), endDate: day(3), reason: 'Việc gia đình', status: 'Pending' }];
  state.attendance = [{ id: 'attendance-yesterday', employeeId: 'e-001', scheduleId: 'schedule-yesterday', checkIn: `${day(-1)}T01:00:00.000Z`, checkOut: `${day(-1)}T09:00:00.000Z` }];
  state.payroll = [
    { id: 'payroll-001', employeeId: 'e-001', month, baseSalary: 7000000, bonus: 500000, deduction: 0, total: 7500000, status: 'Draft' },
    { id: 'payroll-002', employeeId: 'e-002', month, baseSalary: 8500000, bonus: 300000, deduction: 100000, total: 8700000, status: 'Approved' },
    { id: 'payroll-003', employeeId: 'e-003', month, baseSalary: 14000000, bonus: 1000000, deduction: 0, total: 15000000, status: 'Paid' },
  ];
  state.accounts = [
    { id: 'a-admin', username: 'admin', fullName: 'Quản trị hệ thống', role: 'Admin', status: 'Active' },
    { id: 'a-owner', username: 'owner', fullName: 'Chủ cửa hàng', role: 'Owner', status: 'Active' },
    { id: 'a-manager', username: 'manager', fullName: 'Trần Quốc Nam', role: 'Manager', status: 'Active', employeeId: 'e-003' },
    { id: 'a-cashier', username: 'cashier', fullName: 'Nguyễn Thu Hà', role: 'Cashier', status: 'Active', employeeId: 'e-002' },
    { id: 'a-staff', username: 'staff', fullName: 'Phạm Gia Bảo', role: 'Staff', status: 'Active', employeeId: 'e-001' },
  ];
  return state;
}
