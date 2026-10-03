import { setupWorker } from 'msw/browser';
import { http, HttpResponse, delay } from 'msw';
import { createMockEngine } from '../../../mock/engine';
export async function startMocks() {
  const engine = createMockEngine();
  const worker = setupWorker(http.all('*/api/v1/*', async ({ request }) => {
    await delay(120);
    let body: unknown;
    if (request.method !== 'GET' && request.method !== 'HEAD') body = await request.json().catch(() => undefined);
    const url = new URL(request.url);
    const result = engine.handle(request.method, url.pathname + url.search, body, request.headers.get('Authorization') || undefined);
    return HttpResponse.json(result.body as Record<string, unknown>, { status: result.status });
  }));
  await worker.start({ onUnhandledRequest: 'bypass', quiet: true });
}
