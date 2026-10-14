import { useEffect, useMemo, useState } from 'react';
import { App, Button, Card, Col, DatePicker, Input, Modal, Row, Select, Space, Statistic, Table, Tag, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import {
  FileTextOutlined,
  PrinterOutlined,
  SearchOutlined,
  ReloadOutlined,
  DollarOutlined,
  CoffeeOutlined,
  ClockCircleOutlined,
  WalletOutlined
} from '@ant-design/icons';
import { api } from './api';
import { dateTime, money } from './domain';
import { EmptyState, PageTitle } from './components';

const { Text } = Typography;

export type InvoiceDetail = {
  itemName: string;
  quantity: number;
  unitPrice: number;
  subtotal: number;
};

export type Invoice = {
  id: string;
  invoiceCode: string;
  invoiceType: 'TopUp' | 'FoodOrder' | 'SessionRental';
  customerId: string;
  customerName: string;
  computerId: string | null;
  totalAmount: number;
  cashierName: string | null;
  createdAt: string;
  status: 'Paid' | 'Refunded';
  details: InvoiceDetail[];
};

const typeLabel: Record<Invoice['invoiceType'], string> = {
  TopUp: 'Nạp tiền',
  FoodOrder: 'Dịch vụ F&B',
  SessionRental: 'Giờ chơi máy'
};

const typeColor: Record<Invoice['invoiceType'], string> = {
  TopUp: 'cyan',
  FoodOrder: 'orange',
  SessionRental: 'purple'
};

export function InvoicesPage() {
  const { message } = App.useApp();
  const [invoices, setInvoices] = useState<Invoice[]>([]);
  const [loading, setLoading] = useState(false);
  const [search, setSearch] = useState('');
  const [typeFilter, setTypeFilter] = useState<Invoice['invoiceType'] | undefined>();
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');
  const [selectedInvoice, setSelectedInvoice] = useState<Invoice | null>(null);

  async function loadInvoices() {
    setLoading(true);
    try {
      const data = await api<Invoice[]>('/invoices?page=1&pageSize=200');
      setInvoices(data || []);
    } catch (error) {
      message.error((error as Error).message || 'Không thể tải danh sách hóa đơn.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadInvoices();
  }, []);

  const filteredRows = useMemo(() => {
    return invoices.filter(invoice => {
      const query = search.trim().toLowerCase();
      const date = invoice.createdAt ? invoice.createdAt.slice(0, 10) : '';
      const matchSearch =
        !query ||
        `${invoice.invoiceCode} ${invoice.customerName} ${invoice.customerId} ${invoice.computerId || ''}`
          .toLowerCase()
          .includes(query);
      const matchType = !typeFilter || invoice.invoiceType === typeFilter;
      const matchFrom = !fromDate || date >= fromDate;
      const matchTo = !toDate || date <= toDate;
      return matchSearch && matchType && matchFrom && matchTo;
    });
  }, [invoices, search, typeFilter, fromDate, toDate]);

  // Aggregate stats
  const stats = useMemo(() => {
    let topUpTotal = 0;
    let foodTotal = 0;
    let sessionTotal = 0;
    for (const inv of filteredRows) {
      if (inv.status === 'Paid') {
        if (inv.invoiceType === 'TopUp') topUpTotal += inv.totalAmount;
        else if (inv.invoiceType === 'FoodOrder') foodTotal += inv.totalAmount;
        else if (inv.invoiceType === 'SessionRental') sessionTotal += inv.totalAmount;
      }
    }
    return {
      totalRevenue: topUpTotal + foodTotal + sessionTotal,
      topUpTotal,
      foodTotal,
      sessionTotal,
      count: filteredRows.length
    };
  }, [filteredRows]);

  const handlePrint = () => {
    window.print();
  };

  const columns: ColumnsType<Invoice> = [
    {
      title: 'Mã hóa đơn',
      dataIndex: 'invoiceCode',
      key: 'invoiceCode',
      render: (code, row) => (
        <Space direction="vertical" size={0}>
          <Text strong style={{ color: '#38D9FF' }}>{code}</Text>
          <Text type="secondary" style={{ fontSize: 11 }}>{row.id}</Text>
        </Space>
      )
    },
    {
      title: 'Loại dịch vụ',
      dataIndex: 'invoiceType',
      key: 'invoiceType',
      render: (val: Invoice['invoiceType']) => (
        <Tag color={typeColor[val] || 'default'}>{typeLabel[val] || val}</Tag>
      )
    },
    {
      title: 'Khách hàng',
      dataIndex: 'customerName',
      key: 'customerName',
      render: (name, row) => (
        <Space direction="vertical" size={0}>
          <Text strong>{name}</Text>
          <Text type="secondary" style={{ fontSize: 11 }}>{row.customerId}</Text>
        </Space>
      )
    },
    {
      title: 'Máy trạm',
      dataIndex: 'computerId',
      key: 'computerId',
      render: cid => (cid ? <Tag color="blue">{cid}</Tag> : <Text type="secondary">—</Text>)
    },
    {
      title: 'Tổng thanh toán',
      dataIndex: 'totalAmount',
      key: 'totalAmount',
      align: 'right',
      render: val => <Text strong style={{ color: '#52c41a' }}>{money(val)}</Text>
    },
    {
      title: 'Người lập / Thu ngân',
      dataIndex: 'cashierName',
      key: 'cashierName',
      render: val => val || <Text type="secondary">Hệ thống tự động</Text>
    },
    {
      title: 'Thời gian lập',
      dataIndex: 'createdAt',
      key: 'createdAt',
      render: val => dateTime(val)
    },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      render: status => (
        <Tag color={status === 'Refunded' ? 'red' : 'green'}>
          {status === 'Refunded' ? 'Đã hoàn tiền' : 'Đã thanh toán'}
        </Tag>
      )
    },
    {
      title: 'Thao tác',
      key: 'action',
      align: 'center',
      render: (_, row) => (
        <Button
          type="primary"
          ghost
          size="small"
          icon={<PrinterOutlined />}
          onClick={() => setSelectedInvoice(row)}
        >
          Xem / In K80
        </Button>
      )
    }
  ];

  return (
    <>
      <PageTitle
        eyebrow="VẬN HÀNH"
        title="Quản lý Hóa đơn & Biên nhận"
        description="Tra cứu lịch sử thu chi, biên nhận nạp tiền, hóa đơn dịch vụ đồ ăn và giờ chơi máy trạm. Hỗ trợ xuất và in hóa đơn nhiệt chuẩn K80."
        actions={
          <Button icon={<ReloadOutlined />} onClick={loadInvoices} loading={loading}>
            Làm mới
          </Button>
        }
      />

      <Row gutter={[16, 16]} style={{ marginBottom: 16 }}>
        <Col xs={24} sm={12} md={6}>
          <Card className="metric-card">
            <Statistic
              title="Tổng doanh thu lọc"
              value={stats.totalRevenue}
              formatter={val => money(Number(val))}
              prefix={<DollarOutlined style={{ color: '#52c41a' }} />}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} md={6}>
          <Card className="metric-card">
            <Statistic
              title="Tiền nạp tài khoản"
              value={stats.topUpTotal}
              formatter={val => money(Number(val))}
              prefix={<WalletOutlined style={{ color: '#13c2c2' }} />}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} md={6}>
          <Card className="metric-card">
            <Statistic
              title="Dịch vụ F&B (Món ăn)"
              value={stats.foodTotal}
              formatter={val => money(Number(val))}
              prefix={<CoffeeOutlined style={{ color: '#fa8c16' }} />}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} md={6}>
          <Card className="metric-card">
            <Statistic
              title="Tiền giờ máy trạm"
              value={stats.sessionTotal}
              formatter={val => money(Number(val))}
              prefix={<ClockCircleOutlined style={{ color: '#722ed1' }} />}
            />
          </Card>
        </Col>
      </Row>

      <Card className="data-card">
        <div className="table-toolbar invoice-filters" style={{ display: 'flex', gap: 12, marginBottom: 16, flexWrap: 'wrap' }}>
          <Input
            prefix={<SearchOutlined />}
            placeholder="Tìm theo mã HĐ, tên khách, mã máy..."
            allowClear
            value={search}
            onChange={e => setSearch(e.target.value)}
            style={{ width: 280 }}
          />
          <Select
            placeholder="Tất cả loại hóa đơn"
            allowClear
            value={typeFilter}
            onChange={setTypeFilter}
            style={{ width: 180 }}
            options={[
              { value: 'TopUp', label: 'Nạp tiền (TopUp)' },
              { value: 'FoodOrder', label: 'Dịch vụ F&B' },
              { value: 'SessionRental', label: 'Giờ chơi máy' }
            ]}
          />
          <DatePicker
            placeholder="Từ ngày"
            onChange={d => setFromDate(d?.format('YYYY-MM-DD') || '')}
            style={{ width: 140 }}
          />
          <DatePicker
            placeholder="Đến ngày"
            onChange={d => setToDate(d?.format('YYYY-MM-DD') || '')}
            style={{ width: 140 }}
          />
          <Button onClick={() => { setSearch(''); setTypeFilter(undefined); setFromDate(''); setToDate(''); }}>
            Đặt lại bộ lọc
          </Button>
        </div>

        {filteredRows.length === 0 && !loading ? (
          <EmptyState
            description="Không có hóa đơn phù hợp với bộ lọc tìm kiếm hiện tại."
          />
        ) : (
          <Table<Invoice>
            rowKey="id"
            columns={columns}
            dataSource={filteredRows}
            loading={loading}
            pagination={{ pageSize: 15, showTotal: (total, range) => `${range[0]}-${range[1]} của ${total} hóa đơn` }}
            scroll={{ x: 1000 }}
          />
        )}
      </Card>

      {/* Printable K80 Thermal Receipt Modal */}
      <Modal
        open={Boolean(selectedInvoice)}
        onCancel={() => setSelectedInvoice(null)}
        footer={[
          <Button key="close" onClick={() => setSelectedInvoice(null)}>
            Đóng
          </Button>,
          <Button key="print" type="primary" icon={<PrinterOutlined />} onClick={handlePrint}>
            In hóa đơn (K80)
          </Button>
        ]}
        width={420}
        destroyOnClose
      >
        {selectedInvoice && (
          <div className="invoice-print" id="k80-receipt">
            <div className="receipt-header">
              <strong>CYBERCAFE GAMING</strong>
              <span>HỆ THỐNG PHÒNG MÁY CAO CẤP</span>
              <span>Hotline: 1900 8888 · Wifi: CyberCafe_5G (pass: 88888888)</span>
            </div>

            <div className="receipt-rule" />

            <h2>
              {selectedInvoice.invoiceType === 'TopUp'
                ? 'BIÊN NHẬN NẠP TIỀN'
                : selectedInvoice.invoiceType === 'FoodOrder'
                ? 'HÓA ĐƠN DỊCH VỤ F&B'
                : 'HÓA ĐƠN TIỀN GIỜ CHƠI'}
            </h2>

            <div className="receipt-meta">
              <span>Mã hóa đơn:</span>
              <strong>{selectedInvoice.invoiceCode}</strong>
            </div>
            <div className="receipt-meta">
              <span>Ngày giờ:</span>
              <span>{dateTime(selectedInvoice.createdAt)}</span>
            </div>
            <div className="receipt-meta">
              <span>Khách hàng:</span>
              <strong>{selectedInvoice.customerName} ({selectedInvoice.customerId})</strong>
            </div>
            {selectedInvoice.computerId && (
              <div className="receipt-meta">
                <span>Máy trạm:</span>
                <strong>{selectedInvoice.computerId}</strong>
              </div>
            )}
            <div className="receipt-meta">
              <span>Thu ngân / Lập bởi:</span>
              <span>{selectedInvoice.cashierName || 'Tự động (Hệ thống)'}</span>
            </div>

            <div className="receipt-rule" />

            <table className="receipt-items">
              <thead>
                <tr>
                  <th>Tên món / Dịch vụ</th>
                  <th style={{ textAlign: 'center' }}>SL</th>
                  <th>Đơn giá</th>
                  <th>T.Tiền</th>
                </tr>
              </thead>
              <tbody>
                {selectedInvoice.details && selectedInvoice.details.length > 0 ? (
                  selectedInvoice.details.map((item, idx) => (
                    <tr key={idx}>
                      <td>{item.itemName}</td>
                      <td style={{ textAlign: 'center' }}>{item.quantity}</td>
                      <td>{money(item.unitPrice)}</td>
                      <td>{money(item.subtotal)}</td>
                    </tr>
                  ))
                ) : (
                  <tr>
                    <td colSpan={3}>
                      {selectedInvoice.invoiceType === 'TopUp'
                        ? 'Nạp tiền tài khoản hội viên'
                        : selectedInvoice.invoiceType === 'FoodOrder'
                        ? 'Dịch vụ đồ ăn & nước uống'
                        : 'Tiền giờ chơi máy trạm'}
                    </td>
                    <td style={{ textAlign: 'right' }}>{money(selectedInvoice.totalAmount)}</td>
                  </tr>
                )}
              </tbody>
            </table>

            <div className="receipt-rule" />

            <div className="receipt-total">
              <strong>TỔNG THANH TOÁN:</strong>
              <strong style={{ fontSize: 16 }}>{money(selectedInvoice.totalAmount)}</strong>
            </div>
            <div className="receipt-meta">
              <span>Phương thức:</span>
              <span>Tài khoản / Tiền mặt</span>
            </div>
            <div className="receipt-meta">
              <span>Trạng thái:</span>
              <strong>{selectedInvoice.status === 'Refunded' ? 'ĐÃ HOÀN TIỀN' : 'ĐÃ THANH TOÁN'}</strong>
            </div>

            <div className="receipt-rule" />

            <div className="receipt-thanks">
              CẢM ƠN QUÝ KHÁCH VÀ HẸN GẶP LẠI!
            </div>

            <div className="receipt-qr">
              <div className="receipt-qr-pattern" />
              <span>Quét mã xác thực hóa đơn trực tuyến trên hệ thống CyberCafe</span>
            </div>
          </div>
        )}
      </Modal>
    </>
  );
}
