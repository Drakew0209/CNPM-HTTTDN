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
  if (value === null || value === undefined || value === '') return '—';
  const lookup: Record<string,string> = { customerId:'customers',employeeId:'employees',productId:'products',computerId:'computers',supplierId:'suppliers',categoryId:'categories',shiftId:'shifts' };
  if (lookup[key]) { const row = workspace[lookup[key]]?.find(r => r.id === value); return String(row?.fullName || row?.name || value); }
  if (['balance','amount','total','price','baseSalary','bonus','deduction','hourlyRate','startBalance'].includes(key)) return money(value);
  if (['createdAt','checkIn','checkOut'].includes(key) || (['startTime','endTime'].includes(key) && String(value).includes('T'))) return dateTime(value);
  if (typeof value === 'boolean') return value ? 'Có' : 'Không';
  if (Array.isArray(value)) return `${value.length}`;
  if (typeof value === 'object') return JSON.stringify(value);
  return labels[String(value)] || String(value);
}
export function Value({ k, value, workspace }: { k: string; value: unknown; workspace: Workspace }) {
  if (k === 'status') return <StatusTag value={value}/>;
  if (k === 'active' || k === 'online') return <Tag color={value ? 'green' : 'default'}>{k === 'online' ? (value ? 'Trực tuyến' : 'Mất kết nối') : (value ? 'Hoạt động' : 'Tạm ngừng')}</Tag>;
  return <span className={['balance','amount','total','price','stock'].includes(k) ? 'numeric' : undefined}>{displayValue(k,value,workspace)}</span>;
}
export function PageTitle({ eyebrow, title, description, actions }: { eyebrow?: string; title: string; description: string; actions?: ReactNode }) {
  return <div className="page-title"><div><div className="eyebrow">{eyebrow || 'INTERNETCAFE / VẬN HÀNH'}</div><h1>{title}</h1><p>{description}</p></div><div className="page-actions">{actions}</div></div>;
}
export function DateFilter({ from,to,onFrom,onTo }: { from:string;to:string;onFrom:(s:string)=>void;onTo:(s:string)=>void }) {
  return <Space wrap className="date-filter"><label>Từ ngày <input aria-label="Từ ngày" type="date" value={from} max={to || undefined} onChange={e=>onFrom(e.target.value)}/></label><label>Đến ngày <input aria-label="Đến ngày" type="date" value={to} min={from || undefined} onChange={e=>onTo(e.target.value)}/></label>{(from||to)&&<Button size="small" onClick={()=>{onFrom('');onTo('');}}>Xóa ngày</Button>}</Space>;
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
