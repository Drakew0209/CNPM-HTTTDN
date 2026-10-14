import { useEffect, useState } from 'react';
import { Alert, App, Button, Card, Checkbox, Descriptions, Divider, Form, Input, InputNumber, Modal, Select, Space, Switch, Table, Tag, Tooltip } from 'antd';
import { DeleteOutlined, DownloadOutlined, EditOutlined, EyeOutlined, LockOutlined, PlusOutlined, SearchOutlined, SendOutlined, UnlockOutlined, WalletOutlined } from '@ant-design/icons';
import { api, ApiError } from './api';
import { dateTime, downloadCsv, filterDates, labels, money, type Row } from './domain';
import { columnLabels, type Field, type Module } from './modules';
import { DateFilter, displayValue, EmptyState, PageTitle, PrintButton, useCafe, Value } from './components';

export function ResourcePage({ module, readonly = false }: { module: Module; readonly?: boolean }) {
  const { workspace, session, refresh } = useCafe(); const { message, modal } = App.useApp();
  const [search,setSearch]=useState(''),[status,setStatus]=useState<string>(),[from,setFrom]=useState(''),[to,setTo]=useState('');
  const [editor,setEditor]=useState<Row | 'new' | null>(null),[detail,setDetail]=useState<Row|null>(null),[busy,setBusy]=useState(false),[saveError,setSaveError]=useState('');
  const [publish,setPublish]=useState<Row|null>(null),[recipients,setRecipients]=useState<string[]>([]); const [form]=Form.useForm();
  const [topupCustomer,setTopupCustomer]=useState<Row|null>(null),[topupAmount,setTopupAmount]=useState<number>(50000);
  const [combos,setCombos]=useState<{id:string;name:string;price:number;bonusBalance:number;totalValue?:number;isActive?:boolean}[]>([]);
  const [selectedCombo,setSelectedCombo]=useState<string|null>(null);
  const [withdrawCustomer,setWithdrawCustomer]=useState<Row|null>(null),[withdrawAmount,setWithdrawAmount]=useState<number>(50000),[withdrawNote,setWithdrawNote]=useState<string>('');
  const [adminCheckoutRow,setAdminCheckoutRow]=useState<Row|null>(null),[adminCheckoutNote,setAdminCheckoutNote]=useState<string>('Quản lý chốt công cuối ca');
  const [directTopupOpen,setDirectTopupOpen]=useState(false),[directCustomerId,setDirectCustomerId]=useState<string>('');
  const canEdit=!readonly && (module.editRoles || module.roles).includes(session.user.role);
  const all=workspace[module.key] || [];
  const dateField=module.key==='sessions'?'startTime':module.key==='attendance'?'checkIn':'createdAt';
  const rows=filterDates(all,from,to,dateField).filter(row=>(!status||row.status===status)&&(!search||[row.id,...module.columns.map(k=>displayValue(k,row[k],workspace))].join(' ').toLocaleLowerCase('vi').includes(search.toLocaleLowerCase('vi'))));
  const lowStockItems=(workspace.products||[]).filter(p=>Number(p.stock)<=5);
  const lowStockMaterials=(workspace.materials||[]).filter(m=>Number(m.stockQuantity)<=Number(m.reorderLevel));
  useEffect(()=>{
    if(!topupCustomer && !directTopupOpen) return;
    api<{data?:any[]}|any[]>('/combos','GET')
      .then(res=>{const list=Array.isArray(res)?res:((res as any)?.data||[]);setCombos(list);})
      .catch(()=>setCombos([]));
  },[topupCustomer, directTopupOpen]);
  async function act(path:string,body:unknown,success:string,method='POST') {
    setBusy(true);try {await api(path,method,body);await refresh();message.success(success);}catch(e){message.error((e as Error).message);throw e;}finally{setBusy(false);}
  }
  function confirm(title:string,path:string,body:unknown,method='POST') { modal.confirm({title,content:'Thao tác sẽ được kiểm tra quyền và trạng thái tại service.',okText:'Xác nhận',cancelText:'Quay lại',onOk:()=>act(path,body,'Đã cập nhật thành công.',method)}); }
  function openEditor(row:Row|'new') { setEditor(row); setSaveError(''); }
  useEffect(() => {
    if (editor === null) return;
    form.resetFields();
    const defaults: Record<string, unknown> = { active:true, tier:'Member', status:module.key==='schedules'?'Scheduled':'Active', type:'Import', quantity:1, bonus:0, deduction:0 };
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
  const columnWidths: Record<string, number> = {
    fullName: 140,
    username: 115,
    citizenId: 110,
    phone: 110,
    tier: 95,
    balance: 105,
    status: 105,
    name: 140,
    category: 120,
    categoryId: 120,
    price: 105,
    stock: 90,
    active: 95,
    total: 110,
    amount: 110,
    createdAt: 140,
    startTime: 135,
    endTime: 135,
    role: 110,
    department: 110,
    position: 120,
    qualification: 110,
    baseSalary: 115,
    month: 100,
    code: 110,
    unit: 80,
    stockQuantity: 110,
    reorderLevel: 115,
    unitCost: 115,
    supplierName: 140,
    itemType: 110,
    materialName: 140,
    productName: 140,
    requesterName: 130,
    deciderName: 130,
  };
  const columns=[...module.columns.map(k=>({title:columnLabels[k]||k,dataIndex:k,key:k,width:columnWidths[k],sorter:(a:Row,b:Row)=>typeof a[k]==='number'?Number(a[k])-Number(b[k]):displayValue(k,a[k],workspace).localeCompare(displayValue(k,b[k],workspace),'vi'),render:(v:unknown)=><Value k={k} value={v} workspace={workspace}/>})),{
    title:'Thao tác',
    key:'actions',
    fixed:'right' as const,
    width:module.key==='customers'?290:module.key==='orders'?230:['topups','leaves','material-requests'].includes(module.key)?200:160,
    render:(_:unknown,row:Row)=><div className="table-actions">
      {canEdit&&module.key==='customers'&&row.status!=='Inactive'&&(
        <>
          <Button size="small" type="primary" className="btn-topup" onClick={()=>{setTopupCustomer(row);setTopupAmount(50000);setSelectedCombo(null);}}>Nạp tiền</Button>
          <Button size="small" onClick={()=>{setWithdrawCustomer(row);setWithdrawAmount(Math.min(Number(row.balance||0),50000));setWithdrawNote('');}}>Rút tiền</Button>
        </>
      )}
      {canEdit&&['topups','leaves','material-requests'].includes(module.key)&&row.status==='Pending'&&(
        <>
          <Button type="primary" size="small" loading={busy} onClick={()=>confirm(module.key==='topups'?'Đã nhận đủ tiền tại quầy?':module.key==='leaves'?'Duyệt đơn nghỉ này?':'Duyệt xuất kho / trừ định mức BOM?',`/${module.key}/${row.id}/decision`,{status:'Approved'})}>Duyệt</Button>
          <Button size="small" danger onClick={()=>confirm('Từ chối yêu cầu?',`/${module.key}/${row.id}/decision`,{status:'Rejected'})}>Từ chối</Button>
        </>
      )}
      {canEdit&&module.key==='attendance'&&!row.checkOut&&<Button size="small" type="primary" onClick={()=>setAdminCheckoutRow(row)}>Chốt công ra</Button>}
      {canEdit&&module.key==='orders'&&['Pending','Preparing'].includes(String(row.status))&&(
        <>
          <Button size="small" type="primary" onClick={()=>confirm(row.status==='Pending'?'Nhận chuẩn bị đơn?':'Xác nhận đã phục vụ?',`/orders/${row.id}/transition`,{status:row.status==='Pending'?'Preparing':'Served'})}>{row.status==='Pending'?'Nhận đơn':'Phục vụ'}</Button>
          {(row.status==='Pending'||session.user.role!=='Cashier')&&<Button size="small" danger onClick={()=>confirm('Hủy đơn và hoàn tiền qua service?',`/orders/${row.id}/transition`,{status:'Cancelled'})}>Hủy</Button>}
        </>
      )}
      {canEdit&&module.key==='surveys'&&row.status==='Draft'&&<Button size="small" icon={<SendOutlined/>} onClick={()=>{setPublish(row);setRecipients([]);}}>Phát hành</Button>}
      {canEdit&&module.key==='surveys'&&row.status==='Published'&&<Button size="small" onClick={()=>confirm('Đóng khảo sát?',`/surveys/${row.id}`,{status:'Closed'},'PATCH')}>Đóng</Button>}
      <Tooltip title="Xem chi tiết">
        <Button size="small" className="btn-icon-action" aria-label={`Chi tiết ${row.id}`} icon={<EyeOutlined/>} onClick={()=>setDetail(row)}/>
      </Tooltip>
      {canEdit&&module.edit&&!(module.key==='surveys'&&row.status!=='Draft')&&(
        <Tooltip title="Chỉnh sửa">
          <Button size="small" className="btn-icon-action" aria-label={`Sửa ${row.id}`} icon={<EditOutlined/>} onClick={()=>openEditor(row)}/>
        </Tooltip>
      )}
      {canEdit&&module.key==='customers'&&row.status!=='Inactive'&&(
        <Tooltip title={row.status==='Banned'?'Mở khóa tài khoản khách hàng':'Khóa tài khoản khách hàng'}>
          <Button size="small" className={row.status==='Banned'?'btn-icon-action btn-icon-success':'btn-icon-action btn-icon-danger'} aria-label={row.status==='Banned'?`Mở khóa ${row.id}`:`Khóa ${row.id}`} icon={row.status==='Banned'?<UnlockOutlined/>:<LockOutlined/>} onClick={()=>confirm(row.status==='Banned'?'Mở khóa khách hàng?':'Khóa tài khoản khách hàng?',`/customers/${row.id}`,{status:row.status==='Banned'?'Active':'Banned'},'PATCH')}/>
        </Tooltip>
      )}
      {canEdit&&module.remove&&(
        <Tooltip title="Xóa bản ghi">
          <Button size="small" className="btn-icon-action btn-icon-danger" aria-label={`Xóa ${row.id}`} icon={<DeleteOutlined/>} onClick={()=>confirm('Ngừng sử dụng bản ghi này?',`/${module.key}/${row.id}`,undefined,'DELETE')}/>
        </Tooltip>
      )}
    </div>}];
  return <>
    <PageTitle eyebrow={readonly?'INTERNETCAFE / TRA CỨU HỆ THỐNG':module.section.toLocaleUpperCase('vi')} title={module.title} description={module.description} actions={<Space wrap>{canEdit&&(module.key==='topups'||module.key==='customers')&&<Button type="primary" icon={<WalletOutlined/>} style={{background:'linear-gradient(135deg, #10b981, #059669)',borderColor:'#10b981'}} onClick={()=>{setDirectCustomerId('');setTopupAmount(50000);setSelectedCombo(null);setDirectTopupOpen(true);}}>Nạp tiền tại quầy</Button>}<Button icon={<DownloadOutlined/>} onClick={()=>downloadCsv(module.key,rows,module.columns.map(key=>({key,label:columnLabels[key]||key})))}>Xuất CSV</Button>{canEdit&&module.create&&<Button type="primary" icon={<PlusOutlined/>} onClick={()=>openEditor('new')}>Thêm {module.singular}</Button>}</Space>}/>
    {module.key==='inventory'&&(lowStockItems.length>0?<Alert className="section-alert" type="warning" showIcon title={`Cảnh báo: Có ${lowStockItems.length} món tồn kho thấp (≤ 5) hoặc đã hết hàng!`} description={`Các món cần nhập thêm: ${lowStockItems.slice(0, 5).map(p=>`${p.name} (còn ${p.stock})`).join(', ')}${lowStockItems.length>5?'...':''}. Vui lòng liên hệ nhà cung cấp để lập phiếu nhập kho.`}/>:<Alert className="section-alert" type="success" showIcon title="Tồn kho an toàn" description="Tất cả các món đều có số lượng tồn kho đảm bảo sẵn sàng phục vụ khách hàng."/>)}
    {module.key==='materials'&&(lowStockMaterials.length>0?<Alert className="section-alert" type="warning" showIcon title={`Cảnh báo định mức MRP: Có ${lowStockMaterials.length} nguyên vật liệu chạm hoặc dưới mức tồn tối thiểu!`} description={`Các nguyên vật liệu cần nhập: ${lowStockMaterials.slice(0, 5).map(m=>`${m.name} (hiện có ${m.stockQuantity} ${m.unit}, mức tối thiểu: ${m.reorderLevel})`).join(', ')}${lowStockMaterials.length>5?'...':''}. Vui lòng đề xuất nhập vật tư.`}/>:<Alert className="section-alert" type="success" showIcon title="Định mức tồn kho an toàn" description="Tất cả nguyên vật liệu đều ở trên mức định mức tối thiểu."/>)}
    {module.key==='material-requests'&&<Alert className="section-alert" type="info" showIcon title="Quy trình hoạch định MRP & Sản xuất" description="Duyệt xuất kho NVL: hệ thống tự động kiểm tra tồn kho và trừ trực tiếp. Duyệt sản xuất thành phẩm: hệ thống tự động kiểm tra đủ tất cả NVL theo công thức định mức BOM, trừ kho NVL và cộng thành phẩm vào thực đơn."/>}
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
    <Modal open={!!topupCustomer} title={`Nạp tiền cho ${String(topupCustomer?.fullName||topupCustomer?.id||'')}`} onCancel={()=>setTopupCustomer(null)} confirmLoading={busy} okText="Xác nhận nạp" onOk={async()=>{if(!topupCustomer)return;if(!topupAmount||topupAmount<1000||topupAmount>10000000){message.error('Số tiền nạp từ 1.000 đến 10.000.000 đồng.');return;}await act(`/customers/${topupCustomer.id}/topup`,{amount:topupAmount,comboId:selectedCombo||undefined},'Nạp tiền thành công.');setTopupCustomer(null);}}>
      <p>Số dư hiện tại: <strong>{displayValue('price',topupCustomer?.balance,workspace)}</strong></p>
      {combos.length>0&&<div style={{marginBottom:14}}><p style={{marginBottom:6,fontWeight:500}}>Chọn gói Combo ưu đãi (nạp tiền nhận thêm số dư):</p><Space wrap size={[6,6]}><Tag.CheckableTag checked={selectedCombo===null} onChange={()=>setSelectedCombo(null)}>Tự nhập số tiền</Tag.CheckableTag>{combos.filter(c=>c.isActive!==false).map(c=><Tag.CheckableTag key={c.id} checked={selectedCombo===c.id} onChange={()=>{setSelectedCombo(c.id);setTopupAmount(c.price);}}>{c.name} ({money(c.price)} → nhận {money(c.totalValue||(c.price+c.bonusBalance))})</Tag.CheckableTag>)}</Space></div>}
      <InputNumber aria-label="Số tiền nạp" min={1000} max={10000000} step={1000} precision={0} style={{width:'100%'}} addonAfter="VND" value={topupAmount} onChange={v=>{setSelectedCombo(null);setTopupAmount(Number(v||0));}}/>
    </Modal>
    <Modal open={directTopupOpen} title="Nạp tiền trực tiếp tại quầy thu ngân" onCancel={()=>setDirectTopupOpen(false)} confirmLoading={busy} okText="Xác nhận thu tiền & Nạp" okButtonProps={{disabled:!directCustomerId}} onOk={async()=>{if(!directCustomerId){message.error('Vui lòng chọn khách hàng.');return;}if(!topupAmount||topupAmount<1000||topupAmount>10000000){message.error('Số tiền nạp từ 1.000 đến 10.000.000 đồng.');return;}await act(`/customers/${directCustomerId}/topup`,{amount:topupAmount,comboId:selectedCombo||undefined},'Nạp tiền tại quầy thành công.');setDirectTopupOpen(false);}}>
      <div style={{marginBottom:14}}>
        <label style={{display:'block',marginBottom:6,fontWeight:500}}>Chọn tài khoản khách hàng:</label>
        <Select showSearch placeholder="Gõ tìm theo tên, username hoặc số điện thoại..." optionFilterProp="label" value={directCustomerId||undefined} onChange={v=>setDirectCustomerId(v)} style={{width:'100%'}} options={(workspace.customers||[]).filter(c=>c.status==='Active').map(c=>({value:c.id,label:`${c.fullName} (@${c.username||c.id}) · Dư: ${money(c.balance||0)}`} ) ) } />
      </div>
      {directCustomerId&&(()=>{const c=(workspace.customers||[]).find(x=>x.id===directCustomerId);return c?<p style={{fontSize:13,color:'#64748b',marginBottom:12}}>Khách: <strong>{String(c.fullName || '')}</strong> | Số dư hiện tại: <strong style={{color:'#10b981'}}>{money(c.balance||0)}</strong></p>:null;})()}
      <div style={{marginBottom:14}}>
        <label style={{display:'block',marginBottom:6,fontWeight:500}}>Mệnh giá nạp nhanh:</label>
        <Space wrap size={[6,6]}>
          {[10000, 20000, 50000, 100000, 200000, 500000].map(amt=>(
            <Button key={amt} size="small" type={topupAmount===amt&&!selectedCombo?'primary':'default'} onClick={()=>{setSelectedCombo(null);setTopupAmount(amt);}}>
              {money(amt)}
            </Button>
          ))}
        </Space>
      </div>
      {combos.length>0&&<div style={{marginBottom:14}}><p style={{marginBottom:6,fontWeight:500}}>Hoặc chọn gói Combo ưu đãi:</p><Space wrap size={[6,6]}><Tag.CheckableTag checked={selectedCombo===null} onChange={()=>setSelectedCombo(null)}>Không dùng combo</Tag.CheckableTag>{combos.filter(c=>c.isActive!==false).map(c=><Tag.CheckableTag key={c.id} checked={selectedCombo===c.id} onChange={()=>{setSelectedCombo(c.id);setTopupAmount(c.price);}}>{c.name} ({money(c.price)} → nhận {money(c.totalValue||(c.price+c.bonusBalance))})</Tag.CheckableTag>)}</Space></div>}
      <div style={{marginBottom:12}}>
        <label style={{display:'block',marginBottom:4,fontWeight:500}}>Số tiền nạp (VND):</label>
        <InputNumber aria-label="Số tiền nạp" min={1000} max={10000000} step={1000} precision={0} style={{width:'100%'}} addonAfter="VND" value={topupAmount} onChange={v=>{setSelectedCombo(null);setTopupAmount(Number(v||0));}}/>
      </div>
      <Alert type="info" showIcon message="Thu ngân xác nhận đã thu đủ tiền mặt hoặc đã nhận tiền chuyển khoản của khách tại quầy trước khi nhấn xác nhận."/>
    </Modal>
    <Modal open={!!withdrawCustomer} title={`Rút tiền / Trả lại số dư cho ${String(withdrawCustomer?.fullName||withdrawCustomer?.id||'')}`} onCancel={()=>setWithdrawCustomer(null)} confirmLoading={busy} okText="Xác nhận rút" onOk={async()=>{if(!withdrawCustomer)return;if(!withdrawAmount||withdrawAmount<1000||withdrawAmount>10000000){message.error('Số tiền rút từ 1.000 đến 10.000.000 đồng.');return;}await act(`/customers/${withdrawCustomer.id}/refund`,{amount:withdrawAmount,note:withdrawNote||'Rút tiền thừa'},'Rút tiền thành công.');setWithdrawCustomer(null);}}>
      <p>Số dư hiện tại: <strong>{displayValue('price',withdrawCustomer?.balance,workspace)}</strong></p>
      <div style={{marginBottom:12}}><label style={{display:'block',marginBottom:4,fontWeight:500}}>Số tiền rút (VND):</label><InputNumber min={1000} max={Number(withdrawCustomer?.balance||10000000)} step={1000} precision={0} style={{width:'100%'}} addonAfter="VND" value={withdrawAmount} onChange={v=>setWithdrawAmount(Number(v||0))}/></div>
      <div><label style={{display:'block',marginBottom:4,fontWeight:500}}>Lý do / Ghi chú rút:</label><Input placeholder="Ví dụ: Khách trả máy rút tiền thừa..." value={withdrawNote} onChange={e=>setWithdrawNote(e.target.value)}/></div>
    </Modal>
    <Modal open={!!adminCheckoutRow} title={`Quản lý chốt công ra · ${displayValue('employeeId',adminCheckoutRow?.employeeId,workspace)}`} onCancel={()=>setAdminCheckoutRow(null)} confirmLoading={busy} okText="Chốt công ngay" onOk={async()=>{if(!adminCheckoutRow)return;if(!adminCheckoutNote.trim()){message.error('Vui lòng nhập lý do quản lý can thiệp chốt ca.');return;}await act(`/attendance/${adminCheckoutRow.id}/admin-checkout`,{note:adminCheckoutNote.trim()},'Chốt công ra thành công.');setAdminCheckoutRow(null);}}>
      <p>Thời điểm vào ca: <strong>{dateTime(adminCheckoutRow?.checkIn)}</strong></p>
      <p>Nhân viên quên quẹt check-out? Quản lý xác nhận kết thúc ca làm tại thời điểm hiện tại.</p>
      <label style={{display:'block',marginBottom:4,fontWeight:500}}>Ghi chú lý do:</label><Input value={adminCheckoutNote} onChange={e=>setAdminCheckoutNote(e.target.value)} placeholder="Nhập lý do can thiệp..."/>
    </Modal>
    
  </>;
}
