import React from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import { ConfigProvider, App as AntApp } from 'antd';
import viVN from 'antd/locale/vi_VN';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { App } from './App';
import { apiMode } from './api';
import './styles.css';

const queryClient = new QueryClient({ defaultOptions: { queries: { retry: 1, refetchOnWindowFocus: true, staleTime: 5000 }, mutations: { retry: false } } });
async function bootstrap() {
  if (apiMode === 'mock') { const { startMocks } = await import('./mocks'); await startMocks(); }
  createRoot(document.getElementById('root')!).render(<React.StrictMode><ConfigProvider locale={viVN} theme={{ token: { colorPrimary: '#137c70', colorInfo: '#137c70', borderRadius: 9, fontFamily: '"Segoe UI", system-ui, sans-serif', colorBgLayout: '#f4f6f4', controlHeight: 38 }, components: { Table: { headerBg: '#f5f7f5', cellPaddingBlock: 15 }, Menu: { itemBorderRadius: 8 } } }}><AntApp><QueryClientProvider client={queryClient}><BrowserRouter><App/></BrowserRouter></QueryClientProvider></AntApp></ConfigProvider></React.StrictMode>);
}
void bootstrap().catch(() => { document.getElementById('root')!.textContent = 'Không khởi tạo được ứng dụng demo. Thử tải lại trang; nếu vẫn lỗi, kiểm tra service worker và console.'; });
