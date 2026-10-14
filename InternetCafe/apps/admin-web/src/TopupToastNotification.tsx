import { useEffect, useState, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQueryClient } from '@tanstack/react-query';
import { Button, Tag, Space, message } from 'antd';
import {
  BellOutlined,
  CheckCircleOutlined,
  CloseOutlined,
  DesktopOutlined,
  DollarOutlined,
  QrcodeOutlined,
  UserOutlined,
  ArrowRightOutlined,
  CloseCircleOutlined,
} from '@ant-design/icons';
import { subscribeTopupRequested, type TopupRequestedEvent } from './realtime';
import { api } from './api';

function playChime() {
  try {
    const AudioContextClass = window.AudioContext || (window as unknown as { webkitAudioContext: typeof AudioContext }).webkitAudioContext;
    if (!AudioContextClass) return;
    const ctx = new AudioContextClass();
    
    // Play two-tone pleasant notification chime (D5 -> A5)
    const now = ctx.currentTime;
    const osc1 = ctx.createOscillator();
    const gain1 = ctx.createGain();
    osc1.type = 'sine';
    osc1.frequency.setValueAtTime(587.33, now); // D5
    gain1.gain.setValueAtTime(0.18, now);
    gain1.gain.exponentialRampToValueAtTime(0.001, now + 0.25);
    osc1.connect(gain1);
    gain1.connect(ctx.destination);
    osc1.start(now);
    osc1.stop(now + 0.25);

    const osc2 = ctx.createOscillator();
    const gain2 = ctx.createGain();
    osc2.type = 'sine';
    osc2.frequency.setValueAtTime(880, now + 0.12); // A5
    gain2.gain.setValueAtTime(0.2, now + 0.12);
    gain2.gain.exponentialRampToValueAtTime(0.001, now + 0.45);
    osc2.connect(gain2);
    gain2.connect(ctx.destination);
    osc2.start(now + 0.12);
    osc2.stop(now + 0.45);
  } catch {
    // Ignore audio permission or context restrictions
  }
}

interface ToastItem extends TopupRequestedEvent {
  receivedAt: number;
  processing?: boolean;
}

