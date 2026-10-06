import { useCallback, useEffect, useState, type FormEvent } from 'react';
import axios from 'axios';
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { useTranslation } from 'react-i18next';
import { type TFunction } from 'i18next';
import i18n from './i18n';

const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5200').replace(/\/$/, '');
const HUB_URL = import.meta.env.VITE_SIGNALR_HUB_URL ?? `${API_BASE_URL}/cafeHub`;
const TOP_UP_BANK = { name: 'MB Bank', accountNumber: '0382533868', vietQrBankCode: 'MB' };

const getAccessToken = () => localStorage.getItem('accessToken');

const api = axios.create({ baseURL: API_BASE_URL, withCredentials: true });
api.interceptors.request.use((config) => {
  const token = getAccessToken();
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

type LoginResponse = {
  accessToken: string;
  role: string;
};

type ApiCustomer = {
  customer_ID: number;
  tier_ID: number;
  username: string;
  full_Name: string;
  balance: number;
  tier_Name: string;
  status: string;
};

type ApiProductCategory = {
  category_ID: number;
  category_Name: string;
};

type ApiProduct = {
  product_ID: number;
  product_Name: string;
  price: number;
  stock_Quantity: number;
  image_Url: string | null;
  category_ID: number;
  category_Name: string;
  status?: string | null;
};

type ApiOrderLine = {
  product_ID: number;
  product_Name: string;
  quantity: number;
  unit_Price: number;
  line_Total: number;
};

type ApiPendingOrder = {
  order_ID: number;
  customer_ID: number;
  customer_Name: string;
  computer_ID: number | null;
  computer_Name: string | null;
  status: string;
  total_Amount: number;
  order_Date: string | null;
  items: ApiOrderLine[];
};

type ApiTransaction = {
  transaction_ID: number;
  customer_ID: number;
  customer_Username: string;
  customer_Name: string;
  amount: number;
  trans_Type: string;
  trans_Date: string | null;
};

type ApiUsageSession = {
  session_ID: number;
  customer_ID: number;
  customer_Name: string;
  computer_ID: number;
  computer_Name: string;
  start_Time: string;
  end_Time: string | null;
  total_Hours: number | null;
  applied_Hourly_Rate: number | null;
  amount: number | null;
  status: string;
};

type ApiFeedback = {
  feedback_ID: number;
  customer_ID: number;
  customer_Name: string;
  handled_By_Name: string | null;
  subject: string;
  content: string;
  submitted_Date: string | null;
  status: string | null;
  manager_Notes: string | null;
};

type ApiEmployee = {
  employee_ID: number;
  username: string;
  full_Name: string;
  position_Name: string;
  access_Level: string;
  hire_Date: string | null;
  base_Salary: number | null;
  status: string | null;
};

type ApiWorkSchedule = {
  schedule_ID: number;
  employee_ID: number;
  employee_Name: string;
  shift_Name: string;
  work_Date: string;
  start_Time: string;
  end_Time: string;
  status: string | null;
};

type ApiAttendance = {
  attendance_ID: number;
  employee_ID: number;
  employee_Name: string;
  work_Date: string;
  shift_Name: string;
  check_In_Time: string | null;
  check_Out_Time: string | null;
  note: string | null;
  schedule_Status: string | null;
};

type ApiPayroll = {
  payroll_ID: number;
  employee_ID: number;
  employee_Name: string;
  pay_Month: number;
  pay_Year: number;
  base_Salary: number;
  bonus: number | null;
  deduction: number | null;
  net_Salary: number | null;
  payment_Date: string | null;
  status: string | null;
};

type ApiLeaveRequest = {
  leave_ID: number;
  employee_ID: number;
  employee_Name: string;
  leave_Type: string;
  start_Date: string;
  end_Date: string;
  reason: string | null;
  status: string | null;
  approved_By_Name: string | null;
  request_Date: string | null;
};

type ApiInventoryTransaction = {
  inv_Trans_ID: number;
  product_ID: number;
  product_Name: string;
  employee_ID: number;
  employee_Name: string;
  trans_Type: string;
  quantity: number;
  note: string | null;
  created_Date: string | null;
};

type ApiInventoryProduct = {
  product_ID: number;
  product_Name: string;
  category_ID: number;
  category_Name: string;
  price: number;
  stock_Quantity: number;
  status: string | null;
};

type ApiPositionOption = {
  position_ID: number;
  position_Name: string;
  department_ID: number;
  department_Name: string;
  access_Level: string;
};

type ApiPayrollRun = {
  created_Count: number;
  payrolls: ApiPayroll[];
};

// Lucide icons equivalent
const LayoutGridIcon = () => <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><rect width="7" height="7" x="3" y="3" rx="1" /><rect width="7" height="7" x="14" y="3" rx="1" /><rect width="7" height="7" x="14" y="14" rx="1" /><rect width="7" height="7" x="3" y="14" rx="1" /></svg>;
const MonitorIcon = () => <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><rect width="20" height="14" x="2" y="3" rx="2" /><line x1="8" x2="16" y1="21" y2="21" /><line x1="12" x2="12" y1="17" y2="21" /></svg>;
const UsersIcon = () => <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2" /><circle cx="9" cy="7" r="4" /><path d="M22 21v-2a4 4 0 0 0-3-3.87" /><path d="M16 3.13a4 4 0 0 1 0 7.75" /></svg>;
const ShoppingCartIcon = () => <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><circle cx="8" cy="21" r="1" /><circle cx="19" cy="21" r="1" /><path d="M2.05 2.05h2l2.66 12.42a2 2 0 0 0 2 1.58h9.78a2 2 0 0 0 1.95-1.57l1.65-7.43H5.12" /></svg>;
const HistoryIcon = () => <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M3 12a9 9 0 1 0 9-9 9.75 9.75 0 0 0-6.74 2.74L3 8" /><path d="M3 3v5h5" /><path d="M12 7v5l4 2" /></svg>;
const CreditCardIcon = () => <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><rect width="20" height="14" x="2" y="5" rx="2" /><line x1="2" x2="22" y1="10" y2="10" /></svg>;
const MessageIcon = () => <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M7.9 20A9 9 0 1 0 4 16.1L2 22Z" /></svg>;
const BriefcaseIcon = () => <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><rect width="20" height="14" x="2" y="7" rx="2" ry="2" /><path d="M16 21V5a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v16" /></svg>;
const BoxIcon = () => <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M21 8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16Z" /><path d="m3.3 7 8.7 5 8.7-5" /><path d="M12 22V12" /></svg>;

type ApiComputer = {
  computer_ID: number;
  computer_Name: string;
  status: string;
  zone_Type: string;
  hourly_Rate: number;
  active_Session_ID: number | null;
  active_Customer_ID: number | null;
};

type PcViewModel = {
  id: number;
  name: string;
  status: 'available' | 'inuse' | 'maintenance';
  zone: string;
  user: string | null;
  time: string | null;
  specs: string;
  rate: string;
  activeSessionId: number | null;
  activeCustomerId: number | null;
};

const formatMoney = (amount: number) =>
  new Intl.NumberFormat(i18n.resolvedLanguage === 'en' ? 'en-US' : 'vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(amount);

const formatDateTime = (value: string | null | undefined) =>
  value ? new Date(value).toLocaleString(i18n.resolvedLanguage === 'en' ? 'en-US' : 'vi-VN') : '—';

const translateStatus = (status: string | null | undefined) => {
  if (!status) return i18n.t('common.notAvailable');
  const keys: Record<string, string> = {
    Available: 'available', InUse: 'inUse', Maintenance: 'maintenance', Active: 'active',
    Banned: 'banned', Inactive: 'inactive', Pending: 'pending', Preparing: 'preparing',
    Served: 'served', Completed: 'completed', Cancelled: 'cancelled', Discontinued: 'discontinued',
    Resigned: 'resigned', Suspended: 'suspended', Scheduled: 'scheduled', Absent: 'absent',
    OnLeave: 'onLeave', Paid: 'paid', Unpaid: 'unpaid', Reviewed: 'reviewed', Resolved: 'resolved',
  };
  return keys[status] ? i18n.t(`status.${keys[status]}`) : status;
};

const getLocalizedApiError = (error: unknown, t: TFunction, fallbackKey = 'errors.requestFailed') => {
  if (!axios.isAxiosError(error)) return t(fallbackKey);
  const status = error.response?.status;
  if (status === 400) return t('errors.badRequest');
  if (status === 401) return t('errors.unauthorized');
  if (status === 403) return t('errors.forbidden');
  if (status === 404) return t('errors.notFound');
  if (status === 409) return t('errors.conflict');
  return t(fallbackKey);
};

function useAdminRows<T>(endpoint: string): T[] {
  const [rows, setRows] = useState<T[]>([]);

  useEffect(() => {
    let active = true;
    api.get<T[]>(endpoint)
      .then(response => { if (active) setRows(response.data); })
      .catch(error => {
        console.error(`Could not load ${endpoint}.`, error);
        if (active) setRows([]);
      });
    return () => { active = false; };
  }, [endpoint]);

  return rows;
}

const toPcViewModel = (computer: ApiComputer, customerLabel: string): PcViewModel => {
  const rawStatus = computer.status.toLowerCase();
  const status: PcViewModel['status'] = rawStatus === 'inuse'
    ? 'inuse'
    : rawStatus === 'available'
      ? 'available'
      : 'maintenance';

  return {
    id: computer.computer_ID,
    name: computer.computer_Name,
    status,
    zone: computer.zone_Type ?? 'Standard',
    user: computer.active_Customer_ID ? `${customerLabel} #${computer.active_Customer_ID}` : null,
    time: null,
    specs: `Station #${computer.computer_ID}`,
    rate: `${formatMoney(computer.hourly_Rate)}/hr`,
    activeSessionId: computer.active_Session_ID,
    activeCustomerId: computer.active_Customer_ID,
  };
};

// Sub-components
const DashboardView = () => {
  const { t } = useTranslation();
  const [pcs, setPcs] = useState<PcViewModel[]>([]);
  const transactions = useAdminRows<ApiTransaction>('/api/admin-data/transactions');

  const loadComputers = useCallback(async () => {
    try {
      const response = await api.get<ApiComputer[]>('/api/computers');
      setPcs(response.data.map(computer => toPcViewModel(computer, t('common.customer'))));
    } catch (error) {
      console.error(t('errors.couldNotLoadComputers'), error);
      setPcs([]);
    }
  }, [t]);

  useEffect(() => {
    void loadComputers();

    const token = getAccessToken();
    if (!token) return;

    const connection = new HubConnectionBuilder()
      .withUrl(HUB_URL, {
        accessTokenFactory: () => getAccessToken() ?? '',
        withCredentials: true,
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    const refreshComputers = () => { void loadComputers(); };
    connection.on('ComputerStatusChanged', refreshComputers);
    connection.onreconnected(refreshComputers);
    void connection.start().catch((error) => {
      console.error(t('errors.couldNotConnectHub'), error);
    });

    return () => {
      connection.off('ComputerStatusChanged', refreshComputers);
      void connection.stop();
    };
  }, [loadComputers, t]);

  const handleComputerClick = async (pc: PcViewModel) => {
    if (!getAccessToken()) {
      window.alert(t('dashboard.authRequired'));
      return;
    }
    try {
      if (pc.status === 'maintenance') {
        window.alert(t('dashboard.pcMaintenance', { computer: pc.name }));
        return;
      }
      if (pc.status === 'available') {
        const customerInput = window.prompt(t('dashboard.startSessionPrompt', { computer: pc.name }));
        if (customerInput === null) return;
        const customerId = Number(customerInput);
        if (!Number.isInteger(customerId) || customerId <= 0) {
          window.alert(t('dashboard.invalidCustomerId'));
          return;
        }
        await api.post('/api/sessions/start', { computer_ID: pc.id, customer_ID: customerId });
        window.alert(t('dashboard.sessionStarted', { computer: pc.name }));
      } else {
        const action = window.prompt(t('dashboard.inUsePrompt', { computer: pc.name }));
        if (action === '1') {
          if (!pc.activeCustomerId) {
            window.alert(t('dashboard.missingActiveCustomer'));
            return;
          }
          const amountInput = window.prompt(t('dashboard.topUpPrompt', { customerId: pc.activeCustomerId }));
          if (amountInput === null) return;
          const amount = Number(amountInput.replaceAll(',', '').trim());
          if (!Number.isFinite(amount) || amount <= 0) {
            window.alert(t('dashboard.invalidTopUpAmount'));
            return;
          }
          await api.post('/api/transactions/topup', { customer_ID: pc.activeCustomerId, amount, combo_ID: null });
          window.alert(t('dashboard.topUpSuccess'));
        } else if (action === '2') {
          if (!pc.activeSessionId) {
            window.alert(t('dashboard.missingActiveSession'));
            return;
          }
          if (!window.confirm(t('dashboard.endSessionConfirm', { computer: pc.name }))) return;
          await api.post(`/api/sessions/${pc.activeSessionId}/end`);
          window.alert(t('dashboard.sessionEnded', { computer: pc.name }));
        } else {
          return;
        }
      }
      await loadComputers();
    } catch (error) {
      window.alert(getLocalizedApiError(error, t, 'dashboard.requestFailed'));
    }
  };

  const stats = {
    inuse: pcs.filter(p => p.status === 'inuse').length,
    available: pcs.filter(p => p.status === 'available').length,
    maintenance: pcs.filter(p => p.status === 'maintenance').length,
    total: pcs.length
  };

  const todayRevenue = transactions
    .filter(t => t.trans_Date && new Date(t.trans_Date).toDateString() === new Date().toDateString())
    .reduce((sum, t) => sum + t.amount, 0);

  const zones = Array.from(new Set(pcs.map(p => p.zone)));

  return (
    <>
      <div className="stats-container">
        <div className="stat-box glass-panel" style={{ border: '1px solid var(--status-inuse)', boxShadow: '0 4px 15px var(--status-inuse-glow)' }}>
          <span className="stat-title" style={{ color: 'var(--status-inuse)' }}>{t('dashboard.activeUsers')}</span>
          <span className="stat-value">{stats.inuse} / {stats.total}</span>
        </div>
        <div className="stat-box glass-panel" style={{ border: '1px solid var(--status-available)' }}>
          <span className="stat-title" style={{ color: 'var(--status-available)' }}>{t('dashboard.availablePcs')}</span>
          <span className="stat-value">{stats.available}</span>
        </div>
        <div className="stat-box glass-panel" style={{ border: '1px solid var(--primary)', boxShadow: '0 4px 15px rgba(0, 240, 255, 0.2)' }}>
          <span className="stat-title" style={{ color: 'var(--primary)' }}>{t('dashboard.todayRevenue')}</span>
          <span className="stat-value">{formatMoney(todayRevenue)}</span>
        </div>
        <div className="stat-box glass-panel" style={{ border: '1px solid var(--status-maintenance)' }}>
          <span className="stat-title" style={{ color: 'var(--status-maintenance)' }}>{t('dashboard.maintenance')}</span>
          <span className="stat-value">{stats.maintenance}</span>
        </div>
      </div>

      {zones.map(zone => (
        <div key={zone} style={{ marginBottom: '32px' }}>
          <h3 style={{ marginBottom: '16px', color: 'var(--text-muted)', display: 'flex', alignItems: 'center', gap: '8px' }}>
            <MonitorIcon /> {t('dashboard.zone', { zone: t(`zones.${zone.toLowerCase()}`, { defaultValue: zone }) })}
          </h3>
          <div className="grid-container">
            {pcs.filter(p => p.zone === zone).map((pc) => (
              <div key={pc.id} className={`pc-card glass-panel ${pc.status}`} onClick={() => void handleComputerClick(pc)}>
                <div className="pc-header">
                  <span className="pc-name">{pc.name}</span>
                  <div className="status-indicator"></div>
                </div>
                <div className="pc-info">
                  {pc.status === 'inuse' ? (
                    <><span>{t('dashboard.user', { user: pc.user })}</span><span style={{ color: 'var(--status-inuse)' }}>{t('common.online')}</span></>
                  ) : pc.status === 'available' ? (
                    <><span>{t('dashboard.idle')}</span><span>{t('dashboard.ratePerHour', { rate: pc.rate })}</span></>
                  ) : (
                    <><span>{t('common.offline')}</span><span style={{ color: 'var(--status-maintenance)' }}>{t('dashboard.maintenance')}</span></>
                  )}
                </div>
                <div className="pc-footer"><span>{pc.specs}</span></div>
              </div>
            ))}
          </div>
        </div>
      ))}
    </>
  );
};

function UsersView() {
  const { t } = useTranslation();
  const [users, setUsers] = useState<ApiCustomer[]>([]);
  const [showCustomerModal, setShowCustomerModal] = useState(false);
  const [editingCustomer, setEditingCustomer] = useState<ApiCustomer | null>(null);
  const [customerForm, setCustomerForm] = useState({ username: '', password: '', fullName: '', tierId: '1' });
  const [topUpCustomer, setTopUpCustomer] = useState<ApiCustomer | null>(null);
  const [topUpAmount, setTopUpAmount] = useState('50000');
  const [topUpBusy, setTopUpBusy] = useState(false);

  const loadUsers = useCallback(async () => {
    try {
      const response = await api.get<ApiCustomer[]>('/api/customers');
      setUsers(response.data);
    } catch (error) {
      console.error('Could not load customers.', error);
      setUsers([]);
    }
  }, []);

  useEffect(() => {
    void loadUsers();
  }, [loadUsers]);

  const openCreateCustomer = () => {
    setEditingCustomer(null);
    setCustomerForm({ username: '', password: '', fullName: '', tierId: '1' });
    setShowCustomerModal(true);
  };

  const openEditCustomer = (customer: ApiCustomer) => {
    setEditingCustomer(customer);
    setCustomerForm({ username: customer.username, password: '', fullName: customer.full_Name, tierId: String(customer.tier_ID) });
    setShowCustomerModal(true);
  };

  const closeCustomerModal = () => {
    setShowCustomerModal(false);
    setEditingCustomer(null);
  };

  const saveCustomer = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const tierId = Number(customerForm.tierId);
    if (!Number.isInteger(tierId) || tierId <= 0) {
      window.alert(t('customers.validTier'));
      return;
    }

    try {
      if (editingCustomer) {
        await api.put(`/api/customers/${editingCustomer.customer_ID}`, {
          Full_Name: customerForm.fullName.trim(),
          Tier_ID: tierId,
          Password: customerForm.password || null,
        });
      } else {
        await api.post('/api/customers', {
          Username: customerForm.username.trim(),
          Password: customerForm.password,
          Full_Name: customerForm.fullName.trim(),
          Tier_ID: tierId,
        });
      }

      closeCustomerModal();
      await loadUsers();
    } catch (error) {
      window.alert(getLocalizedApiError(error, t, 'customers.saveFailed'));
    }
  };

  const toggleCustomerStatus = async (customer: ApiCustomer) => {
    const nextStatus = customer.status === 'Active' ? 'Banned' : 'Active';
    const confirmMessage = nextStatus === 'Banned' ? 'customers.confirmBan' : 'customers.confirmUnban';
    if (!window.confirm(t(confirmMessage, { username: customer.username }))) return;

    try {
      await api.put(`/api/customers/${customer.customer_ID}/status`, { Status: nextStatus });
      await loadUsers();
    } catch (error) {
      window.alert(getLocalizedApiError(error, t, 'customers.statusFailed'));
    }
  };

  const calculateBonus = (amount: number) => amount >= 500000 ? 80000 : amount >= 100000 ? 10000 : 0;
  const parsedTopUpAmount = Number(topUpAmount.replaceAll(',', '').trim());
  const validTopUpAmount = Number.isFinite(parsedTopUpAmount) && parsedTopUpAmount > 0;
  const topUpBonus = validTopUpAmount ? calculateBonus(parsedTopUpAmount) : 0;
  const totalTopUpCredit = validTopUpAmount ? parsedTopUpAmount + topUpBonus : 0;
  const topUpQrUrl = validTopUpAmount
    ? `https://img.vietqr.io/image/${TOP_UP_BANK.vietQrBankCode}-${TOP_UP_BANK.accountNumber}-compact2.png?amount=${Math.round(parsedTopUpAmount)}&addInfo=${encodeURIComponent(`NETZONE TOPUP ${topUpCustomer?.customer_ID ?? ''}`)}`
    : '';

  const submitTopUp = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!topUpCustomer || !validTopUpAmount) return;
    setTopUpBusy(true);
    try {
      // Transactions.Amount drives the DB balance trigger. Send the credited amount
      // (paid amount plus this modal's promotion) so the balance receives the bonus.
      const response = await api.post('/api/transactions/topup', {
        customer_ID: topUpCustomer.customer_ID,
        amount: totalTopUpCredit,
        combo_ID: null,
      });
      await loadUsers();
      setTopUpCustomer(null);
      window.alert(t('customers.topUpSuccess', { balance: formatMoney(response.data.balance) }));
    } catch (error) {
      window.alert(getLocalizedApiError(error, t, 'customers.topUpFailed'));
    } finally {
      setTopUpBusy(false);
    }
  };

  return (
    <div className="glass-panel" style={{ padding: '24px', flex: 1 }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '24px' }}>
        <h2>{t('customers.title')}</h2>
        <div style={{ display: 'flex', gap: '12px' }}>
          <input type="text" className="search-bar" placeholder={t('customers.search')} />
          <button className="cyber-btn" onClick={openCreateCustomer}>+ {t('customers.add')}</button>
        </div>
      </div>
      <div className="table-container">
        <table className="cyber-table">
          <thead>
            <tr>
              <th>{t('common.id')}</th><th>{t('common.username')}</th><th>{t('common.fullName')}</th><th>{t('common.balance')}</th><th>{t('common.tier')}</th><th>{t('common.status')}</th><th>{t('common.action')}</th>
            </tr>
          </thead>
          <tbody>
            {users.map(user => (
              <tr key={user.customer_ID}>
                <td>#{user.customer_ID}</td>
                <td style={{ fontWeight: 600, color: 'var(--primary)' }}>{user.username}</td>
                <td>{user.full_Name}</td>
                <td style={{ color: 'var(--status-available)' }}>{formatMoney(user.balance)}</td>
                <td>{user.tier_Name}</td>
                <td><span className={`badge ${user.status === 'Active' ? 'active' : ''}`}>{translateStatus(user.status)}</span></td>
                <td style={{display: 'flex', gap: '8px'}}>
                  <button className="cyber-btn" style={{padding: '4px 10px', fontSize: '12px'}} onClick={() => { setTopUpCustomer(user); setTopUpAmount('50000'); }}>{t('customers.topUp')}</button>
                  <button className="cyber-btn" style={{padding: '4px 10px', fontSize: '12px'}} onClick={() => openEditCustomer(user)}>{t('common.edit')}</button>
                  <button className="cyber-btn" style={{padding: '4px 10px', fontSize: '12px'}} onClick={() => void toggleCustomerStatus(user)}>
                    {user.status === 'Active' ? t('customers.ban') : t('customers.unban')}
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {showCustomerModal && (
        <div role="presentation" onClick={closeCustomerModal} style={{position: 'fixed', inset: 0, zIndex: 1000, display: 'flex', alignItems: 'center', justifyContent: 'center', padding: '20px', background: 'rgba(0,0,0,0.75)', backdropFilter: 'blur(8px)'}}>
          <form role="dialog" aria-modal="true" aria-labelledby="customer-modal-title" onClick={event => event.stopPropagation()} onSubmit={event => void saveCustomer(event)} className="glass-panel" style={{width: '100%', maxWidth: '480px', padding: '28px', display: 'flex', flexDirection: 'column', gap: '14px'}}>
            <h2 id="customer-modal-title">{editingCustomer ? t('customers.edit') : t('customers.add')}</h2>
            {!editingCustomer && <label style={{display: 'flex', flexDirection: 'column', gap: '6px'}}>{t('common.username')}<input className="search-bar" style={{width: '100%'}} value={customerForm.username} onChange={event => setCustomerForm({ ...customerForm, username: event.target.value })} minLength={3} maxLength={50} required /></label>}
            <label style={{display: 'flex', flexDirection: 'column', gap: '6px'}}>{editingCustomer ? t('customers.newPasswordOptional') : t('common.password')}<input className="search-bar" style={{width: '100%'}} type="password" value={customerForm.password} onChange={event => setCustomerForm({ ...customerForm, password: event.target.value })} minLength={editingCustomer && !customerForm.password ? undefined : 8} maxLength={72} required={!editingCustomer} autoComplete="new-password" /></label>
            <label style={{display: 'flex', flexDirection: 'column', gap: '6px'}}>{t('common.fullName')}<input className="search-bar" style={{width: '100%'}} value={customerForm.fullName} onChange={event => setCustomerForm({ ...customerForm, fullName: event.target.value })} maxLength={100} required /></label>
            <label style={{display: 'flex', flexDirection: 'column', gap: '6px'}}>{t('common.tier')}<select className="search-bar" style={{width: '100%'}} value={customerForm.tierId} onChange={event => setCustomerForm({ ...customerForm, tierId: event.target.value })}><option value="1">{t('customers.standard')}</option><option value="2">{t('customers.vip')}</option></select></label>
            <div style={{display: 'flex', justifyContent: 'flex-end', gap: '10px', marginTop: '8px'}}>
              <button className="cyber-btn" type="button" onClick={closeCustomerModal}>{t('common.cancel')}</button>
              <button className="cyber-btn" type="submit">{editingCustomer ? t('customers.saveChanges') : t('customers.create')}</button>
            </div>
          </form>
        </div>
      )}
      {topUpCustomer && (
        <div role="presentation" onClick={() => !topUpBusy && setTopUpCustomer(null)} style={{position: 'fixed', inset: 0, zIndex: 1000, display: 'flex', alignItems: 'center', justifyContent: 'center', padding: '20px', background: 'rgba(0,0,0,0.78)', backdropFilter: 'blur(9px)'}}>
          <form role="dialog" aria-modal="true" aria-labelledby="topup-modal-title" onClick={event => event.stopPropagation()} onSubmit={event => void submitTopUp(event)} className="glass-panel" style={{width: '100%', maxWidth: '560px', padding: '28px', display: 'flex', flexDirection: 'column', gap: '18px'}}>
            <div>
              <h2 id="topup-modal-title">{t('topUp.title')}</h2>
              <p style={{color: 'var(--text-muted)', marginTop: '6px'}}>{topUpCustomer.username} · {t('common.customer')} #{topUpCustomer.customer_ID}</p>
            </div>
            <div>
              <p style={{marginBottom: '10px', color: 'var(--text-muted)'}}>{t('topUp.quickSelect')}</p>
              <div style={{display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: '8px'}}>
                {[50000, 100000, 200000, 500000].map(amount => (
                  <button key={amount} className="cyber-btn" type="button" aria-pressed={Number(topUpAmount) === amount} onClick={() => setTopUpAmount(String(amount))} style={{padding: '9px 6px', borderColor: Number(topUpAmount) === amount ? 'var(--secondary)' : undefined}}>
                    {formatMoney(amount)}
                  </button>
                ))}
              </div>
            </div>
            <label style={{display: 'flex', flexDirection: 'column', gap: '7px'}}>
              {t('topUp.customAmount')}
              <input className="search-bar" style={{width: '100%'}} type="number" min="1" step="1000" value={topUpAmount} onChange={event => setTopUpAmount(event.target.value)} required />
            </label>
            <div className="glass-panel" style={{padding: '16px', display: 'flex', justifyContent: 'space-between', alignItems: 'center'}}>
              <div>
                <p style={{color: 'var(--text-muted)'}}>{t('topUp.paidAmount')}: {validTopUpAmount ? formatMoney(parsedTopUpAmount) : '—'}</p>
                <p style={{color: 'var(--status-available)', marginTop: '4px'}}>{t('topUp.bonus')}: + {formatMoney(topUpBonus)}</p>
              </div>
              <div style={{textAlign: 'right'}}>
                <p style={{color: 'var(--text-muted)', fontSize: '12px'}}>{t('topUp.totalReceived')}</p>
                <strong style={{fontSize: '24px', color: 'var(--primary)'}}>{formatMoney(totalTopUpCredit)}</strong>
              </div>
            </div>
            <div className="glass-panel" style={{padding: '14px', display: 'flex', alignItems: 'center', gap: '14px'}}>
              {topUpQrUrl && <img src={topUpQrUrl} alt={t('topUp.qrAlt')} style={{width: '120px', height: '120px', flex: '0 0 120px', objectFit: 'contain', background: '#fff', borderRadius: '8px'}} />}
              <div>
                <strong style={{color: 'var(--secondary)'}}>{t('topUp.qrName', { bank: TOP_UP_BANK.name })}</strong>
                <p style={{marginTop: '6px'}}>{t('topUp.accountNumber', { account: TOP_UP_BANK.accountNumber })}</p>
                <p style={{marginTop: '6px'}}>{t('topUp.scanToPay')}</p>
                <p style={{color: 'var(--text-muted)', fontSize: '12px', marginTop: '4px'}}>{t('topUp.qrNote')}</p>
              </div>
            </div>
            <div style={{display: 'flex', justifyContent: 'flex-end', gap: '10px'}}>
              <button className="cyber-btn" type="button" disabled={topUpBusy} onClick={() => setTopUpCustomer(null)}>{t('topUp.cancel')}</button>
              <button className="cyber-btn" type="submit" disabled={!validTopUpAmount || topUpBusy}>{topUpBusy ? t('topUp.processing') : t('topUp.confirm')}</button>
            </div>
          </form>
        </div>
      )}
    </div>
  );
}

function CafeShopView() {
  const { t } = useTranslation();
  const [products, setProducts] = useState<ApiProduct[]>([]);
  const [categories, setCategories] = useState<ApiProductCategory[]>([]);
  const [orders, setOrders] = useState<ApiPendingOrder[]>([]);
  const [showProductModal, setShowProductModal] = useState(false);
  const [editingProduct, setEditingProduct] = useState<ApiProduct | null>(null);
  const [productForm, setProductForm] = useState({ name: '', price: '', stock: '', categoryId: '' });

  const loadProducts = useCallback(async () => {
    try {
      const response = await api.get<ApiProduct[]>('/api/products');
      setProducts(response.data);
    } catch (error) {
      console.error('Could not load the menu.', error);
      setProducts([]);
    }
  }, []);

  const loadCategories = useCallback(async () => {
    try {
      const response = await api.get<ApiProductCategory[]>('/api/products/categories');
      setCategories(response.data);
    } catch (error) {
      console.error('Could not load product categories.', error);
      setCategories([]);
    }
  }, []);

  const loadOrders = useCallback(async () => {
    try {
      const response = await api.get<ApiPendingOrder[]>('/api/orders');
      setOrders(response.data);
    } catch (error) {
      console.error('Could not load pending orders.', error);
      setOrders([]);
    }
  }, []);

  useEffect(() => {
    void loadProducts();
    void loadCategories();
    void loadOrders();

    const token = getAccessToken();
    if (!token) return;

    const connection = new HubConnectionBuilder()
      .withUrl(HUB_URL, {
        accessTokenFactory: () => getAccessToken() ?? '',
        withCredentials: true,
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();
    const refreshOrders = () => { void loadOrders(); };
    connection.on('NewOrderReceived', refreshOrders);
    connection.on('OrderStatusChanged', refreshOrders);
    connection.onreconnected(refreshOrders);
    void connection.start().catch((error) => {
      console.error('Could not connect to CafeHub for order updates.', error);
    });

    return () => {
      connection.off('NewOrderReceived', refreshOrders);
      connection.off('OrderStatusChanged', refreshOrders);
      void connection.stop();
    };
  }, [loadCategories, loadOrders, loadProducts]);

  const updateOrderStatus = async (orderId: number, status: 'Preparing' | 'Served' | 'Cancelled') => {
    try {
      await api.put(`/api/orders/${orderId}/status`, { Status: status });
      await loadOrders();
    } catch (error) {
      window.alert(getLocalizedApiError(error, t, 'orders.updateFailed'));
    }
  };

  const openCreateProduct = () => {
    setEditingProduct(null);
    setProductForm({ name: '', price: '', stock: '0', categoryId: categories[0] ? String(categories[0].category_ID) : '' });
    setShowProductModal(true);
  };

  const openEditProduct = (product: ApiProduct) => {
    setEditingProduct(product);
    setProductForm({
      name: product.product_Name,
      price: String(product.price),
      stock: String(product.stock_Quantity),
      categoryId: String(product.category_ID),
    });
    setShowProductModal(true);
  };

  const closeProductModal = () => {
    setShowProductModal(false);
    setEditingProduct(null);
  };

  const saveProduct = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const price = Number(productForm.price);
    const stock = Number(productForm.stock);
    const categoryId = Number(productForm.categoryId);
    if (!Number.isFinite(price) || price <= 0 || !Number.isInteger(stock) || stock < 0 || !Number.isInteger(categoryId) || categoryId <= 0) {
      window.alert(t('products.invalidForm'));
      return;
    }

    const payload = {
      Product_Name: productForm.name.trim(),
      Price: price,
      Stock_Quantity: stock,
      Category_ID: categoryId,
    };

    try {
      if (editingProduct) {
        await api.put(`/api/products/${editingProduct.product_ID}`, payload);
      } else {
        await api.post('/api/products', payload);
      }
      closeProductModal();
      await loadProducts();
    } catch (error) {
      window.alert(getLocalizedApiError(error, t, 'products.saveFailed'));
    }
  };

  const deactivateProduct = async (product: ApiProduct) => {
    if (!window.confirm(t('products.deactivateConfirm', { product: product.product_Name }))) return;
    try {
      await api.delete(`/api/products/${product.product_ID}`);
      await loadProducts();
    } catch (error) {
      window.alert(getLocalizedApiError(error, t, 'products.deactivateFailed'));
    }
  };

  return (
    <div className="shop-layout">
      {/* Kitchen Queue */}
      <div className="glass-panel" style={{ padding: '20px', display: 'flex', flexDirection: 'column' }}>
        <h2 style={{ marginBottom: '16px', color: 'var(--secondary)' }}>{t('orders.pendingTitle')}</h2>
        <div className="order-list">
          {orders.map(order => (
            <div key={order.order_ID} className="order-card glass-panel" style={{borderLeftColor: order.status === 'Pending' ? '#F59E0B' : 'var(--status-inuse)', boxShadow: order.status === 'Pending' ? '0 0 18px rgba(245,158,11,0.14)' : '0 0 18px var(--status-inuse-glow)'}}>
              <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                <h3>Order #{order.order_ID}</h3>
                <span className={`badge ${order.status === 'Pending' ? 'pending' : ''}`} style={order.status === 'Preparing' ? {background: 'var(--status-inuse-glow)', color: 'var(--status-inuse)', display: 'inline-flex', alignItems: 'center', gap: '6px'} : {display: 'inline-flex', alignItems: 'center', gap: '6px'}}>
                  {order.status === 'Pending' && <svg width="10" height="10" viewBox="0 0 10 10" aria-hidden="true"><circle cx="5" cy="5" r="4" fill="#F59E0B"><animate attributeName="opacity" values="1;0.2;1" dur="1.2s" repeatCount="indefinite" /></circle></svg>}
                  {order.status === 'Preparing' && <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="3" aria-hidden="true"><path d="M12 3a9 9 0 1 0 9 9"><animateTransform attributeName="transform" type="rotate" from="0 12 12" to="360 12 12" dur="1s" repeatCount="indefinite" /></path></svg>}
                  {translateStatus(order.status)}
                </span>
              </div>
              <div style={{ color: 'var(--primary)', fontWeight: 600, marginBottom: '8px' }}>
                {order.computer_Name ?? `Computer #${order.computer_ID ?? '—'}`}
              </div>
              <div className="order-items">
                {order.items.map(item => `${item.quantity}x ${item.product_Name}`).join(', ')}
              </div>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: '12px' }}>
                <strong>{formatMoney(order.total_Amount)}</strong>
                {order.status === 'Pending' && <div style={{display: 'flex', gap: '8px'}}>
                  <button className="cyber-btn" onClick={() => void updateOrderStatus(order.order_ID, 'Preparing')}>{t('orders.receive')}</button>
                  <button className="cyber-btn" onClick={() => void updateOrderStatus(order.order_ID, 'Cancelled')} style={{borderColor: 'var(--status-maintenance)', color: 'var(--status-maintenance)', background: 'rgba(239,68,68,0.12)'}}>{t('orders.cancel')}</button>
                </div>}
                {order.status === 'Preparing' && <button className="cyber-btn" onClick={() => void updateOrderStatus(order.order_ID, 'Served')} style={{borderColor: 'var(--status-available)', color: 'var(--status-available)', background: 'var(--status-available-glow)'}}>{t('orders.served')}</button>}
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* Inventory/Menu Grid */}
      <div className="glass-panel" style={{ padding: '20px' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '24px' }}>
          <h2>{t('products.title')}</h2>
          <button className="cyber-btn" onClick={openCreateProduct}>+ {t('products.add')}</button>
        </div>
        <div className="grid-container" style={{ gridTemplateColumns: 'repeat(auto-fill, minmax(180px, 1fr))' }}>
          {products.map(product => (
            <div key={product.product_ID} className="pc-card glass-panel" style={{ alignItems: 'center', textAlign: 'center' }}>
              <div style={{ width: '80px', height: '80px', background: 'rgba(255,255,255,0.05)', borderRadius: '50%', marginBottom: '12px' }}></div>
              <h4 style={{ marginBottom: '4px' }}>{product.product_Name}</h4>
              <p style={{ color: 'var(--status-available)' }}>{formatMoney(product.price)}</p>
              <p style={{ fontSize: '12px', color: 'var(--text-muted)', marginTop: '8px' }}>
                {t('products.stockCount', { count: product.stock_Quantity })} · {product.category_Name}
              </p>
              <div style={{display: 'flex', gap: '8px', marginTop: '8px'}}>
                <button className="cyber-btn" style={{padding: '4px 10px', fontSize: '12px'}} onClick={() => openEditProduct(product)}>{t('common.edit')}</button>
                <button className="cyber-btn" style={{padding: '4px 10px', fontSize: '12px'}} onClick={() => void deactivateProduct(product)}>{t('products.deactivate')}</button>
              </div>
            </div>
          ))}
        </div>
      </div>
      {showProductModal && (
        <div role="presentation" onClick={closeProductModal} style={{position: 'fixed', inset: 0, zIndex: 1000, display: 'flex', alignItems: 'center', justifyContent: 'center', padding: '20px', background: 'rgba(0,0,0,0.75)', backdropFilter: 'blur(8px)'}}>
          <form role="dialog" aria-modal="true" aria-labelledby="product-modal-title" onClick={event => event.stopPropagation()} onSubmit={event => void saveProduct(event)} className="glass-panel" style={{width: '100%', maxWidth: '480px', padding: '28px', display: 'flex', flexDirection: 'column', gap: '14px'}}>
            <h2 id="product-modal-title">{editingProduct ? t('products.edit') : t('products.add')}</h2>
            <label style={{display: 'flex', flexDirection: 'column', gap: '6px'}}>{t('products.name')}<input className="search-bar" style={{width: '100%'}} value={productForm.name} onChange={event => setProductForm({ ...productForm, name: event.target.value })} maxLength={100} required /></label>
            <label style={{display: 'flex', flexDirection: 'column', gap: '6px'}}>{t('products.priceVnd')}<input className="search-bar" style={{width: '100%'}} type="number" min="0.01" step="0.01" value={productForm.price} onChange={event => setProductForm({ ...productForm, price: event.target.value })} required /></label>
            <label style={{display: 'flex', flexDirection: 'column', gap: '6px'}}>{t('products.stock')}<input className="search-bar" style={{width: '100%'}} type="number" min="0" step="1" value={productForm.stock} onChange={event => setProductForm({ ...productForm, stock: event.target.value })} required /></label>
            <label style={{display: 'flex', flexDirection: 'column', gap: '6px'}}>{t('products.category')}<select className="search-bar" style={{width: '100%'}} value={productForm.categoryId} onChange={event => setProductForm({ ...productForm, categoryId: event.target.value })} required>
              <option value="" disabled>{t('products.selectCategory')}</option>
              {categories.map(category => <option key={category.category_ID} value={category.category_ID}>{category.category_Name}</option>)}
            </select></label>
            <div style={{display: 'flex', justifyContent: 'flex-end', gap: '10px', marginTop: '8px'}}>
              <button className="cyber-btn" type="button" onClick={closeProductModal}>{t('common.cancel')}</button>
              <button className="cyber-btn" type="submit">{editingProduct ? t('products.save') : t('products.create')}</button>
            </div>
          </form>
        </div>
      )}
    </div>
  );
}

const TransactionsView = () => {
  const { t } = useTranslation();
  const rows = useAdminRows<ApiTransaction>('/api/admin-data/transactions');

  return (
    <div className="glass-panel" style={{ padding: '24px', flex: 1 }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '24px' }}>
        <h2>{t('transactions.title')}</h2>
        <input type="text" className="search-bar" placeholder={t('transactions.search')} />
      </div>
      <div className="table-container">
        <table className="cyber-table">
          <thead>
            <tr><th>{t('common.id')}</th><th>{t('transactions.customer')}</th><th>{t('transactions.amount')}</th><th>{t('transactions.type')}</th><th>{t('transactions.date')}</th></tr>
          </thead>
          <tbody>
            {rows.map(row => {
              const isCredit = row.trans_Type === 'TopUp' || row.trans_Type === 'Refund';
              return (
                <tr key={row.transaction_ID}>
                  <td>#{row.transaction_ID}</td>
                  <td>{row.customer_Username} · {row.customer_Name}</td>
                  <td style={{ color: isCredit ? 'var(--status-available)' : 'var(--status-maintenance)' }}>
                    {isCredit ? '+' : '-'} {formatMoney(row.amount)}
                  </td>
                  <td>{t(`transactions.${row.trans_Type === 'FoodOrder' ? 'foodOrder' : row.trans_Type === 'TopUp' ? 'topUp' : row.trans_Type === 'Rental' ? 'rental' : row.trans_Type === 'Refund' ? 'refund' : row.trans_Type}`, { defaultValue: row.trans_Type })}</td>
                  <td>{formatDateTime(row.trans_Date)}</td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
};

const SessionsView = () => {
  const { t } = useTranslation();
  const [rows, setRows] = useState<ApiUsageSession[]>([]);
  const [statusFilter, setStatusFilter] = useState('All');
  const [searchTerm, setSearchTerm] = useState('');
  const [endingSessionId, setEndingSessionId] = useState<number | null>(null);

  const loadSessions = useCallback(async () => {
    try {
      const response = await api.get<ApiUsageSession[]>('/api/admin-data/sessions');
      setRows(response.data);
    } catch (error) {
      console.error('Could not load usage sessions.', error);
      setRows([]);
    }
  }, []);

  useEffect(() => { void loadSessions(); }, [loadSessions]);

  const visibleRows = rows.filter(row => {
    const matchesStatus = statusFilter === 'All' || row.status === statusFilter;
    const normalizedSearch = searchTerm.trim().toLowerCase();
    const matchesSearch = !normalizedSearch ||
      row.customer_Name.toLowerCase().includes(normalizedSearch) ||
      row.computer_Name.toLowerCase().includes(normalizedSearch);
    return matchesStatus && matchesSearch;
  });

  const terminateSession = async (session: ApiUsageSession) => {
    if (!window.confirm(t('sessions.confirmTerminate', { sessionId: session.session_ID, computer: session.computer_Name }))) return;
    setEndingSessionId(session.session_ID);
    try {
      await api.post(`/api/sessions/${session.session_ID}/end`);
      await loadSessions();
    } catch (error) {
      window.alert(getLocalizedApiError(error, t, 'sessions.terminateFailed'));
    } finally {
      setEndingSessionId(null);
    }
  };

  return (
    <div className="glass-panel" style={{ padding: '24px', flex: 1 }}>
      <div style={{display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: '12px', marginBottom: '24px'}}>
        <h2>{t('sessions.title')}</h2>
        <div style={{display: 'flex', gap: '10px'}}>
          <input className="search-bar" placeholder={t('sessions.search')} value={searchTerm} onChange={event => setSearchTerm(event.target.value)} />
          <select className="search-bar" style={{width: '170px'}} value={statusFilter} onChange={event => setStatusFilter(event.target.value)}>
            <option value="All">{t('sessions.allStatuses')}</option>
            <option value="Active">{t('status.active')}</option>
            <option value="Completed">{t('status.completed')}</option>
            <option value="Cancelled">{t('status.cancelled')}</option>
          </select>
        </div>
      </div>
      <div className="table-container">
        <table className="cyber-table">
          <thead><tr><th>{t('common.id')}</th><th>{t('sessions.customer')}</th><th>{t('sessions.computer')}</th><th>{t('sessions.start')}</th><th>{t('sessions.end')}</th><th>{t('sessions.hours')}</th><th>{t('sessions.rate')}</th><th>{t('sessions.amount')}</th><th>{t('sessions.status')}</th><th>{t('sessions.action')}</th></tr></thead>
          <tbody>
            {visibleRows.map(row => (
              <tr key={row.session_ID}>
                <td>#{row.session_ID}</td><td>{row.customer_Name}</td><td>{row.computer_Name}</td>
                <td>{formatDateTime(row.start_Time)}</td><td>{formatDateTime(row.end_Time)}</td>
                <td>{row.total_Hours?.toFixed(2) ?? '—'}</td>
                <td>{row.applied_Hourly_Rate == null ? '—' : formatMoney(row.applied_Hourly_Rate)}</td>
                <td>{row.amount == null ? '—' : formatMoney(row.amount)}</td>
                <td><span className={`badge ${row.status === 'Active' ? 'active' : ''}`} style={row.status === 'Active' ? {color: 'var(--status-available)', fontWeight: 700} : undefined}>{row.status === 'Active' ? `● ${t('sessions.active')}` : translateStatus(row.status)}</span></td>
                <td>{row.status === 'Active' && <button className="cyber-btn" disabled={endingSessionId === row.session_ID} onClick={() => void terminateSession(row)} style={{padding: '5px 10px', borderColor: 'var(--status-maintenance)', color: 'var(--status-maintenance)', background: 'rgba(239,68,68,0.12)'}}>{endingSessionId === row.session_ID ? t('sessions.ending') : t('sessions.terminate')}</button>}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};

const FeedbackView = () => {
  const { t } = useTranslation();
  const rows = useAdminRows<ApiFeedback>('/api/admin-data/feedback');

  return (
    <div className="glass-panel" style={{ padding: '24px', flex: 1 }}>
      <h2 style={{ marginBottom: '24px' }}>{t('feedback.title')}</h2>
      <div className="table-container">
        <table className="cyber-table">
          <thead><tr><th>{t('common.id')}</th><th>{t('feedback.customer')}</th><th>{t('feedback.subject')}</th><th>{t('feedback.content')}</th><th>{t('feedback.handler')}</th><th>{t('feedback.status')}</th><th>{t('feedback.submitted')}</th><th>{t('feedback.managerNotes')}</th></tr></thead>
          <tbody>
            {rows.map(row => (
              <tr key={row.feedback_ID}>
                <td>#{row.feedback_ID}</td><td>{row.customer_Name}</td><td>{row.subject}</td>
                <td>{row.content}</td><td>{row.handled_By_Name ?? t('common.notAvailable')}</td><td>{translateStatus(row.status)}</td>
                <td>{formatDateTime(row.submitted_Date)}</td><td>{row.manager_Notes ?? '—'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};

const HRMView = () => {
  const { t } = useTranslation();
  const [employees, setEmployees] = useState<ApiEmployee[]>([]);
  const [payroll, setPayroll] = useState<ApiPayroll[]>([]);
  const [positions, setPositions] = useState<ApiPositionOption[]>([]);
  const schedules = useAdminRows<ApiWorkSchedule>('/api/admin-data/work-schedules');
  const attendance = useAdminRows<ApiAttendance>('/api/admin-data/attendance');
  const leaveRequests = useAdminRows<ApiLeaveRequest>('/api/admin-data/leave-requests');
  const [showEmployeeModal, setShowEmployeeModal] = useState(false);
  const [showPayrollModal, setShowPayrollModal] = useState(false);
  const [employeeBusy, setEmployeeBusy] = useState(false);
  const [payrollBusy, setPayrollBusy] = useState(false);
  const [employeeForm, setEmployeeForm] = useState({ username: '', password: '', fullName: '', positionId: '', baseSalary: '' });
  const currentDate = new Date();
  const [payrollForm, setPayrollForm] = useState({
    month: String(currentDate.getMonth() + 1),
    year: String(currentDate.getFullYear()),
    employeeId: '',
    bonus: '0',
    deduction: '0',
  });

  const loadEmployees = useCallback(async () => {
    try {
      const response = await api.get<ApiEmployee[]>('/api/admin-data/employees');
      setEmployees(response.data);
    } catch (error) {
      console.error('Could not load employees.', error);
      setEmployees([]);
    }
  }, []);

  const loadPayroll = useCallback(async () => {
    try {
      const response = await api.get<ApiPayroll[]>('/api/admin-data/payroll');
      setPayroll(response.data);
    } catch (error) {
      console.error('Could not load payroll records.', error);
      setPayroll([]);
    }
  }, []);

  const loadPositions = useCallback(async () => {
    try {
      const response = await api.get<ApiPositionOption[]>('/api/hrm/positions');
      setPositions(response.data);
    } catch (error) {
      console.error('Could not load employee positions.', error);
      setPositions([]);
    }
  }, []);

  useEffect(() => {
    void loadEmployees();
    void loadPayroll();
    void loadPositions();
  }, [loadEmployees, loadPayroll, loadPositions]);

  const createEmployee = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const salary = Number(employeeForm.baseSalary);
    const positionId = Number(employeeForm.positionId);
    if (!Number.isFinite(salary) || salary < 0 || !Number.isInteger(positionId) || positionId <= 0) {
      window.alert(t('hrm.invalidEmployee'));
      return;
    }

    setEmployeeBusy(true);
    try {
      await api.post('/api/hrm/employees', {
        Username: employeeForm.username.trim(),
        Password: employeeForm.password,
        Full_Name: employeeForm.fullName.trim(),
        Position_ID: positionId,
        Base_Salary: salary,
      });
      setShowEmployeeModal(false);
      setEmployeeForm({ username: '', password: '', fullName: '', positionId: '', baseSalary: '' });
      await loadEmployees();
      window.alert(t('hrm.employeeCreated'));
    } catch (error) {
      window.alert(getLocalizedApiError(error, t, 'hrm.employeeCreateFailed'));
    } finally {
      setEmployeeBusy(false);
    }
  };

  const runPayroll = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const month = Number(payrollForm.month);
    const year = Number(payrollForm.year);
    const bonus = Number(payrollForm.bonus || 0);
    const deduction = Number(payrollForm.deduction || 0);
    const employeeId = payrollForm.employeeId ? Number(payrollForm.employeeId) : null;
    if (!Number.isInteger(month) || month < 1 || month > 12 || !Number.isInteger(year) || year < 2000 || year > 9999 || bonus < 0 || deduction < 0) {
      window.alert(t('hrm.invalidPayroll'));
      return;
    }

    setPayrollBusy(true);
    try {
      const response = await api.post<ApiPayrollRun>('/api/hrm/payroll/run', {
        Pay_Month: month,
        Pay_Year: year,
        Employee_ID: employeeId,
        Bonus: bonus,
        Deduction: deduction,
      });
      setShowPayrollModal(false);
      await loadPayroll();
      window.alert(t('hrm.payrollGenerated', { count: response.data.created_Count }));
    } catch (error) {
      window.alert(getLocalizedApiError(error, t, 'hrm.payrollFailed'));
    } finally {
      setPayrollBusy(false);
    }
  };

  return (
    <div className="glass-panel" style={{ padding: '24px', flex: 1, display: 'flex', flexDirection: 'column', gap: '28px' }}>
      <section>
        <div style={{display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px'}}>
          <h2>{t('hrm.employees')}</h2>
          <button className="cyber-btn" onClick={() => setShowEmployeeModal(true)}>+ {t('hrm.addEmployee')}</button>
        </div>
        <div className="table-container"><table className="cyber-table">
          <thead><tr><th>{t('common.id')}</th><th>{t('common.username')}</th><th>{t('hrm.fullName')}</th><th>{t('hrm.position')}</th><th>{t('hrm.access')}</th><th>{t('hrm.hireDate')}</th><th>{t('hrm.salary')}</th><th>{t('hrm.status')}</th></tr></thead>
          <tbody>{employees.map(row => <tr key={row.employee_ID}><td>#{row.employee_ID}</td><td>{row.username}</td><td>{row.full_Name}</td><td>{row.position_Name}</td><td>{t(`roles.${row.access_Level.toLowerCase()}`, {defaultValue: row.access_Level})}</td><td>{formatDateTime(row.hire_Date)}</td><td>{row.base_Salary == null ? t('common.notAvailable') : formatMoney(row.base_Salary)}</td><td>{translateStatus(row.status)}</td></tr>)}</tbody>
        </table></div>
      </section>
      <section>
        <h2 style={{ marginBottom: '16px' }}>{t('hrm.workSchedules')}</h2>
        <div className="table-container"><table className="cyber-table">
          <thead><tr><th>{t('common.id')}</th><th>{t('common.employee')}</th><th>{t('hrm.workDate')}</th><th>{t('hrm.shift')}</th><th>{t('hrm.hours')}</th><th>{t('hrm.status')}</th></tr></thead>
          <tbody>{schedules.map(row => <tr key={row.schedule_ID}><td>#{row.schedule_ID}</td><td>{row.employee_Name}</td><td>{formatDateTime(row.work_Date)}</td><td>{row.shift_Name}</td><td>{row.start_Time} - {row.end_Time}</td><td>{translateStatus(row.status)}</td></tr>)}</tbody>
        </table></div>
      </section>
      <section>
        <h2 style={{ marginBottom: '16px' }}>{t('hrm.attendance')}</h2>
        <div className="table-container"><table className="cyber-table">
          <thead><tr><th>{t('common.id')}</th><th>{t('common.employee')}</th><th>{t('hrm.date')}</th><th>{t('hrm.shift')}</th><th>{t('hrm.checkIn')}</th><th>{t('hrm.checkOut')}</th><th>{t('hrm.schedule')}</th><th>{t('hrm.note')}</th></tr></thead>
          <tbody>{attendance.map(row => <tr key={row.attendance_ID}><td>#{row.attendance_ID}</td><td>{row.employee_Name}</td><td>{formatDateTime(row.work_Date)}</td><td>{row.shift_Name}</td><td>{formatDateTime(row.check_In_Time)}</td><td>{formatDateTime(row.check_Out_Time)}</td><td>{translateStatus(row.schedule_Status)}</td><td>{row.note ?? t('common.notAvailable')}</td></tr>)}</tbody>
        </table></div>
      </section>
      <section>
        <div style={{display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px'}}>
          <h2>{t('hrm.payroll')}</h2>
          <button className="cyber-btn" onClick={() => setShowPayrollModal(true)}>{t('hrm.runPayroll')}</button>
        </div>
        <div className="table-container"><table className="cyber-table">
          <thead><tr><th>{t('common.id')}</th><th>{t('common.employee')}</th><th>{t('hrm.period')}</th><th>{t('hrm.base')}</th><th>{t('common.bonus')}</th><th>{t('common.deduction')}</th><th>{t('hrm.net')}</th><th>{t('hrm.paidDate')}</th><th>{t('hrm.status')}</th></tr></thead>
          <tbody>{payroll.map(row => <tr key={row.payroll_ID}><td>#{row.payroll_ID}</td><td>{row.employee_Name}</td><td>{row.pay_Month}/{row.pay_Year}</td><td>{formatMoney(row.base_Salary)}</td><td>{formatMoney(row.bonus ?? 0)}</td><td>{formatMoney(row.deduction ?? 0)}</td><td>{row.net_Salary == null ? t('common.notAvailable') : formatMoney(row.net_Salary)}</td><td>{formatDateTime(row.payment_Date)}</td><td>{translateStatus(row.status)}</td></tr>)}</tbody>
        </table></div>
      </section>
      <section>
        <h2 style={{ marginBottom: '16px' }}>{t('hrm.leaveRequests')}</h2>
        <div className="table-container"><table className="cyber-table">
          <thead><tr><th>{t('common.id')}</th><th>{t('common.employee')}</th><th>{t('hrm.type')}</th><th>{t('hrm.from')}</th><th>{t('hrm.to')}</th><th>{t('hrm.reason')}</th><th>{t('hrm.status')}</th><th>{t('hrm.approvedBy')}</th></tr></thead>
          <tbody>{leaveRequests.map(row => <tr key={row.leave_ID}><td>#{row.leave_ID}</td><td>{row.employee_Name}</td><td>{t(`hrm.leaveType.${row.leave_Type}`, {defaultValue: row.leave_Type})}</td><td>{formatDateTime(row.start_Date)}</td><td>{formatDateTime(row.end_Date)}</td><td>{row.reason ?? t('common.notAvailable')}</td><td>{translateStatus(row.status)}</td><td>{row.approved_By_Name ?? t('common.notAvailable')}</td></tr>)}</tbody>
        </table></div>
      </section>
      {showEmployeeModal && (
        <div role="presentation" onClick={() => !employeeBusy && setShowEmployeeModal(false)} style={{position: 'fixed', inset: 0, zIndex: 1000, display: 'flex', alignItems: 'center', justifyContent: 'center', padding: '20px', background: 'rgba(0,0,0,0.78)', backdropFilter: 'blur(9px)'}}>
          <form role="dialog" aria-modal="true" aria-labelledby="employee-modal-title" onClick={event => event.stopPropagation()} onSubmit={event => void createEmployee(event)} className="glass-panel" style={{width: '100%', maxWidth: '500px', padding: '28px', display: 'flex', flexDirection: 'column', gap: '14px'}}>
            <h2 id="employee-modal-title">{t('hrm.addEmployee')}</h2>
            <label style={{display: 'flex', flexDirection: 'column', gap: '6px'}}>{t('hrm.username')}<input className="search-bar" style={{width: '100%'}} value={employeeForm.username} onChange={event => setEmployeeForm({ ...employeeForm, username: event.target.value })} minLength={3} maxLength={50} required /></label>
            <label style={{display: 'flex', flexDirection: 'column', gap: '6px'}}>{t('hrm.password')}<input className="search-bar" style={{width: '100%'}} type="password" autoComplete="new-password" value={employeeForm.password} onChange={event => setEmployeeForm({ ...employeeForm, password: event.target.value })} minLength={8} maxLength={72} required /></label>
            <label style={{display: 'flex', flexDirection: 'column', gap: '6px'}}>{t('hrm.fullName')}<input className="search-bar" style={{width: '100%'}} value={employeeForm.fullName} onChange={event => setEmployeeForm({ ...employeeForm, fullName: event.target.value })} maxLength={100} required /></label>
            <label style={{display: 'flex', flexDirection: 'column', gap: '6px'}}>{t('hrm.position')}<select className="search-bar" style={{width: '100%'}} value={employeeForm.positionId} onChange={event => setEmployeeForm({ ...employeeForm, positionId: event.target.value })} required><option value="" disabled>{t('hrm.selectPosition')}</option>{positions.map(position => <option key={position.position_ID} value={position.position_ID}>{position.position_Name} · {position.department_Name}</option>)}</select></label>
            <label style={{display: 'flex', flexDirection: 'column', gap: '6px'}}>{t('hrm.baseSalary')}<input className="search-bar" style={{width: '100%'}} type="number" min="0" step="1000" value={employeeForm.baseSalary} onChange={event => setEmployeeForm({ ...employeeForm, baseSalary: event.target.value })} required /></label>
            <div style={{display: 'flex', justifyContent: 'flex-end', gap: '10px', marginTop: '8px'}}><button className="cyber-btn" type="button" disabled={employeeBusy} onClick={() => setShowEmployeeModal(false)}>{t('common.cancel')}</button><button className="cyber-btn" type="submit" disabled={employeeBusy}>{employeeBusy ? t('hrm.savingEmployee') : t('hrm.createEmployee')}</button></div>
          </form>
        </div>
      )}
      {showPayrollModal && (
        <div role="presentation" onClick={() => !payrollBusy && setShowPayrollModal(false)} style={{position: 'fixed', inset: 0, zIndex: 1000, display: 'flex', alignItems: 'center', justifyContent: 'center', padding: '20px', background: 'rgba(0,0,0,0.78)', backdropFilter: 'blur(9px)'}}>
          <form role="dialog" aria-modal="true" aria-labelledby="payroll-modal-title" onClick={event => event.stopPropagation()} onSubmit={event => void runPayroll(event)} className="glass-panel" style={{width: '100%', maxWidth: '500px', padding: '28px', display: 'flex', flexDirection: 'column', gap: '14px'}}>
            <h2 id="payroll-modal-title">{t('hrm.runPayroll')}</h2>
            <div style={{display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px'}}>
              <label style={{display: 'flex', flexDirection: 'column', gap: '6px'}}>{t('hrm.periodMonth')}<input className="search-bar" style={{width: '100%'}} type="number" min="1" max="12" value={payrollForm.month} onChange={event => setPayrollForm({ ...payrollForm, month: event.target.value })} required /></label>
              <label style={{display: 'flex', flexDirection: 'column', gap: '6px'}}>{t('hrm.year')}<input className="search-bar" style={{width: '100%'}} type="number" min="2000" max="9999" value={payrollForm.year} onChange={event => setPayrollForm({ ...payrollForm, year: event.target.value })} required /></label>
            </div>
            <label style={{display: 'flex', flexDirection: 'column', gap: '6px'}}>{t('hrm.employeesTarget')}<select className="search-bar" style={{width: '100%'}} value={payrollForm.employeeId} onChange={event => setPayrollForm({ ...payrollForm, employeeId: event.target.value })}><option value="">{t('hrm.allActiveEmployees')}</option>{employees.map(employee => <option key={employee.employee_ID} value={employee.employee_ID}>{employee.full_Name} (#{employee.employee_ID})</option>)}</select></label>
            <div style={{display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px'}}>
              <label style={{display: 'flex', flexDirection: 'column', gap: '6px'}}>{t('hrm.bonus')}<input className="search-bar" style={{width: '100%'}} type="number" min="0" step="1000" value={payrollForm.bonus} onChange={event => setPayrollForm({ ...payrollForm, bonus: event.target.value })} /></label>
              <label style={{display: 'flex', flexDirection: 'column', gap: '6px'}}>{t('hrm.deduction')}<input className="search-bar" style={{width: '100%'}} type="number" min="0" step="1000" value={payrollForm.deduction} onChange={event => setPayrollForm({ ...payrollForm, deduction: event.target.value })} /></label>
            </div>
            <div style={{display: 'flex', justifyContent: 'flex-end', gap: '10px', marginTop: '8px'}}><button className="cyber-btn" type="button" disabled={payrollBusy} onClick={() => setShowPayrollModal(false)}>{t('common.cancel')}</button><button className="cyber-btn" type="submit" disabled={payrollBusy}>{payrollBusy ? t('hrm.generating') : t('hrm.generatePayroll')}</button></div>
          </form>
        </div>
      )}
    </div>
  );
};

const InventoryView = () => {
  const { t } = useTranslation();
  const [products, setProducts] = useState<ApiInventoryProduct[]>([]);
  const [transactions, setTransactions] = useState<ApiInventoryTransaction[]>([]);
  const [showRestockModal, setShowRestockModal] = useState(false);
  const [restockBusy, setRestockBusy] = useState(false);
  const [restockForm, setRestockForm] = useState({ productId: '', quantity: '1', note: '' });

  const loadInventoryProducts = useCallback(async () => {
    try {
      const response = await api.get<ApiInventoryProduct[]>('/api/admin-data/products');
      setProducts(response.data);
    } catch (error) {
      console.error('Could not load inventory products.', error);
      setProducts([]);
    }
  }, []);

  const loadInventoryTransactions = useCallback(async () => {
    try {
      const response = await api.get<ApiInventoryTransaction[]>('/api/admin-data/inventory-transactions');
      setTransactions(response.data);
    } catch (error) {
      console.error('Could not load inventory transactions.', error);
      setTransactions([]);
    }
  }, []);

  useEffect(() => {
    void loadInventoryProducts();
    void loadInventoryTransactions();
  }, [loadInventoryProducts, loadInventoryTransactions]);

  const openRestockModal = () => {
    const firstActiveProduct = products.find(product => product.status === 'Active');
    if (!firstActiveProduct) {
      window.alert(t('inventory.noActiveProducts'));
      return;
    }
    setRestockForm({ productId: String(firstActiveProduct.product_ID), quantity: '1', note: '' });
    setShowRestockModal(true);
  };

  const submitRestock = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const productId = Number(restockForm.productId);
    const quantity = Number(restockForm.quantity);
    if (!Number.isInteger(productId) || productId <= 0 || !Number.isInteger(quantity) || quantity <= 0) {
      window.alert(t('inventory.invalidRestock'));
      return;
    }

    setRestockBusy(true);
    try {
      const response = await api.post('/api/inventory/restock', {
        Product_ID: productId,
        Quantity: quantity,
        Note: restockForm.note.trim() || null,
      });
      setShowRestockModal(false);
      await Promise.all([loadInventoryProducts(), loadInventoryTransactions()]);
      window.alert(t('inventory.restockSuccess', { stock: response.data.stock_After }));
    } catch (error) {
      window.alert(getLocalizedApiError(error, t, 'inventory.restockFailed'));
    } finally {
      setRestockBusy(false);
    }
  };

  return (
    <div className="glass-panel" style={{ padding: '24px', flex: 1, display: 'flex', flexDirection: 'column', gap: '28px' }}>
      <section>
        <div style={{display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px'}}>
          <h2>{t('inventory.productStock')}</h2>
          <button className="cyber-btn" style={{padding: '12px 20px', fontSize: '15px'}} onClick={openRestockModal}>+ {t('inventory.restock')}</button>
        </div>
        <div className="table-container"><table className="cyber-table">
          <thead><tr><th>{t('common.id')}</th><th>{t('inventory.product')}</th><th>{t('common.category')}</th><th>{t('inventory.price')}</th><th>{t('inventory.stock')}</th><th>{t('inventory.status')}</th></tr></thead>
          <tbody>{products.map(row => <tr key={row.product_ID}><td>#{row.product_ID}</td><td>{row.product_Name}</td><td>{row.category_Name}</td><td>{formatMoney(row.price)}</td><td>{row.stock_Quantity}</td><td>{translateStatus(row.status ?? 'Active')}</td></tr>)}</tbody>
        </table></div>
      </section>
      <section>
        <h2 style={{ marginBottom: '16px' }}>{t('inventory.transactions')}</h2>
        <div className="table-container"><table className="cyber-table">
          <thead><tr><th>{t('common.id')}</th><th>{t('inventory.product')}</th><th>{t('inventory.type')}</th><th>{t('inventory.quantity')}</th><th>{t('inventory.employee')}</th><th>{t('inventory.date')}</th><th>{t('inventory.note')}</th></tr></thead>
          <tbody>{transactions.map(row => <tr key={row.inv_Trans_ID}><td>#{row.inv_Trans_ID}</td><td>{row.product_Name}</td><td>{row.trans_Type === 'Import' ? t('inventory.import') : row.trans_Type === 'Export' ? t('inventory.export') : row.trans_Type}</td><td>{row.quantity}</td><td>{row.employee_Name}</td><td>{formatDateTime(row.created_Date)}</td><td>{row.note ?? t('common.notAvailable')}</td></tr>)}</tbody>
        </table></div>
      </section>
      {showRestockModal && (
        <div role="presentation" onClick={() => !restockBusy && setShowRestockModal(false)} style={{position: 'fixed', inset: 0, zIndex: 1000, display: 'flex', alignItems: 'center', justifyContent: 'center', padding: '20px', background: 'rgba(0,0,0,0.78)', backdropFilter: 'blur(9px)'}}>
          <form role="dialog" aria-modal="true" aria-labelledby="restock-modal-title" onClick={event => event.stopPropagation()} onSubmit={event => void submitRestock(event)} className="glass-panel" style={{width: '100%', maxWidth: '480px', padding: '28px', display: 'flex', flexDirection: 'column', gap: '14px'}}>
            <h2 id="restock-modal-title">{t('inventory.restockTitle')}</h2>
            <label style={{display: 'flex', flexDirection: 'column', gap: '6px'}}>{t('inventory.product')}<select className="search-bar" style={{width: '100%'}} value={restockForm.productId} onChange={event => setRestockForm({ ...restockForm, productId: event.target.value })} required>
              <option value="" disabled>{t('inventory.selectProduct')}</option>
              {products.filter(product => product.status === 'Active').map(product => <option key={product.product_ID} value={product.product_ID}>{product.product_Name} · {t('products.stockCount', { count: product.stock_Quantity })}</option>)}
            </select></label>
            <label style={{display: 'flex', flexDirection: 'column', gap: '6px'}}>{t('inventory.quantityToImport')}<input className="search-bar" style={{width: '100%'}} type="number" min="1" step="1" value={restockForm.quantity} onChange={event => setRestockForm({ ...restockForm, quantity: event.target.value })} required /></label>
            <label style={{display: 'flex', flexDirection: 'column', gap: '6px'}}>{t('inventory.note')}<input className="search-bar" style={{width: '100%'}} value={restockForm.note} onChange={event => setRestockForm({ ...restockForm, note: event.target.value })} maxLength={255} placeholder={t('inventory.notePlaceholder')} /></label>
            <div style={{display: 'flex', justifyContent: 'flex-end', gap: '10px', marginTop: '8px'}}><button className="cyber-btn" type="button" disabled={restockBusy} onClick={() => setShowRestockModal(false)}>{t('common.cancel')}</button><button className="cyber-btn" type="submit" disabled={restockBusy}>{restockBusy ? t('inventory.importing') : t('inventory.confirmRestock')}</button></div>
          </form>
        </div>
      )}
    </div>
  );
};

// Main App
function App() {
  const { t, i18n: i18nInstance } = useTranslation();
  const [activeTab, setActiveTab] = useState('dashboard');
  const [token, setToken] = useState(localStorage.getItem('accessToken'));
  const [isLoggedIn, setIsLoggedIn] = useState(Boolean(localStorage.getItem('accessToken')));
  const [identifier, setIdentifier] = useState('');
  const [password, setPassword] = useState('');
  const [loginBusy, setLoginBusy] = useState(false);

  useEffect(() => {
    const language = i18nInstance.resolvedLanguage?.startsWith('en') ? 'en' : 'vi';
    document.documentElement.lang = language;
    document.title = t('seo.title');

    let description = document.querySelector<HTMLMetaElement>('meta[name="description"]');
    if (!description) {
      description = document.createElement('meta');
      description.name = 'description';
      document.head.appendChild(description);
    }
    description.content = t('seo.description');
  }, [i18nInstance.resolvedLanguage, t]);

  useEffect(() => {
    if (token) {
      api.defaults.headers.common.Authorization = `Bearer ${token}`;
    } else {
      delete api.defaults.headers.common.Authorization;
    }
  }, [token]);

  const handleLogin = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setLoginBusy(true);

    try {
      const response = await api.post<LoginResponse>('/api/auth/login', {
        // Username is included for the requested client contract; Identifier is the
        // existing backend LoginRequest property and keeps the current API compatible.
        Username: identifier.trim(),
        Identifier: identifier.trim(),
        Password: password,
      });
      const accessToken = response.data.accessToken;
      if (!accessToken) {
        throw new Error(t('login.missingToken'));
      }

      localStorage.setItem('accessToken', accessToken);
      api.defaults.headers.common.Authorization = `Bearer ${accessToken}`;
      setToken(accessToken);
      setIsLoggedIn(true);
    } catch (error) {
      const message = axios.isAxiosError(error)
        ? error.response?.data?.detail ?? t('login.failed')
        : t('login.tryAgain');
      window.alert(typeof message === 'string' ? message : JSON.stringify(message));
    } finally {
      setLoginBusy(false);
    }
  };

  if (!isLoggedIn || !token) {
    return (
      <div style={{ minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center', padding: '24px', background: 'var(--bg-main)', backgroundImage: 'radial-gradient(circle at 20% 35%, rgba(0, 240, 255, 0.12), transparent 35%), radial-gradient(circle at 80% 70%, rgba(123, 97, 255, 0.12), transparent 35%)' }}>
        <form onSubmit={handleLogin} className="glass-panel" style={{ width: '100%', maxWidth: '420px', padding: '36px', display: 'flex', flexDirection: 'column', gap: '18px', boxShadow: '0 20px 70px rgba(0,0,0,0.45)' }}>
          <div style={{ textAlign: 'center', marginBottom: '8px' }}>
            <div className="brand-icon" style={{ marginBottom: '12px' }}><MonitorIcon /></div>
            <h1 className="text-gradient" style={{ fontSize: '28px' }}>{t('login.brand')}</h1>
            <p style={{ color: 'var(--text-muted)', marginTop: '8px' }}>{t('login.subtitle')}</p>
          </div>
          <label style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
            <span>{t('login.username')}</span>
            <input
              className="search-bar"
              style={{ width: '100%' }}
              autoComplete="username"
              value={identifier}
              onChange={event => setIdentifier(event.target.value)}
              required
            />
          </label>
          <label style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
            <span>{t('login.password')}</span>
            <input
              className="search-bar"
              style={{ width: '100%' }}
              type="password"
              autoComplete="current-password"
              value={password}
              onChange={event => setPassword(event.target.value)}
              required
            />
          </label>
          <button className="cyber-btn" type="submit" disabled={loginBusy} style={{ marginTop: '8px' }}>
            {loginBusy ? t('login.submitting') : t('login.submit')}
          </button>
        </form>
      </div>
    );
  }

  const logout = () => {
    localStorage.removeItem('accessToken');
    delete api.defaults.headers.common.Authorization;
    setToken(null);
    setIsLoggedIn(false);
    window.location.reload();
  };

  return (
    <div className="app-container">
      {/* Sidebar */}
      <aside className="sidebar">
        <div className="brand">
          <div className="brand-icon"><MonitorIcon /></div>
          <span className="text-gradient">NetZone</span>
        </div>
        <nav className="nav-menu">
          <div className="nav-section">{t('navigation.main')}</div>
          <div className={`nav-item ${activeTab === 'dashboard' ? 'active' : ''}`} onClick={() => setActiveTab('dashboard')}>
            <LayoutGridIcon /> {t('navigation.dashboard')}
          </div>

          <div className="nav-section" style={{ marginTop: '16px', marginBottom: '8px', fontSize: '11px', color: 'var(--text-muted)', fontWeight: 'bold', letterSpacing: '1px' }}>{t('navigation.crm')}</div>
          <div className={`nav-item ${activeTab === 'users' ? 'active' : ''}`} onClick={() => setActiveTab('users')}>
            <UsersIcon /> {t('navigation.members')}
          </div>
          <div className={`nav-item ${activeTab === 'feedback' ? 'active' : ''}`} onClick={() => setActiveTab('feedback')}>
            <MessageIcon /> {t('navigation.feedback')}
          </div>

          <div className="nav-section" style={{ marginTop: '16px', marginBottom: '8px', fontSize: '11px', color: 'var(--text-muted)', fontWeight: 'bold', letterSpacing: '1px' }}>{t('navigation.enterprise')}</div>
          <div className={`nav-item ${activeTab === 'hrm' ? 'active' : ''}`} onClick={() => setActiveTab('hrm')}>
            <BriefcaseIcon /> {t('navigation.hrm')}
          </div>
          <div className={`nav-item ${activeTab === 'inventory' ? 'active' : ''}`} onClick={() => setActiveTab('inventory')}>
            <BoxIcon /> {t('navigation.inventory')}
          </div>

          <div className="nav-section" style={{ marginTop: '16px', marginBottom: '8px', fontSize: '11px', color: 'var(--text-muted)', fontWeight: 'bold', letterSpacing: '1px' }}>{t('navigation.salesLogs')}</div>
          <div className={`nav-item ${activeTab === 'shop' ? 'active' : ''}`} onClick={() => setActiveTab('shop')}>
            <ShoppingCartIcon /> {t('navigation.cafeShop')}
          </div>
          <div className={`nav-item ${activeTab === 'transactions' ? 'active' : ''}`} onClick={() => setActiveTab('transactions')}>
            <CreditCardIcon /> {t('navigation.financials')}
          </div>
          <div className={`nav-item ${activeTab === 'sessions' ? 'active' : ''}`} onClick={() => setActiveTab('sessions')}>
            <HistoryIcon /> {t('navigation.sessions')}
          </div>
        </nav>
      </aside>

      {/* Main Content */}
      <main className="main-content">
        <header className="header">
          <div>
            <p>
              {activeTab === 'dashboard' ? t('header.dashboardSubtitle') :
                activeTab === 'users' ? t('header.usersSubtitle') :
                  activeTab === 'shop' ? t('header.shopSubtitle') :
                    activeTab === 'transactions' ? t('header.transactionsSubtitle') :
                      activeTab === 'sessions' ? t('header.sessionsSubtitle') :
                        activeTab === 'feedback' ? t('header.feedbackSubtitle') :
                          activeTab === 'hrm' ? t('header.hrmSubtitle') :
                            activeTab === 'inventory' ? t('header.inventorySubtitle') :
                              t('header.settingsSubtitle')}
            </p>
            <h1>
              {activeTab === 'dashboard' ? t('header.dashboardTitle') :
                activeTab === 'users' ? t('header.usersTitle') :
                  activeTab === 'shop' ? t('header.shopTitle') :
                    activeTab === 'transactions' ? t('header.transactionsTitle') :
                      activeTab === 'sessions' ? t('header.sessionsTitle') :
                        activeTab === 'feedback' ? t('header.feedbackTitle') :
                          activeTab === 'hrm' ? t('header.hrmTitle') :
                            activeTab === 'inventory' ? t('header.inventoryTitle') :
                              t('header.settingsTitle')}
            </h1>
          </div>
          <div className="user-profile">
            <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'flex-end' }}>
              <span style={{ fontSize: '14px', fontWeight: 600 }}>{t('header.adminName')}</span>
              <span style={{ fontSize: '12px', color: 'var(--text-muted)' }}>{t('header.adminRole')}</span>
            </div>
            <div style={{ width: '36px', height: '36px', borderRadius: '50%', background: 'var(--primary)', display: 'flex', alignItems: 'center', justifyContent: 'center', color: '#000', fontWeight: 'bold' }}>
              A
            </div>
          </div>
          <div role="group" aria-label={t('navigation.language')} style={{display: 'flex', gap: '4px'}}>
            <button className="cyber-btn" type="button" aria-pressed={i18nInstance.resolvedLanguage?.startsWith('en')} onClick={() => void i18nInstance.changeLanguage('en')} style={{padding: '6px 10px'}}>EN</button>
            <button className="cyber-btn" type="button" aria-pressed={i18nInstance.resolvedLanguage?.startsWith('vi')} onClick={() => void i18nInstance.changeLanguage('vi')} style={{padding: '6px 10px'}}>VI</button>
          </div>
          <button className="cyber-btn" type="button" onClick={logout}>{t('navigation.logout')}</button>
        </header>

        {activeTab === 'dashboard' && <DashboardView />}
        {activeTab === 'users' && <UsersView />}
        {activeTab === 'shop' && <CafeShopView />}
        {activeTab === 'transactions' && <TransactionsView />}
        {activeTab === 'sessions' && <SessionsView />}
        {activeTab === 'feedback' && <FeedbackView />}
        {activeTab === 'hrm' && <HRMView />}
        {activeTab === 'inventory' && <InventoryView />}
        {activeTab === 'settings' && (
          <div className="glass-panel" style={{ padding: '32px', textAlign: 'center', flex: 1 }}>
            <h2>{t('header.settingsTitle')}</h2>
            <p style={{ color: 'var(--text-muted)' }}>{t('header.settingsPlaceholder')}</p>
          </div>
        )}
      </main>
    </div>
  );
}

export default App;
