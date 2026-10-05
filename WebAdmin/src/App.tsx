import { useCallback, useEffect, useState, type FormEvent } from 'react';
import axios from 'axios';
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';

const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5200').replace(/\/$/, '');
const HUB_URL = import.meta.env.VITE_SIGNALR_HUB_URL ?? `${API_BASE_URL}/cafeHub`;

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
  username: string;
  full_Name: string;
  balance: number;
  tier_Name: string;
  status: string;
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

// Lucide icons equivalent
const LayoutGridIcon = () => <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><rect width="7" height="7" x="3" y="3" rx="1"/><rect width="7" height="7" x="14" y="3" rx="1"/><rect width="7" height="7" x="14" y="14" rx="1"/><rect width="7" height="7" x="3" y="14" rx="1"/></svg>;
const MonitorIcon = () => <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><rect width="20" height="14" x="2" y="3" rx="2"/><line x1="8" x2="16" y1="21" y2="21"/><line x1="12" x2="12" y1="17" y2="21"/></svg>;
const UsersIcon = () => <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M22 21v-2a4 4 0 0 0-3-3.87"/><path d="M16 3.13a4 4 0 0 1 0 7.75"/></svg>;
const ShoppingCartIcon = () => <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><circle cx="8" cy="21" r="1"/><circle cx="19" cy="21" r="1"/><path d="M2.05 2.05h2l2.66 12.42a2 2 0 0 0 2 1.58h9.78a2 2 0 0 0 1.95-1.57l1.65-7.43H5.12"/></svg>;
const SettingsIcon = () => <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M12.22 2h-.44a2 2 0 0 0-2 2v.18a2 2 0 0 1-1 1.73l-.43.25a2 2 0 0 1-2 0l-.15-.08a2 2 0 0 0-2.73.73l-.22.38a2 2 0 0 0 .73 2.73l.15.1a2 2 0 0 1 1 1.72v.51a2 2 0 0 1-1 1.74l-.15.09a2 2 0 0 0-.73 2.73l.22.38a2 2 0 0 0 2.73.73l.15-.08a2 2 0 0 1 2 0l.43.25a2 2 0 0 1 1 1.73V20a2 2 0 0 0 2 2h.44a2 2 0 0 0 2-2v-.18a2 2 0 0 1 1-1.73l.43-.25a2 2 0 0 1 2 0l.15.08a2 2 0 0 0 2.73-.73l.22-.39a2 2 0 0 0-.73-2.73l-.15-.08a2 2 0 0 1-1-1.74v-.5a2 2 0 0 1 1-1.74l.15-.09a2 2 0 0 0 .73-2.73l-.22-.38a2 2 0 0 0-2.73-.73l-.15.08a2 2 0 0 1-2 0l-.43-.25a2 2 0 0 1-1-1.73V4a2 2 0 0 0-2-2z"/><circle cx="12" cy="12" r="3"/></svg>;
const HistoryIcon = () => <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M3 12a9 9 0 1 0 9-9 9.75 9.75 0 0 0-6.74 2.74L3 8"/><path d="M3 3v5h5"/><path d="M12 7v5l4 2"/></svg>;
const CreditCardIcon = () => <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><rect width="20" height="14" x="2" y="5" rx="2"/><line x1="2" x2="22" y1="10" y2="10"/></svg>;
const MessageIcon = () => <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M7.9 20A9 9 0 1 0 4 16.1L2 22Z"/></svg>;
const BriefcaseIcon = () => <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><rect width="20" height="14" x="2" y="7" rx="2" ry="2"/><path d="M16 21V5a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v16"/></svg>;
const BoxIcon = () => <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M21 8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16Z"/><path d="m3.3 7 8.7 5 8.7-5"/><path d="M12 22V12"/></svg>;

type ApiComputer = {
  computer_ID: number;
  computer_Name: string;
  status: string;
  hourly_Rate: number;
  active_Session_ID: number | null;
  active_Customer_ID: number | null;
};

