import { useState } from 'react';
import { Alert, Button, Card, Col, Row as GridRow, Space, Table, Tabs, Tag, Typography } from 'antd';
import { BankOutlined, ClockCircleOutlined, DownloadOutlined, ReloadOutlined, RollbackOutlined, ShoppingCartOutlined, TeamOutlined } from '@ant-design/icons';
import { Bar, BarChart, CartesianGrid, Legend, Line, LineChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { DateFilter, EmptyState, Metric, PageTitle, PrintButton, Value, displayValue, useCafe } from './components';
import { downloadCsv, filterDates, labels, money, revenue, type Row, type Workspace } from './domain';

type ReportColumn = { key: string; label: string; currency?: boolean; reference?: boolean };
const number = (value: unknown) => Number.isFinite(Number(value)) ? Number(value) : 0;
const text = (value: unknown, fallback = 'Chưa cập nhật') => value === undefined || value === null || value === '' ? fallback : String(value);
function localDay(value: unknown) {
  const date = new Date(String(value));
  return Number.isNaN(date.getTime()) ? '' : `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
}
function fullYears(value: unknown, asOf: string): number | null {
  if (!value) return null;
  const source = String(value).slice(0, 10);
  if (!/^\d{4}-\d{2}-\d{2}$/.test(source) || Number.isNaN(Date.parse(source)) || source > asOf) return null;
  return Number(asOf.slice(0, 4)) - Number(source.slice(0, 4)) - (asOf.slice(5) < source.slice(5) ? 1 : 0);
}
function countGroups(rows: Row[], field: string): Row[] {
  const groups = new Map<string, number>();
  for (const row of rows) { const name = text(row[field]); groups.set(name, (groups.get(name) ?? 0) + 1); }
  return [...groups].map(([name, count]) => ({ id: name, name: labels[name] || name, count })).sort((a, b) => b.count - a.count);
}
function ReportTable({ title, rows, columns, workspace, description }: { title: string; rows: Row[]; columns: ReportColumn[]; workspace: Workspace; description?: string }) {
  const printable = rows.map(row => ({ id: row.id, ...Object.fromEntries(columns.map(column => [column.label, column.currency ? money(row[column.key]) : column.reference ? displayValue(column.key, row[column.key], workspace) : text(row[column.key], '—')])) }));
  const exportRows = rows.map(row => ({ ...row, ...Object.fromEntries(columns.filter(column => column.reference).map(column => [column.key, displayValue(column.key, row[column.key], workspace)])) }));
  return <Card title={title} style={{ marginTop: 20 }} extra={rows.length ? <Space wrap>
    <Button icon={<DownloadOutlined />} onClick={() => downloadCsv(title, exportRows, columns)}>CSV</Button>
    <PrintButton title={title} rows={printable} keys={columns.map(column => column.label)} workspace={workspace} />
  </Space> : undefined}>
    {description && <Typography.Paragraph type="secondary">{description}</Typography.Paragraph>}
    <Table<Row> rowKey="id" dataSource={rows} size="middle" scroll={{ x: 'max-content' }} pagination={{ pageSize: 8, showSizeChanger: false, hideOnSinglePage: true }} locale={{ emptyText: <EmptyState /> }}
      columns={columns.map(column => ({ key: column.key, dataIndex: column.key, title: column.label, align: column.currency ? 'right' as const : undefined, render: value => column.currency ? <span className="numeric">{money(value)}</span> : column.reference ? <Value k={column.key} value={value} workspace={workspace} /> : text(value, '—') }))} />
  </Card>;
}
function CountChart({ title, rows, color = '#26796a' }: { title: string; rows: Row[]; color?: string }) {
  return <Card title={title} style={{ height: '100%' }}>
    {rows.length ? <div role="img" aria-label={`${title}: ${rows.map(row => `${row.name} ${row.count}`).join(', ')}`}>
      <ResponsiveContainer width="100%" height={260} minWidth={0}><BarChart data={rows} margin={{ left: 4, right: 12, bottom: 18 }}>
        <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#e4ebe7" /><XAxis dataKey="name" interval={0} tick={{ fontSize: 11 }} /><YAxis allowDecimals={false} width={36} /><Tooltip /><Bar dataKey="count" name="Số lượng" fill={color} radius={[4, 4, 0, 0]} isAnimationActive={false} />
      </BarChart></ResponsiveContainer>
    </div> : <EmptyState />}
  </Card>;
}
const compactMoney = (value: number) => Math.abs(value) >= 1_000_000 ? `${value / 1_000_000}tr` : Math.abs(value) >= 1000 ? `${value / 1000}k` : String(value);

export function ReportsPage() {
  const { workspace, session, refresh, loading } = useCafe();
  const [from, setFrom] = useState(''), [to, setTo] = useState('');
  const isManager = ['Owner', 'Manager'].includes(session.user.role);
  const invalidRange = Boolean(from && to && from > to);
  const period = from || to ? `${from || 'Đầu kỳ'} → ${to || 'Hiện tại'}` : 'Toàn bộ dữ liệu';
  const asOf = to && to < localDay(new Date().toISOString()) ? to : localDay(new Date().toISOString());
  const selectedTransactions = invalidRange ? [] : filterDates(workspace.transactions ?? [], from, to);
  const totals = revenue(selectedTransactions);
  const byDay = new Map<string, Row[]>();
  for (const transaction of selectedTransactions) { const day = localDay(transaction.createdAt); if (day) byDay.set(day, [...(byDay.get(day) ?? []), transaction]); }
  const daily = [...byDay].sort(([a], [b]) => a.localeCompare(b)).map(([date, rows]) => ({ id: date, date, ...revenue(rows) }));
  const selectedOrders = invalidRange ? [] : filterDates(workspace.orders ?? [], from, to).filter(order => order.status !== 'Cancelled');
  const topMap = new Map<string, Row>();
  for (const order of selectedOrders) for (const item of Array.isArray(order.items) ? order.items as Record<string, unknown>[] : []) {
    const productId = text(item.productId, 'unknown'); const old = topMap.get(productId);
    topMap.set(productId, { id: productId, productId, name: text(item.name), quantity: number(old?.quantity) + number(item.quantity), revenue: number(old?.revenue) + number(item.quantity) * number(item.unitPrice) });
  }
  const topProducts = [...topMap.values()].sort((a, b) => number(b.revenue) - number(a.revenue));
  const activeCustomerIds = new Set(selectedTransactions.map(transaction => transaction.customerId));
  const customers: (Row & { spending: number; topup: number; age: number | string; ageGroup: string })[] = (workspace.customers ?? []).filter(customer => !invalidRange && (!(from || to) || activeCustomerIds.has(customer.id))).map(customer => {
    const age = fullYears(customer.dateOfBirth, asOf), spending = revenue(selectedTransactions.filter(transaction => transaction.customerId === customer.id));
    return { ...customer, age: age ?? 'Chưa có ngày sinh', ageGroup: age === null ? 'Chưa có ngày sinh' : age < 18 ? 'Dưới 18' : age <= 24 ? '18–24' : age <= 34 ? '25–34' : '35+', spending: spending.net, topup: spending.topup };
  });
  const hobbyRows: Row[] = customers.flatMap(customer => [...new Set(text(customer.hobbies).split(/[,;]/).map(value => value.trim()).filter(Boolean))].map((name, index) => ({ id: `${customer.id}-${index}`, name })));
  const inventory = invalidRange ? [] : filterDates(workspace.inventory ?? [], from, to);
  const stock: Row[] = (workspace.products ?? []).map(product => ({ ...product, imported: inventory.filter(row => row.productId === product.id && row.type === 'Import').reduce((sum, row) => sum + number(row.quantity), 0), exported: inventory.filter(row => row.productId === product.id && row.type === 'Export').reduce((sum, row) => sum + number(row.quantity), 0), salesQuantity: number(topMap.get(product.id)?.quantity) }));
  const employees: Row[] = (workspace.employees ?? []).filter(employee => !employee.joinedAt || String(employee.joinedAt).slice(0, 10) <= asOf).map(employee => {
    const age = fullYears(employee.dateOfBirth, asOf), years = fullYears(employee.joinedAt, asOf);
    return { ...employee, age: age ?? 'Chưa có ngày sinh', ageGroup: age === null ? 'Chưa có ngày sinh' : age < 25 ? 'Dưới 25' : age < 35 ? '25–34' : age < 45 ? '35–44' : '45+', tenure: years === null ? 'Chưa có ngày vào làm' : `${years} năm`, tenureGroup: years === null ? 'Chưa có ngày vào làm' : years < 1 ? 'Dưới 1 năm' : years < 3 ? '1–2 năm' : years < 5 ? '3–4 năm' : 'Từ 5 năm' };
  });
  const payroll = (workspace.payroll ?? []).filter(row => !invalidRange && (!from || String(row.month) >= from.slice(0, 7)) && (!to || String(row.month) <= to.slice(0, 7)));
  const salaryByMonth = new Map<string, Row>();
  for (const row of payroll) { const month = text(row.month); const previous = salaryByMonth.get(month); salaryByMonth.set(month, { id: month, month, total: number(previous?.total) + number(row.total), paid: number(previous?.paid) + (row.status === 'Paid' ? number(row.total) : 0) }); }
  const salaryMonths = [...salaryByMonth.values()].sort((a, b) => text(a.month).localeCompare(text(b.month)));
  const salaryByYear = new Map<string, Row>();
  for (const row of salaryMonths) { const year = text(row.month).slice(0, 4), old = salaryByYear.get(year); salaryByYear.set(year, { id: year, year, total: number(old?.total) + number(row.total), paid: number(old?.paid) + number(row.paid), months: number(old?.months) + 1 }); }
  const salaryYears = [...salaryByYear.values()];
  const metric = (title: string, value: string | number, note: string, icon: React.ReactNode) => <Col xs={24} sm={12} xl={6}><Metric title={title} value={value} note={note} icon={icon} /></Col>;
  const commonColumns: ReportColumn[] = [{ key: 'id', label: 'Mã' }, { key: 'createdAt', label: 'Thời điểm', reference: true }];

  const revenueTab = <>
    <GridRow gutter={[16, 16]}>
      {metric('Doanh thu giờ chơi', money(totals.rental), 'Giao dịch Rental đã ghi nhận', <ClockCircleOutlined />)}
      {metric('Doanh thu F&B', money(totals.food), 'Giá trị đặt món trước hoàn tiền', <ShoppingCartOutlined />)}
      {metric('Hoàn tiền', money(totals.refund), 'Trừ khỏi doanh thu trong kỳ', <RollbackOutlined />)}
      {metric('Doanh thu thuần', money(totals.net), 'Giờ chơi + F&B − hoàn tiền', <BankOutlined />)}
    </GridRow>
    <Alert style={{ margin: '20px 0' }} type="info" showIcon title={`Nạp vào ví: ${money(totals.topup)} — theo dõi riêng, không cộng vào doanh thu.`} description="Doanh thu lấy từ giao dịch đã ghi sổ; không cộng lại tổng đơn hàng hoặc tiền phiên. Khoản hoàn của kỳ trước có thể làm doanh thu kỳ đang chọn âm." />
    <Card title="Diễn biến theo ngày" extra={<Tag>{period}</Tag>}>
      {daily.length ? <div role="img" aria-label="Biểu đồ doanh thu giờ chơi, dịch vụ, hoàn tiền và doanh thu thuần theo ngày">
        <ResponsiveContainer width="100%" height={320} minWidth={0}><LineChart data={daily} margin={{ top: 10, right: 20, left: 8, bottom: 8 }}>
          <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#e4ebe7" /><XAxis dataKey="date" tickFormatter={value => String(value).slice(5)} /><YAxis tickFormatter={compactMoney} width={65} /><Tooltip formatter={value => money(value)} /><Legend />
          <Line type="monotone" dataKey="rental" name="Giờ chơi" stroke="#236f61" strokeWidth={2} isAnimationActive={false} /><Line type="monotone" dataKey="food" name="F&B" stroke="#bb812c" strokeWidth={2} isAnimationActive={false} /><Line type="monotone" dataKey="refund" name="Hoàn tiền" stroke="#b94747" strokeDasharray="4 4" isAnimationActive={false} /><Line type="monotone" dataKey="net" name="Thuần" stroke="#253b48" strokeWidth={2} isAnimationActive={false} />
        </LineChart></ResponsiveContainer>
      </div> : <EmptyState description="Không có giao dịch trong khoảng ngày đã chọn." />}
    </Card>
    <ReportTable title={`Doanh thu theo ngày · ${period}`} rows={daily} workspace={workspace} columns={[{ key: 'date', label: 'Ngày' }, ...[['rental', 'Giờ chơi'], ['food', 'F&B'], ['refund', 'Hoàn tiền'], ['net', 'Doanh thu thuần'], ['topup', 'Nạp vào ví']].map(([key, label]) => ({ key, label, currency: true }))]} />
    <ReportTable title={`Món bán nhiều · ${period}`} rows={topProducts} workspace={workspace} description="Tổng hợp đơn Pending, Preparing và Served tạo trong kỳ; loại đơn Cancelled. Đây là phân tích món, không cộng thêm vào doanh thu giao dịch." columns={[{ key: 'name', label: 'Sản phẩm' }, { key: 'quantity', label: 'Số lượng đặt' }, { key: 'revenue', label: 'Giá trị món', currency: true }]} />
    <ReportTable title={`Chi tiết giao dịch · ${period}`} rows={selectedTransactions} workspace={workspace} columns={[...commonColumns, { key: 'customerId', label: 'Khách hàng', reference: true }, { key: 'type', label: 'Loại', reference: true }, { key: 'amount', label: 'Số tiền', currency: true }, { key: 'referenceId', label: 'Tham chiếu' }]} />
  </>;
  const customerTab = <>
    <Alert type="info" showIcon style={{ marginBottom: 20 }} title={from || to ? 'Nhóm khách có giao dịch trong khoảng ngày đã chọn' : 'Toàn bộ hồ sơ khách đang được phép xem'} description={`Tuổi tính đến ${asOf}. Hạng và sở thích là thông tin hồ sơ hiện tại; DTO chưa có ngày đăng ký để thống kê khách mới theo kỳ.`} />
    <GridRow gutter={[16, 16]}>
      {metric('Khách trong báo cáo', customers.length, from || to ? 'Có ít nhất một giao dịch trong kỳ' : 'Toàn bộ hồ sơ được cấp quyền', <TeamOutlined />)}
      {metric('Khách hoạt động', customers.filter(row => row.status === 'Active').length, 'Trạng thái hồ sơ hiện tại', <TeamOutlined />)}
      {metric('Chi tiêu thuần', money(customers.reduce((sum, row) => sum + row.spending, 0)), 'Giờ chơi + món − hoàn', <ShoppingCartOutlined />)}
      {metric('Đã nạp trong kỳ', money(customers.reduce((sum, row) => sum + row.topup, 0)), 'Tách khỏi chi tiêu', <BankOutlined />)}
    </GridRow>
    <GridRow gutter={[16, 16]} style={{ marginTop: 20 }}><Col xs={24} lg={8}><CountChart title="Hạng thành viên" rows={countGroups(customers, 'tier')} /></Col><Col xs={24} lg={8}><CountChart title="Nhóm tuổi" rows={countGroups(customers, 'ageGroup')} /></Col><Col xs={24} lg={8}><CountChart title="Sở thích · một khách có thể chọn nhiều" rows={countGroups(hobbyRows, 'name')} /></Col></GridRow>
    <ReportTable title={`Hồ sơ và hoạt động khách · ${period}`} rows={customers} workspace={workspace} columns={[{ key: 'fullName', label: 'Khách hàng' }, { key: 'age', label: 'Tuổi' }, { key: 'hobbies', label: 'Sở thích' }, { key: 'tier', label: 'Hạng', reference: true }, { key: 'status', label: 'Trạng thái', reference: true }, { key: 'spending', label: 'Chi tiêu thuần', currency: true }, { key: 'topup', label: 'Nạp trong kỳ', currency: true }]} />
  </>;
  const inventoryTab = <>
    <Alert type="info" showIcon style={{ marginBottom: 20 }} title="Nhập/xuất theo kỳ, tồn kho là số hiện tại" description="Xuất kho thủ công và lượng món được đặt được trình bày riêng. Seed không có sổ kho mở đầu đầy đủ; báo cáo không suy ngược tồn lịch sử hay định giá vốn từ giá bán." />
    <GridRow gutter={[16, 16]}>
      {metric('Nhập trong kỳ', inventory.filter(row => row.type === 'Import').reduce((sum, row) => sum + number(row.quantity), 0), 'Tổng đơn vị hàng nhập', <ShoppingCartOutlined />)}
      {metric('Xuất thủ công', inventory.filter(row => row.type === 'Export').reduce((sum, row) => sum + number(row.quantity), 0), 'Không gồm tự động giữ hàng cho đơn', <ShoppingCartOutlined />)}
      {metric('Tồn hiện tại', stock.reduce((sum, row) => sum + number(row.stock), 0), 'Ảnh chụp kho; không phụ thuộc bộ lọc ngày', <BankOutlined />)}
      {metric('Mặt hàng sắp hết', stock.filter(row => row.active && number(row.stock) <= 5).length, 'Đang bán, tồn không quá 5', <ShoppingCartOutlined />)}
    </GridRow>
    <ReportTable title={`Tồn hiện tại và biến động · ${period}`} rows={stock} workspace={workspace} columns={[{ key: 'name', label: 'Sản phẩm' }, { key: 'categoryId', label: 'Danh mục', reference: true }, { key: 'stock', label: 'Tồn hiện tại' }, { key: 'imported', label: 'Nhập trong kỳ' }, { key: 'exported', label: 'Xuất thủ công' }, { key: 'salesQuantity', label: 'Đặt món chưa hủy' }, { key: 'active', label: 'Đang bán', reference: true }]} />
    <ReportTable title={`Chi tiết kho · ${period}`} rows={inventory} workspace={workspace} columns={[...commonColumns, { key: 'productId', label: 'Sản phẩm', reference: true }, { key: 'supplierId', label: 'Nhà cung cấp', reference: true }, { key: 'type', label: 'Loại', reference: true }, { key: 'quantity', label: 'Số lượng' }, { key: 'note', label: 'Ghi chú' }]} />
  </>;
  const hrTab = <>
    <Alert type="info" showIcon style={{ marginBottom: 20 }} title={`Hồ sơ nhân sự và thâm niên đến ${asOf}`} description="Phân nhóm dùng hồ sơ hiện tại và ngày vào làm. Lương lọc theo các tháng giao với khoảng ngày; tổng năm bên dưới chỉ cộng những tháng đang được chọn. Bản nháp/đã duyệt là khoản tính lương, chỉ Paid là đã trả." />
    <GridRow gutter={[16, 16]}>
      {metric('Nhân sự', employees.length, 'Có ngày vào làm không sau mốc báo cáo', <TeamOutlined />)}
      {metric('Đang làm việc', employees.filter(row => row.status === 'Active').length, 'Trạng thái hiện tại', <TeamOutlined />)}
      {metric('Tổng bảng lương', money(payroll.reduce((sum, row) => sum + number(row.total), 0)), 'Gồm Draft, Approved và Paid trong kỳ', <BankOutlined />)}
      {metric('Đã trả', money(payroll.filter(row => row.status === 'Paid').reduce((sum, row) => sum + number(row.total), 0)), 'Chỉ các bảng lương Paid', <BankOutlined />)}
    </GridRow>
    <GridRow gutter={[16, 16]} style={{ marginTop: 20 }}><Col xs={24} lg={12}><CountChart title="Phòng ban" rows={countGroups(employees, 'department')} /></Col><Col xs={24} lg={12}><CountChart title="Vị trí công việc" rows={countGroups(employees, 'position')} /></Col><Col xs={24} lg={12}><CountChart title="Trình độ" rows={countGroups(employees, 'qualification')} /></Col><Col xs={24} lg={12}><CountChart title="Thâm niên" rows={countGroups(employees, 'tenureGroup')} /></Col></GridRow>
    {employees.some(row => typeof row.age === 'number') ? <div style={{ marginTop: 20 }}><CountChart title="Tuổi nhân sự có ngày sinh" rows={countGroups(employees, 'ageGroup')} /></div> : <Alert style={{ marginTop: 20 }} type="warning" showIcon title="Chưa thể thống kê tuổi nhân viên" description="DTO nhân viên hiện chưa có dateOfBirth. Báo cáo giữ thông tin này là thiếu dữ liệu; cần backend bổ sung trường ngày sinh." />}
    <Card title="Quỹ lương theo tháng" style={{ marginTop: 20 }}>
      {salaryMonths.length ? <div role="img" aria-label="Biểu đồ tổng bảng lương và lương đã trả theo tháng"><ResponsiveContainer width="100%" height={280} minWidth={0}><BarChart data={salaryMonths}><CartesianGrid strokeDasharray="3 3" vertical={false} /><XAxis dataKey="month" /><YAxis tickFormatter={compactMoney} width={65} /><Tooltip formatter={value => money(value)} /><Legend /><Bar dataKey="total" name="Tổng bảng lương" fill="#276f62" radius={[4, 4, 0, 0]} isAnimationActive={false} /><Bar dataKey="paid" name="Đã trả" fill="#bf862e" radius={[4, 4, 0, 0]} isAnimationActive={false} /></BarChart></ResponsiveContainer></div> : <EmptyState description="Không có bảng lương thuộc kỳ đã chọn." />}
    </Card>
    <ReportTable title={`Nhân sự đến ${asOf}`} rows={employees} workspace={workspace} columns={[{ key: 'fullName', label: 'Nhân viên' }, { key: 'department', label: 'Phòng ban' }, { key: 'position', label: 'Vị trí' }, { key: 'qualification', label: 'Trình độ' }, { key: 'joinedAt', label: 'Ngày vào làm' }, { key: 'tenure', label: 'Thâm niên' }, { key: 'age', label: 'Tuổi' }, { key: 'status', label: 'Trạng thái', reference: true }]} />
    <ReportTable title={`Bảng lương chi tiết · ${period}`} rows={payroll} workspace={workspace} columns={[{ key: 'employeeId', label: 'Nhân viên', reference: true }, { key: 'month', label: 'Tháng' }, { key: 'baseSalary', label: 'Lương cơ bản', currency: true }, { key: 'bonus', label: 'Thưởng', currency: true }, { key: 'deduction', label: 'Khấu trừ', currency: true }, { key: 'total', label: 'Thực lĩnh', currency: true }, { key: 'status', label: 'Trạng thái', reference: true }]} />
    <ReportTable title={`Tổng lương theo năm trong kỳ · ${period}`} rows={salaryYears} workspace={workspace} columns={[{ key: 'year', label: 'Năm' }, { key: 'months', label: 'Số tháng có dữ liệu' }, { key: 'total', label: 'Tổng bảng lương', currency: true }, { key: 'paid', label: 'Đã trả', currency: true }]} />
  </>;

  return <>
    <PageTitle eyebrow="INTERNETCAFE / BÁO CÁO" title="Báo cáo vận hành" description="Tổng hợp dữ liệu được cấp quyền từ hệ thống demo. Chọn kỳ và xuất từng bảng đang xem." actions={<Button icon={<ReloadOutlined />} loading={loading} onClick={() => void refresh()}>Làm mới</Button>} />
    <Space wrap style={{ marginBottom: 20 }}><DateFilter from={from} to={to} onFrom={setFrom} onTo={setTo} /><Tag color="gold">Nguồn: mock</Tag><Typography.Text type="secondary">{period}</Typography.Text></Space>
    {invalidRange && <Alert style={{ marginBottom: 20 }} type="error" showIcon title="Ngày bắt đầu phải trước hoặc bằng ngày kết thúc." />}
    <Tabs items={[{ key: 'revenue', label: 'Doanh thu & món bán', children: revenueTab }, { key: 'customers', label: 'Khách hàng', children: customerTab }, ...(isManager ? [{ key: 'inventory', label: 'Nhập · xuất · tồn', children: inventoryTab }, { key: 'hr', label: 'Nhân sự & tiền lương', children: hrTab }] : [])]} />
  </>;
}
