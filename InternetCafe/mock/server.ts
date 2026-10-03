import { createServer, type IncomingMessage, type ServerResponse } from 'node:http';
import { resolve } from 'node:path';
import { createMockEngine } from './engine';

const maxBodyBytes = 1024 * 1024;
const allowedOrigins = new Set(['http://localhost:5173', 'http://127.0.0.1:5173', 'http://localhost:4173', 'http://127.0.0.1:4173']);

/** One engine instance is shared by all web and desktop requests to this host. */
export function createMockHttpServer(engine = createMockEngine()) {
  const server = createServer(async (request: IncomingMessage, response: ServerResponse) => {
    response.setHeader('Content-Type', 'application/json; charset=utf-8');
    response.setHeader('Cache-Control', 'no-store');
    response.setHeader('X-Content-Type-Options', 'nosniff');
    response.setHeader('Date', new Date().toUTCString());
    response.setHeader('Vary', 'Origin');
    const send = (status: number, body: unknown) => { response.statusCode = status; response.end(JSON.stringify(body)); };
    const error = (status: number, code: string, message: string) => send(status, { error: { code, message } });
    const origin = request.headers.origin;
    if (origin && !allowedOrigins.has(origin)) { error(403, 'ORIGIN_NOT_ALLOWED', 'Only the local demo web origin is allowed.'); request.resume(); return; }
    if (origin) response.setHeader('Access-Control-Allow-Origin', origin);
    response.setHeader('Access-Control-Expose-Headers', 'Date');
    if (request.method === 'OPTIONS') {
      response.setHeader('Access-Control-Allow-Methods', 'GET, POST, PATCH, DELETE, OPTIONS');
      response.setHeader('Access-Control-Allow-Headers', 'Authorization, Content-Type');
      response.setHeader('Access-Control-Max-Age', '600');
      response.statusCode = 204; response.end(); return;
    }
    if (request.method === 'GET' && request.url === '/health') { send(200, { data: { status: 'ok', source: 'mock', persistent: false } }); return; }
    if (!request.url?.startsWith('/api/v1/')) { error(404, 'NOT_FOUND', 'Use the /api/v1 API base.'); request.resume(); return; }
    try {
      let raw = ''; let bytes = 0;
      request.setEncoding('utf8');
      for await (const chunk of request) {
        bytes += Buffer.byteLength(chunk);
        if (bytes > maxBodyBytes) { error(413, 'PAYLOAD_TOO_LARGE', 'JSON body exceeds 1 MiB.'); request.resume(); return; }
        raw += chunk;
      }
      if (raw && !/^application\/json(?:\s*;|$)/i.test(request.headers['content-type'] ?? '')) { error(415, 'UNSUPPORTED_MEDIA_TYPE', 'Use Content-Type: application/json.'); return; }
      let body: unknown;
      try { body = raw ? JSON.parse(raw) : undefined; } catch { error(400, 'INVALID_JSON', 'Request body is not valid JSON.'); return; }
      const result = engine.handle(request.method ?? 'GET', request.url, body, request.headers.authorization);
      send(result.status, result.body);
    } catch {
      if (!response.headersSent) error(500, 'MOCK_INTERNAL_ERROR', 'The mock request could not be processed.');
    }
  });
  server.requestTimeout = 15_000;
  server.headersTimeout = 10_000;
  return server;
}

// Importing the factory from Vitest does not start a listener.
if (process.argv[1] && resolve(process.argv[1]).replace(/\\/g, '/').endsWith('/mock/server.ts')) {
  const server = createMockHttpServer();
  server.on('error', error => { console.error(`Mock API could not start: ${error.message}`); process.exitCode = 1; });
  server.listen(5055, '127.0.0.1', () => console.log('InternetCafe shared mock API: http://127.0.0.1:5055/api/v1 (memory resets on restart)'));
  const stop = () => server.close(() => { process.exitCode = 0; });
  process.once('SIGINT', stop); process.once('SIGTERM', stop);
}