type PcViewModel = {
  id: number;
  name: string;
  status: 'available' | 'inuse' | 'maintenance';
  user: string | null;
  time: string | null;
  specs: string;
  rate: string;
  activeSessionId: number | null;
  activeCustomerId: number | null;
};

const formatMoney = (amount: number) =>
  new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 0 }).format(amount) + 'đ';

const formatDateTime = (value: string | null | undefined) =>
  value ? new Date(value).toLocaleString('vi-VN') : '—';

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

const toPcViewModel = (computer: ApiComputer): PcViewModel => {
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
    user: computer.active_Customer_ID ? `Customer #${computer.active_Customer_ID}` : null,
    time: null,
    specs: `Station #${computer.computer_ID}`,
    rate: `${formatMoney(computer.hourly_Rate)}/hr`,
    activeSessionId: computer.active_Session_ID,
    activeCustomerId: computer.active_Customer_ID,
  };
};

// Sub-components
const DashboardView = () => {
  const [pcs, setPcs] = useState<PcViewModel[]>([]);

  const loadComputers = useCallback(async () => {
    try {
      const response = await api.get<ApiComputer[]>('/api/computers');
      setPcs(response.data.map(toPcViewModel));
    } catch (error) {
      console.error('Could not load computers from the API.', error);
      setPcs([]);
    }
  }, []);

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
      console.error('Could not connect to the CafeHub SignalR hub.', error);
    });

    return () => {
      connection.off('ComputerStatusChanged', refreshComputers);
      void connection.stop();
    };
  }, [loadComputers]);

  const handleComputerClick = async (pc: PcViewModel) => {
    if (!getAccessToken()) {
      window.alert('Please sign in as an employee/admin first. Store the JWT in localStorage under "accessToken".');
      return;
    }

    try {
      if (pc.status === 'maintenance') {
        window.alert(`${pc.name} is under maintenance.`);
        return;
      }

      if (pc.status === 'available') {
        const customerInput = window.prompt(`Enter Customer ID to start a session on ${pc.name}:`);
        if (customerInput === null) return;
        const customerId = Number(customerInput);
        if (!Number.isInteger(customerId) || customerId <= 0) {
          window.alert('Customer ID must be a positive integer.');
          return;
        }

        await api.post('/api/sessions/start', {
          computer_ID: pc.id,
          customer_ID: customerId,
        });
        window.alert(`Session started on ${pc.name}.`);
      } else {
        const action = window.prompt(
          `${pc.name} is in use. Enter 1 to top up or 2 to end the session:`,
        );
        if (action === '1') {
          if (!pc.activeCustomerId) {
            window.alert('The active customer could not be identified. Refresh the dashboard and try again.');
            return;
          }
          const amountInput = window.prompt(`Enter top-up amount for Customer #${pc.activeCustomerId}:`);
          if (amountInput === null) return;
          const amount = Number(amountInput.replaceAll(',', '').trim());
          if (!Number.isFinite(amount) || amount <= 0) {
            window.alert('Top-up amount must be greater than zero.');
            return;
          }

          await api.post('/api/transactions/topup', {
            customer_ID: pc.activeCustomerId,
            amount,
            combo_ID: null,
          });
          window.alert('Top-up completed.');
        } else if (action === '2') {
          if (!pc.activeSessionId) {
            window.alert('The active session could not be identified. Refresh the dashboard and try again.');
            return;
          }

          await api.post(`/api/sessions/${pc.activeSessionId}/end`);
          window.alert(`Session on ${pc.name} ended and the client was locked.`);
        } else {
          return;
        }
      }

      await loadComputers();
    } catch (error) {
      const message = axios.isAxiosError(error)
        ? error.response?.data?.detail ?? error.response?.data ?? error.message
        : 'The requested operation failed.';
      window.alert(typeof message === 'string' ? message : JSON.stringify(message));
    }
  };

  const stats = {
    inuse: pcs.filter(p => p.status === 'inuse').length,
    available: pcs.filter(p => p.status === 'available').length,
    maintenance: pcs.filter(p => p.status === 'maintenance').length,
    total: pcs.length
  };

  return (
    <>
      <div className="stats-container">
        <div className="stat-box glass-panel" style={{border: '1px solid var(--status-inuse)', boxShadow: '0 4px 15px var(--status-inuse-glow)'}}>
          <span className="stat-title" style={{color: 'var(--status-inuse)'}}>Active PCs</span>
          <span className="stat-value">{stats.inuse}/{stats.total}</span>
        </div>
        <div className="stat-box glass-panel" style={{border: '1px solid var(--status-available)'}}>
          <span className="stat-title" style={{color: 'var(--status-available)'}}>Available PCs</span>
          <span className="stat-value">{stats.available}</span>
        </div>
        <div className="stat-box glass-panel" style={{border: '1px solid var(--status-maintenance)'}}>
          <span className="stat-title" style={{color: 'var(--status-maintenance)'}}>Maintenance</span>
          <span className="stat-value">{stats.maintenance}</span>
        </div>
      </div>
      <div className="grid-container">
        {pcs.map((pc) => (
          <div key={pc.id} className={`pc-card glass-panel ${pc.status}`} onClick={() => void handleComputerClick(pc)}>
            <div className="pc-header">
              <span className="pc-name">{pc.name}</span>
              <div className="status-indicator"></div>
            </div>
            <div className="pc-info">
              {pc.status === 'inuse' ? (
                <><span>User: {pc.user}</span><span>Time: {pc.time}</span></>
              ) : pc.status === 'available' ? (
                <><span>Idle</span><span>Rate: {pc.rate}</span></>
              ) : (
                <><span>Offline</span><span>Maintenance</span></>
              )}
            </div>
            <div className="pc-footer"><span>{pc.specs}</span></div>
          </div>
        ))}
      </div>
    </>
  );
};

