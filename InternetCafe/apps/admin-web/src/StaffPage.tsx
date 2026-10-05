import { useEffect, useState } from 'react';
import { Alert, App, Button, Card, Descriptions, Form, Input, Modal, Select, Space, Table, Tabs, Tag } from 'antd';
import { CalendarOutlined, ClockCircleOutlined, DollarOutlined, EditOutlined, PlusOutlined, UserOutlined } from '@ant-design/icons';
import { api, readSession, writeSession } from './api';
import { dateTime, labels, money, type Row } from './domain';
import { EmptyState, Metric, PageTitle, PrintButton, StatusTag, useCafe } from './components';

function localDay() {
  return new Intl.DateTimeFormat('en-CA', { timeZone: 'Asia/Ho_Chi_Minh', year: 'numeric', month: '2-digit', day: '2-digit' }).format(new Date());
}

export function StaffPage() {
  const { workspace, session, refresh } = useCafe();
  const { message, modal } = App.useApp();
  const [dialog, setDialog] = useState<'profile' | 'leave' | 'password' | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [month, setMonth] = useState(localDay().slice(0, 7));
  const [year, setYear] = useState(localDay().slice(0, 4));
  const [form] = Form.useForm();
  const employee = (workspace.employees || []).find(row => row.id === session.user.id);
  const schedules = (workspace.schedules || []).filter(row => row.employeeId === session.user.id);
  const attendance = (workspace.attendance || []).filter(row => row.employeeId === session.user.id);
  const leaves = (workspace.leaves || []).filter(row => row.employeeId === session.user.id);
  const payroll = (workspace.payroll || []).filter(row => row.employeeId === session.user.id);
  const today = localDay();
  const todaysSchedules = schedules.filter(row => row.date === today);
  const openAttendance = attendance.find(row => !row.checkOut);
  const monthlyPayroll = payroll.filter(row => !month || row.month === month);
  const annualTotal = payroll.filter(row => String(row.month).startsWith(`${year}-`)).reduce((sum, row) => sum + Number(row.total || 0), 0);
  const shiftLabel = (id: unknown) => {
    const shift = (workspace.shifts || []).find(row => row.id === id);
    return shift ? `${shift.name} · ${shift.startTime}–${shift.endTime}` : String(id || '—');
  };

  function openDialog(kind: 'profile' | 'leave' | 'password') { setDialog(kind); setError(''); }
  useEffect(() => {
    if (!dialog) return;
    form.resetFields();
    if (dialog === 'profile') form.setFieldsValue(employee || {});
    if (dialog === 'leave') form.setFieldsValue({ type: 'Annual', startDate: today, endDate: today });
  }, [dialog, form]);

  async function save(values: Record<string, string>) {
    setBusy(true); setError('');
    try {
      if (dialog === 'profile') {
        const profile = await api<Row>('/me', 'PATCH', { fullName: values.fullName, phone: values.phone, email: values.email, qualification: values.qualification });
        const current = readSession();
        if (current) writeSession({ ...current, user: { ...current.user, fullName: String(profile.fullName) } });
      } else if (dialog === 'leave') {
        await api('/leaves', 'POST', { type: values.type, startDate: values.startDate, endDate: values.endDate, reason: values.reason });
      } else {
        await api('/me/password', 'POST', { currentPassword: values.currentPassword, newPassword: values.newPassword });
      }
      setDialog(null); await refresh(); message.success('Đã lưu thông tin.');
    } catch (e) { setError((e as Error).message); }
    finally { setBusy(false); }
  }

  function recordAttendance(row: Row, checkout: boolean) {
    modal.confirm({
      title: checkout ? 'Xác nhận kết thúc ca làm?' : 'Xác nhận bắt đầu ca làm?',
      content: 'Thời điểm chấm công được ghi nhận tại service. Bạn không thể tự chỉnh giờ.',
      okText: checkout ? 'Chấm công ra' : 'Chấm công vào', cancelText: 'Quay lại',
      onOk: async () => {
        try {
          await api(checkout ? `/attendance/${row.id}/check-out` : '/attendance/check-in', 'POST', checkout ? {} : { scheduleId: row.id });
          await refresh(); message.success('Đã ghi nhận chấm công.');
        } catch (e) { message.error((e as Error).message); throw e; }
      },
    });
  }

  const scheduleColumns = [
    { title: 'Ngày làm', dataIndex: 'date', key: 'date', sorter: (a: Row, b: Row) => String(a.date).localeCompare(String(b.date)) },
    { title: 'Ca làm', dataIndex: 'shiftId', key: 'shiftId', render: shiftLabel },
    { title: 'Trạng thái', dataIndex: 'status', key: 'status', render: (value: unknown) => <StatusTag value={value} /> },
    { title: 'Chấm công', key: 'action', render: (_: unknown, row: Row) => {
      const record = attendance.find(item => item.scheduleId === row.id);
      if (record?.checkOut) return <Tag color="green">Đã kết thúc</Tag>;
      if (record) return <Button size="small" onClick={() => recordAttendance(record, true)}>Chấm công ra</Button>;
      return <Button size="small" type="primary" disabled={row.date !== today || row.status !== 'Scheduled' || !!openAttendance} onClick={() => recordAttendance(row, false)}>Chấm công vào</Button>;
    } },
  ];

  const attendanceColumns = [
    { title: 'Ca làm', dataIndex: 'scheduleId', key: 'scheduleId', render: (id: unknown) => {
      const schedule = schedules.find(row => row.id === id); return schedule ? `${schedule.date} · ${shiftLabel(schedule.shiftId)}` : String(id);
    } },
    { title: 'Giờ vào', dataIndex: 'checkIn', key: 'checkIn', render: dateTime },
    { title: 'Giờ ra', dataIndex: 'checkOut', key: 'checkOut', render: (value: unknown) => value ? dateTime(value) : <Tag color="blue">Đang trong ca</Tag> },
  ];

  return <>
    <PageTitle eyebrow="CỔNG NHÂN VIÊN" title={`Xin chào, ${employee?.fullName || session.user.fullName}`} description="Lịch làm, chấm công và thông tin cá nhân của bạn trong một không gian." actions={<Button icon={<EditOutlined />} onClick={() => openDialog('profile')}>Cập nhật hồ sơ</Button>} />
    <div className="metric-grid">
      <Metric title="Ca làm hôm nay" value={todaysSchedules.length} note={todaysSchedules.length ? shiftLabel(todaysSchedules[0].shiftId) : 'Hôm nay chưa có lịch phân công'} icon={<CalendarOutlined />} />
      <Metric title="Trạng thái chấm công" value={openAttendance ? 'Trong ca' : 'Ngoài ca'} note={openAttendance ? `Vào lúc ${dateTime(openAttendance.checkIn)}` : 'Chấm công theo lịch được phân công'} icon={<ClockCircleOutlined />} />
      <Metric title="Đơn nghỉ chờ duyệt" value={leaves.filter(row => row.status === 'Pending').length} note="Quản lý sẽ xử lý yêu cầu của bạn" icon={<UserOutlined />} />
      <Metric title="Lương tháng hiện tại" value={money(payroll.find(row => row.month === today.slice(0, 7))?.total)} note="Theo bảng lương hiện có tại service" icon={<DollarOutlined />} />
    </div>
    <Tabs items={[
      { key: 'schedule', label: 'Lịch làm & chấm công', children: <>
        <Card title="Lịch được phân công" className="data-card section-alert"><Table rowKey="id" dataSource={schedules} columns={scheduleColumns} scroll={{ x: 680 }} pagination={{ pageSize: 7 }} locale={{ emptyText: <EmptyState description="Bạn chưa có lịch làm được phân công." /> }} /></Card>
        <Card title="Lịch sử chấm công" className="data-card"><Table rowKey="id" dataSource={attendance} columns={attendanceColumns} scroll={{ x: 650 }} pagination={{ pageSize: 7 }} locale={{ emptyText: <EmptyState description="Chưa ghi nhận chấm công." /> }} /></Card>
      </> },
      { key: 'leave', label: 'Đơn nghỉ của tôi', children: <Card title="Yêu cầu nghỉ phép / nghỉ việc" className="data-card" extra={<Button type="primary" icon={<PlusOutlined />} onClick={() => openDialog('leave')}>Gửi đơn nghỉ</Button>}><Table rowKey="id" dataSource={leaves} scroll={{ x: 650 }} pagination={{ pageSize: 8 }} locale={{ emptyText: <EmptyState description="Bạn chưa gửi đơn nghỉ." /> }} columns={[
        { title: 'Loại đơn', dataIndex: 'type', render: value => labels[String(value)] || String(value) },
        { title: 'Từ ngày', dataIndex: 'startDate' }, { title: 'Đến ngày', dataIndex: 'endDate' },
        { title: 'Lý do', dataIndex: 'reason' }, { title: 'Trạng thái', dataIndex: 'status', render: value => <StatusTag value={value} /> },
      ]} /></Card> },
      { key: 'payroll', label: 'Phiếu lương', children: <Card className="data-card" title="Bảng lương cá nhân">
        <div className="table-toolbar"><label>Tháng <Input aria-label="Tháng lương" type="month" value={month} onChange={e => setMonth(e.target.value)} /></label><label>Năm tổng hợp <Input aria-label="Năm tổng hợp" type="number" min={2000} max={2100} value={year} onChange={e => setYear(e.target.value)} /></label><Tag color="cyan">Tổng năm {year}: {money(annualTotal)}</Tag><PrintButton title={`Phiếu lương ${month || 'tất cả'}`} rows={monthlyPayroll} keys={['employeeId', 'month', 'baseSalary', 'bonus', 'deduction', 'total', 'status']} workspace={workspace} /></div>
        <Alert type="info" showIcon title="Số liệu lương do service cung cấp. Bản nháp chưa phải số tiền đã thanh toán." className="section-alert" />
        <Table rowKey="id" dataSource={monthlyPayroll} scroll={{ x: 720 }} locale={{ emptyText: <EmptyState description="Chưa có bảng lương cho tháng này." /> }} columns={[
          { title: 'Tháng', dataIndex: 'month' }, { title: 'Lương cơ bản', dataIndex: 'baseSalary', render: money }, { title: 'Thưởng', dataIndex: 'bonus', render: money },
          { title: 'Khấu trừ', dataIndex: 'deduction', render: money }, { title: 'Thực nhận', dataIndex: 'total', render: money }, { title: 'Trạng thái', dataIndex: 'status', render: value => <StatusTag value={value} /> },
        ]} />
      </Card> },
      { key: 'profile', label: 'Hồ sơ & bảo mật', children: <Card title="Thông tin cá nhân" extra={<Space><Button onClick={() => openDialog('profile')}>Sửa hồ sơ</Button><Button onClick={() => openDialog('password')}>Đổi mật khẩu</Button></Space>}>
        {employee ? <Descriptions bordered column={{ xs: 1, sm: 2 }} items={[
          { key: 'name', label: 'Họ tên', children: String(employee.fullName) }, { key: 'phone', label: 'Điện thoại', children: String(employee.phone || '—') },
          { key: 'email', label: 'Email', children: String(employee.email || '—') }, { key: 'department', label: 'Bộ phận', children: String(employee.department || '—') },
          { key: 'position', label: 'Chức vụ', children: String(employee.position || '—') }, { key: 'qualification', label: 'Trình độ', children: String(employee.qualification || '—') },
          { key: 'joined', label: 'Ngày vào làm', children: String(employee.joinedAt || '—') }, { key: 'status', label: 'Trạng thái', children: <StatusTag value={employee.status} /> },
        ]} /> : <EmptyState description="Chưa có hồ sơ nhân sự liên kết tài khoản." />}
      </Card> },
    ]} />
    <Modal open={dialog !== null} destroyOnHidden title={dialog === 'profile' ? 'Cập nhật hồ sơ cá nhân' : dialog === 'leave' ? 'Gửi đơn nghỉ' : 'Đổi mật khẩu'} onCancel={() => setDialog(null)} onOk={() => form.submit()} okText={dialog === 'leave' ? 'Gửi yêu cầu' : 'Lưu'} confirmLoading={busy}>
      {error && <Alert type="error" showIcon title={error} className="section-alert" />}
      <Form form={form} layout="vertical" onFinish={save}>
        {dialog === 'profile' && <>
          <Form.Item name="fullName" label="Họ tên" rules={[{ required: true, whitespace: true, message: 'Nhập họ tên.' }]}><Input maxLength={180} /></Form.Item>
          <Form.Item name="phone" label="Điện thoại" rules={[{ required: true }, { pattern: /^[0-9+ ()-]{9,15}$/, message: 'Số điện thoại chưa hợp lệ.' }]}><Input /></Form.Item>
          <Form.Item name="email" label="Email" rules={[{ type: 'email', message: 'Email chưa đúng định dạng.' }]}><Input type="email" /></Form.Item>
          <Form.Item name="qualification" label="Trình độ"><Input maxLength={180} /></Form.Item>
        </>}
        {dialog === 'leave' && <>
          <Form.Item name="type" label="Loại đơn" rules={[{ required: true }]}><Select options={['Annual', 'Sick', 'Resignation'].map(value => ({ value, label: labels[value] }))} /></Form.Item>
          <Form.Item name="startDate" label="Từ ngày" rules={[{ required: true, message: 'Chọn ngày bắt đầu.' }]}><Input type="date" /></Form.Item>
          <Form.Item name="endDate" label="Đến ngày" dependencies={['startDate']} rules={[{ required: true, message: 'Chọn ngày kết thúc.' }, ({ getFieldValue }) => ({ validator: async (_, value) => { if (value && value < getFieldValue('startDate')) throw new Error('Ngày kết thúc phải từ ngày bắt đầu trở đi.'); } })]}><Input type="date" /></Form.Item>
          <Form.Item name="reason" label="Lý do" rules={[{ required: true, whitespace: true, message: 'Nhập lý do nghỉ.' }]}><Input.TextArea rows={4} maxLength={2000} showCount /></Form.Item>
        </>}
        {dialog === 'password' && <>
          <Form.Item name="currentPassword" label="Mật khẩu hiện tại" rules={[{ required: true }]}><Input.Password autoComplete="current-password" /></Form.Item>
          <Form.Item name="newPassword" label="Mật khẩu mới" rules={[{ required: true }, { min: 8, message: 'Mật khẩu cần ít nhất 8 ký tự.' }]}><Input.Password autoComplete="new-password" /></Form.Item>
          <Form.Item name="confirmation" label="Nhập lại mật khẩu mới" dependencies={['newPassword']} rules={[{ required: true }, ({ getFieldValue }) => ({ validator: async (_, value) => { if (value && value !== getFieldValue('newPassword')) throw new Error('Hai mật khẩu chưa khớp.'); } })]}><Input.Password autoComplete="new-password" /></Form.Item>
        </>}
      </Form>
    </Modal>
  </>;
}
