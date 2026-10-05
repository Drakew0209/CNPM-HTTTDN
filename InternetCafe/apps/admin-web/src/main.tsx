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
  createRoot(document.getElementById('root')!).render(<React.StrictMode><ConfigProvider locale={viVN} theme={{ token: { colorPrimary: '#38d9ff', colorInfo: '#38d9ff', colorSuccess: '#45e5ae', colorWarning: '#ffb86b', colorError: '#ff6682', colorBgBase: '#080d1b', colorBgLayout: '#080d1b', colorBgContainer: '#111a31', colorText: '#edf5ff', colorTextSecondary: '#a8b7d1', colorBorder: '#26385c', borderRadius: 10, fontFamily: '"Segoe UI", system-ui, sans-serif', controlHeight: 38 }, components: { Table: { headerBg: '#172442', cellPaddingBlock: 15 }, Menu: { itemBorderRadius: 8, darkItemBg: '#0d152a' } } }}><AntApp><QueryClientProvider client={queryClient}><BrowserRouter><App/></BrowserRouter></QueryClientProvider></AntApp></ConfigProvider></React.StrictMode>);
}
void bootstrap().catch(() => { document.getElementById('root')!.textContent = 'Không khởi tạo được ứng dụng demo. Thử tải lại trang; nếu vẫn lỗi, kiểm tra service worker và console.'; });
