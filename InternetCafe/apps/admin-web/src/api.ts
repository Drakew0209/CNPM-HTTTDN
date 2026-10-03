import type { Session } from './domain';
const sessionKey = 'internetcafe.session';
export const apiMode = import.meta.env.VITE_API_MODE || 'mock';
export const apiBase = (import.meta.env.VITE_API_BASE_URL || '/api/v1').replace(/\/$/, '');
export class ApiError extends Error { constructor(public status: number, public code: string, message: string, public fields?: Record<string, string>) { super(message); } }
export function readSession(): Session | null {
  try { return JSON.parse(sessionStorage.getItem(sessionKey) || 'null') as Session | null; } catch { return null; }
}
export function writeSession(session: Session | null) {
  if (session) sessionStorage.setItem(sessionKey, JSON.stringify(session)); else sessionStorage.removeItem(sessionKey);
  window.dispatchEvent(new Event('session-changed'));
}
export async function api<T>(path: string, method = 'GET', body?: unknown): Promise<T> {
  const controller = new AbortController(); const timer = setTimeout(() => controller.abort(), 12000);
  try {
    const token = readSession()?.accessToken;
    const response = await fetch(`${apiBase}${path}`, { method, signal: controller.signal, headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}) }, ...(body !== undefined ? { body: JSON.stringify(body) } : {}) });
    const result = await response.json().catch(() => ({}));
    if (!response.ok) {
      if (response.status === 401 && path !== '/auth/login') writeSession(null);
      throw new ApiError(response.status, result.error?.code || 'HTTP_ERROR', result.error?.message || `Máy chủ trả lỗi ${response.status}`, result.error?.fields);
    }
    return result.data as T;
  } catch (error) {
    if (error instanceof ApiError) throw error;
    throw new Error(navigator.onLine ? 'Không kết nối được máy chủ. Kiểm tra địa chỉ API rồi thử lại.' : 'Bạn đang mất kết nối mạng. Dữ liệu hiện tại có thể đã cũ.');
  } finally { clearTimeout(timer); }
}
