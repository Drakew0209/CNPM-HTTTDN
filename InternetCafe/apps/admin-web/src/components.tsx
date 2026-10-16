import { Alert, Button, Card, Empty, Space, Tag } from 'antd';
import { PrinterOutlined } from '@ant-design/icons';
import { useOutletContext } from 'react-router-dom';
import type { ReactNode } from 'react';
import { columnLabels } from './modules';
import { dateTime, labels, money, type Row, type Session, type Workspace } from './domain';
export interface CafeContext { session: Session; workspace: Workspace; refresh: () => Promise<unknown>; loading: boolean }
export const useCafe = () => useOutletContext<CafeContext>();
export function StatusTag({ value }: { value: unknown }) {
  const key = String(value ?? '');
  const color = ['Active','Available','Approved','Served','Resolved','Paid','Completed'].includes(key) ? 'green' : ['Banned','Rejected','Cancelled','Absent'].includes(key) ? 'red' : ['Pending','Maintenance','Draft','OnLeave'].includes(key) ? 'gold' : ['InUse','Preparing','Published','Processing'].includes(key) ? 'blue' : 'default';
  return <Tag color={color}>{labels[key] || key || '—'}</Tag>;
}
export function displayValue(key: string, value: unknown, workspace: Workspace): string {
  if (key === 'scheduleId') {
    const sched = (workspace.schedules || []).find(s => s.id === value);
    if (sched) {
      const shift = (workspace.shifts || []).find(sh => sh.id === sched.shiftId);
      return `${sched.date} (${shift?.name || sched.shiftId})`;
    }
    return String(value);
  }
  const lookup: Record<string,string> = { customerId:'customers',employeeId:'employees',productId:'products',computerId:'computers',supplierId:'suppliers',categoryId:'categories',shiftId:'shifts',materialId:'materials' };
  if (lookup[key]) { const row = workspace[lookup[key]]?.find(r => r.id === value); return String(row?.fullName || row?.name || value); }
  if (['balance','amount','total','price','baseSalary','bonus','deduction','hourlyRate','startBalance','unitCost'].includes(key)) return money(value);
  if (['createdAt','checkIn','checkOut'].includes(key) || (['startTime','endTime'].includes(key) && String(value).includes('T'))) return dateTime(value);
  if (typeof value === 'boolean') return value ? 'Có' : 'Không';
  if (Array.isArray(value)) return `${value.length}`;
  if (typeof value === 'object') return JSON.stringify(value);
  return labels[String(value)] || String(value);
}
export function Value({ k, value, row, workspace }: { k: string; value: unknown; row?: Row; workspace: Workspace }) {
  if (k === 'status') return <StatusTag value={value}/>;
  if (k === 'active' || k === 'online') return <Tag color={value ? 'green' : 'default'}>{k === 'online' ? (value ? 'Trực tuyến' : 'Mất kết nối') : (value ? 'Hoạt động' : 'Tạm ngừng')}</Tag>;
  if (k === 'suppliedCount') {
    const rowId = (row as any)?.id; const rowName = (row as any)?.name;
    const count = (workspace.materials || []).filter(m => (rowId && m.supplierId === rowId) || (rowName && m.supplierName === rowName)).length;
    return <Tag color={count > 0 ? 'cyan' : 'default'}>{count > 0 ? `${count} loại NVL` : '0 NVL'}</Tag>;
  }
  if (k === 'importCount') {
    const rowId = (row as any)?.id; const rowName = (row as any)?.name;
    const count = (workspace.inventory || []).filter(i => ((rowId && i.supplierId === rowId) || (rowName && i.supplierName === rowName)) && i.type === 'Import').length;
    return <Tag color={count > 0 ? 'geekblue' : 'default'}>{count > 0 ? `${count} phiếu nhập` : '0 phiếu'}</Tag>;
  }
  if ((k === 'bannerUrl' || k === 'imageUrl') && typeof value === 'string' && value) {
    return <img src={value} alt="banner" style={{ width: 44, height: 28, objectFit: 'cover', borderRadius: 4, verticalAlign: 'middle', border: '1px solid #26385c' }} onError={(e)=>{ (e.target as HTMLElement).style.display = 'none'; }} />;
  }
  return <span className={['balance','amount','total','price','stock','stockQuantity','reorderLevel','unitCost'].includes(k) ? 'numeric' : undefined}>{displayValue(k,value,workspace)}</span>;
}
export function PageTitle({ eyebrow, title, description, actions }: { eyebrow?: string; title: string; description: string; actions?: ReactNode }) {
  return <div className="page-title"><div><div className="eyebrow">{eyebrow || 'INTERNETCAFE / VẬN HÀNH'}</div><h1>{title}</h1><p>{description}</p></div><div className="page-actions">{actions}</div></div>;
}
export function DateFilter({ from,to,onFrom,onTo }: { from:string;to:string;onFrom:(s:string)=>void;onTo:(s:string)=>void }) {
  return (
    <div className="date-filter-group">
      <label className="date-filter-label">
        <span className="date-filter-caption">Từ ngày</span>
        <input aria-label="Từ ngày" type="date" value={from} max={to || undefined} onChange={e=>onFrom(e.target.value)}/>
      </label>
      <label className="date-filter-label">
        <span className="date-filter-caption">Đến ngày</span>
        <input aria-label="Đến ngày" type="date" value={to} min={from || undefined} onChange={e=>onTo(e.target.value)}/>
      </label>
      {(from||to)&&<Button size="middle" className="date-clear-btn" onClick={()=>{onFrom('');onTo('');}}>Xóa lọc</Button>}
    </div>
  );
}
export function EmptyState({ description = 'Chưa có dữ liệu phù hợp.' }: { description?: string }) { return <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description={description}/>; }
export function ErrorState({ error, retry }: { error: Error; retry:()=>void }) { return <Alert type="error" showIcon title="Không tải được dữ liệu" description={error.message} action={<Button onClick={retry}>Thử lại</Button>}/>; }
export function printRows(title: string, rows: Row[], keys: string[], workspace: Workspace) {
  const popup = window.open('', '_blank', 'width=960,height=720'); if (!popup) return false;
  const d = popup.document;
  d.title = title; const style = d.createElement('style'); style.textContent = 'body{font:14px Segoe UI,sans-serif;padding:30px;color:#153d37}table{border-collapse:collapse;width:100%;margin-top:20px}th,td{border:1px solid #ccc;text-align:left;padding:10px}h1{font-size:24px}p{color:#555}@media print{button{display:none}}'; d.head.append(style);
  const heading=d.createElement('h1'); heading.textContent=`InternetCafe · ${title}`;d.body.append(heading);
  const note=d.createElement('p');note.textContent=`Chứng từ demo • ${new Date().toLocaleString('vi-VN')} • Không phải hóa đơn tài chính`;d.body.append(note);
  const table=d.createElement('table'),head=d.createElement('tr'); for(const k of keys){const th=d.createElement('th');th.textContent=columnLabels[k]||k;head.append(th);}table.append(head);
  for(const row of rows){const tr=d.createElement('tr');for(const k of keys){const td=d.createElement('td');td.textContent=displayValue(k,row[k],workspace);tr.append(td);}table.append(tr);}d.body.append(table);
  const button=d.createElement('button');button.textContent='In chứng từ';button.onclick=()=>popup.print();d.body.append(button);popup.focus();popup.print(); return true;
}
export function PrintButton({ title, rows, keys, workspace }: { title:string;rows:Row[];keys:string[];workspace:Workspace }) { return <Button icon={<PrinterOutlined/>} onClick={()=>printRows(title,rows,keys,workspace)}>In</Button>; }
export function Metric({ title,value,note,icon }: { title:string;value:ReactNode;note:string;icon:ReactNode }) { return <Card className="metric-card"><div className="metric-top"><span>{title}</span><span className="metric-icon">{icon}</span></div><div className="metric-value">{value}</div><div className="metric-note">{note}</div></Card>; }