export function TopupToastNotification() {
  const [toasts, setToasts] = useState<ToastItem[]>([]);
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  useEffect(() => {
    const unsubscribe = subscribeTopupRequested((event) => {
      playChime();
      setToasts((prev) => {
        // Prevent duplicate toasts for same topupId
        if (prev.some((t) => t.topupId === event.topupId)) return prev;
        return [{ ...event, receivedAt: Date.now() }, ...prev.slice(0, 4)];
      });
    });
    return unsubscribe;
  }, []);

  const dismiss = useCallback((topupId: string) => {
    setToasts((prev) => prev.filter((t) => t.topupId !== topupId));
  }, []);

  const handleDecision = async (toast: ToastItem, status: 'Approved' | 'Rejected') => {
    setToasts((prev) =>
      prev.map((t) => (t.topupId === toast.topupId ? { ...t, processing: true } : t))
    );
    try {
      await api(`/topups/${toast.topupId}/decision`, 'POST', { status });
      message.success(
        status === 'Approved'
          ? `Đã duyệt nạp ${toast.amount.toLocaleString('vi-VN')} đ cho ${toast.customerName || toast.customerId}`
          : `Đã từ chối yêu cầu nạp tiền`
      );
      void queryClient.invalidateQueries({ queryKey: ['workspace'] });
      dismiss(toast.topupId);
    } catch (err) {
      message.error((err as Error).message || 'Xử lý thất bại');
      setToasts((prev) =>
        prev.map((t) => (t.topupId === toast.topupId ? { ...t, processing: false } : t))
      );
    }
  };

  if (toasts.length === 0) return null;

  return (
    <div
      style={{
        position: 'fixed',
        bottom: 24,
        left: 24,
        zIndex: 9999,
        display: 'flex',
        flexDirection: 'column-reverse',
        gap: 12,
        maxWidth: 380,
        width: 'calc(100vw - 48px)',
        pointerEvents: 'none',
      }}
    >
      {toasts.map((toast) => {
        const isTransfer =
          toast.paymentMethod === 'Transfer' ||
          (toast.note && (toast.note.includes('Chuyển khoản') || toast.note.includes('VietQR')));
        
        return (
          <div
            key={toast.topupId}
            style={{
              pointerEvents: 'auto',
              background: 'linear-gradient(135deg, rgba(15, 23, 42, 0.95), rgba(30, 41, 59, 0.95))',
              backdropFilter: 'blur(12px)',
              WebkitBackdropFilter: 'blur(12px)',
              border: isTransfer ? '1px solid rgba(6, 182, 212, 0.6)' : '1px solid rgba(234, 179, 8, 0.6)',
              boxShadow: isTransfer
                ? '0 10px 25px -5px rgba(6, 182, 212, 0.25), 0 8px 10px -6px rgba(0, 0, 0, 0.3)'
                : '0 10px 25px -5px rgba(234, 179, 8, 0.25), 0 8px 10px -6px rgba(0, 0, 0, 0.3)',
              borderRadius: 12,
              padding: '14px 16px',
              color: '#f8fafc',
              animation: 'topupToastSlideIn 0.35s cubic-bezier(0.16, 1, 0.3, 1) forwards',
            }}
          >
            {/* Header */}
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 10 }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                <div
                  style={{
                    width: 28,
                    height: 28,
                    borderRadius: '50%',
                    background: isTransfer ? 'rgba(6, 182, 212, 0.2)' : 'rgba(234, 179, 8, 0.2)',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    color: isTransfer ? '#38bdf8' : '#facc15',
                  }}
                >
                  <BellOutlined style={{ fontSize: 15 }} />
                </div>
                <div>
                  <div style={{ fontSize: 13, fontWeight: 700, letterSpacing: '0.02em', textTransform: 'uppercase', color: '#f1f5f9' }}>
                    Yêu cầu nạp tiền mới
                  </div>
                  <div style={{ fontSize: 11, color: '#94a3b8' }}>Vừa nhận từ máy khách</div>
                </div>
              </div>
              <Button
                type="text"
                size="small"
                icon={<CloseOutlined style={{ fontSize: 12, color: '#94a3b8' }} />}
                onClick={() => dismiss(toast.topupId)}
                style={{ width: 24, height: 24 }}
              />
            </div>

            {/* Info Body */}
            <div
              style={{
                background: 'rgba(0, 0, 0, 0.25)',
                borderRadius: 8,
                padding: '10px 12px',
                marginBottom: 12,
                fontSize: 13,
                display: 'flex',
                flexDirection: 'column',
                gap: 6,
              }}
            >
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <span style={{ color: '#94a3b8', display: 'flex', alignItems: 'center', gap: 5 }}>
                  <UserOutlined /> Khách hàng:
                </span>
                <span style={{ fontWeight: 600, color: '#f8fafc' }}>
                  {toast.customerName || toast.customerId}
                </span>
              </div>

              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <span style={{ color: '#94a3b8', display: 'flex', alignItems: 'center', gap: 5 }}>
                  <DesktopOutlined /> Vị trí máy:
                </span>
                <span style={{ fontWeight: 600, color: toast.computerCode ? '#38bdf8' : '#cbd5e1' }}>
                  {toast.computerCode ? `Máy ${toast.computerCode}` : 'Chưa ngồi máy'}
                </span>
              </div>

              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <span style={{ color: '#94a3b8' }}>Phương thức:</span>
                {isTransfer ? (
                  <Tag color="cyan" style={{ margin: 0, fontWeight: 600, display: 'flex', alignItems: 'center', gap: 4 }}>
                    <QrcodeOutlined /> Chuyển khoản VietQR
                  </Tag>
                ) : (
                  <Tag color="gold" style={{ margin: 0, fontWeight: 600, display: 'flex', alignItems: 'center', gap: 4 }}>
                    <DollarOutlined /> Tiền mặt tại quầy
                  </Tag>
                )}
              </div>

              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', paddingTop: 4, borderTop: '1px solid rgba(255,255,255,0.08)' }}>
                <span style={{ color: '#94a3b8' }}>Số tiền nạp:</span>
                <span style={{ fontSize: 16, fontWeight: 700, color: '#10b981' }}>
                  {toast.amount.toLocaleString('vi-VN')} đ
                </span>
              </div>

              {toast.note && (
                <div style={{ fontSize: 11, color: '#cbd5e1', fontStyle: 'italic', wordBreak: 'break-word', marginTop: 2 }}>
                  "{toast.note}"
                </div>
              )}
            </div>

            {/* Actions */}
            <div style={{ display: 'flex', gap: 8 }}>
              <Button
                type="primary"
                icon={<CheckCircleOutlined />}
                loading={toast.processing}
                onClick={() => handleDecision(toast, 'Approved')}
                style={{
                  flex: 1,
                  background: 'linear-gradient(135deg, #10b981, #059669)',
                  borderColor: '#10b981',
                  fontWeight: 600,
                }}
              >
                Duyệt ngay
              </Button>
              <Button
                danger
                icon={<CloseCircleOutlined />}
                disabled={toast.processing}
                onClick={() => handleDecision(toast, 'Rejected')}
                style={{ fontWeight: 500 }}
              >
                Từ chối
              </Button>
              <Button
                type="default"
                icon={<ArrowRightOutlined />}
                onClick={() => {
                  navigate('/management/topups');
                  dismiss(toast.topupId);
                }}
                style={{ color: '#94a3b8' }}
              >
                DS
              </Button>
            </div>
          </div>
        );
      })}

      <style>{`
        @keyframes topupToastSlideIn {
          0% {
            opacity: 0;
            transform: translate3d(-30px, 40px, 0) scale(0.92);
          }
          100% {
            opacity: 1;
            transform: translate3d(0, 0, 0) scale(1);
          }
        }
      `}</style>
    </div>
  );
}