function UsersView() {
  const [users, setUsers] = useState<ApiCustomer[]>([]);

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

  const topUpCustomer = async (customer: ApiCustomer) => {
    const amountInput = window.prompt(`Top up ${customer.username} (Customer #${customer.customer_ID}):`);
    if (amountInput === null) return;
    const amount = Number(amountInput.replaceAll(',', '').trim());
    if (!Number.isFinite(amount) || amount <= 0) {
      window.alert('Top-up amount must be greater than zero.');
      return;
    }

    try {
      await api.post('/api/transactions/topup', {
        customer_ID: customer.customer_ID,
        amount,
        combo_ID: null,
      });
      await loadUsers();
      window.alert('Top-up completed.');
    } catch (error) {
      const message = axios.isAxiosError(error)
        ? error.response?.data?.detail ?? error.response?.data ?? error.message
        : 'Top-up failed.';
      window.alert(typeof message === 'string' ? message : JSON.stringify(message));
    }
  };

  return (
    <div className="glass-panel" style={{padding: '24px', flex: 1}}>
      <div style={{display: 'flex', justifyContent: 'space-between', marginBottom: '24px'}}>
        <h2>Customer Management</h2>
        <input type="text" className="search-bar" placeholder="Search username, phone..." />
      </div>
      <div className="table-container">
        <table className="cyber-table">
          <thead>
            <tr>
              <th>ID</th><th>Username</th><th>Full Name</th><th>Balance</th><th>Tier</th><th>Status</th><th>Action</th>
            </tr>
          </thead>
          <tbody>
            {users.map(user => (
              <tr key={user.customer_ID}>
                <td>#{user.customer_ID}</td>
                <td style={{fontWeight: 600, color: 'var(--primary)'}}>{user.username}</td>
                <td>{user.full_Name}</td>
                <td style={{color: 'var(--status-available)'}}>{formatMoney(user.balance)}</td>
                <td>{user.tier_Name}</td>
                <td><span className={`badge ${user.status === 'Active' ? 'active' : ''}`}>{user.status}</span></td>
                <td><button className="cyber-btn" style={{padding: '4px 12px', fontSize: '12px'}} onClick={() => void topUpCustomer(user)}>Top Up</button></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

function CafeShopView() {
  const [products, setProducts] = useState<ApiProduct[]>([]);
  const [orders, setOrders] = useState<ApiPendingOrder[]>([]);

  const loadProducts = useCallback(async () => {
    try {
      const response = await api.get<ApiProduct[]>('/api/products');
      setProducts(response.data);
    } catch (error) {
      console.error('Could not load the menu.', error);
      setProducts([]);
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
  }, [loadOrders, loadProducts]);

  const completeOrder = async (orderId: number) => {
    try {
      await api.post(`/api/orders/${orderId}/complete`);
      await loadOrders();
    } catch (error) {
      const message = axios.isAxiosError(error)
        ? error.response?.data?.detail ?? error.response?.data ?? error.message
        : 'Could not complete the order.';
      window.alert(typeof message === 'string' ? message : JSON.stringify(message));
    }
  };

  return (
    <div className="shop-layout">
      {/* Kitchen Queue */}
      <div className="glass-panel" style={{padding: '20px', display: 'flex', flexDirection: 'column'}}>
        <h2 style={{marginBottom: '16px', color: 'var(--secondary)'}}>Pending Orders</h2>
        <div className="order-list">
          {orders.map(order => (
            <div key={order.order_ID} className="order-card glass-panel">
              <div style={{display: 'flex', justifyContent: 'space-between'}}>
                <h3>Order #{order.order_ID}</h3>
                <span className="badge pending">{order.status}</span>
              </div>
              <div style={{color: 'var(--primary)', fontWeight: 600, marginBottom: '8px'}}>
                {order.computer_Name ?? `Computer #${order.computer_ID ?? '—'}`}
              </div>
              <div className="order-items">
                {order.items.map(item => `${item.quantity}x ${item.product_Name}`).join(', ')}
              </div>
              <div style={{display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: '12px'}}>
                <strong>{formatMoney(order.total_Amount)}</strong>
                <button className="cyber-btn" onClick={() => void completeOrder(order.order_ID)}>Complete</button>
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* Inventory/Menu Grid */}
      <div className="glass-panel" style={{padding: '20px'}}>
        <div style={{display: 'flex', justifyContent: 'space-between', marginBottom: '24px'}}>
          <h2>Menu & Inventory</h2>
          <button className="cyber-btn">Add Product</button>
        </div>
        <div className="grid-container" style={{gridTemplateColumns: 'repeat(auto-fill, minmax(180px, 1fr))'}}>
          {products.map(product => (
            <div key={product.product_ID} className="pc-card glass-panel" style={{alignItems: 'center', textAlign: 'center'}}>
              <div style={{width: '80px', height: '80px', background: 'rgba(255,255,255,0.05)', borderRadius: '50%', marginBottom: '12px'}}></div>
              <h4 style={{marginBottom: '4px'}}>{product.product_Name}</h4>
              <p style={{color: 'var(--status-available)'}}>{formatMoney(product.price)}</p>
              <p style={{fontSize: '12px', color: 'var(--text-muted)', marginTop: '8px'}}>
                Stock: {product.stock_Quantity} · {product.category_Name}
              </p>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

const TransactionsView = () => {
  const rows = useAdminRows<ApiTransaction>('/api/admin-data/transactions');

  return (
    <div className="glass-panel" style={{padding: '24px', flex: 1}}>
      <div style={{display: 'flex', justifyContent: 'space-between', marginBottom: '24px'}}>
        <h2>Transaction Log</h2>
        <input type="text" className="search-bar" placeholder="Search transactions..." />
      </div>
      <div className="table-container">
        <table className="cyber-table">
          <thead>
            <tr><th>ID</th><th>Customer</th><th>Amount</th><th>Type</th><th>Date</th></tr>
          </thead>
          <tbody>
            {rows.map(row => {
              const isCredit = row.trans_Type === 'TopUp' || row.trans_Type === 'Refund';
              return (
                <tr key={row.transaction_ID}>
                  <td>#{row.transaction_ID}</td>
                  <td>{row.customer_Username} · {row.customer_Name}</td>
                  <td style={{color: isCredit ? 'var(--status-available)' : 'var(--status-maintenance)'}}>
                    {isCredit ? '+' : '-'} {formatMoney(row.amount)}
                  </td>
                  <td>{row.trans_Type}</td>
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
  const rows = useAdminRows<ApiUsageSession>('/api/admin-data/sessions');

  return (
    <div className="glass-panel" style={{padding: '24px', flex: 1}}>
      <h2 style={{marginBottom: '24px'}}>Session History</h2>
      <div className="table-container">
        <table className="cyber-table">
          <thead><tr><th>ID</th><th>Customer</th><th>Computer</th><th>Start</th><th>End</th><th>Hours</th><th>Rate</th><th>Amount</th><th>Status</th></tr></thead>
          <tbody>
            {rows.map(row => (
              <tr key={row.session_ID}>
                <td>#{row.session_ID}</td><td>{row.customer_Name}</td><td>{row.computer_Name}</td>
                <td>{formatDateTime(row.start_Time)}</td><td>{formatDateTime(row.end_Time)}</td>
                <td>{row.total_Hours?.toFixed(2) ?? '—'}</td>
                <td>{row.applied_Hourly_Rate == null ? '—' : formatMoney(row.applied_Hourly_Rate)}</td>
                <td>{row.amount == null ? '—' : formatMoney(row.amount)}</td><td>{row.status}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};

const FeedbackView = () => {
  const rows = useAdminRows<ApiFeedback>('/api/admin-data/feedback');

  return (
    <div className="glass-panel" style={{padding: '24px', flex: 1}}>
      <h2 style={{marginBottom: '24px'}}>Feedback & Support</h2>
      <div className="table-container">
        <table className="cyber-table">
          <thead><tr><th>ID</th><th>Customer</th><th>Subject</th><th>Content</th><th>Handler</th><th>Status</th><th>Submitted</th><th>Manager Notes</th></tr></thead>
          <tbody>
            {rows.map(row => (
              <tr key={row.feedback_ID}>
                <td>#{row.feedback_ID}</td><td>{row.customer_Name}</td><td>{row.subject}</td>
                <td>{row.content}</td><td>{row.handled_By_Name ?? '—'}</td><td>{row.status ?? '—'}</td>
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
  const employees = useAdminRows<ApiEmployee>('/api/admin-data/employees');
  const schedules = useAdminRows<ApiWorkSchedule>('/api/admin-data/work-schedules');
  const attendance = useAdminRows<ApiAttendance>('/api/admin-data/attendance');
  const payroll = useAdminRows<ApiPayroll>('/api/admin-data/payroll');
  const leaveRequests = useAdminRows<ApiLeaveRequest>('/api/admin-data/leave-requests');

  return (
    <div className="glass-panel" style={{padding: '24px', flex: 1, display: 'flex', flexDirection: 'column', gap: '28px'}}>
      <section>
        <h2 style={{marginBottom: '16px'}}>Employees</h2>
        <div className="table-container"><table className="cyber-table">
          <thead><tr><th>ID</th><th>Username</th><th>Name</th><th>Position</th><th>Access</th><th>Hire Date</th><th>Salary</th><th>Status</th></tr></thead>
          <tbody>{employees.map(row => <tr key={row.employee_ID}><td>#{row.employee_ID}</td><td>{row.username}</td><td>{row.full_Name}</td><td>{row.position_Name}</td><td>{row.access_Level}</td><td>{formatDateTime(row.hire_Date)}</td><td>{row.base_Salary == null ? '—' : formatMoney(row.base_Salary)}</td><td>{row.status ?? '—'}</td></tr>)}</tbody>
        </table></div>
      </section>
      <section>
        <h2 style={{marginBottom: '16px'}}>Work Schedules</h2>
        <div className="table-container"><table className="cyber-table">
          <thead><tr><th>ID</th><th>Employee</th><th>Work Date</th><th>Shift</th><th>Hours</th><th>Status</th></tr></thead>
          <tbody>{schedules.map(row => <tr key={row.schedule_ID}><td>#{row.schedule_ID}</td><td>{row.employee_Name}</td><td>{formatDateTime(row.work_Date)}</td><td>{row.shift_Name}</td><td>{row.start_Time} - {row.end_Time}</td><td>{row.status ?? '—'}</td></tr>)}</tbody>
        </table></div>
      </section>
      <section>
        <h2 style={{marginBottom: '16px'}}>Attendance</h2>
        <div className="table-container"><table className="cyber-table">
          <thead><tr><th>ID</th><th>Employee</th><th>Date</th><th>Shift</th><th>Check In</th><th>Check Out</th><th>Schedule</th><th>Note</th></tr></thead>
          <tbody>{attendance.map(row => <tr key={row.attendance_ID}><td>#{row.attendance_ID}</td><td>{row.employee_Name}</td><td>{formatDateTime(row.work_Date)}</td><td>{row.shift_Name}</td><td>{formatDateTime(row.check_In_Time)}</td><td>{formatDateTime(row.check_Out_Time)}</td><td>{row.schedule_Status ?? '—'}</td><td>{row.note ?? '—'}</td></tr>)}</tbody>
        </table></div>
      </section>
      <section>
        <h2 style={{marginBottom: '16px'}}>Payroll</h2>
        <div className="table-container"><table className="cyber-table">
          <thead><tr><th>ID</th><th>Employee</th><th>Period</th><th>Base</th><th>Bonus</th><th>Deduction</th><th>Net</th><th>Paid Date</th><th>Status</th></tr></thead>
          <tbody>{payroll.map(row => <tr key={row.payroll_ID}><td>#{row.payroll_ID}</td><td>{row.employee_Name}</td><td>{row.pay_Month}/{row.pay_Year}</td><td>{formatMoney(row.base_Salary)}</td><td>{formatMoney(row.bonus ?? 0)}</td><td>{formatMoney(row.deduction ?? 0)}</td><td>{row.net_Salary == null ? '—' : formatMoney(row.net_Salary)}</td><td>{formatDateTime(row.payment_Date)}</td><td>{row.status ?? '—'}</td></tr>)}</tbody>
        </table></div>
      </section>
      <section>
        <h2 style={{marginBottom: '16px'}}>Leave Requests</h2>
        <div className="table-container"><table className="cyber-table">
          <thead><tr><th>ID</th><th>Employee</th><th>Type</th><th>From</th><th>To</th><th>Reason</th><th>Status</th><th>Approved By</th></tr></thead>
          <tbody>{leaveRequests.map(row => <tr key={row.leave_ID}><td>#{row.leave_ID}</td><td>{row.employee_Name}</td><td>{row.leave_Type}</td><td>{formatDateTime(row.start_Date)}</td><td>{formatDateTime(row.end_Date)}</td><td>{row.reason ?? '—'}</td><td>{row.status ?? '—'}</td><td>{row.approved_By_Name ?? '—'}</td></tr>)}</tbody>
        </table></div>
      </section>
    </div>
  );
};

const InventoryView = () => {
  const products = useAdminRows<ApiInventoryProduct>('/api/admin-data/products');
  const transactions = useAdminRows<ApiInventoryTransaction>('/api/admin-data/inventory-transactions');

  return (
    <div className="glass-panel" style={{padding: '24px', flex: 1, display: 'flex', flexDirection: 'column', gap: '28px'}}>
      <section>
        <h2 style={{marginBottom: '16px'}}>Product Stock</h2>
        <div className="table-container"><table className="cyber-table">
          <thead><tr><th>ID</th><th>Product</th><th>Category</th><th>Price</th><th>Stock</th><th>Status</th></tr></thead>
          <tbody>{products.map(row => <tr key={row.product_ID}><td>#{row.product_ID}</td><td>{row.product_Name}</td><td>{row.category_Name}</td><td>{formatMoney(row.price)}</td><td>{row.stock_Quantity}</td><td>{row.status ?? 'Active'}</td></tr>)}</tbody>
        </table></div>
      </section>
      <section>
        <h2 style={{marginBottom: '16px'}}>Inventory Transactions</h2>
        <div className="table-container"><table className="cyber-table">
          <thead><tr><th>ID</th><th>Product</th><th>Type</th><th>Quantity</th><th>Employee</th><th>Date</th><th>Note</th></tr></thead>
          <tbody>{transactions.map(row => <tr key={row.inv_Trans_ID}><td>#{row.inv_Trans_ID}</td><td>{row.product_Name}</td><td>{row.trans_Type}</td><td>{row.quantity}</td><td>{row.employee_Name}</td><td>{formatDateTime(row.created_Date)}</td><td>{row.note ?? '—'}</td></tr>)}</tbody>
        </table></div>
      </section>
    </div>
  );
};

// Main App
function App() {
  const [activeTab, setActiveTab] = useState('dashboard');
  const [token, setToken] = useState(localStorage.getItem('accessToken'));
  const [identifier, setIdentifier] = useState('');
  const [password, setPassword] = useState('');
  const [loginBusy, setLoginBusy] = useState(false);

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
        identifier: identifier.trim(),
        password,
      });
      const accessToken = response.data.accessToken;
      if (!accessToken) {
        throw new Error('The API did not return an access token.');
      }

      localStorage.setItem('accessToken', accessToken);
      api.defaults.headers.common.Authorization = `Bearer ${accessToken}`;
      setToken(accessToken);
    } catch (error) {
      const message = axios.isAxiosError(error)
        ? error.response?.data?.detail ?? 'Login failed. Check your username/email and password.'
        : 'Login failed. Please try again.';
      window.alert(typeof message === 'string' ? message : JSON.stringify(message));
    } finally {
      setLoginBusy(false);
    }
  };

  if (!token) {
    return (
      <div style={{minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center', padding: '24px', background: 'var(--bg-main)', backgroundImage: 'radial-gradient(circle at 20% 35%, rgba(0, 240, 255, 0.12), transparent 35%), radial-gradient(circle at 80% 70%, rgba(123, 97, 255, 0.12), transparent 35%)'}}>
        <form onSubmit={handleLogin} className="glass-panel" style={{width: '100%', maxWidth: '420px', padding: '36px', display: 'flex', flexDirection: 'column', gap: '18px', boxShadow: '0 20px 70px rgba(0,0,0,0.45)'}}>
          <div style={{textAlign: 'center', marginBottom: '8px'}}>
            <div className="brand-icon" style={{marginBottom: '12px'}}><MonitorIcon /></div>
            <h1 className="text-gradient" style={{fontSize: '28px'}}>NetZone Admin</h1>
            <p style={{color: 'var(--text-muted)', marginTop: '8px'}}>Sign in to manage your Internet Cafe</p>
          </div>
          <label style={{display: 'flex', flexDirection: 'column', gap: '8px'}}>
            <span>Username / Email</span>
            <input
              className="search-bar"
              style={{width: '100%'}}
              autoComplete="username"
              value={identifier}
              onChange={event => setIdentifier(event.target.value)}
              required
            />
          </label>
          <label style={{display: 'flex', flexDirection: 'column', gap: '8px'}}>
            <span>Password</span>
            <input
              className="search-bar"
              style={{width: '100%'}}
              type="password"
              autoComplete="current-password"
              value={password}
              onChange={event => setPassword(event.target.value)}
              required
            />
          </label>
          <button className="cyber-btn" type="submit" disabled={loginBusy} style={{marginTop: '8px'}}>
            {loginBusy ? 'Signing in…' : 'Login'}
          </button>
        </form>
      </div>
    );
  }

  return (
    <div className="app-container">
      {/* Sidebar */}
      <aside className="sidebar">
        <div className="brand">
          <div className="brand-icon"><MonitorIcon /></div>
          <span className="text-gradient">NetZone</span>
        </div>
        <nav className="nav-menu">
          <div className={`nav-item ${activeTab === 'dashboard' ? 'active' : ''}`} onClick={() => setActiveTab('dashboard')}>
            <LayoutGridIcon /> Dashboard
          </div>
          <div className={`nav-item ${activeTab === 'users' ? 'active' : ''}`} onClick={() => setActiveTab('users')}>
            <UsersIcon /> Users
          </div>
          <div className={`nav-item ${activeTab === 'shop' ? 'active' : ''}`} onClick={() => setActiveTab('shop')}>
            <ShoppingCartIcon /> Cafe Shop
          </div>
          <div className={`nav-item ${activeTab === 'transactions' ? 'active' : ''}`} onClick={() => setActiveTab('transactions')}>
            <CreditCardIcon /> Transactions
          </div>
          <div className={`nav-item ${activeTab === 'sessions' ? 'active' : ''}`} onClick={() => setActiveTab('sessions')}>
            <HistoryIcon /> Sessions
          </div>
          <div className={`nav-item ${activeTab === 'feedback' ? 'active' : ''}`} onClick={() => setActiveTab('feedback')}>
            <MessageIcon /> Feedback
          </div>
          <div className={`nav-item ${activeTab === 'hrm' ? 'active' : ''}`} onClick={() => setActiveTab('hrm')}>
            <BriefcaseIcon /> HRM
          </div>
          <div className={`nav-item ${activeTab === 'inventory' ? 'active' : ''}`} onClick={() => setActiveTab('inventory')}>
            <BoxIcon /> Inventory
          </div>
          <div className={`nav-item ${activeTab === 'settings' ? 'active' : ''}`} onClick={() => setActiveTab('settings')}>
            <SettingsIcon /> Settings
          </div>
        </nav>
      </aside>

      {/* Main Content */}
      <main className="main-content">
        <header className="header">
          <div>
            <p>
              {activeTab === 'dashboard' ? 'Station Overview' :
               activeTab === 'users' ? 'Customer Database' :
               activeTab === 'shop' ? 'Orders & Shop' :
               activeTab === 'transactions' ? 'Financials' :
               activeTab === 'sessions' ? 'Logs' :
               activeTab === 'feedback' ? 'Customer Support' :
               activeTab === 'hrm' ? 'Staff Management' :
               activeTab === 'inventory' ? 'Warehouse' :
               'System Configuration'}
            </p>
            <h1>
              {activeTab === 'dashboard' ? 'Dashboard' :
               activeTab === 'users' ? 'Users Management' :
               activeTab === 'shop' ? 'Cafe Shop' :
               activeTab === 'transactions' ? 'Transactions' :
               activeTab === 'sessions' ? 'Sessions History' :
               activeTab === 'feedback' ? 'Feedback' :
               activeTab === 'hrm' ? 'HR & Payroll' :
               activeTab === 'inventory' ? 'Inventory' :
               'Settings'}
            </h1>
          </div>
          <div className="user-profile">
            <div style={{display: 'flex', flexDirection: 'column', alignItems: 'flex-end'}}>
              <span style={{fontSize: '14px', fontWeight: 600}}>Admin | John D.</span>
              <span style={{fontSize: '12px', color: 'var(--text-muted)'}}>System Manager</span>
            </div>
            <div style={{width: '36px', height: '36px', borderRadius: '50%', background: 'var(--primary)', display: 'flex', alignItems: 'center', justifyContent: 'center', color: '#000', fontWeight: 'bold'}}>
              A
            </div>
          </div>
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
          <div className="glass-panel" style={{padding: '32px', textAlign: 'center', flex: 1}}>
            <h2>Settings Module Placeholder</h2>
            <p style={{color: 'var(--text-muted)'}}>Config IP, Prices, Employee roles...</p>
          </div>
        )}
      </main>
    </div>
  );
}

export default App;
