import { useEffect, useState } from 'react';
import { Alert, App, Button, Card, Checkbox, Descriptions, Divider, Form, Input, InputNumber, Modal, Select, Space, Switch, Table, Tag } from 'antd';
import { DeleteOutlined, DownloadOutlined, EditOutlined, EyeOutlined, PlusOutlined, SearchOutlined, SendOutlined } from '@ant-design/icons';
import { api, ApiError } from './api';
import { downloadCsv, filterDates, labels, type Row } from './domain';
import { columnLabels, type Field, type Module } from './modules';
import { DateFilter, displayValue, EmptyState, PageTitle, PrintButton, useCafe, Value } from './components';

export function ResourcePage({ module, readonly = false }: { module: Module; readonly?: boolean }) {
  const { workspace, session, refresh } = useCafe(); const { message, modal } = App.useApp();
  const [search,setSearch]=useState(''),[status,setStatus]=useState<string>(),[from,setFrom]=useState(''),[to,setTo]=useState('');
  const [editor,setEditor]=useState<Row | 'new' | null>(null),[detail,setDetail]=useState<Row|null>(null),[busy,setBusy]=useState(false),[saveError,setSaveError]=useState('');
  const [publish,setPublish]=useState<Row|null>(null),[recipients,setRecipients]=useState<string[]>([]); const [form]=Form.useForm();
  const canEdit=!readonly && (module.editRoles || module.roles).includes(session.user.role);
  const all=workspace[module.key] || [];
  const dateField=module.key==='sessions'?'startTime':module.key==='attendance'?'checkIn':'createdAt';
  const rows=filterDates(all,from,to,dateField).filter(row=>(!status||row.status===status)&&(!search||[row.id,...module.columns.map(k=>displayValue(k,row[k],workspace))].join(' ').toLocaleLowerCase('vi').includes(search.toLocaleLowerCase('vi'))));
  async function act(path:string,body:unknown,success:string,method='POST') {
    setBusy(true);try {await api(path,method,body);await refresh();message.success(success);}catch(e){message.error((e as Error).message);throw e;}finally{setBusy(false);}
  }
  function confirm(title:string,path:string,body:unknown,method='POST') { modal.confirm({title,content:'Thao tác sẽ được kiểm tra quyền và trạng thái tại service.',okText:'Xác nhận',cancelText:'Quay lại',onOk:()=>act(path,body,'Đã cập nhật thành công.',method)}); }
  function openEditor(row:Row|'new') { setEditor(row); setSaveError(''); }
  useEffect(() => {
    if (editor === null) return;
    form.resetFields();
    const defaults: Record<string, unknown> = { active:true, tier:'Silver', status:module.key==='payroll'?'Draft':module.key==='schedules'?'Scheduled':'Active', type:'Import', quantity:1, bonus:0, deduction:0 };
    form.setFieldsValue(editor === 'new' ? {...defaults, questions:[{text:'Bạn hài lòng với dịch vụ như thế nào?', optionsText:'Rất hài lòng\nHài lòng\nCần cải thiện'}]} : {...editor, questions:Array.isArray(editor.questions) ? editor.questions.map(q => ({...q, optionsText:q.options.join('\n')})) : undefined});
  }, [editor, form, module.key]);
  async function save(values:Record<string,unknown>) {
    const payload:Record<string,unknown>={}; for(const field of module.fields) {if(field.createOnly&&editor!=='new')continue;if(values[field.key]!==undefined)payload[field.key]=values[field.key];}
    if(module.key==='surveys')payload.questions=(values.questions as {id?:string;text:string;optionsText:string}[]).map((q,i)=>({id:q.id||`q-${i+1}`,text:q.text.trim(),options:q.optionsText.split('\n').map(s=>s.trim()).filter(Boolean)}));
    if(module.key==='inventory' && !payload.supplierId) delete payload.supplierId;
    setBusy(true);setSaveError('');
    try{await api(`/${module.key}${editor!=='new'?'/'+(editor as Row).id:''}`,editor==='new'?'POST':'PATCH',payload);setEditor(null);await refresh();message.success('Đã lưu dữ liệu.');}
    catch(e){setSaveError((e as Error).message);if(e instanceof ApiError&&e.fields)form.setFields(Object.entries(e.fields).map(([name,error])=>({name,errors:[error]})));}
    finally{setBusy(false);}
  }
  function control(field:Field) {
    if(field.type==='select')return <Select allowClear showSearch optionFilterProp="label" options={field.resource?(workspace[field.resource]||[]).map(r=>({value:r.id,label:String(r.fullName||r.name||r.id)})):field.options?.map(s=>({value:s,label:labels[s]||s}))}/>;
    if(field.type==='boolean')return <Switch/>;
    if(field.type==='number'||field.type==='money')return <InputNumber min={field.min??0} max={field.max??1000000000} precision={0} style={{width:'100%'}} addonAfter={field.type==='money'?'VND':undefined}/>;
    if(field.type==='textarea')return <Input.TextArea rows={3} maxLength={2000} showCount/>;
    if(field.type==='password')return <Input.Password autoComplete="new-password" maxLength={100}/>;
    return <Input type={field.type==='email'?'email':['date','time','month'].includes(field.type||'')?field.type:'text'} maxLength={field.type==='text'?180:undefined}/>;
  }
  const columns=[...module.columns.map(k=>({title:columnLabels[k]||k,dataIndex:k,key:k,sorter:(a:Row,b:Row)=>typeof a[k]==='number'?Number(a[k])-Number(b[k]):displayValue(k,a[k],workspace).localeCompare(displayValue(k,b[k],workspace),'vi'),render:(v:unknown)=><Value k={k} value={v} workspace={workspace}/>})),{title:'Thao tác',key:'actions',fixed:'right' as const,width:module.key==='orders'?240:180,render:(_:unknown,row:Row)=><Space wrap size={4}>
    <Button size="small" aria-label={`Chi tiết ${row.id}`} icon={<EyeOutlined/>} onClick={()=>setDetail(row)}>Xem</Button>
    {canEdit&&module.edit&&!(module.key==='surveys'&&row.status!=='Draft')&&<Button size="small" aria-label={`Sửa ${row.id}`} icon={<EditOutlined/>} onClick={()=>openEditor(row)}>Sửa</Button>}
    {canEdit&&module.key==='customers'&&row.status!=='Inactive'&&<Button size="small" danger={row.status==='Active'} onClick={()=>confirm(row.status==='Banned'?'Mở khóa khách hàng?':'Khóa tài khoản khách hàng?',`/customers/${row.id}`,{status:row.status==='Banned'?'Active':'Banned'},'PATCH')}>{row.status==='Banned'?'Mở khóa':'Khóa'}</Button>}
    {canEdit&&['topups','leaves'].includes(module.key)&&row.status==='Pending'&&<><Button type="primary" size="small" loading={busy} onClick={()=>confirm(module.key==='topups'?'Đã nhận đủ tiền tại quầy?':'Duyệt đơn nghỉ này?',`/${module.key}/${row.id}/decision`,{status:'Approved'})}>Duyệt</Button><Button size="small" danger onClick={()=>confirm('Từ chối yêu cầu?',`/${module.key}/${row.id}/decision`,{status:'Rejected'})}>Từ chối</Button></>}
    {canEdit&&module.key==='orders'&&['Pending','Preparing'].includes(String(row.status))&&<><Button size="small" type="primary" onClick={()=>confirm(row.status==='Pending'?'Nhận chuẩn bị đơn?':'Xác nhận đã phục vụ?',`/orders/${row.id}/transition`,{status:row.status==='Pending'?'Preparing':'Served'})}>{row.status==='Pending'?'Nhận đơn':'Phục vụ'}</Button>{(row.status==='Pending'||session.user.role!=='Cashier')&&<Button size="small" danger onClick={()=>confirm('Hủy đơn và hoàn tiền qua service?',`/orders/${row.id}/transition`,{status:'Cancelled'})}>Hủy</Button>}</>}
    {canEdit&&module.key==='surveys'&&row.status==='Draft'&&<Button size="small" icon={<SendOutlined/>} onClick={()=>{setPublish(row);setRecipients([]);}}>Phát hành</Button>}
    {canEdit&&module.key==='surveys'&&row.status==='Published'&&<Button size="small" onClick={()=>confirm('Đóng khảo sát?',`/surveys/${row.id}`,{status:'Closed'},'PATCH')}>Đóng</Button>}
    {canEdit&&module.remove&&<Button size="small" danger aria-label={`Xóa ${row.id}`} icon={<DeleteOutlined/>} onClick={()=>confirm('Ngừng sử dụng bản ghi này?',`/${module.key}/${row.id}`,undefined,'DELETE')}/>}
  </Space>}];
  return <>
    <PageTitle eyebrow={readonly?'INTERNETCAFE / TRA CỨU HỆ THỐNG':module.section.toLocaleUpperCase('vi')} title={module.title} description={module.description} actions={<Space wrap><Button icon={<DownloadOutlined/>} onClick={()=>downloadCsv(module.key,rows,module.columns.map(key=>({key,label:columnLabels[key]||key})))}>Xuất CSV</Button>{canEdit&&module.create&&<Button type="primary" icon={<PlusOutlined/>} onClick={()=>openEditor('new')}>Thêm {module.singular}</Button>}</Space>}/>
    {module.key==='inventory'&&<Alert className="section-alert" type="warning" showIcon title={`${(workspace.products||[]).filter(p=>Number(p.stock)<=5).length} món có tồn kho thấp (≤ 5).`} description="Kiểm tra số lượng trước khi lập phiếu xuất. Kho F&B chưa phải phân hệ MRP."/>}
    {module.key==='accounts'&&<Alert className="section-alert" type="info" showIcon title="Admin quản lý truy cập. Owner/Manager vận hành quán. Staff chỉ xem dữ liệu cá nhân."/>}
    <Card className="data-card"><div className="table-toolbar"><Input prefix={<SearchOutlined/>} placeholder={`Tìm trong ${module.title.toLocaleLowerCase('vi')}…`} aria-label="Tìm kiếm" allowClear value={search} onChange={e=>setSearch(e.target.value)} className="search-input"/>{module.statuses&&<Select placeholder="Tất cả trạng thái" aria-label="Lọc trạng thái" allowClear value={status} onChange={setStatus} style={{minWidth:165}} options={module.statuses.map(value=>({value,label:labels[value]}))}/>}<Tag>{rows.length} bản ghi</Tag></div>{module.dates&&<DateFilter from={from} to={to} onFrom={setFrom} onTo={setTo}/>}<Table rowKey="id" columns={columns} dataSource={rows} scroll={{x:'max-content'}} pagination={{pageSize:8,showSizeChanger:true,pageSizeOptions:[8,20,50],showTotal:total=>`${total} bản ghi`}} locale={{emptyText:<EmptyState/>}}/></Card>
    <Modal open={editor!==null} onCancel={()=>setEditor(null)} title={`${editor==='new'?'Thêm':'Cập nhật'} ${module.singular}`} width={680} destroyOnHidden footer={<Space><Button onClick={()=>setEditor(null)}>Hủy</Button><Button type="primary" loading={busy} onClick={()=>form.submit()}>Lưu thông tin</Button></Space>}>
      {saveError&&<Alert type="error" showIcon title={saveError} className="section-alert"/>}<Form form={form} layout="vertical" onFinish={save}><div className="form-grid">{module.fields.filter(f=>!f.createOnly||editor==='new').map(field=><Form.Item className={field.type==='textarea'?'span-2':undefined} key={field.key} name={field.key} label={field.label} valuePropName={field.type==='boolean'?'checked':'value'} extra={field.help} rules={[{required:field.required,message:`Vui lòng nhập ${field.label.toLocaleLowerCase('vi')}.`},...(field.type==='email'?[{type:'email' as const,message:'Email chưa đúng định dạng.'}]:[]),...(field.type==='password'?[{min:8,message:'Mật khẩu cần ít nhất 8 ký tự.'}]:[]),...(field.key==='phone'?[{pattern:/^[0-9+ ()-]{9,15}$/,message:'Điện thoại cần 9–15 ký tự hợp lệ.'}]:[])]}>{control(field)}</Form.Item>)}</div>
      {module.key==='surveys'&&<Form.List name="questions" rules={[{validator:async(_,items)=>{if(!items?.length)throw new Error('Cần ít nhất một câu hỏi.');}}]}>{(fields,{add,remove},{errors})=><><Divider>Câu hỏi khảo sát</Divider>{fields.map(({key,name,...rest})=><Card key={key} size="small" className="question-card"><Form.Item {...rest} name={[name,'text']} label={`Câu ${name+1}`} rules={[{required:true,message:'Nhập câu hỏi.'}]}><Input/></Form.Item><Form.Item {...rest} name={[name,'optionsText']} label="Đáp án (mỗi dòng một lựa chọn)" rules={[{required:true,message:'Nhập đáp án.'},{validator:async(_,v)=>{const opts=String(v||'').split('\n').map(s=>s.trim()).filter(Boolean);if(opts.length<2||new Set(opts).size!==opts.length)throw new Error('Cần ít nhất hai đáp án khác nhau.');}}]}><Input.TextArea rows={3}/></Form.Item><Button danger onClick={()=>remove(name)}>Xóa câu hỏi</Button></Card>)}<Form.ErrorList errors={errors}/><Button icon={<PlusOutlined/>} onClick={()=>add({text:'',optionsText:''})}>Thêm câu hỏi</Button></>}</Form.List>}
      </Form>
    </Modal>
    <Modal open={!!publish} title="Chọn khách nhận khảo sát" onCancel={()=>setPublish(null)} confirmLoading={busy} okText="Phát hành khảo sát" okButtonProps={{disabled:!recipients.length}} onOk={async()=>{if(publish){await act(`/surveys/${publish.id}/publish`,{customerIds:recipients},'Đã phát hành khảo sát.');setPublish(null);}}}>
      <p>Khách được chọn sẽ thấy khảo sát trong client sau lần đồng bộ tiếp theo.</p><Checkbox checked={recipients.length===(workspace.customers||[]).filter(c=>c.status==='Active').length&&recipients.length>0} onChange={e=>setRecipients(e.target.checked?(workspace.customers||[]).filter(c=>c.status==='Active').map(c=>c.id):[])}>Chọn tất cả khách đang hoạt động</Checkbox><Select aria-label="Khách nhận khảo sát" className="full-width" mode="multiple" optionFilterProp="label" value={recipients} onChange={setRecipients} options={(workspace.customers||[]).filter(c=>c.status==='Active').map(c=>({value:c.id,label:String(c.fullName)}))}/>
    </Modal>
    <Modal open={!!detail} title={`${module.title} · ${detail?.id||''}`} onCancel={()=>setDetail(null)} footer={<Space><PrintButton title={module.title} rows={detail?[detail]:[]} keys={module.columns} workspace={workspace}/><Button onClick={()=>setDetail(null)}>Đóng</Button></Space>} width={800}>
      {detail&&<><Descriptions bordered column={{xs:1,sm:2}} size="small" items={Object.entries(detail).filter(([k,v])=>!Array.isArray(v)&&typeof v!=='object').map(([k,v])=>({key:k,label:columnLabels[k]||k,children:<Value k={k} value={v} workspace={workspace}/>}))}/>
      {Array.isArray(detail.items)&&<><Divider>Chi tiết đơn</Divider><Table size="small" rowKey="productId" pagination={false} dataSource={detail.items} columns={[{title:'Món',dataIndex:'name'},{title:'Số lượng',dataIndex:'quantity'},{title:'Đơn giá',dataIndex:'unitPrice',render:v=>displayValue('price',v,workspace)}]}/></>}
      {module.key==='customers'&&<><Divider>Lịch sử của khách</Divider><Table size="small" rowKey="id" dataSource={(workspace.transactions||[]).filter(r=>r.customerId===detail.id)} pagination={{pageSize:5}} columns={['type','amount','createdAt'].map(k=>({title:columnLabels[k],dataIndex:k,render:v=><Value k={k} value={v} workspace={workspace}/>}))}/></>}
      {module.key==='surveys'&&<><Divider>Kết quả khảo sát</Divider><p>{Array.isArray(detail.responses)?detail.responses.length:0} / {Array.isArray(detail.customerIds)?detail.customerIds.length:0} khách đã trả lời</p>{(Array.isArray(detail.questions)?detail.questions:[]).map((q:{id:string;text:string;options:string[]})=><Card key={q.id} size="small" title={q.text} className="question-card">{q.options.map(option=>{const count=(Array.isArray(detail.responses)?detail.responses:[]).filter((r:{answers:Record<string,string>})=>r.answers[q.id]===option).length;return <div className="survey-result" key={option}><span>{option}</span><Tag color="cyan">{count} lượt</Tag></div>;})}</Card>)}</>}
      </>}
    </Modal>
  </>;
}
